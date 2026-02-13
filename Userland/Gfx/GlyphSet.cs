using System.Collections;
using IronKernel.Common;

namespace Userland.Gfx;

/// <summary>
/// A larger image that has been diced up into a list of smaller images.
/// </summary>
public class GlyphSet<TImage> : IReadOnlyList<TImage>
	where TImage : IImage<TImage>
{
	#region Fields

	public readonly int TileWidth;
	public readonly int TileHeight;

	private readonly List<TImage> _tiles;

	#endregion

	#region Constructors

	public GlyphSet(TImage image, int tileWidth, int tileHeight)
	{
		TileWidth = tileWidth;
		TileHeight = tileHeight;

		_tiles = new List<TImage>();
		for (var y = 0; y < image.Size.Height; y += tileHeight)
		{
			for (var x = 0; x < image.Size.Width; x += tileWidth)
			{
				_tiles.Add(image.Crop(x, y, tileWidth, tileHeight));
			}
		}
	}

	#endregion

	#region Properties

	public TImage this[int index] => _tiles[index];

	public int Count => _tiles.Count;

	#endregion

	#region Methods

	public IEnumerator<TImage> GetEnumerator()
	{
		return _tiles.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _tiles.GetEnumerator();
	}

	#endregion
}