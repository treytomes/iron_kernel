using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public sealed class KernelService : BackgroundService
{
	#region Fields

	private readonly IHostApplicationLifetime _appLifetime;
	private readonly ILogger<KernelService> _logger;
	private readonly IEnumerable<IKernelModule> _modules;
	private readonly IKernelState _state;
	private readonly IMessageBus _bus;

	private readonly CancellationTokenSource _kernelCts = new();
	private IDisposable? _faultSubscription;

	#endregion

	#region Constructors

	public KernelService(
	IHostApplicationLifetime appLifetime,
		ILogger<KernelService> logger,
		IEnumerable<IKernelModule> modules,
		IKernelState state,
		IMessageBus bus)
	{
		_appLifetime = appLifetime;
		_logger = logger;
		_modules = modules;
		_state = state;
		_bus = bus;
	}

	#endregion

	#region Execution

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(
				stoppingToken,
				_kernelCts.Token);

		var runtimes = new List<ModuleRuntime>();

		_faultSubscription =
			_bus.Subscribe<ModuleFaulted>(OnModuleFaulted);

		_logger.LogInformation("Kernel starting");
		_bus.Publish(new KernelStarting());

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

				_logger.LogDebug(
					"Starting module {Module}",
					moduleType.Name);

				_bus.Publish(new ModuleStarted(moduleType));

				await module.StartAsync(
					_state,
					runtime,
					linkedCts.Token);
			}

			_bus.Publish(new KernelStarted());

			// Block until all supervised tasks complete
			await Task.WhenAll(
				runtimes.Select(r => r.WaitAllAsync()));
		}
		catch (OperationCanceledException)
		{
			// Normal shutdown path
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Kernel crashed");
			throw;
		}
		finally
		{
			_logger.LogInformation("Kernel execution loop exited");
		}
	}

	#endregion

	#region Fault Handling

	private Task OnModuleFaulted(ModuleFaulted fault, CancellationToken ct)
	{
		_logger.LogCritical(
			fault.Exception,
			"Module {Module} task '{Task}' faulted — shutting down kernel",
			fault.ModuleType.Name,
			fault.TaskName);

		// Stop kernel execution
		_kernelCts.Cancel();

		// Stop the host (this ends the app)
		_appLifetime.StopApplication();

		return Task.CompletedTask;
	}

	#endregion

	#region Shutdown

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Kernel stopping");
		_bus.Publish(new KernelStopping());

		// Modules will be disposed by DI container
		foreach (var module in _modules)
		{
			_bus.Publish(new ModuleStopped(module.GetType()));
		}

		_bus.Publish(new KernelStopped());

		_faultSubscription?.Dispose();
		_kernelCts.Dispose();

		await base.StopAsync(cancellationToken);
	}

	#endregion
}
