using IronKernel.Common;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.AssetLoader.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.AssetLoader;

internal sealed class AssetLoaderModule(
	AppSettings settings,
	IMessageBus bus,
	ILogger<AssetLoaderModule> logger,
	IResourceManager resourceManager
) : IKernelModule
{
	#region Fields

	private readonly AppSettings.AssetDirectory _assetDirectory = settings.Assets;
	private readonly IMessageBus _bus = bus;
	private readonly ILogger<AssetLoaderModule> _logger = logger;
	private readonly IResourceManager _resourceManager = resourceManager;

	#endregion

	#region Methods

	public Task StartAsync(IKernelState state, IModuleRuntime runtime, CancellationToken stoppingToken)
	{
		_logger.LogInformation("Initializing AssetLoader.");

		_resourceManager.Register<Image, ImageLoader>();

		_bus.Subscribe<AssetImageQuery>(runtime, "ImageQueryHandler", (msg, ct) =>
		{
			var path = GetPathFromUrl(msg.Url);
			if (path != null)
			{
				var image = _resourceManager.Load<Image>(path);
				_bus.Publish(new AssetImageResponse(msg.CorrelationID, msg.Url, image));
			}
			return Task.CompletedTask;
		});

		return Task.CompletedTask;
	}

	public string? GetPathFromUrl(string url)
	{
		if (!url.StartsWith("asset://")) return null;

		var assetId = url["asset://".Length..];
		var pieces = assetId.ToLower().Split('.');
		if (pieces[0].Trim() == "image")
		{
			return _assetDirectory.Image[pieces[1].Trim()];
		}

		_logger.LogError($"Asset url is undefined: {url}");
		return null;
	}

	public ValueTask DisposeAsync()
	{
		_logger.LogInformation("AssetLoader disposed.");
		return ValueTask.CompletedTask;
	}

	#endregion
}