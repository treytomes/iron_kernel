using System.Drawing;
using IronKernel.Common.ValueObjects;
using Miniscript;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland;

public static class ValueExtensions
{
	public static bool IsColor(this ValMap? @this)
	{
		return @this != null
			&& @this["r"] != null
			&& @this["g"] != null
			&& @this["b"] != null;
	}

	public static Color ToFloatColor(this ValMap @this)
	{
		var red = (float)@this["r"].FloatValue();
		var green = (float)@this["g"].FloatValue();
		var blue = (float)@this["b"].FloatValue();
		return new Color(red, green, blue);
	}

	public static ValMap ToMiniScriptValue(this Color @this)
	{
		var color = new ValMap();
		color["r"] = new ValNumber(@this.R);
		color["g"] = new ValNumber(@this.G);
		color["b"] = new ValNumber(@this.B);
		return color;
	}

	// Point: accepts [x, y] list or {x, y} map.

	public static bool IsPoint(this Value? v) => v switch
	{
		ValList l => l.values.Count == 2,
		ValMap m  => m["x"] != null && m["y"] != null,
		_         => false
	};

	// Keep the old ValMap overload so callers that already cast don't break.
	public static bool IsPoint(this ValMap? @this) => IsPoint((Value?)@this);

	public static Point ToPoint(this Value v) => v switch
	{
		ValList l => new Point(l.values[0].IntValue(), l.values[1].IntValue()),
		ValMap m  => new Point(m["x"].IntValue(), m["y"].IntValue()),
		_         => Point.Empty
	};

	public static Point ToPoint(this ValMap @this) => ToPoint((Value)@this);

	public static ValList ToMiniScriptValue(this Point @this)
	{
		var list = new ValList();
		list.values.Add(new ValNumber(@this.X));
		list.values.Add(new ValNumber(@this.Y));
		return list;
	}

	// Size: accepts [w, h] list or {w, h} map.

	public static bool IsSize(this Value? v) => v switch
	{
		ValList l => l.values.Count == 2,
		ValMap m  => m["w"] != null && m["h"] != null,
		_         => false
	};

	public static bool IsSize(this ValMap? @this) => IsSize((Value?)@this);

	public static Size ToSize(this Value v) => v switch
	{
		ValList l => new Size(l.values[0].IntValue(), l.values[1].IntValue()),
		ValMap m  => new Size(m["w"].IntValue(), m["h"].IntValue()),
		_         => Size.Empty
	};

	public static Size ToSize(this ValMap @this) => ToSize((Value)@this);

	public static ValList ToMiniScriptValue(this Size @this)
	{
		var list = new ValList();
		list.values.Add(new ValNumber(@this.Width));
		list.values.Add(new ValNumber(@this.Height));
		return list;
	}
}
