using IronKernel.Common.ValueObjects;
using Miniscript;

namespace Userland.Morphic;

public sealed class TileInfo
{
	public int TileIndex;
	public RadialColor ForegroundColor = RadialColor.White;
	public RadialColor BackgroundColor = RadialColor.Black;
	public bool BlocksMovement;
	public bool BlocksVision;
	public string Tag = "floor";

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
		var map = new ValMap
		{
			["tileIndex"] = new ValNumber(TileIndex),
			["foregroundColor"] = ForegroundColor.ToMiniScriptValue(),
			["backgroundColor"] = BackgroundColor.ToMiniScriptValue(),
			["blocksMovement"] = ValNumber.Truth(BlocksMovement),
			["blocksVision"] = ValNumber.Truth(BlocksVision),
			["tag"] = new ValString(Tag)
		};
		return map;
	}

	public void ApplyScriptState()
	{
		if (_scriptObject == null) return;

		TileIndex = _scriptObject["tileIndex"].IntValue();

		if (_scriptObject["foregroundColor"] is ValMap fg && fg.IsColor())
			ForegroundColor = fg.ToColor();

		if (_scriptObject["backgroundColor"] is ValMap bg && bg.IsColor())
			BackgroundColor = bg.ToColor();

		BlocksMovement = _scriptObject["blocksMovement"].BoolValue();
		BlocksVision = _scriptObject["blocksVision"].BoolValue();
		Tag = _scriptObject["tag"]?.ToString() ?? string.Empty;
	}
}