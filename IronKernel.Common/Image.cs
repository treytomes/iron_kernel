using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Common;

/// <summary>
/// An image is filled with palette-indexed pixel data.
/// </summary>
public class Image : IImage<Image, RadialColor>
{
	#region Constants

	private const int BPP = 1;

	#endregion

	#region Fields

	public readonly RadialColor?[] Data;

	#endregion

	#region Constructors

	public Image(int width, int height, RadialColor?[] data, int scale)
	{
		if (scale < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(scale));
		}
		Size = new Size(width * scale, height * scale);
		Data = new RadialColor?[Size.Width * Size.Height];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var color = data[y * width + x];

				for (var sy = 0; sy < scale; sy++)
				{
					for (var sx = 0; sx < scale; sx++)
					{
						var dy = y * scale + sy;
						var dx = x * scale + sx;
						Data[dy * Size.Width + dx] = color;
					}
				}
			}
		}
	}

	public Image(int width, int height, RadialColor?[] data)
	{
		Size = new Size(width, height);
		Data = (RadialColor?[])data.Clone();
	}

	public Image(int width, int height)
	{
		Size = new Size(width, height);
		Data = new RadialColor[width * height];
	}

	#endregion

	#region Properties

	public Size Size { get; }

	public RadialColor? this[int x, int y]
	{
		get
		{
			return GetPixel(x, y);
		}
		set
		{
			SetPixel(x, y, value);
		}
	}

	#endregion

	#region Methods

	public void WritePixels(ReadOnlySpan<RadialColor?> pixels)
	{
		if (pixels.Length != Data.Length)
			throw new ArgumentException("Pixel buffer size mismatch.");

		pixels.CopyTo(Data);
	}

	public void Recolor(RadialColor? oldColor, RadialColor? newColor)
	{
		for (var i = 0; i < Data.Length; i++)
		{
			if (Data[i] == oldColor)
			{
				Data[i] = newColor;
			}
		}
	}

	public void Clear(RadialColor color)
	{
		Array.Fill(Data, color);
	}

	public RadialColor? GetPixel(int x, int y)
	{
		var index = (y * Size.Width + x) * BPP;
		if (index < 0 || index > Data.Length) return null;
		return Data[index];
	}

	public void SetPixel(int x, int y, RadialColor? color)
	{
		var index = (y * Size.Width + x) * BPP;
		if (index < 0 || index > Data.Length) return;
		Data[index] = color;
	}

	/// <summary>
	/// Create a new image from a rectangle of this image.
	/// </summary>
	public Image Crop(int x, int y, int width, int height)
	{
		var data = new RadialColor?[width * height * BPP];

		for (var i = 0; i < height; i++)
		{
			for (var j = 0; j < width; j++)
			{
				var color = GetPixel(x + j, y + i);
				var index = (i * width + j) * BPP;
				data[index] = color;
			}
		}

		return new Image(width, height, data);
	}

	public Image Scale(int factor)
	{
		if (factor < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(factor));
		}
		return new Image(Size.Width, Size.Height, Data, factor);
	}

	#endregion
}
