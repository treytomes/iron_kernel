using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

internal sealed class ModuleRuntime : IModuleRuntime
{
	private readonly Type _moduleType;
	private readonly IMessageBus _bus;
	private readonly ILogger _logger;

	private readonly List<Task> _tasks = new();

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
			try
			{
				await work(stoppingToken);
			}
			catch (OperationCanceledException)
			{
				// Normal shutdown path
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Module {Module} task '{Task}' faulted",
					_moduleType.Name,
					name);

				_bus.Publish(new ModuleFaulted(
					_moduleType,
					name,
					ex));

				throw;
			}
		}, stoppingToken);

		lock (_tasks)
		{
			_tasks.Add(task);
		}

		return task;
	}

	public async Task WaitAllAsync()
	{
		Task[] snapshot;

		lock (_tasks)
		{
			snapshot = _tasks.ToArray();
		}

		await Task.WhenAll(snapshot);
	}
}
