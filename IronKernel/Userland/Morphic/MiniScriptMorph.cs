using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using Miniscript;
using System.Drawing;

namespace IronKernel.Userland.Morphic;

public sealed class MiniScriptMorph : Morph
{
	private readonly Dictionary<string, Value> _slots = new();

	public MiniScriptMorph()
		: base()
	{
		IsSelectable = true;
		ShouldClipToBounds = true;

		SetSlot("fillColor", new RadialColor(5, 0, 0).ToMiniScriptValue());
		SetSlot("borderColor", new RadialColor(0, 5, 0).ToMiniScriptValue());
	}

	public bool HasSlot(string key) => _slots.ContainsKey(key);
	public TValue? GetSlot<TValue>(string key) where TValue : Value => (_slots.TryGetValue(key, out var v) ? v : null) as TValue;
	public void SetSlot(string key, Value value) => _slots[key] = value;
	public bool DeleteSlot(string key) => _slots.Remove(key);

	protected override void DrawSelf(IRenderingContext rc)
	{
		var backgroundColor = GetSlot<ValMap>("fillColor");
		if (backgroundColor != null)
		{
			var color = backgroundColor.ToRadialColor();
			rc.RenderFilledRect(new Rectangle(Point.Empty, Size), color);
		}

		var foregroundColor = GetSlot<ValMap>("borderColor");
		if (foregroundColor != null)
		{
			var color = foregroundColor.ToRadialColor();
			rc.RenderRect(new Rectangle(Point.Empty, Size), color);
		}
	}
}
