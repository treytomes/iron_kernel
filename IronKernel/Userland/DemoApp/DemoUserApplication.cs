using IronKernel.Modules.ApplicationHost;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic;
using Microsoft.Extensions.Logging;
using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Roguey;

namespace IronKernel.Userland.DemoApp;

/// <summary>
/// Simple demo user application.
/// </summary>
public sealed class DemoUserApplication(
	ILogger<DemoUserApplication> logger
) : IUserApplication
{
	private readonly ILogger<DemoUserApplication> _logger = logger;
	private readonly object _updateLock = new();
	private readonly object _renderLock = new();

	public async Task RunAsync(
		IApplicationContext context,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("DemoUserApplication starting");

		RogueyIntrinsics.Create();

		var rc = new RenderingContext(_logger, context.Bus);
		await rc.InitializeAsync();

		// Initialize application state.
		// context.State.Set("position", new Point(100, 100));

		var world = new WorldMorph(new Size(960, 480), new AssetService(context.Bus));

		var toolbar = new ToolbarMorph
		{
			Position = new Point(4, 4)
		};

		toolbar.AddItem(
			"Undo",
			new ActionCommand(
				world.Commands.Undo,
				canExecute: () => world.Commands.CanUndo
			)
		);

		toolbar.AddItem(
			"Redo",
			new ActionCommand(
				world.Commands.Redo,
				canExecute: () => world.Commands.CanRedo
			)
		);

		toolbar.AddItem(
			"Apps",
			new ActionCommand(() =>
			{
				var launcher = new LauncherMorph(new Point(16, 16));
				launcher.AddApp<DummyReplMorph>("Dummy REPL");
				launcher.AddApp<MiniScriptReplMorph>("MiniScript REPL");
				world.AddMorph(launcher);
			})
		);

		world.AddMorph(toolbar);

		var text = "Name";
		world.AddMorph(new TextEditMorph(new Point(0, 32), text, x => text = x));

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

		// world.AddMorph(new TileMorph()
		// {
		// 	Position = new Point(100, 100),
		// 	ForegroundColor = RadialColor.Red,
		// 	BackgroundColor = RadialColor.Blue,
		// 	TileIndex = 176
		// });

		var mapWidth = 256;
		var mapHeight = 256;
		var tileSetInfo = new TileSetInfo("image.oem437_8", new Size(8, 8));
		var map = new TileMapMorph(new Size(320, 240), new Size(256, 256), tileSetInfo);
		for (var y = 0; y < mapHeight; y++)
		{
			for (var x = 0; x < mapWidth; x++)
			{
				var tile = map.GetTile(x, y);
				if (tile == null) continue;
				tile.TileIndex = Random.Shared.Next(176, 179);
			}
		}
		map.Position = new Point(128, 128);
		world.AddMorph(map);

		// world.AddMorph(new MiniScriptMorph()
		// {
		// 	Position = new Point(200, 200),
		// 	Size = new Size(200, 200)
		// });

		// world.AddMorph(new DummyReplMorph(new Point(175, 175)));

		context.Bus.Subscribe<AppMouseWheelEvent>(
			"MouseWheelHandler",
			async (e, ct) =>
			{
				// Console.WriteLine($"Mouse wheel: {e.OffsetX}, {e.OffsetY}");
				var pnt = new Point(e.OffsetX, e.OffsetY);
				world.PointerWheel(pnt);
				await Task.CompletedTask;
			}
		);

		context.Bus.Subscribe<AppMouseMoveEvent>(
			"MouseMoveHandler",
			async (e, ct) =>
			{
				var pnt = new Point(e.X, e.Y);
				world.PointerMove(pnt);
				await Task.CompletedTask;
			}
		);

		context.Bus.Subscribe<AppMouseButtonEvent>(
			"MouseButtonHandler",
			async (e, ct) =>
			{
				world.PointerButton(e.Button, e.Action, e.Modifiers);
				await Task.CompletedTask;
			}
		);

		context.Bus.Subscribe<AppKeyboardEvent>(
			"KeyboardHandler",
			async (e, ct) =>
			{
				world.KeyPress(e.Action, e.Modifiers, e.Key);
				await Task.CompletedTask;
			}
		);

		context.Bus.Subscribe<AppUpdateTick>(
			"UpdateTickHandler",
			async (e, ct) =>
			{
				if (Monitor.TryEnter(_updateLock))
				{
					try
					{
						world.Update(e.ElapsedTime);
					}
					finally
					{
						Monitor.Exit(_updateLock);
					}
				}
				await Task.CompletedTask;
			}
		);

		context.Bus.Subscribe<AppRenderTick>(
			"RenderTickHandler",
			async (e, ct) =>
			{
				if (Monitor.TryEnter(_renderLock))
				{
					try
					{
						rc.Fill(new RadialColor(0, 2, 5));
						world.Draw(rc);
						rc.Present();
						await Task.CompletedTask;
					}
					finally
					{
						Monitor.Exit(_renderLock);
					}
				}
			}
		);

		// context.Bus.Publish(new AppFbClear(RadialColor.Green));
		context.Bus.Publish(new AppFbSetBorder(RadialColor.Green));

		// return Task.CompletedTask;
	}
}