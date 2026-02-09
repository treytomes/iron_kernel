using System.Drawing;

namespace IronKernel.Userland.Roguey;

public sealed class TileSetInfo(string url, Size tileSize)
{
	public string Url => url;
	public Size TileSize => tileSize;
}