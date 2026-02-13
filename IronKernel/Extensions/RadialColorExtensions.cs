using IronKernel.Common.ValueObjects;
using OpenTK.Mathematics;

namespace IronKernel;

internal static class RadialColorExtensions
{
	/// <summary>  
	/// Converts this RadialColor to a standard Color.  
	/// </summary>  
	public static Color4 ToColor(this RadialColor @this)
	{
		return new Color4(@this.R / 5.0f, @this.G / 5.0f, @this.B / 5.0f, 1.0f);
	}
}