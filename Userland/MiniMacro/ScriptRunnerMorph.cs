using Miniscript;
using Userland.Morphic;
using Userland.Scripting;

namespace Userland.MiniMacro;

/// <summary>
/// Invisible morph that runs a MiniScript program and removes itself when done.
/// </summary>
public sealed class ScriptRunnerMorph : Morph
{
	private readonly Interpreter _interpreter;

	public ScriptRunnerMorph(WorldScriptContext ctx, string source)
	{
		IsSelectable = false;

		_interpreter = new Interpreter();
		ctx.Output.Attach(_interpreter);
		_interpreter.hostData = ctx;
		_interpreter.Reset(source);
		_interpreter.Compile();

		if (_interpreter.NeedMoreInput())
		{
			ctx.Output.Errors.AppendLine("Script compile error.");
			MarkForDeletion();
		}
	}

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

		if (_interpreter.Running())
		{
			_interpreter.RunUntilDone(0.03f);
			GetWorld()?.ApplyScriptEdits();
		}
		else
		{
			MarkForDeletion();
		}
	}
}
