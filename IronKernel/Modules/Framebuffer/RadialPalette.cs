using System.Collections;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.Framebuffer;

/// <summary>
/// Generate a palette with 6 increments each of red, green, and blue.
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

	public RadialPalette()
	{
		// Generate the colors.
		const int BITS = 6;
		_colors = new List<Color>();
		for (var r = 0; r < BITS; r++)
		{
			for (var g = 0; g < BITS; g++)
			{
				for (var b = 0; b < BITS; b++)
				{
					var rr = r * 255 / (BITS - 1);
					var gg = g * 255 / (BITS - 1);
					var bb = b * 255 / (BITS - 1);

					var mid = (rr * 30 + gg * 59 + bb * 11) / 100;

					var r1 = (rr + mid) / 2 * 230 / 255 + 10;
					var g1 = (gg + mid) / 2 * 230 / 255 + 10;
					var b1 = (bb + mid) / 2 * 230 / 255 + 10;

					_colors.Add(new Color(r1 / 255f, g1 / 255f, b1 / 255f));
				}
			}
		}

		while (_colors.Count < PALETTE_SIZE)
		{
			_colors.Add(Color.Black);
		}

		// Populate palette texture data (byte RGB for GL).
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

	public int Id => _texture.Id;

	/// <summary>
	/// Retrieve the color associated with a palette index.
	/// </summary>
	public Color this[int index] => _colors[index];

	/// <summary>
	/// Retrieve the palette index associated with a radial RGB value.
	/// </summary>
	/// <param name="r6">0-5</param>
	/// <param name="g6">0-5</param>
	/// <param name="b6">0-5</param>
	/// <returns></returns>
	public byte this[byte r6, byte g6, byte b6]
	{
		get
		{
			return (byte)(r6 * 6 * 6 + g6 * 6 + b6);
		}
	}

	public int Count => _colors.Count;

	#endregion

	#region Methods

	public static byte GetIndex(byte r6, byte g6, byte b6)
	{
		return (byte)(r6 * 6 * 6 + g6 * 6 + b6);
	}

	public IEnumerator<Color> GetEnumerator() => _colors.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects)
				_texture?.Dispose();
			}

			// Dispose unmanaged state.

			disposedValue = true;
		}
	}

	~RadialPalette()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}