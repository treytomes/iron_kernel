using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniscript;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;
using Userland.Services;
using Userland.Morphic;

namespace Userland.Scripting;

public sealed class WorldScriptContext : IScriptHost
{
	#region Fields

	private readonly ILogger<WorldScriptContext> _logger;
	private readonly WorldMorph _world;
	private readonly IServiceProvider _services;
	private readonly MorphHandleRegistry _handles = new();

	#endregion

	#region Constructors

	public WorldScriptContext(ILogger<WorldScriptContext> logger, WorldMorph world, IServiceProvider services)
	{
		_logger = logger;
		_world = world;
		_services = services;
		WindowService = new WindowService(world, services);
		FileSystem = services.GetRequiredService<IFileSystem>();
		Sound = services.GetRequiredService<ISoundService>();
	}

	#endregion

	#region Properties

	public Action<string>? RunSourceRequested { get; set; }
	public Action? ClearOutputRequested { get; set; }
	public IWindowService WindowService { get; }
	public IFileSystem FileSystem { get; }
	public ISoundService Sound { get; }
	public MorphHandleRegistry Handles => _handles;
	public WorldMorph World => _world;
	public WorldCommandManager Commands => _world.Commands;
	public ScriptOutputHub Output => _world.ScriptOutput;
	public KeyboardState Keyboard { get; } = new();
	public double TotalTime { get; set; }
	public bool IsMouseButtonDown => _world.IsMouseButtonDown;
	public System.Drawing.Point MousePosition => _world.Hand.Position;

	#endregion

	#region Methods

	public Func<string, Task<string?>>? ReadLineOverride { get; set; }

	public Task<string?> ReadLineAsync(string prompt) =>
		ReadLineOverride != null
			? ReadLineOverride(prompt)
			: WindowService.PromptAsync(prompt);

	public void OnKey(KeyEvent e)
	{
		try
		{
			// Maintain key-down state for polling APIs
			switch (e.Action)
			{
				case InputAction.Press:
				case InputAction.Repeat:
					Keyboard.SetKeyState(e.Key, true);
					break;

				case InputAction.Release:
					Keyboard.SetKeyState(e.Key, false);
					break;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process key event ({Action}, {Handled}, {Key}, {Modifiers}).", e.Action, e.Handled, e.Key, e.Modifiers);
		}
	}

	public void EnsureEnv(TAC.Context ctx)
	{
		if (ctx.interpreter.GetGlobalValue("env") is not ValMap env)
		{
			env = new ValMap();
		}
		if (env["curdir"] is not Value)
		{
			env["curdir"] = new ValString("file://");
		}

		ctx.interpreter.SetGlobalValue("env", env);
	}

	#endregion
}