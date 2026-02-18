using System.Drawing;
using IronKernel.Common.ValueObjects;
using Miniscript;

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

	public static RadialColor ToColor(this ValMap @this)
	{
		var red = (byte)@this["r"].IntValue();
		var green = (byte)@this["g"].IntValue();
		var blue = (byte)@this["b"].IntValue();
		return new RadialColor(red, green, blue);
	}

	public static ValMap ToMiniScriptValue(this RadialColor @this)
	{
		var color = new ValMap();
		color["r"] = new ValNumber(@this.R);
		color["g"] = new ValNumber(@this.G);
		color["b"] = new ValNumber(@this.B);
		return color;
	}

	public static bool IsPoint(this ValMap? @this)
	{
		return @this != null
			&& @this["x"] != null
			&& @this["y"] != null;
	}

	public static Point ToPoint(this ValMap @this)
	{
		var x = @this["x"].IntValue();
		var y = @this["y"].IntValue();
		return new Point(x, y);
	}

	public static ValMap ToMiniScriptValue(this Point @this)
	{
		var point = new ValMap();
		point["x"] = new ValNumber(@this.X);
		point["y"] = new ValNumber(@this.Y);
		return point;
	}

	public static bool IsSize(this ValMap? @this)
	{
		return @this != null
			&& @this["w"] != null
			&& @this["h"] != null;
	}

	public static Size ToSize(this ValMap @this)
	{
		var w = @this["w"].IntValue();
		var h = @this["h"].IntValue();
		return new Size(w, h);
	}

	public static ValMap ToMiniScriptValue(this Size @this)
	{
		var size = new ValMap();
		size["w"] = new ValNumber(@this.Width);
		size["h"] = new ValNumber(@this.Height);
		return size;
	}
}