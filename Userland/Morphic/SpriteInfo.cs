using IronKernel.Common.ValueObjects;
using Miniscript;

namespace Userland.Morphic;

public sealed class SpriteInfo
{
	public int X;
	public int Y;
	public int TileIndex;
	public RadialColor ForegroundColor = RadialColor.White;
	public RadialColor BackgroundColor = RadialColor.Black;

	private ValMap? _scriptObject;

	public ValMap ScriptObject
	{
		get
		{
			if (_scriptObject == null)
				_scriptObject = CreateScriptObject();
			return _scriptObject;
		}
	}

	private ValMap CreateScriptObject()
	{
		return new ValMap
		{
			["x"] = new ValNumber(X),
			["y"] = new ValNumber(Y),
			["tileIndex"] = new ValNumber(TileIndex),
			["foregroundColor"] = ForegroundColor.ToMiniScriptValue(),
			["backgroundColor"] = BackgroundColor.ToMiniScriptValue()
		};
	}

	public void ApplyScriptState()
	{
		if (_scriptObject == null) return;

		X = _scriptObject["x"].IntValue();
		Y = _scriptObject["y"].IntValue();
		TileIndex = _scriptObject["tileIndex"].IntValue();

		if (_scriptObject["foregroundColor"] is ValMap fg && fg.IsColor())
			ForegroundColor = fg.ToColor();

		if (_scriptObject["backgroundColor"] is ValMap bg && bg.IsColor())
			BackgroundColor = bg.ToColor();
	}
}