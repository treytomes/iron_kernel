using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.AssetLoader;

/// <summary>
/// Loads image files and converts them to the game's internal image format.
/// The image is loaded with 24-bit RGB colors, then compressed into the radial palette.
/// </summary>
class ImageLoader : IResourceLoader<Image>
{
	private const int SRC_BPP = 4; // Source bytes per pixel (RGBA32)
	private const int DST_BPP = 1; // Destination bytes per pixel (palette index)

	// Color quantization constants
	private const int COLOR_LEVELS = 6; // Number of levels per color channel
	private const double COLOR_DIVISOR = 255.0 / (COLOR_LEVELS - 1); // Division factor for quantization

	private readonly ILogger<ImageLoader>? _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="ImageLoader"/> class.
	/// </summary>
	public ImageLoader()
	{
		// This constructor is used when created via Activator.CreateInstance
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ImageLoader"/> class with a logger.
	/// </summary>
	/// <param name="logger">The logger for this class.</param>
	public ImageLoader(ILogger<ImageLoader> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Loads an image from the specified path and converts it to the game's internal format.
	/// </summary>
	/// <param name="resourceManager">The resource manager.</param>
	/// <param name="path">The path to the image file.</param>
	/// <returns>The loaded image.</returns>
	public Image Load(IResourceManager resourceManager, string path)
	{
		if (resourceManager == null)
		{
			throw new ArgumentNullException(nameof(resourceManager));
		}
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentNullException(nameof(path));
		}

		_logger?.LogDebug("Loading image from: {Path}", path);

		if (!File.Exists(path))
		{
			var exception = new FileNotFoundException($"Image file not found: {path}", path);
			_logger?.LogError(exception, "Image file not found: {Path}", path);
			throw exception;
		}

		try
		{
			// Load the image using ImageSharp
			using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(path);
			_logger?.LogDebug("Loaded image: {Width}x{Height} pixels", image.Width, image.Height);

			return ConvertImage(image);
		}
		catch (Exception ex) when (
			ex is not FileNotFoundException &&
			ex is not ArgumentNullException)
		{
			_logger?.LogError(ex, "Failed to load image: {Path}", path);
			throw new InvalidOperationException($"Failed to load image: {path}", ex);
		}
	}

	/// <summary>
	/// Converts an ImageSharp image to the game's internal image format.
	/// </summary>
	/// <param name="image">The source image.</param>
	/// <returns>The converted image.</returns>
	private Image ConvertImage(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image)
	{
		// Create buffers for the source and destination pixel data
		var sourcePixels = new byte[image.Width * image.Height * SRC_BPP];
		var destPixels = new RadialColor[image.Width * image.Height * DST_BPP];

		// Copy the pixel data from the image to the source buffer
		image.CopyPixelDataTo(sourcePixels);

		_logger?.LogDebug("Converting image to palette format");

		// Convert each pixel from RGBA32 to palette index
		for (int srcIndex = 0, dstIndex = 0; srcIndex < sourcePixels.Length; srcIndex += SRC_BPP, dstIndex += DST_BPP)
		{
			var r = sourcePixels[srcIndex];
			var g = sourcePixels[srcIndex + 1];
			var b = sourcePixels[srcIndex + 2];
			// Alpha is ignored (srcIndex + 3)

			// Quantize each color channel to COLOR_LEVELS levels
			var r6 = (byte)Math.Round(r / COLOR_DIVISOR);
			var g6 = (byte)Math.Round(g / COLOR_DIVISOR);
			var b6 = (byte)Math.Round(b / COLOR_DIVISOR);

			// Calculate the palette index using the formula:
			// index = r * COLOR_LEVELS² + g * COLOR_LEVELS + b
			// var index = (byte)(r6 * COLOR_LEVELS * COLOR_LEVELS + g6 * COLOR_LEVELS + b6);

			destPixels[dstIndex] = new RadialColor(r6, g6, b6);
		}

		_logger?.LogDebug("Image conversion complete");

		return new Image(image.Width, image.Height, destPixels);
	}
}