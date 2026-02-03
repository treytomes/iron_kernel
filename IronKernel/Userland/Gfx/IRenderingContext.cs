using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Gfx;

/// <summary>  
/// Provides a high-level drawing API for rendering to a VirtualDisplay.  
/// </summary>
public interface IRenderingContext
{
	/// <summary>  
	/// Gets the size of the rendering context in pixels.  
	/// </summary>  
	Size Size { get; }

	/// <summary>  
	/// Gets a box representing the bounds of the rendering context.  
	/// </summary>  
	Rectangle Bounds { get; }

	int OffsetStackSize { get; }
	int ClipStackSize { get; }

	void ResetTransform();

	/// <returns>
	/// The size of the stack.
	/// </returns>
	int PushOffset(Point offset, string source);
	void PopOffset(int targetCount, string source);

	/// <returns>
	/// The size of the stack.
	/// </returns>
	int PushClip(Rectangle rect, string source);
	void PopClip(int targetCount, string source);

	/// <summary>  
	/// Fills the entire rendering context with the specified color.  
	/// </summary>  
	/// <param name="color">The color to fill with.</param>  
	void Fill(RadialColor color);

	/// <summary>  
	/// Clears the rendering context (fills with black).  
	/// </summary>
	void Clear();

	/// <summary>  
	/// Sets a pixel at the specified coordinates to the specified palette color.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <param name="color">The color to set.</param>  
	void SetPixel(Point pnt, RadialColor color);

	/// <summary>  
	/// Gets the palette index of the pixel at the specified position.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <returns>The color of the pixel, or black if out of bounds.</returns>  
	RadialColor GetPixel(Point pnt);

	/// <summary>  
	/// Renders a filled rectangle with the specified corners and palette index.  
	/// </summary>  
	/// <param name="rect">The bounds of the rect to fill.</param>  
	/// <param name="color">The color to fill with.</param>  
	void RenderFilledRect(Rectangle rect, RadialColor color);

	/// <summary>  
	/// Renders a rectangle outline with the specified bounds, color, and thickness.  
	/// </summary>  
	/// <param name="rect">The bounds of the rectangle.</param>  
	/// <param name="color">The color of the outline.</param>
	/// <param name="thickness">The thickness of the outline (default is 1).</param>
	void RenderRect(Rectangle rect, RadialColor color, int thickness = 1);

	/// <summary>  
	/// Renders a horizontal line with the specified starting point, length, and palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point of the line.</param>  
	/// <param name="len">The length of the line in pixels.</param>  
	/// <param name="color">The color of the line.</param>  
	void RenderHLine(Point pnt, int len, RadialColor color);

	/// <summary>  
	/// Renders a vertical line with the specified starting point, length, and palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point of the line.</param>  
	/// <param name="len">The length of the line in pixels.</param>  
	/// <param name="color">The color of the line.</param>  
	void RenderVLine(Point pnt, int len, RadialColor color);

	/// <summary>  
	/// Renders a line between two points with the specified palette index.  
	/// </summary>  
	/// <param name="pnt1">The start point of the line.</param>  
	/// <param name="pnt2">The end point of the line.</param>  
	/// <param name="color">The color of the line.</param>  
	void RenderLine(Point pnt1, Point pnt2, RadialColor color);

	/// <summary>  
	/// Renders a circle with ordered dithering for a soft edge effect.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The primary color of the circle.</param>  
	/// <param name="falloffStart">The point at which the falloff begins (0.0-1.0).</param>  
	/// <param name="secondaryColor">Optional secondary color for the dithered region.</param>  
	void RenderOrderedDitheredCircle(Point center, int radius, RadialColor color, float falloffStart = 0.6f, RadialColor? secondaryColor = null);

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and color.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The color of the circle.</param>  
	void RenderCircle(Point center, int radius, RadialColor color);

	/// <summary>  
	/// Renders a filled circle with the specified center, radius, and color.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The color to fill with.</param>  
	void RenderFilledCircle(Point center, int radius, RadialColor color);

	/// <summary>  
	/// Performs a flood fill starting at the specified point with the specified palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point for the fill.</param>  
	/// <param name="color">The color to fill with.</param>  
	void FloodFill(Point pnt, RadialColor color);

	/// <summary>  
	/// Updates the virtual display with the current pixel data if it has changed.  
	/// </summary>  
	void Present();
}
