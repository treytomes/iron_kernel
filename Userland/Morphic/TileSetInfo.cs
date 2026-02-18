using System.Drawing;

namespace Userland.Morphic;

public sealed class TileSetInfo(string url, Size tileSize)
{
	public string Url => url;
	public Size TileSize => tileSize;
}