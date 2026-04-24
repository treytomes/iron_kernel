using System.Collections;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.Framebuffer;

/// <summary>
/// Generates a palette with <paramref name="colorDepth"/> discrete intensity levels per RGB channel.
/// The default depth of 6 produces the 216-color RadialColor-equivalent palette.
/// </summary>
public class RadialPalette : IReadOnlyList<Color>, IDisposable
{
	#region Fields

	private readonly Texture _texture;
	private readonly List<Color> _colors;
	private bool disposedValue;

	#endregion

	#region Constructors

	public RadialPalette(int colorDepth = 6)
	{
		ColorDepth = colorDepth;
		PaletteSize = colorDepth * colorDepth * colorDepth;

		// Linear RGB grid: each channel evenly spaced 0..1 across colorDepth steps.
		_colors = new List<Color>(PaletteSize);
		for (var r = 0; r < colorDepth; r++)
		{
			for (var g = 0; g < colorDepth; g++)
			{
				for (var b = 0; b < colorDepth; b++)
				{
					_colors.Add(new Color(
						r / (colorDepth - 1f),
						g / (colorDepth - 1f),
						b / (colorDepth - 1f)));
				}
			}
		}

		var data = new byte[PaletteSize * 3];
		for (int i = 0; i < PaletteSize; i++)
		{
			var color = _colors[i];
			data[i * 3]     = (byte)(color.R * 255f);
			data[i * 3 + 1] = (byte)(color.G * 255f);
			data[i * 3 + 2] = (byte)(color.B * 255f);
		}

		_texture = new Texture(PaletteSize, 1, false);
		_texture.Data = data;
	}

	#endregion

	#region Properties

	public int ColorDepth { get; }
	public int PaletteSize { get; }
	public int Id => _texture.Id;
	public Color this[int index] => _colors[index];
	public int Count => _colors.Count;

	#endregion

	#region Methods

	public IEnumerator<Color> GetEnumerator() => _colors.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
				_texture?.Dispose();
			disposedValue = true;
		}
	}

	~RadialPalette()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}
