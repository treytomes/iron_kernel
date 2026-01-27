using IronKernel.Common;
using IronKernel.Modules.ApplicationHost;
using System.Collections.Concurrent;

namespace IronKernel.Userland;

internal class AssetService(IApplicationBus bus) : IAssetService
{
	private readonly IApplicationBus _bus = bus;
	private readonly ConcurrentDictionary<string, RenderImage> _cache = new();

	public async Task<RenderImage> LoadImageAsync(string assetId)
	{
		if (!_cache.ContainsKey(assetId))
		{
			var response = await _bus.QueryAsync<
				AppAssetImageQuery,
				AppAssetImageResponse>(
					id => new AppAssetImageQuery(id, assetId));

			var image = new RenderImage(response.Image);
			_cache[assetId] = image;
		}
		return _cache[assetId];
	}
}