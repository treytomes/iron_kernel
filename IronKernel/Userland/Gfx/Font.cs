using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Gfx;

public class Font
{
	#region Fields

	private readonly GlyphSet<Bitmap> _tiles;

	#endregion

	#region Constructors

	public Font(GlyphSet<Bitmap> tiles)
	{
		_tiles = tiles;
	}

	#endregion

	#region Methods

	public void WriteString(IRenderingContext rc, string text, Point position, RadialColor fg, RadialColor? bg = null)
	{
		for (int i = 0; i < text.Length; i++)
		{
			_tiles[text[i]].Render(rc, new Point(position.X + i * 8, position.Y), fg, bg);
		}
	}

	public Size MeasureString(string text)
	{
		return new Size(text.Length * _tiles.TileWidth, _tiles.TileHeight);
	}

	#endregion
}