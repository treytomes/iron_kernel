using System.Collections.Concurrent;
using System.Drawing;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;
using Userland.Gfx;
using Userland.Morphic;
using Userland.Morphic.Commands;
using Userland.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Userland.MiniMacro;

public sealed class MiniMacroRoot
{
	#region Constants

	private static readonly Color BORDER_COLOR = new Color(2f / 5f, 1f / 5f, 0f);

	#endregion

	#region Fields

	private readonly IServiceProvider _services;
	private readonly ILogger<MiniMacroRoot> _logger;
	private readonly IRenderingContext _rc;
	private readonly IApplicationContext _context;
	private readonly IFileSystem _fileSystem;
	private readonly IAssetService _assets;

	private readonly object _updateLock = new();
	private readonly object _renderLock = new();
	private readonly ConcurrentQueue<AppKeyboardEvent> _keyQueue = new();

	#endregion

	#region Constructors

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

	#endregion

	#region Methods

	public async Task RunAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation($"{nameof(MiniMacroRoot)} starting");

		// RogueyIntrinsics.Create();
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
				launcher.AddApp<MiniScriptReplMorph>("MiniScript REPL");
				launcher.AddApp<TextEditorWindowMorph>("Text Editor");
				launcher.AddApp<FireDemoMorph>("Fire Demo");
				launcher.AddApp<ColorGridMorph>("Color Grid");
				launcher.AddAction("Toast Test", () =>
				{
					world.ShowToast("This is an info toast", ToastSeverity.Info);
					world.ShowToast("This is a warning toast", ToastSeverity.Warning);
					world.ShowToast("This is an error toast", ToastSeverity.Error);
				});
				launcher.AddAction("Fault Test", () =>
				{
					var morph = new FaultTestMorph { Position = new Point(200, 100) };
					world.AddMorph(morph);
				});

				world.AddMorph(launcher);
			}));

		world.AddMorph(toolbar);

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
				lock (_updateLock)
					world.PointerWheel(new Point(e.OffsetX, e.OffsetY));
				return Task.CompletedTask;
			});

		bus.Subscribe<AppMouseMoveEvent>(
			"MouseMove",
			(e, _) =>
			{
				lock (_updateLock)
					world.PointerMove(new Point(e.X, e.Y));
				return Task.CompletedTask;
			});

		bus.Subscribe<AppMouseButtonEvent>(
			"MouseButton",
			(e, _) =>
			{
				lock (_updateLock)
					world.PointerButton(e.Button, e.Action, e.Modifiers);
				return Task.CompletedTask;
			});

		// Enqueue key events rather than dispatching immediately. Each bus
		// handler runs as a separate Task.Run, so without queueing the thread
		// pool can reorder a Release before the preceding Press, permanently
		// leaving KeyboardState in a stuck "down" state.
		bus.Subscribe<AppKeyboardEvent>(
			"Keyboard",
			(e, _) =>
			{
				_keyQueue.Enqueue(e);
				return Task.CompletedTask;
			});

		bus.Subscribe<AppUpdateTick>(
			"Update",
			(e, _) =>
			{
				lock (_updateLock)
				{
					while (_keyQueue.TryDequeue(out var key))
						world.KeyPress(key.Action, key.Modifiers, key.Key);
					world.Update(e.ElapsedTime);
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
						world.Draw(_rc);
						_rc.Present();
					}
					finally { Monitor.Exit(_renderLock); }
				}
				return Task.CompletedTask;
			});

		bus.Publish(new AppFbSetBorder(BORDER_COLOR));

		_logger.LogInformation($"{nameof(MiniMacroRoot)} initialized.");
	}

	#endregion
}