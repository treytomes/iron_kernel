using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.Framebuffer.ValueObjects;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace IronKernel.Modules.Framebuffer;

internal sealed class FramebufferModule(
	IMessageBus bus,
	ILogger<FramebufferModule> logger,
	IVirtualDisplay virtualDisplay
) : IKernelModule
{
	#region Constants

	private static readonly RadialColor DEFAULT_BORDER_COLOR = RadialColor.DarkGray;

	#endregion

	#region Fields

	private readonly IMessageBus _bus = bus;
	private readonly ILogger<FramebufferModule> _logger = logger;
	private readonly IVirtualDisplay _virtualDisplay =
		virtualDisplay ?? throw new ArgumentNullException(nameof(virtualDisplay));
	private readonly List<IDisposable> _subscriptions = new();
	private ulong _currentFrameId;
	private bool _isVideoReady = false;

	#endregion

	#region Methods
	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("Starting framebuffer.");

		// // NEW: participate in frame cadence
		// _subscriptions.Add(_bus.Subscribe<HostRenderTick>(
		// 	runtime,
		// 	"RenderTickHandler",
		// 	(msg, ct) =>
		// 	{
		// 		// At this point:
		// 		// - All FbWriteSpan / FbClear messages already queued
		// 		// - No GL work is required here
		// 		// - Framebuffer state is coherent for rendering

		// 		_bus.Publish(new FbFrameReady(msg.FrameId));
		// 		return Task.CompletedTask;
		// 	}
		// ));

		_subscriptions.Add(_bus.Subscribe<HostWindowReady>(
			runtime,
			"VideoReadyHandler",
			(msg, ct) =>
			{
				_isVideoReady = true;
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<HostRenderTick>(
			runtime,
			"RenderTickHandler",
			(msg, ct) =>
			{
				_currentFrameId = msg.FrameId;
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<HostResizeEvent>(
			runtime,
			"ResizeTickHandler",
			(msg, ct) =>
			{
				_virtualDisplay.Resize(
					new OpenTK.Mathematics.Vector2i(msg.Width, msg.Height));
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<FbWriteSpan>(
			runtime,
			"WriteSpanHandler",
			(msg, ct) =>
			{
				var x = msg.X;
				var y = msg.Y;

				for (var n = 0; n < msg.Data.Count && !ct.IsCancellationRequested; n++)
				{
					_virtualDisplay.SetPixel(x, y, msg.Data[n]?.Index ?? 0);
					x++;

					if (x >= _virtualDisplay.Width)
					{
						x = 0;
						y++;
						if (y >= _virtualDisplay.Height)
							break;
					}
				}

				if (msg.IsComplete)
				{
					_bus.Publish(new FbFrameReady(_currentFrameId));
				}

				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<FbSetBorder>(
			runtime,
			"SetBorderHandler",
			(msg, ct) =>
			{
				_bus.Publish(
					new HostSetBorderColor(msg.Color.ToColor()));
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<FbInfoQuery>(
			runtime,
			"InfoQueryHandler",
			(msg, ct) =>
			{
				if (!_isVideoReady)
				{
					// Wait a bit and try again.
					_bus.Publish(msg);
					return Task.CompletedTask;
				}

				var width = _virtualDisplay.Width;
				var height = _virtualDisplay.Height;

				_bus.Publish(
					new FbInfoResponse(
						msg.CorrelationID,
						new Size(width, height)));

				return Task.CompletedTask;
			}
		));

		_bus.Publish(
			new HostSetBorderColor(
				DEFAULT_BORDER_COLOR.ToColor()));

		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		_logger.LogInformation("Disposing framebuffer.");

		foreach (var s in _subscriptions)
			s.Dispose();

		_subscriptions.Clear();
		return ValueTask.CompletedTask;
	}
	#endregion
}