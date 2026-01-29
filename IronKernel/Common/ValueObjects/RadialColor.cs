using OpenTK.Mathematics;

namespace IronKernel.Common.ValueObjects;

public sealed record RadialColor : IEquatable<RadialColor>
{
	#region Fields

	public readonly byte R;
	public readonly byte G;
	public readonly byte B;

	#endregion

	#region Constructors

	public RadialColor(byte r, byte g, byte b)
	{
		if (r > 5)
		{
			throw new ArgumentException($"Invalid color value: {r}", nameof(r));
		}
		if (g > 5)
		{
			throw new ArgumentException($"Invalid color value: {g}", nameof(g));
		}
		if (b > 5)
		{
			throw new ArgumentException($"Invalid color value: {b}", nameof(b));
		}
		R = r;
		G = g;
		B = b;
	}

	#endregion

	#region Properties

	public static RadialColor Black => new RadialColor(0, 0, 0);
	public static RadialColor DarkerGray => new RadialColor(1, 1, 1);
	public static RadialColor DarkSlateGray => new RadialColor(1, 2, 2);
	public static RadialColor DarkGray => new RadialColor(2, 2, 2);
	public static RadialColor Gray => new RadialColor(3, 3, 3);
	public static RadialColor LightGray => new RadialColor(4, 4, 4);
	public static RadialColor White => new RadialColor(5, 5, 5);

	public static RadialColor Red => new RadialColor(5, 0, 0);
	public static RadialColor Orange => new RadialColor(5, 3, 0);
	public static RadialColor Yellow => new RadialColor(5, 5, 0);
	public static RadialColor Green => new RadialColor(0, 5, 0);
	public static RadialColor Cyan => new RadialColor(0, 5, 5);
	public static RadialColor Blue => new RadialColor(0, 0, 5);

	/// <summary>
	/// Calculate the palette index for this color.
	/// </summary>
	public byte Index
	{
		get
		{
			return (byte)((R * 6 * 6) + (G * 6) + B);
		}
	}

	#endregion

	#region Methods

	public bool Equals(RadialColor? other)
	{
		return R == other?.R && G == other?.G && B == other?.B;
	}

	public override int GetHashCode()
	{
		return Index;
	}

	public RadialColor Add(RadialColor other)
	{
		return new RadialColor(
			(byte)Math.Min(5, R + other.R),
			(byte)Math.Min(5, G + other.G),
			(byte)Math.Min(5, B + other.B)
		);
	}

	/// <summary>  
	/// Converts a standard Color to a RadialColor.  
	/// </summary>  
	public static RadialColor FromColor(Color4 color)
	{
		var r = (byte)Math.Min(5, Math.Round(color.R * 5));
		var g = (byte)Math.Min(5, Math.Round(color.G * 5));
		var b = (byte)Math.Min(5, Math.Round(color.B * 5));
		return new RadialColor(r, g, b);
	}

	/// <summary>  
	/// Converts a standard Color to a RadialColor.  
	/// </summary>  
	public static RadialColor FromColor(Color color)
	{
		var r = (byte)Math.Min(5, Math.Round(color.Red / 255f * 5));
		var g = (byte)Math.Min(5, Math.Round(color.Green / 255f * 5));
		var b = (byte)Math.Min(5, Math.Round(color.Blue / 255f * 5));
		return new RadialColor(r, g, b);
	}

	public static RadialColor FromHSL(float h, float s, float l)
	{
		return FromColor(Color.FromHSL(h, s, l));
	}

	/// <summary>  
	/// Converts this RadialColor to a standard Color.  
	/// </summary>  
	public Color4 ToColor()
	{
		return new Color4(R / 5.0f, G / 5.0f, B / 5.0f, 1.0f);
	}
	/// <summary>  
	/// Linearly interpolates between two RadialColors.  
	/// </summary>  
	/// <param name="other">The target color.</param>  
	/// <param name="t">Interpolation factor (0.0 to 1.0).</param>  
	/// <returns>The interpolated color.</returns>  
	public RadialColor Lerp(RadialColor other, float t)
	{
		t = Math.Clamp(t, 0.0f, 1.0f);
		float r = MathHelper.Lerp(R, other.R, t);
		float g = MathHelper.Lerp(G, other.G, t);
		float b = MathHelper.Lerp(B, other.B, t);
		return new RadialColor(
			(byte)Math.Round(r),
			(byte)Math.Round(g),
			(byte)Math.Round(b)
		);
	}

	public RadialColor WithR(byte r) => new(r, G, B);
	public RadialColor WithG(byte g) => new(R, g, B);
	public RadialColor WithB(byte b) => new(R, G, b);

	public override string ToString() => $"(R:{R} G:{G} B:{B})";

	public static RadialColor operator +(RadialColor a, RadialColor b)
	{
		return a.Add(b);
	}

	private static byte Clamp(byte v) => Math.Clamp(v, (byte)0, (byte)5);

	#endregion
}