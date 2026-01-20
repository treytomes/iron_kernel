using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.ApplicationHost;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace IronKernel.Userland;

/// <summary>  
/// Provides a high-level drawing API for rendering to a VirtualDisplay.  
/// </summary>  
public sealed class RenderingContext(IApplicationBus bus, ILogger<RenderingContext> logger) : IRenderingContext
{
	#region Fields  

	private readonly ILogger<RenderingContext> _logger = logger;
	private readonly IApplicationBus _bus = bus;
	private bool _isDirty = true;
	private RadialColor[]? _data = null;
	private bool _isInitialized = false;

	/// <summary>  
	/// Gets the current scale factor applied to the virtual display.  
	/// </summary>  
	private float _scale = 1.0f;

	/// <summary>  
	/// Gets the current padding applied to center the display.  
	/// </summary>  
	private Point _padding = new Point(0, 0);

	#endregion

	#region Properties  

	/// <summary>  
	/// Gets the width of the rendering context in pixels.  
	/// </summary>  
	public int Width { get; private set; }

	/// <summary>  
	/// Gets the height of the rendering context in pixels.  
	/// </summary>  
	public int Height { get; private set; }

	/// <summary>  
	/// Gets the size of the viewport as a Vector2.  
	/// </summary>  
	public Point ViewportSize => new(Width, Height);

	/// <summary>  
	/// Gets a box representing the bounds of the rendering context.  
	/// </summary>  
	public Rectangle Bounds => new(0, 0, Width, Height);

	#endregion

	#region Methods  

	public async Task InitializeAsync()
	{
		var subs = new List<IDisposable>();
		subs.Add(_bus.Subscribe<AppFbInfo>("FbInfoHandler", (msg, ct) =>
		{
			Width = msg.Width;
			Height = msg.Height;
			_padding = msg.Padding;
			_scale = msg.Scale;
			_data = new RadialColor[Width * Height];
			_isInitialized = true;
			foreach (var sub in subs) sub.Dispose();
			return Task.CompletedTask;
		}));
		_bus.Publish(new AppFbInfoQuery());
	}

	/// <summary>  
	/// Convert actual screen coordinates to virtual coordinates.  
	/// </summary>  
	/// <param name="actualPoint">The point in actual screen coordinates.</param>  
	/// <returns>The corresponding point in virtual display coordinates.</returns>  
	public Point ActualToVirtualPoint(Point actualPoint)
	{
		var x = actualPoint.X - _padding.X / _scale;
		var y = actualPoint.Y - _padding.Y / _scale;
		return new Point((int)x, (int)y);
	}

	/// <summary>  
	/// Convert virtual coordinates to actual screen coordinates.  
	/// </summary>  
	/// <param name="virtualPoint">The point in virtual display coordinates.</param>  
	/// <returns>The corresponding point in actual screen coordinates.</returns>  
	public Point VirtualToActualPoint(Point virtualPoint)
	{
		var x = virtualPoint.X * _scale + _padding.X;
		var y = virtualPoint.Y * _scale + _padding.Y;
		return new Point((int)x, (int)y);
	}

