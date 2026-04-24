using System.Drawing;
using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

namespace IronKernel.Common;

public class Image : IImage<Image>
{
	#region Constants

	private const int BPP = 1;

	#endregion

	#region Fields

	public readonly Color?[] Data;

	#endregion

	#region Constructors

	public Image(int width, int height, Color?[] data, int scale)
	{
		if (scale < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(scale));
		}
		Size = new Size(width * scale, height * scale);
		Data = new Color?[Size.Width * Size.Height];

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

	public Image(int width, int height, Color?[] data)
	{
		Size = new Size(width, height);
		Data = (Color?[])data.Clone();
	}

	public Image(int width, int height)
	{
		Size = new Size(width, height);
		Data = new Color?[width * height];
	}

	#endregion

	#region Properties

	public Size Size { get; }

	public Color? this[int x, int y]
	{
		get => GetPixel(x, y);
		set => SetPixel(x, y, value);
	}

	#endregion

	#region Methods

	public void WritePixels(ReadOnlySpan<Color?> pixels)
	{
		if (pixels.Length != Data.Length)
			throw new ArgumentException("Pixel buffer size mismatch.");

		pixels.CopyTo(Data);
	}

	public void Recolor(Color? oldColor, Color? newColor)
	{
		for (var i = 0; i < Data.Length; i++)
		{
			if (Data[i] == oldColor)
			{
				Data[i] = newColor;
			}
		}
	}

	public void Clear(Color? color)
	{
		Array.Fill(Data, color);
	}

	public Color? GetPixel(int x, int y)
	{
		var index = (y * Size.Width + x) * BPP;
		if (index < 0 || index > Data.Length) return null;
		return Data[index];
	}

	public void SetPixel(int x, int y, Color? color)
	{
		var index = (y * Size.Width + x) * BPP;
		if (index < 0 || index > Data.Length) return;
		Data[index] = color;
	}

	public Image Crop(int x, int y, int width, int height)
	{
		var data = new Color?[width * height * BPP];

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
