using IronKernel.Common.ValueObjects;
using Miniscript;

namespace Userland;

public static class ValueExtensions
{
	public static RadialColor ToRadialColor(this ValMap @this)
	{
		var red = (byte)@this["red"].IntValue();
		var green = (byte)@this["green"].IntValue();
		var blue = (byte)@this["blue"].IntValue();
		return new RadialColor(red, green, blue);
	}

	public static ValMap ToMiniScriptValue(this RadialColor @this)
	{
		var color = new ValMap();
		color["red"] = new ValNumber(@this.R);
		color["green"] = new ValNumber(@this.G);
		color["blue"] = new ValNumber(@this.B);
		return color;
	}
}