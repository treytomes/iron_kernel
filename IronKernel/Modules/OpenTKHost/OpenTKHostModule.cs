using IronKernel.Common.ValueObjects;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using IronKernel.Modules.Framebuffer;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace IronKernel.Modules.OpenTKHost;

internal sealed class OpenTKHostModule(
	IMessageBus bus,
	ILogger<OpenTKHostModule> logger,
	IVirtualDisplay virtualDisplay
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
	// private readonly ConcurrentQueue<Action> _renderCommands = new();
	private readonly IVirtualDisplay _virtualDisplay = virtualDisplay ?? throw new ArgumentNullException(nameof(virtualDisplay));
	private bool _isReady = false;
	private Color4 _borderColor = Color4.Black;

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

		_bus.Subscribe<KernelShutdownRequested>(
			runtime,
			"KernelShutdownRequestHandler",
			OnShutdownRequested
		);

		// _bus.Subscribe<HostRenderCommand>(
		// 	runtime,
		// 	"RenderCommandHandler",
		// 	(cmd, ct) =>
		// 	{
		// 		_renderCommands.Enqueue(cmd.Execute);
		// 		return Task.CompletedTask;
		// 	});

		_bus.Subscribe<HostSetBorderColor>(
			runtime,
			"SetBorderColorHandler",
			(msg, ct) =>
			{
				_borderColor = msg.Color;
				return Task.CompletedTask;
			}
		);

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
			GL.ClearColor(_borderColor);
			GL.Clear(ClearBufferMask.ColorBufferBit);

			// while (_renderCommands.TryDequeue(out var cmd))
			// 	cmd();

			if (!_isReady)
			{
				_bus.Publish(new HostWindowReady());
				_virtualDisplay.Initialize();
				_isReady = true;
			}

			_totalRenderTime += e.Time;
			_bus.Publish(new HostRenderTick(_totalRenderTime, e.Time));
			_virtualDisplay.Render();
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

		_window.MouseMove += HandleMouseMove;

		_window.MouseWheel += e =>
		{
			_bus.Publish(new HostMouseWheelEvent((int)e.OffsetX, (int)e.OffsetY));
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


	private void HandleMouseMove(MouseMoveEventArgs e)
	{
		if (_window == null) return;

		var position = _virtualDisplay.ActualToVirtualPoint(e.Position);
		var delta = e.Delta / _virtualDisplay.Scale;

		if (position.X < 0 || position.Y < 0 || position.X > _virtualDisplay.Width || position.Y > _virtualDisplay.Height)
		{
			// The cursor has fallen off the virtual display.  
			_window.CursorState = CursorState.Normal;
		}
		else
		{
			_window.CursorState = CursorState.Hidden;
		}

		e = new MouseMoveEventArgs(position, delta);

		_bus.Publish(new HostMouseMoveEvent((int)e.X, (int)e.Y, (int)e.DeltaX, (int)e.DeltaY));
	}

	public ValueTask DisposeAsync()
	{
		_window?.Dispose();
		return ValueTask.CompletedTask;
	}

	#endregion
}
