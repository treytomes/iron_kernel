using Microsoft.Extensions.DependencyInjection;
using Userland.Morphic.Commands;
using Userland.Services;
using Userland.Scripting;

namespace Userland.Morphic;

public sealed class WorldScriptContext
{
	#region Fields

	private readonly WorldMorph _world;
	private readonly IServiceProvider _services;
	private readonly MorphHandleRegistry _handles = new();

	#endregion

	#region Constructions

	public WorldScriptContext(WorldMorph world, IServiceProvider services)
	{
		_world = world;

		// TODO: I hate instantiating like this, but there's a circular reference between WorldMorph and WindowService now.
		WindowService = new WindowService(world, services);

		_services = services;
		FileSystem = services.GetRequiredService<IFileSystem>();
	}

	#endregion

	public string? PendingRunSource { get; set; }
	public IWindowService WindowService { get; }
	public IFileSystem FileSystem { get; }

	#region World access

	public WorldMorph World => _world;
	public WorldCommandManager Commands => _world.Commands;
	public ScriptOutputHub Output => _world.ScriptOutput;

	public void StopWorldScript() => _world.Interpreter.Stop();
	public void ResetWorldScript() => _world.Interpreter.Reset();
	public bool IsRunning => _world.Interpreter.Running();

	#endregion

	public MorphHandleRegistry Handles => _handles;
}