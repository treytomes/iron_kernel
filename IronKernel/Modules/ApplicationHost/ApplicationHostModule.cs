using System.Drawing;
using IronKernel.Common;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.AssetLoader.ValueObjects;
using IronKernel.Modules.Framebuffer.ValueObjects;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.ApplicationHost;

/// <summary>
/// Kernel module responsible for hosting a single user application.
/// </summary>
internal sealed class ApplicationHostModule(
	IUserApplicationFactory factory,
	IMessageBus kernelBus,
	ILogger<ApplicationHostModule> logger
) : IKernelModule
{
	#region Constants

	private const double TARGET_UPDATE_FPS = 60.0;
	private const double TARGET_RENDER_FPS = 60.0;

	#endregion

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

		_bridge.ForwardClocked<HostUpdateTick, AppUpdateTick>(
			"UpdateTickHandler",
			TimeSpan.FromSeconds(1.0 / TARGET_UPDATE_FPS),
			(clock, _) => new(
				clock.TotalTime,
				clock.ElapsedTime
			)
		);

		// _bridge.ForwardClocked<HostRenderTick, ApplicationRenderTick>(
		// 	"RenderTickHandler",
		// 	TimeSpan.FromSeconds(1.0 / TARGET_RENDER_FPS),
		// 	(clock, _) => new(
		// 		clock.TotalTime,
		// 		clock.ElapsedTime
		// 	)
		// );

		_bridge.Forward<HostResizeEvent, AppResizeEvent>(
			"ResizeEventHandler",
			(e, ct) => new(
				e.Width,
				e.Height
			)
		);

		_bridge.Forward<HostShutdown, AppShutdown>(
			"ShutdownHandler",
			(e, ct) => new(
			)
		);

		_bridge.Forward<HostAcquiredFocus, AppAcquiredFocus>(
			"AcquiredFocusHandler",
			(e, ct) => new(
			)
		);

		_bridge.Forward<HostLostFocus, AppLostFocus>(
			"LostFocusHandler",
			(e, ct) => new(
			)
		);

		_bridge.Forward<HostMouseWheelEvent, AppMouseWheelEvent>(
			"MouseWheelHandler",
			(e, ct) => new(
				e.OffsetX,
				e.OffsetY
			)
		);

		_bridge.Forward<HostMouseMoveEvent, AppMouseMoveEvent>(
			"MouseMoveHandler",
			(e, ct) => new(
				e.X,
				e.Y,
				e.DeltaX,
				e.DeltaY
			)
		);

		_bridge.Forward<HostMouseButtonEvent, AppMouseButtonEvent>(
			"MouseButtonHandler",
			(e, ct) => new(
				e.Action,
				e.Button,
				e.Modifiers
			)
		);

		_bridge.Forward<HostKeyboardEvent, AppKeyboardEvent>(
			"KeyboardHandler",
			(e, ct) => new(
				e.Action,
				e.Modifiers,
				e.Key
			)
		);

		_bridge.Request<AppFbWriteSpan, FbWriteSpan>(
			"AppFbWriteSpanHandler",
			(e, ct) => new(e.X, e.Y, e.Data)
		);

		_bridge.Request<AppFbClear, FbClear>(
			"AppFbClearHandler",
			(e, ct) => new(e.Color)
		);

		_bridge.Request<AppFbSetBorder, FbSetBorder>(
			"AppFbSetBorderHandler",
			(e, ct) => new(e.Color)
		);

		_bridge.Forward<FbInfoResponse, AppFbInfoResponse>(
			"FbInfoHandler",
			(e, ct) => new(e.CorrelationID, e.Size)
		);

		_bridge.Request<AppFbInfoQuery, FbInfoQuery>(
			"AppFbInfoHandler",
			(e, ct) => new(e.CorrelationID)
		);

		_bridge.Request<AppAssetImageQuery, AssetImageQuery>("AppAssetImageQueryHandler", (e, ct) => new(e.CorrelationID, e.AssetId));
		_bridge.Forward<AssetImageResponse, AppAssetImageResponse>("AppAssetImageResponse", (e, ct) => new(e.CorrelationID, e.AssetId, e.Image));

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
