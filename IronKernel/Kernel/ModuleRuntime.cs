using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

/// <remarks>
/// ModuleRuntime is a kernel internal supervisor, not a module utility.
/// </remarks>
public sealed class ModuleRuntime : IModuleRuntime
{
	private static readonly TimeSpan SlowTaskThreshold = TimeSpan.FromSeconds(1);

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

		var task = Task.Run(async () =>
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

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

				// Intentionally do NOT rethrow
			}
		}, stoppingToken);

		lock (_tasks)
		{
			_tasks.Add(new(name, task));
		}

		return task;
	}

	public async Task WaitAllAsync()
	{
		Task[] snapshot;

		lock (_tasks)
		{
			snapshot = _tasks.Select(t => t.Task).ToArray();
		}

		foreach (var task in snapshot)
		{
			try
			{
				await task;
			}
			catch
			{
				// Fault already reported
			}
		}
	}
}
