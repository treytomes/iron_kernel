using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Roguey;
using IronKernel.Userland.Services;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace IronKernel.Userland.DemoApp;

public sealed class DemoAppRoot
{
	private readonly ILogger<DemoAppRoot> _logger;
	private readonly RenderingContext _rc;
	private readonly IApplicationContext _context;
	private readonly IFileSystem _fileSystem;

	private readonly object _updateLock = new();
	private readonly object _renderLock = new();

	public DemoAppRoot(
		ILogger<DemoAppRoot> logger,
		RenderingContext rc,
		IApplicationContext context,
		IFileSystem fileSystem)
	{
		_logger = logger;
		_rc = rc;
		_context = context;
		_fileSystem = fileSystem;
	}

	public async Task RunAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("DemoAppRoot starting");

		RogueyIntrinsics.Create();
		await _rc.InitializeAsync();

		var world = new WorldMorph(
			new Size(960, 480),
			new AssetService(_context.Bus));

		// ------------------------------------------------------------------
		// UI setup
		// ------------------------------------------------------------------
		var toolbar = new ToolbarMorph
		{
			Position = new Point(4, 4)
		};

		toolbar.AddItem(
			"Undo",
			new ActionCommand(
				world.Commands.Undo,
				canExecute: () => world.Commands.CanUndo));

		toolbar.AddItem(
			"Redo",
			new ActionCommand(
				world.Commands.Redo,
				canExecute: () => world.Commands.CanRedo));

		toolbar.AddItem(
			"Apps",
			new ActionCommand(() =>
			{
				var launcher = new LauncherMorph(new Point(16, 16));
				launcher.AddApp<DummyReplMorph>("Dummy REPL");
				launcher.AddApp<MiniScriptReplMorph>("MiniScript REPL");
				launcher.AddApp(
					"Text Editor",
					() => new TextEditorWindowMorph(_fileSystem));

				world.AddMorph(launcher);
			}));

		world.AddMorph(toolbar);

		var text = "Name";
		world.AddMorph(
			new TextEditMorph(new Point(0, 32), text, x => text = x));

		world.AddMorph(
			new BoxMorph(new Point(50, 50), new Size(40, 30))
			{
				FillColor = RadialColor.Red,
				BorderColor = RadialColor.Blue.Lerp(
					RadialColor.White, 0.5f)
			});

		world.AddMorph(
			new LabelMorph
			{
				Text = "Hello world!",
				ForegroundColor = RadialColor.Red,
				BackgroundColor = RadialColor.Blue
			});

		// ------------------------------------------------------------------
		// Tile map demo
		// ------------------------------------------------------------------
		var tileSetInfo =
			new TileSetInfo("image.screen_font", new Size(16, 24));

		var map = new TileMapMorph(
			new Size(320, 240),
			new Size(256, 256),
			tileSetInfo);

		for (var y = 0; y < 256; y++)
			for (var x = 0; x < 256; x++)
			{
				var tile = map.GetTile(x, y);
				if (tile != null)
					tile.TileIndex = Random.Shared.Next(176, 179);
			}

		map.Position = new Point(128, 128);
		world.AddMorph(map);

		// ------------------------------------------------------------------
		// Event wiring
		// ------------------------------------------------------------------
		var bus = _context.Bus;

		bus.Subscribe<AppMouseWheelEvent>(
			"MouseWheel",
			(e, _) =>
			{
				world.PointerWheel(
					new Point(e.OffsetX, e.OffsetY));
				return Task.CompletedTask;
			});

		bus.Subscribe<AppMouseMoveEvent>(
			"MouseMove",
			(e, _) =>
			{
				world.PointerMove(
					new Point(e.X, e.Y));
				return Task.CompletedTask;
			});

		bus.Subscribe<AppMouseButtonEvent>(
			"MouseButton",
			(e, _) =>
			{
				world.PointerButton(
					e.Button, e.Action, e.Modifiers);
				return Task.CompletedTask;
			});

		bus.Subscribe<AppKeyboardEvent>(
			"Keyboard",
			(e, _) =>
			{
				world.KeyPress(
					e.Action, e.Modifiers, e.Key);
				return Task.CompletedTask;
			});

		bus.Subscribe<AppUpdateTick>(
			"Update",
			(e, _) =>
			{
				if (Monitor.TryEnter(_updateLock))
				{
					try { world.Update(e.ElapsedTime); }
					finally { Monitor.Exit(_updateLock); }
				}
				return Task.CompletedTask;
			});

		bus.Subscribe<AppRenderTick>(
			"Render",
			(e, _) =>
			{
				if (Monitor.TryEnter(_renderLock))
				{
					try
					{
						_rc.Fill(new RadialColor(0, 2, 5));
						world.Draw(_rc);
						_rc.Present();
					}
					finally { Monitor.Exit(_renderLock); }
				}
				return Task.CompletedTask;
			});

		bus.Publish(new AppFbSetBorder(RadialColor.Green));

		_logger.LogInformation("DemoAppRoot initialized");
	}
}