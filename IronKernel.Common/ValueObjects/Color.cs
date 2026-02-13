namespace IronKernel.Common.ValueObjects;

public sealed record Color : IEquatable<Color>
{
	#region Fields

	public readonly byte Red;
	public readonly byte Green;
	public readonly byte Blue;

	#endregion

	#region Constructors

	public Color(byte r, byte g, byte b)
	{
		Red = r;
		Green = g;
		Blue = b;
	}

	#endregion

	#region Methods

	/// <remarks>
	/// From https://lodev.org/cgtutor/color.html
	/// </remarks>
	public static Color FromHSL(float h, float s, float l)
	{
		float r, g, b; //this function works with floats between 0 and 1
		float temp1, temp2, tempR, tempG, tempB;
		h = h / 256.0f;
		s = s / 256.0f;
		l = l / 256.0f;


		//If saturation is 0, the color is a shade of gray
		if (s == 0) r = g = b = l;

		//If saturation > 0, more complex calculations are needed
		else
		{
			//Set the temporary values
			if (l < 0.5) temp2 = l * (1 + s);
			else temp2 = (l + s) - (l * s);
			temp1 = 2 * l - temp2;
			tempR = h + 1.0f / 3.0f;
			if (tempR > 1) tempR--;
			tempG = h;
			tempB = h - 1.0f / 3.0f;
			if (tempB < 0) tempB++;

			//Red
			if (tempR < 1.0 / 6.0) r = temp1 + (temp2 - temp1) * 6.0f * tempR;
			else if (tempR < 0.5) r = temp2;
			else if (tempR < 2.0 / 3.0) r = temp1 + (temp2 - temp1) * ((2.0f / 3.0f) - tempR) * 6.0f;
			else r = temp1;

			//Green
			if (tempG < 1.0 / 6.0) g = temp1 + (temp2 - temp1) * 6.0f * tempG;
			else if (tempG < 0.5) g = temp2;
			else if (tempG < 2.0 / 3.0) g = temp1 + (temp2 - temp1) * ((2.0f / 3.0f) - tempG) * 6.0f;
			else g = temp1;

			//Blue
			if (tempB < 1.0 / 6.0) b = temp1 + (temp2 - temp1) * 6.0f * tempB;
			else if (tempB < 0.5) b = temp2;
			else if (tempB < 2.0 / 3.0) b = temp1 + (temp2 - temp1) * ((2.0f / 3.0f) - tempB) * 6.0f;
			else b = temp1;
		}

		var colorRGB = new Color(
			(byte)(r * 255.0f),
			(byte)(g * 255.0f),
			(byte)(b * 255.0f)
		);
		return colorRGB;
	}

	public bool Equals(Color? other)
	{
		return Red == other?.Red && Green == other?.Green && Blue == other?.Blue;
	}

	// public override bool Equals(object? obj)
	// {
	// 	return (obj != null) && obj is Color && Equals((Color)obj);
	// }

	public override int GetHashCode()
	{
		return (Red << 16) | (Green << 8) | Blue;
	}

	// public static bool operator ==(Color a, Color b)
	// {
	// 	return a.Equals(b);
	// }

	// public static bool operator !=(Color a, Color b)
	// {
	// 	return !(a == b);
	// }

	#endregion
}