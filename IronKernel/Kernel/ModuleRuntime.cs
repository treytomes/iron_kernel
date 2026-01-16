using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IronKernel.Kernel;

/// <remarks>
/// ModuleRuntime is a kernel internal supervisor, not a module utility.
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

	public Task RunAsync(
		string name,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken)
	{
		if (work is null)
			throw new ArgumentNullException(nameof(work));

		var startedAt = DateTime.UtcNow;
		var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

		var task = Task.Run(async () =>
		{
			ModuleContext.CurrentModule.Value = _moduleType;
			var sw = Stopwatch.StartNew();

			try
			{
				await work(stoppingToken);
				sw.Stop();

				if (sw.Elapsed > SlowTaskThreshold)
				{
					_bus.Publish(new ModuleTaskSlow(
						_moduleType,
						name,
						sw.Elapsed));
				}

				_bus.Publish(new ModuleTaskCompleted(
					_moduleType,
					name));
			}
			catch (OperationCanceledException)
			{
				sw.Stop();

				if (sw.Elapsed > SlowTaskThreshold)
				{
					_bus.Publish(new ModuleTaskSlow(
						_moduleType,
						name,
						sw.Elapsed));
				}

				_bus.Publish(new ModuleTaskCancelled(
					_moduleType,
					name));
			}
			catch (Exception ex)
			{
				sw.Stop();

				if (sw.Elapsed > SlowTaskThreshold)
				{
					_bus.Publish(new ModuleTaskSlow(
						_moduleType,
						name,
						sw.Elapsed));
				}

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
				// Stop watchdog once task exits
				watchdogCts.Cancel();
				ModuleContext.CurrentModule.Value = null;
			}
		}, stoppingToken);

		// Start watchdog
		_ = WatchdogAsync(
			name,
			task,
			startedAt,
			watchdogCts.Token);

		lock (_tasks)
		{
			_tasks.Add(new ModuleTask(
				name,
				task,
				watchdogCts));
		}

		return task;
	}

	private async Task WatchdogAsync(
		string name,
		Task task,
		DateTime startedAt,
		CancellationToken ct)
	{
		try
		{
			await Task.Delay(HungTaskThreshold, ct);

			if (!task.IsCompleted)
			{
				_bus.Publish(new ModuleTaskHung(
					_moduleType,
					name,
					DateTime.UtcNow - startedAt));
			}
		}
		catch (OperationCanceledException)
		{
			// Normal: task completed or kernel shutting down
		}
	}

	public async Task WaitAllAsync()
	{
		ModuleTask[] snapshot;

		lock (_tasks)
		{
			snapshot = _tasks.ToArray();
		}

		foreach (var entry in snapshot)
		{
			try
			{
				await entry.Task;
			}
			catch
			{
				// Fault already observed and reported
			}
			finally
			{
				entry.WatchdogCts.Dispose();
			}
		}
	}
}
