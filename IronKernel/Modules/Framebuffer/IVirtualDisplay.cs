using IronKernel.Common.ValueObjects;
using OpenTK.Mathematics;

namespace IronKernel.Modules.Framebuffer;

internal interface IVirtualDisplay : IDisposable
{
	bool IsInitialized { get; }

	/// <summary>  
	/// Gets the width of the virtual display in pixels.  
	/// </summary>  
	int Width { get; }

	/// <summary>  
	/// Gets the height of the virtual display in pixels.  
	/// </summary>  
	int Height { get; }

	/// <summary>  
	/// Gets the current scale factor applied to the virtual display.  
	/// </summary>  
	float Scale { get; }


	/// <summary>  
	/// Convert actual screen coordinates to virtual coordinates.  
	/// </summary>  
	/// <param name="actualPoint">The point in actual screen coordinates.</param>  
	/// <returns>The corresponding point in virtual display coordinates.</returns>  
	Vector2 ActualToVirtualPoint(Vector2 actualPoint);

	/// <summary>  
	/// Convert virtual coordinates to actual screen coordinates.  
	/// </summary>  
	/// <param name="virtualPoint">The point in virtual display coordinates.</param>  
	/// <returns>The corresponding point in actual screen coordinates.</returns>  
	Vector2 VirtualToActualPoint(Vector2 virtualPoint);

	/// <summary>  
	/// Gets the current padding applied to center the display.  
	/// </summary>  
	Vector2 Padding { get; }

	/// <summary>  
	/// Gets the color palette used by the rendering context.  
	/// </summary>  
	RadialPalette Palette { get; }

	/// <summary>
	/// Initialize the video subsystem after the rendering window is ready.
	/// </summary>
	void Initialize();

	/// <summary>  
	/// Updates the pixel data for the virtual display.  
	/// </summary>
	/// <param name="pixelData">The new pixel data (palette indices).</param>  
	/// <exception cref="ArgumentNullException">Thrown if pixelData is null.</exception>  
	/// <exception cref="ArgumentException">Thrown if pixelData length doesn't match display size.</exception>  
	void UpdatePixels(RadialColor[] pixelData);

	void SetPixels(
		int x,
		int y,
		int width,
		int height,
		RadialColor[] data);

	/// <summary>  
	/// Sets a single pixel in the virtual display.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <param name="color">The palette index to set.</param>  
	/// <returns>True if the pixel was set, false if coordinates were out of bounds.</returns>  
	bool SetPixel(int x, int y, RadialColor color);

	/// <summary>  
	/// Gets the color index at the specified pixel.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <returns>The palette index at the specified pixel, or 0 if out of bounds.</returns>  
	RadialColor GetPixel(int x, int y);

	/// <summary>  
	/// Clears the virtual display to the specified color index.  
	/// </summary>  
	/// <param name="color">The palette index to fill with.</param>  
	void Clear(RadialColor color);

	/// <summary>  
	/// Resizes the display to fit the new window size.  
	/// </summary>  
	/// <param name="windowSize">The new window size.</param>  
	void Resize(Vector2i windowSize);

	/// <summary>  
	/// Renders the virtual display to the current framebuffer.  
	/// </summary>  
	void Render();
}
