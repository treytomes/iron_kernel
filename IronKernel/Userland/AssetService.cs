using IronKernel.Common;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Userland.Gfx;
using System.Collections.Concurrent;
using System.Drawing;

namespace IronKernel.Userland;

internal class AssetService(IApplicationBus bus) : IAssetService
{
	private readonly IApplicationBus _bus = bus;
	private readonly ConcurrentDictionary<string, Font> _fontCache = new();
	private readonly ConcurrentDictionary<string, GlyphSet<Bitmap>> _glyphSetCache = new();
	private readonly ConcurrentDictionary<string, RenderImage> _imageCache = new();

	public async Task<Font> LoadFontAsync(string assetId, Size tileSize, int glyphOffset)
	{
		if (!_fontCache.ContainsKey(assetId))
		{
			var glyphs = await LoadGlyphSetAsync(assetId, tileSize);
			var font = new Font(glyphs, glyphOffset);
			_fontCache[assetId] = font;
		}
		return _fontCache[assetId];
	}

	public async Task<GlyphSet<Bitmap>> LoadGlyphSetAsync(string assetId, Size tileSize)
	{
		if (!_glyphSetCache.ContainsKey(assetId))
		{
			var image = await LoadImageAsync(assetId);
			var bitmap = new Bitmap(image);
			var glyphs = new GlyphSet<Bitmap>(bitmap, tileSize.Width, tileSize.Height);
			_glyphSetCache[assetId] = glyphs;
		}
		return _glyphSetCache[assetId];
	}

	public async Task<RenderImage> LoadImageAsync(string assetId)
	{
		if (!_imageCache.ContainsKey(assetId))
		{
			var response = await _bus.QueryAsync<
				AppAssetImageQuery,
				AppAssetImageResponse>(
					id => new AppAssetImageQuery(id, assetId));

			var image = new RenderImage(response.Image);
			_imageCache[assetId] = image;
		}
		return _imageCache[assetId];
	}
}