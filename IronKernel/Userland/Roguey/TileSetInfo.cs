using System.Drawing;

namespace IronKernel.Userland.Roguey;

public sealed class TileSetInfo(string assetId, Size tileSize)
{
	public string AssetId => assetId;
	public Size TileSize => tileSize;
}