	/// <inheritdoc/>
	public void Fill(RadialColor color)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");
		Array.Fill(_data!, color);
		_isDirty = true;
	}

	/// <inheritdoc/>
	public void Clear()
	{
		Fill(RadialColor.Black);
	}

	/// <inheritdoc/>
	public void SetPixel(Point pnt, RadialColor color)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		if (pnt.X < 0 || pnt.X >= Width || pnt.Y < 0 || pnt.Y >= Height)
		{
			return;
		}

		var index = pnt.Y * Width + pnt.X;
		_data![index] = color;
		_isDirty = true;
	}

	/// <summary>  
	/// Gets the palette index of the pixel at the specified position.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <returns>The color of the pixel, or black if out of bounds.</returns>  
	public RadialColor GetPixel(Point pnt)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		if (pnt.X < 0 || pnt.X >= Width || pnt.Y < 0 || pnt.Y >= Height)
		{
			return RadialColor.Black;
		}

		var index = pnt.Y * Width + pnt.X;
		return _data![index];
	}

	/// <summary>  
	/// Renders a filled rectangle with the specified corners and palette index.  
	/// </summary>  
	/// <param name="rect">The bounds of the rect to fill.</param>  
	/// <param name="color">The color to fill with.</param>  
	public void RenderFilledRect(Rectangle rect, RadialColor color)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		var x1 = rect.Left;
		var x2 = rect.Right;
		var y1 = rect.Top;
		var y2 = rect.Bottom;

		// Ensure x1 <= x2 and y1 <= y2  
		if (x1 > x2)
		{
			(x1, x2) = (x2, x1);
		}
		if (y1 > y2)
		{
			(y1, y2) = (y2, y1);
		}

		// Clip to bounds  
		x1 = Math.Max(0, Math.Min(Width - 1, x1));
		y1 = Math.Max(0, Math.Min(Height - 1, y1));
		x2 = Math.Max(0, Math.Min(Width - 1, x2));
		y2 = Math.Max(0, Math.Min(Height - 1, y2));

		// Optimized direct buffer access for filled rectangle  
		for (int y = y1; y <= y2; y++)
		{
			int rowOffset = y * Width;
			for (int x = x1; x <= x2; x++)
			{
				_data![rowOffset + x] = color;
			}
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void RenderRect(Rectangle rect, RadialColor color, int thickness = 1)
	{
		var x1 = rect.Left;
		var x2 = rect.Right;
		var y1 = rect.Top;
		var y2 = rect.Bottom;

		// Ensure x1,y1 is the top-left and x2,y2 is the bottom-right
		if (x1 > x2)
		{
			(x1, x2) = (x2, x1);
		}
		if (y1 > y2)
		{
			(y1, y2) = (y2, y1);
		}

		// Calculate the actual thickness (clamped to available space)
		thickness = Math.Min(thickness, Math.Min((x2 - x1) / 2, (y2 - y1) / 2));

		// Draw multiple concentric rectangles to achieve the desired thickness
		for (var i = 0; i < thickness; i++)
		{
			// Draw the four sides of the rectangle
			RenderHLine(new Point(x1 + i, y1 + i), x2 - i, color);  // Top
			RenderHLine(new Point(x1 + i, y2 - i), x2 - i, color);  // Bottom
			RenderVLine(new Point(x1 + i, y1 + i + 1), y2 - i - 1, color);  // Left
			RenderVLine(new Point(x2 - i, y1 + i + 1), y2 - i - 1, color);  // Right
		}
	}

	/// <inheritdoc/>
	public void RenderHLine(Point pnt, int len, RadialColor color)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		var x1 = pnt.X;
		var x2 = x1 + len - 1;
		var y = pnt.Y;
		if (y < 0 || y >= Height)
			return;

		if (x1 > x2)
			(x1, x2) = (x2, x1);

		x1 = Math.Max(0, x1);
		x2 = Math.Min(Width - 1, x2);

		// Optimized direct buffer access  
		int offset = y * Width;
		for (int x = x1; x <= x2; x++)
		{
			_data![offset + x] = color;
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void RenderVLine(Point pnt, int len, RadialColor color)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		var x = pnt.X;
		var y1 = pnt.Y;
		var y2 = pnt.Y + len - 1;
		if (x < 0 || x >= Width)
			return;

		if (y1 > y2)
			(y1, y2) = (y2, y1);

		y1 = Math.Max(0, y1);
		y2 = Math.Min(Height - 1, y2);

		// Direct buffer access  
		for (int y = y1; y <= y2; y++)
		{
			_data![y * Width + x] = color;
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void RenderLine(Point pnt1, Point pnt2, RadialColor color)
	{
		var x1 = pnt1.X;
		var y1 = pnt1.Y;
		var x2 = pnt2.X;
		var y2 = pnt2.Y;

		// Bresenham's line algorithm  
		int dx = Math.Abs(x2 - x1);
		int sx = x1 < x2 ? 1 : -1;
		int dy = -Math.Abs(y2 - y1);
		int sy = y1 < y2 ? 1 : -1;
		int err = dx + dy;

		while (true)
		{
			SetPixel(new Point(x1, y1), color);
			if (x1 == x2 && y1 == y2)
				break;

			int e2 = 2 * err;
			if (e2 >= dy)
			{
				if (x1 == x2)
					break;
				err += dy;
				x1 += sx;
			}
			if (e2 <= dx)
			{
				if (y1 == y2)
					break;
				err += dx;
				y1 += sy;
			}
		}
	}

	/// <inheritdoc/>
	public void RenderOrderedDitheredCircle(Point center, int radius, RadialColor color, float falloffStart = 0.6f, RadialColor? secondaryColor = null)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		// Bayer 4x4 dithering matrix  
		var bayerMatrix = new int[,] {
			{  0, 12,  3, 15 },
			{  8,  4, 11,  7 },
			{  2, 14,  1, 13 },
			{ 10,  6,  9,  5 }
		};

		var innerRadiusSquared = (radius * falloffStart) * (radius * falloffStart);
		var outerRadiusSquared = radius * radius;

		// Calculate bounds for the circle and clip to the display area  
		int minX = Math.Max(0, (int)(center.X - radius));
		int maxX = Math.Min(Width - 1, (int)(center.X + radius));
		int minY = Math.Max(0, (int)(center.Y - radius));
		int maxY = Math.Min(Height - 1, (int)(center.Y + radius));

		for (var y = minY; y <= maxY; y++)
		{
			int rowOffset = y * Width;
			for (var x = minX; x <= maxX; x++)
			{
				int dx = x - (int)center.X;
				int dy = y - (int)center.Y;
				var distanceSquared = dx * dx + dy * dy;

				if (distanceSquared > outerRadiusSquared)
					continue;

				if (distanceSquared <= innerRadiusSquared)
				{
					_data![rowOffset + x] = color;
					continue;
				}

				// Calculate dithering threshold from 0.0 to 1.0  
				var normalizedDistance = (distanceSquared - innerRadiusSquared) / (outerRadiusSquared - innerRadiusSquared);

				// Get the appropriate threshold from the Bayer matrix (0-15, normalized to 0.0-1.0)  
				var bayerX = Math.Abs(dx) % 4;
				var bayerY = Math.Abs(dy) % 4;
				var threshold = bayerMatrix[bayerY, bayerX] / 16.0f;

				// Draw pixel if the normalized distance is less than the threshold  
				if (normalizedDistance < threshold)
				{
					_data![rowOffset + x] = color;
				}
				else if (secondaryColor != null)
				{
					_data![rowOffset + x] = secondaryColor;
				}
			}
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void RenderCircle(Point center, int radius, RadialColor color)
	{
		var xc = center.X;
		var yc = center.Y;
		int x = 0;
		int y = radius;
		int d = 3 - (radius << 1);

		while (y >= x)
		{
			RenderCirclePoints(xc, yc, x, y, color);
			x++;

			// Check for decision parameter and correspondingly update d, x, y.  
			if (d > 0)
			{
				y--;
				d += ((x - y) << 2) + 10;
			}
			else
			{
				d += (x << 2) + 6;
			}
		}
	}

	/// <summary>  
	/// Helper method to render the eight symmetrical points of a circle.  
	/// </summary>  
	private void RenderCirclePoints(int xc, int yc, int x, int y, RadialColor color)
	{
		SetPixel(new Point(xc + x, yc + y), color);
		SetPixel(new Point(xc + x, yc - y), color);
		SetPixel(new Point(xc - x, yc + y), color);
		SetPixel(new Point(xc - x, yc - y), color);
		SetPixel(new Point(xc + y, yc + x), color);
		SetPixel(new Point(xc + y, yc - x), color);
		SetPixel(new Point(xc - y, yc + x), color);
		SetPixel(new Point(xc - y, yc - x), color);
	}

	/// <inheritdoc/>
	public void RenderFilledCircle(Point center, int radius, RadialColor color)
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");

		var xc = center.X;
		var yc = center.Y;

		// Clip to bounds for optimization  
		int minX = Math.Max(0, xc - radius);
		int maxX = Math.Min(Width - 1, xc + radius);
		int minY = Math.Max(0, yc - radius);
		int maxY = Math.Min(Height - 1, yc + radius);

		int radiusSquared = radius * radius;

		// Use a more efficient algorithm that avoids redundant calculations  
		for (int y = minY; y <= maxY; y++)
		{
			int dy = y - yc;
			int dy2 = dy * dy;
			int rowOffset = y * Width;

			for (int x = minX; x <= maxX; x++)
			{
				int dx = x - xc;
				if (dx * dx + dy2 <= radiusSquared)
				{
					_data![rowOffset + x] = color;
				}
			}
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void FloodFill(Point pnt, RadialColor color)
	{
		var x = pnt.X;
		var y = pnt.Y;

		// Check if the starting point is valid  
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return;

		var targetColor = GetPixel(pnt);

		// If the target color is already the fill color, nothing to do  
		if (targetColor == color)
			return;

		// Use a more memory-efficient algorithm with a stack instead of a queue  
		// and use a span-filling approach to reduce the number of stack operations  
		Stack<(int X, int Y)> stack = new Stack<(int X, int Y)>();
		stack.Push((x, y));

		while (stack.Count > 0)
		{
			var (px, py) = stack.Pop();

			// Find the leftmost and rightmost pixels of the current span  
			int leftX = px;
			while (leftX > 0 && GetPixel(new Point(leftX - 1, py)) == targetColor)
				leftX--;

			int rightX = px;
			while (rightX < Width - 1 && GetPixel(new Point(rightX + 1, py)) == targetColor)
				rightX++;

			// Fill the span  
			for (int i = leftX; i <= rightX; i++)
				SetPixel(new Point(i, py), color);

			// Check spans above and below  
			CheckFillSpan(leftX, rightX, py - 1, targetColor, color, stack);
			CheckFillSpan(leftX, rightX, py + 1, targetColor, color, stack);
		}
	}

	/// <summary>  
	/// Helper method for flood fill to check and queue spans to fill.  
	/// </summary>  
	private void CheckFillSpan(int leftX, int rightX, int y, RadialColor targetColor, RadialColor fillColor, Stack<(int X, int Y)> stack)
	{
		if (y < 0 || y >= Height)
			return;

		bool inSpan = false;

		for (int x = leftX; x <= rightX; x++)
		{
			if (!inSpan && GetPixel(new Point(x, y)) == targetColor)
			{
				stack.Push((x, y));
				inSpan = true;
			}
			else if (inSpan && GetPixel(new Point(x, y)) != targetColor)
			{
				inSpan = false;
			}
		}
	}

	/// <summary>  
	/// Updates the virtual display with the current pixel data if it has changed.  
	/// </summary>  
	public void Present()
	{
		if (!_isInitialized) throw new InvalidOperationException("Rendering context is not initialized.");
		if (_isDirty)
		{
			_bus.Publish(new AppFbWriteSpan(0, 0, _data!));
			_isDirty = false;
		}
	}

	#endregion
}