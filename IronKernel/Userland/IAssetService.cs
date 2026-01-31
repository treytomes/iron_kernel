using System.Drawing;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland;

public interface IAssetService
{
	Task<RenderImage> LoadImageAsync(string assetId);
	Task<Font> LoadFontAsync(string assetId, Size tileSize, int glyphOffset);
}