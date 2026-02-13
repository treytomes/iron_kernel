using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using System.Collections.Concurrent;
using System.Drawing;

namespace Userland.Services;

internal class AssetService(IApplicationBus bus) : IAssetService
{
	private readonly IApplicationBus _bus = bus;
	private readonly ConcurrentDictionary<string, Font> _fontCache = new();
	private readonly ConcurrentDictionary<string, GlyphSet<Bitmap>> _glyphSetCache = new();
	private readonly ConcurrentDictionary<string, RenderImage> _imageCache = new();

	public async Task<Font> LoadFontAsync(string url, Size tileSize, int glyphOffset)
	{
		if (!_fontCache.ContainsKey(url))
		{
			var glyphs = await LoadGlyphSetAsync(url, tileSize);
			var font = new Font(glyphs, glyphOffset);
			_fontCache[url] = font;
		}
		return _fontCache[url];
	}

	public async Task<GlyphSet<Bitmap>> LoadGlyphSetAsync(string url, Size tileSize)
	{
		if (!_glyphSetCache.ContainsKey(url))
		{
			var image = await LoadImageAsync(url);
			var bitmap = new Bitmap(image);
			var glyphs = new GlyphSet<Bitmap>(bitmap, tileSize.Width, tileSize.Height);
			_glyphSetCache[url] = glyphs;
		}
		return _glyphSetCache[url];
	}

	public async Task<RenderImage> LoadImageAsync(string url)
	{
		if (!_imageCache.ContainsKey(url))
		{
			var response = await _bus.QueryAsync<
				AppAssetImageQuery,
				AppAssetImageResponse>(
					id => new AppAssetImageQuery(id, url));

			var image = new RenderImage(response.Image);
			_imageCache[url] = image;
		}
		return _imageCache[url];
	}
}