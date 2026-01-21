using IronKernel.Common;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.AssetLoader.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.AssetLoader;

internal sealed class AssetLoaderModule(
	IMessageBus bus,
	ILogger<AssetLoaderModule> logger,
	IResourceManager resourceManager
) : IKernelModule
{
	#region Fields

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
			var path = GetPathFromAssetId(msg.AssetId);
			var image = _resourceManager.Load<Image>(path);
			_bus.Publish(new AssetImageResponse(msg.CorrelationID, msg.AssetId, image));
			return Task.CompletedTask;
		});

		return Task.CompletedTask;
	}

	public string GetPathFromAssetId(string assetId)
	{
		return assetId switch
		{
			"mouse_cursor" => "mouse_cursor.png",
			"oem437_8" => "oem437_8.png",
			_ => throw new FileNotFoundException($"Asset id is undefined: {assetId}"),
		};
	}

	public ValueTask DisposeAsync()
	{
		_logger.LogInformation("AssetLoader disposed.");
		return ValueTask.CompletedTask;
	}

	#endregion
}