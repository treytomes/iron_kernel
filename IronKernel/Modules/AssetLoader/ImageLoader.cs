using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging;
using Color = IronKernel.Common.ValueObjects.Color;

namespace IronKernel.Modules.AssetLoader;

class ImageLoader : IResourceLoader<Image>
{
	private const int SRC_BPP = 4; // RGBA32

	private readonly ILogger<ImageLoader>? _logger;

	public ImageLoader() { }

	public ImageLoader(ILogger<ImageLoader> logger)
	{
		_logger = logger;
	}

	public Image Load(IResourceManager resourceManager, string path)
	{
		if (resourceManager == null)
			throw new ArgumentNullException(nameof(resourceManager));
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentNullException(nameof(path));

		_logger?.LogDebug("Loading image from: {Path}", path);

		if (!File.Exists(path))
		{
			var exception = new FileNotFoundException($"Image file not found: {path}", path);
			_logger?.LogError(exception, "Image file not found: {Path}", path);
			throw exception;
		}

		try
		{
			using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(path);
			_logger?.LogDebug("Loaded image: {Width}x{Height} pixels", image.Width, image.Height);
			return ConvertImage(image);
		}
		catch (Exception ex) when (ex is not FileNotFoundException && ex is not ArgumentNullException)
		{
			_logger?.LogError(ex, "Failed to load image: {Path}", path);
			throw new InvalidOperationException($"Failed to load image: {path}", ex);
		}
	}

	private Image ConvertImage(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image)
	{
		var sourcePixels = new byte[image.Width * image.Height * SRC_BPP];
		var destPixels = new Color?[image.Width * image.Height];

		image.CopyPixelDataTo(sourcePixels);

		for (int srcIndex = 0, dstIndex = 0; srcIndex < sourcePixels.Length; srcIndex += SRC_BPP, dstIndex++)
		{
			var r = sourcePixels[srcIndex] / 255f;
			var g = sourcePixels[srcIndex + 1] / 255f;
			var b = sourcePixels[srcIndex + 2] / 255f;
			var a = sourcePixels[srcIndex + 3];

			destPixels[dstIndex] = a == 0 ? null : new Color(r, g, b);
		}

		_logger?.LogDebug("Image conversion complete");

		return new Image(image.Width, image.Height, destPixels);
	}
}
