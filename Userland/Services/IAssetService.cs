using System.Drawing;
using Userland.Gfx;

namespace Userland.Services;

public interface IAssetService
{
	Task<RenderImage> LoadImageAsync(string url);
	Task<GlyphSet<Bitmap>> LoadGlyphSetAsync(string url, Size tileSize);
	Task<Font> LoadFontAsync(string url, Size tileSize, int glyphOffset);
}