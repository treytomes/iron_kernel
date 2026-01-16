using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.ApplicationHost;

/// <summary>
/// Kernel module responsible for hosting a single user application.
/// </summary>
public sealed class ApplicationHostModule : IKernelModule
{
	private readonly IUserApplicationFactory _factory;
	private readonly ILogger<ApplicationHostModule> _logger;
	private readonly IMessageBus _kernelBus;

	private IUserApplication? _application;
	private ApplicationRuntime? _runtime;
	private ApplicationBus? _bus;
	private ApplicationState? _state;

	public ApplicationHostModule(
		IUserApplicationFactory factory,
		IMessageBus kernelBus,
		ILogger<ApplicationHostModule> logger)
	{
		_factory = factory;
		_kernelBus = kernelBus;
		_logger = logger;
	}

	public Task StartAsync(
		IKernelState kernelState,
		IModuleRuntime kernelRuntime,
		CancellationToken stoppingToken)
	{
		_application = _factory.Create();

		_logger.LogInformation(
			"Starting application {Application}",
			_application.GetType().Name);

		_state = new ApplicationState();
		_bus = new ApplicationBus(kernelRuntime, _kernelBus);
		_runtime = new ApplicationRuntime(kernelRuntime);

		var context = new ApplicationContext(
			_bus,
			_runtime,
			_state);

		kernelRuntime.RunDetached(
			"ApplicationMain",
			ModuleTaskKind.Resident,
			ct => _application.RunAsync(context, ct),
			stoppingToken);

		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		if (_application is not null)
		{
			_logger.LogInformation(
				"Disposing application {Application}",
				_application.GetType().Name);
		}

		_bus?.Dispose();
		return ValueTask.CompletedTask;
	}
}
