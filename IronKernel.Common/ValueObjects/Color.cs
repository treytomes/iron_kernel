using System.Runtime.InteropServices;

namespace IronKernel.Common.ValueObjects;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Color : IEquatable<Color>
{
	#region Fields

	public readonly float R;
	public readonly float G;
	public readonly float B;

	#endregion

	#region Constructors

	public Color(float r, float g, float b)
	{
		R = Math.Clamp(r, 0f, 1f);
		G = Math.Clamp(g, 0f, 1f);
		B = Math.Clamp(b, 0f, 1f);
	}

	#endregion

	#region Named constants

	public static Color Black       => new(0f,   0f,   0f);
	public static Color DarkerGray  => new(0.2f, 0.2f, 0.2f);
	public static Color DarkSlateGray => new(0.2f, 0.4f, 0.4f);
	public static Color DarkGray    => new(0.4f, 0.4f, 0.4f);
	public static Color Gray        => new(0.6f, 0.6f, 0.6f);
	public static Color LightGray   => new(0.8f, 0.8f, 0.8f);
	public static Color White       => new(1f,   1f,   1f);

	public static Color Red         => new(1f,   0f,   0f);
	public static Color Orange      => new(1f,   0.6f, 0f);
	public static Color Yellow      => new(1f,   1f,   0f);
	public static Color Green       => new(0f,   1f,   0f);
	public static Color Cyan        => new(0f,   1f,   1f);
	public static Color Blue        => new(0f,   0f,   1f);

	#endregion

	#region Methods

	public Color Lerp(Color other, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return new Color(
			R + (other.R - R) * t,
			G + (other.G - G) * t,
			B + (other.B - B) * t);
	}

	public Color Add(Color other) =>
		new(R + other.R, G + other.G, B + other.B);

	public Color WithR(float r) => new(r, G, B);
	public Color WithG(float g) => new(R, g, B);
	public Color WithB(float b) => new(R, G, b);

	/// <summary>
	/// Creates a Color from HSL values in the 0–1 range.
	/// </summary>
	public static Color FromHSL(float h, float s, float l)
	{
		float r, g, b;

		if (s == 0f)
		{
			r = g = b = l;
		}
		else
		{
			var temp2 = l < 0.5f ? l * (1f + s) : (l + s) - (l * s);
			var temp1 = 2f * l - temp2;

			r = HueToChannel(temp1, temp2, h + 1f / 3f);
			g = HueToChannel(temp1, temp2, h);
			b = HueToChannel(temp1, temp2, h - 1f / 3f);
		}

		return new Color(r, g, b);
	}

	private static float HueToChannel(float t1, float t2, float hue)
	{
		if (hue < 0f) hue += 1f;
		if (hue > 1f) hue -= 1f;

		if (hue < 1f / 6f) return t1 + (t2 - t1) * 6f * hue;
		if (hue < 0.5f)    return t2;
		if (hue < 2f / 3f) return t1 + (t2 - t1) * (2f / 3f - hue) * 6f;
		return t1;
	}

	public bool Equals(Color other) =>
		R == other.R && G == other.G && B == other.B;

	public override int GetHashCode() =>
		HashCode.Combine(R, G, B);

	public static Color operator +(Color a, Color b) => a.Add(b);

	public override string ToString() => $"(R:{R:F3} G:{G:F3} B:{B:F3})";

	#endregion
}
