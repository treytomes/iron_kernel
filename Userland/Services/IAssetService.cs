using System.Drawing;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Services;

public interface IAssetService
{
	Task<RenderImage> LoadImageAsync(string url);
	Task<GlyphSet<Bitmap>> LoadGlyphSetAsync(string url, Size tileSize);
	Task<Font> LoadFontAsync(string url, Size tileSize, int glyphOffset);
}