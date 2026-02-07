using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IronKernel.Kernel;

/// <remarks>
/// ModuleRuntime is a kernel-internal supervisor.
/// It enforces task lifecycle policy and protects the kernel
/// from misbehaving modules.
/// </remarks>
public sealed class ModuleRuntime : IModuleRuntime
{
	private static readonly TimeSpan SlowTaskThreshold = TimeSpan.FromSeconds(1);
	private static readonly TimeSpan HungTaskThreshold = TimeSpan.FromSeconds(5);

	private readonly Type _moduleType;
	private readonly IMessageBus _bus;
	private readonly ILogger _logger;

	private readonly List<ModuleTask> _tasks = new();

	public ModuleRuntime(
		Type moduleType,
		IMessageBus bus,
		ILogger logger)
	{
		_moduleType = moduleType;
		_bus = bus;
		_logger = logger;
	}

	public void RunDetached(
		string name,
		ModuleTaskKind kind,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken)
	{
		_ = RunAsync(name, kind, work, stoppingToken);
	}

	public Task RunAsync(
	string name,
	ModuleTaskKind kind,
	Func<CancellationToken, Task> work,
	CancellationToken stoppingToken)
	{
		if (work is null)
			throw new ArgumentNullException(nameof(work));

		var startedAt = DateTime.UtcNow;
		var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

		// Create entry FIRST
		var entry = new ModuleTask(
			name,
			kind,
			watchdogCts)
		{
			State = ModuleTaskState.Running
		};

		Task task = Task.Run(async () =>
		{
			ModuleContext.CurrentModule.Value = _moduleType;
			var sw = Stopwatch.StartNew();

			try
			{
				await work(stoppingToken);
				sw.Stop();

				if (kind == ModuleTaskKind.Finite)
				{
					MarkSlowIfNeeded(entry, sw.Elapsed);
					entry.State = ModuleTaskState.Completed;

					_bus.Publish(new ModuleTaskCompleted(
						_moduleType,
						name));
				}
			}
			catch (OperationCanceledException)
			{
				sw.Stop();

				MarkSlowIfNeeded(entry, sw.Elapsed);
				entry.State = ModuleTaskState.Cancelled;

				_bus.Publish(new ModuleTaskCancelled(
					_moduleType,
					name));
			}
			catch (Exception ex)
			{
				sw.Stop();

				MarkSlowIfNeeded(entry, sw.Elapsed);
				entry.State = ModuleTaskState.Faulted;

				_logger.LogError(
					ex,
					"Module {Module} task '{Task}' faulted",
					_moduleType.Name,
					name);

				_bus.Publish(new ModuleFaulted(
					_moduleType,
					name,
					ex));
			}
			finally
			{
				watchdogCts.Cancel();
				ModuleContext.CurrentModule.Value = null;
			}
		}, CancellationToken.None);

		entry.Task = task;

		lock (_tasks)
		{
			_tasks.Add(entry);
		}

		if (kind == ModuleTaskKind.Finite)
		{
			_ = WatchdogAsync(
				entry,
				startedAt,
				watchdogCts.Token);
		}

		return task;
	}

	private void MarkSlowIfNeeded(ModuleTask entry, TimeSpan elapsed)
	{
		if (elapsed > SlowTaskThreshold &&
			entry.State == ModuleTaskState.Running)
		{
			entry.State = ModuleTaskState.Slow;

			_bus.Publish(new ModuleTaskSlow(
				_moduleType,
				entry.Name,
				elapsed));
		}
	}

	private async Task WatchdogAsync(
		ModuleTask entry,
		DateTime startedAt,
		CancellationToken ct)
	{
		try
		{
			await Task.Delay(HungTaskThreshold, ct);

			if (!entry.Task.IsCompleted &&
				entry.State is ModuleTaskState.Running or ModuleTaskState.Slow)
			{
				entry.State = ModuleTaskState.Hung;

				_bus.Publish(new ModuleTaskHung(
					_moduleType,
					entry.Name,
					DateTime.UtcNow - startedAt));

				// Kernel must never wait for this task again
				entry.State = ModuleTaskState.Detached;
			}
		}
		catch (OperationCanceledException)
		{
			// Normal: task completed or kernel shutting down
		}
	}

	public async Task WaitAllAsync(TimeSpan? grace = null)
	{
		ModuleTask[] snapshot;

		lock (_tasks)
		{
			snapshot = _tasks.ToArray();
		}

		var waitables = snapshot
			.Where(t =>
				t.Kind == ModuleTaskKind.Finite &&
				t.State is ModuleTaskState.Running or ModuleTaskState.Slow)
			.Select(t => t.Task)
			.ToArray();

		if (waitables.Length != 0)
		{
			if (grace is null)
			{
				await Task.WhenAll(waitables);
			}
			else
			{
				await Task.WhenAny(
					Task.WhenAll(waitables),
					Task.Delay(grace.Value));
			}
		}

		foreach (var entry in snapshot)
		{
			if (!entry.Task.IsCompleted &&
				entry.State != ModuleTaskState.Detached)
			{
				entry.State = ModuleTaskState.Detached;

				_bus.Publish(new ModuleTaskAbandoned(
					_moduleType,
					entry.Name));
			}

			entry.WatchdogCts.Dispose();
		}
	}
}
