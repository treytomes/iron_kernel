using System.Drawing;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging;
using IronKernel.Morphic;

namespace IronKernel.Userland.DemoApp;

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

	public Task RunAsync(
		IApplicationContext context,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("DemoUserApplication starting");

		// Initialize application state.
		// context.State.Set("position", new Point(100, 100));

		var world = new WorldMorph(new Size(320, 240));
		var hand = new HandMorph { Position = new Point(10, 10) };

		world.AddMorph(new BoxMorph(new Point(50, 50), new Size(40, 30), RadialColor.Blue));

		world.AddMorph(hand);

		var canvas = new FramebufferCanvas(context.Bus);

		// context.Bus.Subscribe<AppKeyboardEvent>(
		// 	"KeyboardHandler",
		// 	async (msg, ct) =>
		// 	{
		// 		_logger.LogInformation(
		// 			"Received keyboard event: {Key}, pressed: {}",
		// 			msg.Key, msg.Action == InputAction.Press);

		// 		if (msg.Action == InputAction.Press || msg.Action == InputAction.Repeat)
		// 		{
		// 			context.State.TryGet("position", out Point position);
		// 			switch (msg.Key)
		// 			{
		// 				case Key.W:
		// 					position.Y--;
		// 					break;
		// 				case Key.S:
		// 					position.Y++;
		// 					break;
		// 				case Key.A:
		// 					position.X--;
		// 					break;
		// 				case Key.D:
		// 					position.X++;
		// 					break;
		// 			}
		// 			context.State.Set("position", position);

		// 			_logger.LogInformation("Position: {Position}", position);
		// 		}
		// 		await Task.CompletedTask;
		// 	});

		context.Bus.Subscribe<AppUpdateTick>(
			"UpdateTickHandler",
			async (e, ct) =>
			{
				canvas.Clear(RadialColor.Black);
				world.Draw(canvas);
				await Task.CompletedTask;
			}
		);

		// context.Bus.Publish(new AppFbClear(RadialColor.Green));
		context.Bus.Publish(new AppFbSetBorder(RadialColor.Green));

		return Task.CompletedTask;
	}
}