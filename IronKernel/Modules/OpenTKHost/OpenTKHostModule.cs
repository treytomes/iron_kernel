using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;
using OpenTK.Windowing.Desktop;

namespace IronKernel.Modules.OpenTKHost;

public sealed class OpenTKHostModule(
	IMessageBus bus,
	ILogger<OpenTKHostModule> logger
) : IKernelModule,
	IPrimaryKernelModule,
	IAsyncDisposable
{
	#region Fields

	private readonly IMessageBus _bus = bus;
	private readonly ILogger<OpenTKHostModule> _logger = logger;

	private GameWindow? _window;
	private volatile bool _shutdownRequested;
	private double _totalRenderTime = 0.0;
	private double _totalUpdateTime = 0.0f;

	#endregion

	#region Methods

	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		var settings = GameWindowSettings.Default;
		var native = NativeWindowSettings.Default;

		_window = new GameWindow(settings, native);

		_bus.Subscribe<KernelShutdownRequested>(runtime, "KernelShutdownRequestHandler", OnShutdownRequested);

		HookEvents();

		return Task.CompletedTask;
	}

	public void Run()
	{
		_logger.LogInformation("OpenTK host running");

		_window!.Run();

		_logger.LogInformation("OpenTK host exited");
	}

	private Task OnShutdownRequested(
		KernelShutdownRequested msg,
		CancellationToken _)
	{
		_logger.LogInformation(
			"OpenTK received shutdown request: {Reason}",
			msg.Reason);

		_shutdownRequested = true;
		return Task.CompletedTask;
	}

	private void HookEvents()
	{
		_window!.UpdateFrame += e =>
		{
			// Tick
			if (_shutdownRequested)
				_window.Close();

			_totalUpdateTime += e.Time;
			_bus.Publish(new HostUpdateTick(_totalUpdateTime, e.Time));
		};

		_window.RenderFrame += e =>
		{
			// Vertical sync
			_totalRenderTime += e.Time;
			_bus.Publish(new HostRenderTick(_totalRenderTime, e.Time));
			_window.SwapBuffers();
		};

		_window.KeyDown += e =>
		{
			// e.ScanCode feels like the wrong king of hardware abstraction to publish, but I'm not sure.
			var action = e.IsRepeat ? InputAction.Repeat : InputAction.Press;
			_bus.Publish(new HostKeyboardEvent(
				action,
				e.Modifiers.ToHost(),
				e.Key.ToHost()
			));
		};

		_window.KeyUp += e =>
		{
			// e.ScanCode feels like the wrong king of hardware abstraction to publish, but I'm not sure.
			var action = e.IsRepeat ? InputAction.Repeat : InputAction.Release;

			// The alt, command, control, and shift state are all in the modifiers property, so we don't need those either.
			_bus.Publish(new HostKeyboardEvent(
				action,
				e.Modifiers.ToHost(),
				e.Key.ToHost()
			));
		};

		_window.MouseDown += e =>
		{
			// The e.IsPressed property feels redundant when we also have e.Action.
			_bus.Publish(new HostMouseButtonEvent(
				e.Action.ToHost(),
				e.Button.ToHost(),
				e.Modifiers.ToHost()
			));
		};

		_window.MouseUp += e =>
		{
			// The e.IsPressed property feels redundant when we also have e.Action.
			_bus.Publish(new HostMouseButtonEvent(
				e.Action.ToHost(),
				e.Button.ToHost(),
				e.Modifiers.ToHost()
			));
		};

		_window.MouseMove += e =>
		{
			_bus.Publish(new HostMouseMoveEvent(e.X, e.Y, e.DeltaX, e.DeltaY));
		};

		_window.MouseWheel += e =>
		{
			_bus.Publish(new HostMouseWheelEvent(e.OffsetX, e.OffsetY));
		};

		_window.Resize += e =>
		{
			_bus.Publish(new HostResizeEvent(e.Width, e.Height));
		};

		_window.Closing += _ =>
			_bus.Publish(new HostShutdown());

		_window.MouseEnter += () =>
		{
			_bus.Publish(new HostAcquiredFocus());
		};

		_window.MouseLeave += () =>
		{
			_bus.Publish(new HostLostFocus());
		};
	}

	public ValueTask DisposeAsync()
	{
		_window?.Dispose();
		return ValueTask.CompletedTask;
	}

	#endregion
}
