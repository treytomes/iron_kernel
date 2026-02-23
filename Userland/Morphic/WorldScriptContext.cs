using Microsoft.Extensions.DependencyInjection;
using Userland.Morphic.Commands;
using Userland.Services;
using Userland.Scripting;
using Userland.Morphic.Events;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Userland.Morphic;

public sealed class WorldScriptContext
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

		// TODO: I hate instantiating like this, but there's a circular reference between WorldMorph and WindowService now.
		WindowService = new WindowService(world, services);

		_services = services;
		FileSystem = services.GetRequiredService<IFileSystem>();
	}

	#endregion

	#region Properties

	public string? PendingRunSource { get; set; }
	public IWindowService WindowService { get; }
	public IFileSystem FileSystem { get; }
	public MorphHandleRegistry Handles => _handles;
	public WorldMorph World => _world;
	public WorldCommandManager Commands => _world.Commands;
	public ScriptOutputHub Output => _world.ScriptOutput;
	public bool IsRunning => _world.Interpreter.Running();
	public KeyboardState Keyboard { get; } = new();

	#endregion

	#region Methods

	public void StopWorldScript() => _world.Interpreter.Stop();
	public void ResetWorldScript() => _world.Interpreter.Reset();

	public void OnKey(KeyEvent e)
	{
		try
		{
			// Maintain key-down state for polling APIs
			switch (e.Action)
			{
				case InputAction.Press:
					Keyboard.KeysDown.Add(e.Key);
					break;

				case InputAction.Release:
					Keyboard.KeysDown.Remove(e.Key);
					break;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process key event ({Action}, {Handled}, {Key}, {Modifiers}).", e.Action, e.Handled, e.Key, e.Modifiers);
		}
	}

	#endregion
}