using System.Drawing;
using System.Numerics;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Modules.Common.ValueObjects;
using IronKernel.Modules.OpenTKHost.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Applications.DemoApp;

/// <summary>
/// Simple demo user application.
/// </summary>
public sealed class DemoUserApplication : IUserApplication
{
	private readonly ILogger<DemoUserApplication> _logger;

	public DemoUserApplication(ILogger<DemoUserApplication> logger)
	{
		_logger = logger;
	}

	public async Task RunAsync(
		IApplicationContext context,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("DemoUserApplication starting");

		// Initialize application state.
		context.State.Set("position", new Point(100, 100));

		context.Bus.Subscribe<AppKeyboardEvent>(
			"KeyboardHandler",
			async (msg, ct) =>
			{
				_logger.LogInformation(
					"Received keyboard event: {Key}, pressed: {}",
					msg.Key, msg.Action == InputAction.Press);

				if (msg.Action == InputAction.Press || msg.Action == InputAction.Repeat)
				{
					context.State.TryGet("position", out Point position);
					switch (msg.Key)
					{
						case Key.W:
							position.Y--;
							break;
						case Key.S:
							position.Y++;
							break;
						case Key.A:
							position.X--;
							break;
						case Key.D:
							position.X++;
							break;
					}
					context.State.Set("position", position);

					_logger.LogInformation("Position: {Position}", position);
				}
				await Task.CompletedTask;
			});

		context.Bus.Subscribe<AppUpdateTick>(
			"UpdateTickHandler",
			async (e, ct) =>
			{
				context.State.TryGet("position", out Point position);
				context.Bus.Publish(new AppFbWriteSpan(position.X, position.Y, [RadialColor.Red]));
				await Task.CompletedTask;
			}
		);

		context.Bus.Publish(new AppFbClear(RadialColor.Green));
		context.Bus.Publish(new AppFbSetBorder(RadialColor.DarkGray));

		// Keep main alive until shutdown.
		// Not really sure this is necessary.
		// await Task.Delay(Timeout.Infinite, stoppingToken);
	}
}