using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using Miniscript;

namespace IronKernel.Userland.Morphic;

public sealed class MiniScriptMorph : Morph
{
	private readonly Dictionary<string, Value> _slots = new();

	public MiniScriptMorph()
		: base()
	{
		IsSelectable = true;
		ShouldClipToBounds = true;

		var backgroundColor = new ValMap();
		backgroundColor["red"] = new ValNumber(5);
		backgroundColor["green"] = new ValNumber(0);
		backgroundColor["blue"] = new ValNumber(0);
		SetSlot("backgroundColor", backgroundColor);

		var foregroundColor = new ValMap();
		foregroundColor["red"] = new ValNumber(0);
		foregroundColor["green"] = new ValNumber(5);
		foregroundColor["blue"] = new ValNumber(0);
		SetSlot("foregroundColor", foregroundColor);
	}

	public bool HasSlot(string key) => _slots.ContainsKey(key);
	public TValue? GetSlot<TValue>(string key) where TValue : Value => (_slots.TryGetValue(key, out var v) ? v : null) as TValue;
	public void SetSlot(string key, Value value) => _slots[key] = value;
	public bool DeleteSlot(string key) => _slots.Remove(key);

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);
		var backgroundColor = GetSlot<ValMap>("backgroundColor");
		if (backgroundColor != null)
		{
			var red = (byte)backgroundColor["red"].IntValue();
			var green = (byte)backgroundColor["green"].IntValue();
			var blue = (byte)backgroundColor["blue"].IntValue();
			var color = new RadialColor(red, green, blue);
			rc.RenderFilledRect(new Rectangle(Point.Empty, Size), color);
		}

		var foregroundColor = GetSlot<ValMap>("foregroundColor");
		if (foregroundColor != null)
		{
			var red = (byte)foregroundColor["red"].IntValue();
			var green = (byte)foregroundColor["green"].IntValue();
			var blue = (byte)foregroundColor["blue"].IntValue();
			var color = new RadialColor(red, green, blue);
			rc.RenderRect(new Rectangle(Point.Empty, Size), color);
		}
	}
}