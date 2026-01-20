using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.Framebuffer;
using OpenTK.Mathematics;

namespace IronKernel.Userland;

public interface IRenderingContext
{
	/// <summary>  
	/// Gets the width of the rendering context in pixels.  
	/// </summary>  
	int Width { get; }

	/// <summary>  
	/// Gets the height of the rendering context in pixels.  
	/// </summary>  
	int Height { get; }

	/// <summary>  
	/// Gets the size of the viewport as a Vector2.  
	/// </summary>  
	Point ViewportSize { get; }

	/// <summary>  
	/// Gets a box representing the bounds of the rendering context.  
	/// </summary>  
	Rectangle Bounds { get; }

	/// <summary>  
	/// Convert actual screen coordinates to virtual coordinates.  
	/// </summary>  
	/// <param name="actualPoint">The point in actual screen coordinates.</param>  
	/// <returns>The corresponding point in virtual display coordinates.</returns>  
	Point ActualToVirtualPoint(Point actualPoint);

	/// <summary>  
	/// Convert virtual coordinates to actual screen coordinates.  
	/// </summary>  
	/// <param name="virtualPoint">The point in virtual display coordinates.</param>  
	/// <returns>The corresponding point in actual screen coordinates.</returns>  
	Point VirtualToActualPoint(Point virtualPoint);

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
