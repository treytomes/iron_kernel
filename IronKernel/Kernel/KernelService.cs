using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public sealed class KernelService : BackgroundService
{
	#region Fields

	private readonly ILogger<KernelService> _logger;
	private readonly IEnumerable<IKernelModule> _modules;
	private readonly IKernelState _state;
	private readonly IMessageBus _bus;
	private readonly List<Task> _runningTasks = new();

	#endregion

	#region Constructors

	public KernelService(
		ILogger<KernelService> logger,
		IEnumerable<IKernelModule> modules,
		IKernelState state,
		IMessageBus bus)
	{
		_logger = logger;
		_modules = modules;
		_state = state;
		_bus = bus;
	}

	#endregion

	#region Methods

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Kernel starting");
		_bus.Publish(new KernelStarting());

		var runtimes = new List<ModuleRuntime>();

		try
		{
			foreach (var module in _modules)
			{
				var moduleType = module.GetType();

				var runtime = new ModuleRuntime(
					moduleType,
					_bus,
					_logger);

				runtimes.Add(runtime);

				_logger.LogDebug("Starting module {Module}", moduleType.Name);
				_bus.Publish(new ModuleStarted(moduleType));

				await module.StartAsync(_state, runtime, stoppingToken);
			}

			_bus.Publish(new KernelStarted());

			// Wait for supervised tasks
			await Task.WhenAll(
				runtimes.Select(r => r.WaitAllAsync()));
		}
		catch (OperationCanceledException)
		{
			// Normal shutdown
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Kernel crashed");
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Kernel stopping");
		_bus.Publish(new KernelStopping());

		foreach (var module in _modules)
		{
			try
			{
				var moduleType = module.GetType();
				await module.DisposeAsync();
				_bus.Publish(new ModuleStopped(moduleType));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing module {Module}", module.GetType().Name);
			}
		}

		_bus.Publish(new KernelStopped());

		await base.StopAsync(cancellationToken);
	}

	#endregion
}
