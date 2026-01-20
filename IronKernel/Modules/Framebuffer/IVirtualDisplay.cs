using OpenTK.Mathematics;

namespace IronKernel.Modules.Framebuffer;

public interface IVirtualDisplay : IDisposable
{
	public bool IsInitialized { get; }

	/// <summary>  
	/// Gets the width of the virtual display in pixels.  
	/// </summary>  
	public int Width { get; }

	/// <summary>  
	/// Gets the height of the virtual display in pixels.  
	/// </summary>  
	public int Height { get; }

	/// <summary>  
	/// Gets the current scale factor applied to the virtual display.  
	/// </summary>  
	public float Scale { get; }

	/// <summary>  
	/// Gets the current padding applied to center the display.  
	/// </summary>  
	public Vector2 Padding { get; }

	/// <summary>  
	/// Gets the color palette used by the rendering context.  
	/// </summary>  
	public RadialPalette Palette { get; }

	/// <summary>
	/// Initialize the video subsystem after the rendering window is ready.
	/// </summary>
	public void Initialize();

	/// <summary>  
	/// Updates the pixel data for the virtual display.  
	/// </summary>
	/// <param name="pixelData">The new pixel data (palette indices).</param>  
	/// <exception cref="ArgumentNullException">Thrown if pixelData is null.</exception>  
	/// <exception cref="ArgumentException">Thrown if pixelData length doesn't match display size.</exception>  
	public void UpdatePixels(byte[] pixelData);

	/// <summary>  
	/// Sets a single pixel in the virtual display.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <param name="colorIndex">The palette index to set.</param>  
	/// <returns>True if the pixel was set, false if coordinates were out of bounds.</returns>  
	public bool SetPixel(int x, int y, byte colorIndex);

	/// <summary>  
	/// Gets the color index at the specified pixel.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <returns>The palette index at the specified pixel, or 0 if out of bounds.</returns>  
	public byte GetPixel(int x, int y);

	/// <summary>  
	/// Clears the virtual display to the specified color index.  
	/// </summary>  
	/// <param name="colorIndex">The palette index to fill with.</param>  
	public void Clear(byte colorIndex = 0);

	/// <summary>  
	/// Resizes the display to fit the new window size.  
	/// </summary>  
	/// <param name="windowSize">The new window size.</param>  
	public void Resize(Vector2i windowSize);

	/// <summary>  
	/// Renders the virtual display to the current framebuffer.  
	/// </summary>  
	public void Render();

	/// <summary>  
	/// Draws a line between two points using Bresenham's algorithm.  
	/// </summary>  
	/// <param name="x0">Starting x-coordinate.</param>  
	/// <param name="y0">Starting y-coordinate.</param>  
	/// <param name="x1">Ending x-coordinate.</param>  
	/// <param name="y1">Ending y-coordinate.</param>  
	/// <param name="colorIndex">The palette index to use for the line.</param>  
	public void DrawLine(int x0, int y0, int x1, int y1, byte colorIndex);

	/// <summary>  
	/// Draws a rectangle outline.  
	/// </summary>  
	/// <param name="x">X-coordinate of the top-left corner.</param>  
	/// <param name="y">Y-coordinate of the top-left corner.</param>  
	/// <param name="width">Width of the rectangle.</param>  
	/// <param name="height">Height of the rectangle.</param>  
	/// <param name="colorIndex">The palette index to use for the rectangle.</param>  
	public void DrawRectangle(int x, int y, int width, int height, byte colorIndex);

	/// <summary>  
	/// Fills a rectangle with the specified color.  
	/// </summary>  
	/// <param name="x">X-coordinate of the top-left corner.</param>  
	/// <param name="y">Y-coordinate of the top-left corner.</param>  
	/// <param name="width">Width of the rectangle.</param>  
	/// <param name="height">Height of the rectangle.</param>  
	/// <param name="colorIndex">The palette index to use for filling.</param>  
	public void FillRectangle(int x, int y, int width, int height, byte colorIndex);
}
