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
	private readonly IVirtualDisplay _virtualDisplay = virtualDisplay ?? throw new ArgumentNullException(nameof(virtualDisplay));
	private readonly List<IDisposable> _subscriptions = new();

	#endregion

	#region Methods

	public Task StartAsync(IKernelState state, IModuleRuntime runtime, CancellationToken stoppingToken)
	{
		_logger.LogInformation("Starting framebuffer.");

		// _subscriptions.Add(_bus.Subscribe<HostRenderTick>(
		// 	runtime,
		// 	"RenderTickHandler",
		// 	(msg, ct) =>
		// 	{
		// 		_virtualDisplay.Clear(RadialColor.Green.Index);
		// 		return Task.CompletedTask;
		// 	}
		// ));

		_subscriptions.Add(_bus.Subscribe<HostResizeEvent>(
			runtime,
			"ResizeTickHandler",
			(msg, ct) =>
			{
				_virtualDisplay.Resize(new OpenTK.Mathematics.Vector2i(msg.Width, msg.Height));
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
						{
							break;
						}
					}
				}
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<FbClear>(
			runtime,
			"ClearHandler",
			(msg, ct) =>
			{
				_virtualDisplay.Clear(msg.Color.Index);
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<FbSetBorder>(
			runtime,
			"SetBorderHandler",
			(msg, ct) =>
			{
				_bus.Publish(new HostSetBorderColor(msg.Color.ToColor()));
				return Task.CompletedTask;
			}
		));

		_subscriptions.Add(_bus.Subscribe<FbInfoQuery>(
			runtime,
			"InfoQueryHandler",
			(msg, ct) =>
			{
				// if (!_virtualDisplay.IsInitialized) throw new InvalidOperationException("Virtual display is not initialized.");
				while (!_virtualDisplay.IsInitialized) ; // TODO: This is working, but it's an odd construct.
				var width = _virtualDisplay.Width;
				var height = _virtualDisplay.Height;
				var paletteSize = _virtualDisplay.Palette.Count;
				var paddingX = _virtualDisplay.Padding.X;
				var paddingY = _virtualDisplay.Padding.Y;
				var scale = _virtualDisplay.Scale;
				_bus.Publish(new FbInfoResponse(msg.CorrelationID, new Size(width, height)));
				return Task.CompletedTask;
			}
		));

		_bus.Publish(new HostSetBorderColor(DEFAULT_BORDER_COLOR.ToColor()));

		// _subscriptions.Add(_bus.Subscribe<HostWindowReady>(
		// 	runtime,
		// 	"ReadyHandler",
		// 	(msg, ct) =>
		// 	{
		// 		_bus.Publish(new HostRenderCommand(() =>
		// 		{
		// 			var version = GL.GetString(StringName.Version);
		// 			_logger.LogInformation("GL Version in Initialize: {Version}", version);

		// 			_virtualDisplay.Initialize();
		// 		}));
		// 		return Task.CompletedTask;
		// 	}
		// ));

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