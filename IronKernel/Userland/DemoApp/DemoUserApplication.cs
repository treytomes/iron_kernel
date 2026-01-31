using IronKernel.Modules.ApplicationHost;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic;
using Microsoft.Extensions.Logging;
using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;

namespace IronKernel.Userland.DemoApp;

/// <summary>
/// Simple demo user application.
/// </summary>
public sealed class DemoUserApplication(
	ILogger<DemoUserApplication> logger
) : IUserApplication
{
	private readonly ILogger<DemoUserApplication> _logger = logger;

	public async Task RunAsync(
		IApplicationContext context,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("DemoUserApplication starting");

		var rc = new RenderingContext(context.Bus);
		await rc.InitializeAsync();

		// Initialize application state.
		// context.State.Set("position", new Point(100, 100));

		var world = new WorldMorph(new Size(320, 240), new AssetService(context.Bus));

		world.AddMorph(new ButtonMorph(
			new Point(10, 10),
			new Size(48, 16),
			"Undo")
		{
			Command = new ActionCommand(
				world.Commands.Undo, canExecute: () => world.Commands.CanUndo
			)
		});

		world.AddMorph(new ButtonMorph(
			new Point(64, 10),
			new Size(48, 16),
			"Redo")
		{
			Command = new ActionCommand(
				world.Commands.Redo, canExecute: () => world.Commands.CanRedo
			)
		});

		world.AddMorph(new BoxMorph(new Point(50, 50), new Size(40, 30))
		{
			FillColor = RadialColor.Red,
			BorderColor = RadialColor.Blue.Lerp(RadialColor.White, 0.5f)
		});

		world.AddMorph(new LabelMorph()
		{
			ForegroundColor = RadialColor.Red,
			BackgroundColor = RadialColor.Blue,
			Text = "Hello world!",
		});

		var window = new WindowMorph(new Point(175, 175), new Size(128, 96), "Sample");
		window.Content.AddMorph(new LabelMorph()
		{
			Text = "Here's a label.",
		});
		window.Content.AddMorph(new ButtonMorph(new Point(0, 8), new Size(56, 16), "Hello!")
		{
			Command = new ActionCommand(() => Console.WriteLine("Hello!")),
		});
		world.AddMorph(window);

		var canvas = new FramebufferCanvas(context.Bus);

		context.Bus.Subscribe<AppMouseMoveEvent>(
				"MouseMoveHandler",
				async (e, ct) =>
				{
					var pnt = new Point(e.X, e.Y);
					world.PointerMove(pnt);
					await Task.CompletedTask;
				});

		context.Bus.Subscribe<AppMouseButtonEvent>(
			"MouseButtonHandler",
			async (e, ct) =>
			{
				world.PointerButton(e.Button, e.Action);
				await Task.CompletedTask;
			}
		);

		context.Bus.Subscribe<AppKeyboardEvent>(
			"KeyboardHandler",
			async (e, ct) =>
			{
				if (world.KeyboardFocus != null)
				{
					world.KeyboardFocus.OnKeyDown(e);
				}
				await Task.CompletedTask;
			}
		);

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
				world.Update(e.ElapsedTime);
				rc.Clear();
				world.Draw(rc);
				rc.Present();
				await Task.CompletedTask;
			}
		);

		// context.Bus.Publish(new AppFbClear(RadialColor.Green));
		context.Bus.Publish(new AppFbSetBorder(RadialColor.Green));

		// return Task.CompletedTask;
	}
}