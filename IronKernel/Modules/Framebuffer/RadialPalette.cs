using System.Collections;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.Framebuffer;

/// <summary>
/// Generates a palette with <paramref name="colorDepth"/> discrete intensity levels per RGB channel.
/// The default depth of 6 produces the 216-color RadialColor palette.
/// </summary>
public class RadialPalette : IReadOnlyList<Color>, IDisposable
{
	#region Constants

	private const int PALETTE_SIZE = 256;

	#endregion

	#region Fields

	private readonly Texture _texture;
	private readonly List<Color> _colors;
	private bool disposedValue;

	#endregion

	#region Constructors

	public RadialPalette(int colorDepth = 6)
	{
		ColorDepth = colorDepth;

		_colors = new List<Color>();
		for (var r = 0; r < colorDepth; r++)
		{
			for (var g = 0; g < colorDepth; g++)
			{
				for (var b = 0; b < colorDepth; b++)
				{
					var rr = r * 255 / (colorDepth - 1);
					var gg = g * 255 / (colorDepth - 1);
					var bb = b * 255 / (colorDepth - 1);

					var mid = (rr * 30 + gg * 59 + bb * 11) / 100;

					var r1 = (rr + mid) / 2 * 230 / 255 + 10;
					var g1 = (gg + mid) / 2 * 230 / 255 + 10;
					var b1 = (bb + mid) / 2 * 230 / 255 + 10;

					_colors.Add(new Color(r1 / 255f, g1 / 255f, b1 / 255f));
				}
			}
		}

		while (_colors.Count < PALETTE_SIZE)
			_colors.Add(Color.Black);

		var data = new byte[PALETTE_SIZE * 3];
		for (int i = 0; i < PALETTE_SIZE; i++)
		{
			var color = _colors[i];
			data[i * 3]     = (byte)(color.R * 255f);
			data[i * 3 + 1] = (byte)(color.G * 255f);
			data[i * 3 + 2] = (byte)(color.B * 255f);
		}

		_texture = new Texture(PALETTE_SIZE, 1, false);
		_texture.Data = data;
	}

	#endregion

	#region Properties

	public int ColorDepth { get; }

	public int Id => _texture.Id;

	public Color this[int index] => _colors[index];

	public byte this[byte r, byte g, byte b]
		=> (byte)(r * ColorDepth * ColorDepth + g * ColorDepth + b);

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
