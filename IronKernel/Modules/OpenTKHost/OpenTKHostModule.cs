using IronKernel.Common.ValueObjects;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using IronKernel.Modules.Framebuffer;
using IronKernel.Modules.Framebuffer.ValueObjects;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;

namespace IronKernel.Modules.OpenTKHost;

internal sealed class OpenTKHostModule(
	AppSettings settings,
	IMessageBus bus,
	ILogger<OpenTKHostModule> logger,
	IVirtualDisplay virtualDisplay
) : IKernelModule,
	IPrimaryKernelModule,
	IAsyncDisposable
{
	#region Fields
	private readonly AppSettings.WindowSettings _settings = settings.Window;
	private readonly IMessageBus _bus = bus;
	private readonly ILogger<OpenTKHostModule> _logger = logger;
	private readonly IVirtualDisplay _virtualDisplay = virtualDisplay;

	private GameWindow? _window;
	private bool _isDisposed;
	private volatile bool _shutdownRequested;

	private double _totalRenderTime;
	private double _totalUpdateTime;

	private bool _isReady;
	private Color4 _borderColor = Color4.Black;

	private ulong _nextFrameId;
	private readonly ConcurrentDictionary<ulong, TaskCompletionSource> _frameBarriers = new();
	#endregion

	#region Start / Run
	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		_window = new GameWindow(
			GameWindowSettings.Default,
			NativeWindowSettings.Default);

		if (_settings.Fullscreen)
			_window.WindowState = WindowState.Fullscreen;
		else if (_settings.Maximize)
			_window.WindowState = WindowState.Maximized;

		_bus.Subscribe<KernelShutdownRequested>(
			runtime,
			"KernelShutdownRequestHandler",
			OnShutdownRequested);

		_bus.Subscribe<HostSetBorderColor>(
			runtime,
			"SetBorderColorHandler",
			(msg, ct) =>
			{
				_borderColor = msg.Color;
				return Task.CompletedTask;
			});

		_bus.Subscribe<FbFrameReady>(
			runtime,
			"FramebufferReadyHandler",
			OnFramebufferReady);

		HookEvents();
		return Task.CompletedTask;
	}

	public void Run()
	{
		_logger.LogInformation("OpenTK host running");
		_window!.Run();
		_logger.LogInformation("OpenTK host exited");
	}
	#endregion

	#region Message handlers
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

	private Task OnFramebufferReady(
		FbFrameReady msg,
		CancellationToken _)
	{
		if (_frameBarriers.TryRemove(msg.FrameId, out var tcs))
			tcs.TrySetResult();

		return Task.CompletedTask;
	}
	#endregion

	#region Window event wiring
	private void HookEvents()
	{
		_window!.UpdateFrame += e =>
		{
			if (_shutdownRequested)
				_window.Close();

			_totalUpdateTime += e.Time;
			_bus.Publish(new HostUpdateTick(_totalUpdateTime, e.Time));
		};

		_window.RenderFrame += e =>
		{
			if (!_isReady)
			{
				_virtualDisplay.Initialize();
				_bus.Publish(new HostWindowReady());
				_isReady = true;
			}

			var frameId = Interlocked.Increment(ref _nextFrameId);
			_totalRenderTime += e.Time;

			var tcs = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously);

			_frameBarriers[frameId] = tcs;

			_bus.Publish(new HostRenderTick(frameId, _totalRenderTime, e.Time));

			// ---- FRAME BARRIER (SYNC, BOUNDED) ----
			bool completed = false;
			try
			{
				completed = tcs.Task.Wait(1000);
			}
			catch (AggregateException ex)
			{
				_logger.LogError(ex, "Exception while waiting for framebuffer");
			}

			if (!completed)
			{
				_logger.LogWarning(
					"FramebufferReady timeout for frame {FrameId}",
					frameId);

				_frameBarriers.TryRemove(frameId, out _);
			}

			// ---- SAFE GL SECTION ----
			GL.ClearColor(_borderColor);
			GL.Clear(ClearBufferMask.ColorBufferBit);

			_virtualDisplay.Render();
			_window.SwapBuffers();
		};

		_window.KeyDown += e =>
		{
			var action = e.IsRepeat ? InputAction.Repeat : InputAction.Press;
			_bus.Publish(new HostKeyboardEvent(
				action,
				e.Modifiers.ToHost(),
				e.Key.ToHost()));
		};

		_window.KeyUp += e =>
		{
			var action = e.IsRepeat ? InputAction.Repeat : InputAction.Release;
			_bus.Publish(new HostKeyboardEvent(
				action,
				e.Modifiers.ToHost(),
				e.Key.ToHost()));
		};

		_window.MouseDown += e =>
			_bus.Publish(new HostMouseButtonEvent(
				e.Action.ToHost(),
				e.Button.ToHost(),
				e.Modifiers.ToHost()));

		_window.MouseUp += e =>
			_bus.Publish(new HostMouseButtonEvent(
				e.Action.ToHost(),
				e.Button.ToHost(),
				e.Modifiers.ToHost()));

		_window.MouseMove += HandleMouseMove;

		_window.MouseWheel += e =>
			_bus.Publish(new HostMouseWheelEvent(
				(int)e.OffsetX,
				(int)e.OffsetY));

		_window.Resize += e =>
			_bus.Publish(new HostResizeEvent(e.Width, e.Height));

		_window.MouseEnter += () =>
			_bus.Publish(new HostAcquiredFocus());

		_window.MouseLeave += () =>
			_bus.Publish(new HostLostFocus());

		_window.Closing += _ =>
			_bus.Publish(new HostShutdown());
	}
	#endregion

	#region Mouse handling
	private void HandleMouseMove(MouseMoveEventArgs e)
	{
		if (_window == null)
			return;

		var position = _virtualDisplay.ActualToVirtualPoint(e.Position);
		var delta = e.Delta / _virtualDisplay.Scale;

		if (position.X < 0 || position.Y < 0 ||
			position.X > _virtualDisplay.Width ||
			position.Y > _virtualDisplay.Height)
		{
			_window.CursorState = CursorState.Normal;
		}
		else
		{
			_window.CursorState = CursorState.Hidden;
		}

		_bus.Publish(new HostMouseMoveEvent(
			(int)position.X,
			(int)position.Y,
			(int)delta.X,
			(int)delta.Y));
	}
	#endregion

	#region Disposal
	public ValueTask DisposeAsync()
	{
		if (!_isDisposed)
		{
			_window?.Dispose();
			_isDisposed = true;
		}
		return ValueTask.CompletedTask;
	}
	#endregion
}