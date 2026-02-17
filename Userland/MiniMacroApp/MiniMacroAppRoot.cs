using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic;
using Userland.Morphic.Commands;
using Userland.Roguey;
using Userland.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace Userland.MiniMacro;

public sealed class MiniMacroRoot
{
	private readonly IServiceProvider _services;
	private readonly ILogger<MiniMacroRoot> _logger;
	private readonly IRenderingContext _rc;
	private readonly IApplicationContext _context;
	private readonly IFileSystem _fileSystem;
	private readonly IAssetService _assets;

	private readonly object _updateLock = new();
	private readonly object _renderLock = new();

	public MiniMacroRoot(
		IServiceProvider services,
		ILogger<MiniMacroRoot> logger,
		IRenderingContext rc,
		IApplicationContext context,
		IAssetService assets,
		IFileSystem fileSystem)
	{
		_services = services;
		_logger = logger;
		_rc = rc;
		_context = context;
		_fileSystem = fileSystem;
		_assets = assets;
	}

	public async Task RunAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation($"{nameof(MiniMacroRoot)} starting");

		RogueyIntrinsics.Create();
		await _rc.InitializeAsync();

		var world = _services.GetRequiredService<WorldMorph>();

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
				var launcher = new LauncherMorph(_services, new Point(16, 16));
				launcher.AddApp<DummyReplMorph>("Dummy REPL");
				launcher.AddApp<MiniScriptReplMorph>("MiniScript REPL");
				launcher.AddApp<TextEditorWindowMorph>("Text Editor");

				world.AddMorph(launcher);
			}));

		world.AddMorph(toolbar);

		var value = 2.0f;
		world.AddMorph(new SliderWithEditorMorph(2, x => value = x)
		{
			Position = new Point(128, 128),
			Min = 0.0f,
			Max = 5.0f,
			Step = 1.0f,
		});

		// var text = "Name";
		// world.AddMorph(new TextEditMorph(new Point(0, 32), text, x => text = x));

		// world.AddMorph(
		// 	new BoxMorph(new Point(50, 50), new Size(40, 30))
		// 	{
		// 		FillColor = RadialColor.Red,
		// 		BorderColor = RadialColor.Blue.Lerp(
		// 			RadialColor.White, 0.5f)
		// 	});

		// world.AddMorph(
		// 	new LabelMorph
		// 	{
		// 		Text = "Hello world!",
		// 		ForegroundColor = RadialColor.Red,
		// 		BackgroundColor = RadialColor.Blue
		// 	});

		// ------------------------------------------------------------------
		// Tile map demo
		// ------------------------------------------------------------------
		var tileSetInfo =
			new TileSetInfo("image.screen_font", new Size(16, 24));

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

		_logger.LogInformation($"{nameof(MiniMacroRoot)} initialized.");
	}
}