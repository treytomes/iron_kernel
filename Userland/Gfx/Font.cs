using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace Userland.Gfx;

public sealed class Font
{
	#region Fields

	private readonly GlyphSet<Bitmap> _tiles;
	private readonly int _glyphOffset;

	#endregion

	#region Constructors

	public Font(GlyphSet<Bitmap> tiles, int glyphOffset = 0)
	{
		_tiles = tiles;
		_glyphOffset = glyphOffset;
	}

	#endregion

	#region Properties

	public Size TileSize => new Size(_tiles.TileWidth, _tiles.TileHeight);

	#endregion

	#region Methods

	public void WriteChar(IRenderingContext rc, char ch, Point position, RadialColor fg, RadialColor? bg = null)
	{
		var index = ch + _glyphOffset;
		if (index >= 0 && index < _tiles.Count)
			_tiles[index].Render(rc, position, fg, bg);
	}

	public void WriteString(IRenderingContext rc, string text, Point position, RadialColor fg, RadialColor? bg = null)
	{
		for (int i = 0; i < text.Length; i++)
		{
			var index = text[i] + _glyphOffset;
			if (index >= 0 && index < _tiles.Count)
				_tiles[index].Render(rc, new Point(position.X + i * _tiles.TileWidth, position.Y), fg, bg);
		}
	}

	public Size MeasureString(string text)
	{
		return new Size(text.Length * _tiles.TileWidth, _tiles.TileHeight);
	}

	#endregion
}