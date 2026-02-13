using IronKernel.Common.ValueObjects;
using OpenTK.Mathematics;

namespace IronKernel;

internal static class Color4Extensions
{
	/// <summary>  
	/// Converts a standard Color to a RadialColor.  
	/// </summary>  
	public static RadialColor ToRadialColor(this Color4 @this)
	{
		var r = (byte)Math.Min(5, Math.Round(@this.R * 5));
		var g = (byte)Math.Min(5, Math.Round(@this.G * 5));
		var b = (byte)Math.Min(5, Math.Round(@this.B * 5));
		return new RadialColor(r, g, b);
	}
}