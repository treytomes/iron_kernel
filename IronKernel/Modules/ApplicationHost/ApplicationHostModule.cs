using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.ApplicationHost;

/// <summary>
/// Kernel module responsible for hosting a single user application.
/// </summary>
public sealed class ApplicationHostModule(
	IUserApplicationFactory factory,
	IMessageBus kernelBus,
	ILogger<ApplicationHostModule> logger
) : IKernelModule
{
	#region Fields

	private readonly IUserApplicationFactory _factory = factory;
	private readonly ILogger<ApplicationHostModule> _logger = logger;
	private readonly IMessageBus _kernelBus = kernelBus;

	private IUserApplication? _application;
	private ApplicationRuntime? _runtime;
	private ApplicationBus? _bus;
	private ApplicationState? _state;
	private ApplicationBusBridge? _bridge;

	#endregion

	#region Methods

	public Task StartAsync(
		IKernelState kernelState,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		_application = _factory.Create();

		_logger.LogInformation("Starting application {Application}", _application.GetType().Name);

		_state = new ApplicationState();
		_bus = new ApplicationBus(runtime, _kernelBus);
		_runtime = new ApplicationRuntime(runtime);
		_bridge = new ApplicationBusBridge(_kernelBus, _bus, runtime);

		_bridge.Forward<HostUpdateTick, ApplicationUpdateTick>(
			"UpdateTickHandler",
			(e, ct) => new(
				e.TotalTime,
				e.ElapsedTime
			)
		);

		_bridge.Forward<HostRenderTick, ApplicationRenderTick>(
			"RenderTickHandler",
			(e, ct) => new(
				e.TotalTime,
				e.ElapsedTime
			)
		);

		_bridge.Forward<HostResizeEvent, ApplicationResizeEvent>(
			"ResizeEventHandler",
			(e, ct) => new(
				e.Width,
				e.Height
			)
		);

		_bridge.Forward<HostShutdown, ApplicationShutdown>(
			"ShutdownHandler",
			(e, ct) => new(
			)
		);

		_bridge.Forward<HostAcquiredFocus, ApplicationAcquiredFocus>(
			"AcquiredFocusHandler",
			(e, ct) => new(
			)
		);

		_bridge.Forward<HostLostFocus, ApplicationLostFocus>(
			"LostFocusHandler",
			(e, ct) => new(
			)
		);

		_bridge.Forward<HostMouseWheelEvent, ApplicationMouseWheelEvent>(
			"MouseWheelHandler",
			(e, ct) => new(
				e.OffsetX,
				e.OffsetY
			)
		);

		_bridge.Forward<HostMouseMoveEvent, ApplicationMouseMoveEvent>(
			"MouseMoveHandler",
			(e, ct) => new(
				e.X,
				e.Y,
				e.DeltaX,
				e.DeltaY
			)
		);

		_bridge.Forward<HostMouseButtonEvent, ApplicationMouseButtonEvent>(
			"MouseButtonHandler",
			(e, ct) => new(
				e.Action,
				e.Button,
				e.Modifiers
			)
		);

		_bridge.Forward<HostKeyboardEvent, ApplicationKeyboardEvent>(
			"KeyboardHandler",
			(e, ct) => new(
				e.Action,
				e.Modifiers,
				e.Key
			)
		);

		var context = new ApplicationContext(
			_bus,
			_runtime,
			_state
		);

		runtime.RunDetached(
			"ApplicationMain",
			ModuleTaskKind.Resident,
			ct => _application.RunAsync(context, ct),
			stoppingToken);

		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		_bridge?.Dispose();

		if (_application is not null)
		{
			_logger.LogInformation(
				"Disposing application {Application}",
				_application.GetType().Name);
		}

		_bus?.Dispose();
		return ValueTask.CompletedTask;
	}

	#endregion
}
