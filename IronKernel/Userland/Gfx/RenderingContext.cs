using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.ApplicationHost;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace IronKernel.Userland.Gfx;

/// <inheritdoc/>
public sealed class RenderingContext(IApplicationBus bus) : IRenderingContext
{
	#region Fields  

	private readonly IApplicationBus _bus = bus;
	private bool _isDirty = true;
	private RadialColor[]? _data = null;

	// Transformation and clipping stack.
	private readonly Stack<Point> _offsetStack = new();
	private Point _currentOffset = Point.Empty;

	private readonly Stack<Rectangle> _clipStack = new();
	private Rectangle? _currentClip = null;

	#endregion

	#region Properties

	/// <inheritdoc/>
	public Size Size { get; private set; }

	/// <inheritdoc/>
	public Rectangle Bounds => new(new Point(0, 0), Size);

	#endregion

	#region Methods

	public void ResetTransform()
	{
		_offsetStack.Clear();
		_currentOffset = Point.Empty;

		_clipStack.Clear();
		_currentClip = new Rectangle(0, 0, Bounds.Width, Bounds.Height);
	}

	public int PushOffset(Point offset)
	{
		var size = _offsetStack.Count;
		_offsetStack.Push(_currentOffset);
		_currentOffset = new Point(
			_currentOffset.X + offset.X,
			_currentOffset.Y + offset.Y);
		return size;
	}

	public void PopOffset()
	{
		if (_offsetStack.Count == 0)
			throw new InvalidOperationException($"{nameof(PopOffset)} without matching {nameof(PushOffset)}");
		_currentOffset = _offsetStack.Pop();
	}

	public int PushClip(Rectangle rect)
	{
		var size = _clipStack.Count;
		// rect is in *local* space → transform it
		var transformed = new Rectangle(
			rect.X + _currentOffset.X,
			rect.Y + _currentOffset.Y,
			rect.Width,
			rect.Height);

		_clipStack.Push(_currentClip ?? Bounds);

		_currentClip = Rectangle.Intersect(
			_clipStack.Peek(),
			transformed);

		return size;
	}

	public void PopClip()
	{
		if (_clipStack.Count == 0)
			throw new InvalidOperationException($"{nameof(PopClip)} without matching {nameof(PushClip)}");
		_currentClip = _clipStack.Pop();
	}

	public async Task InitializeAsync()
	{
		var response = await _bus.QueryAsync<AppFbInfoQuery, AppFbInfoResponse>(id => new AppFbInfoQuery(id));
		Size = response.Size;
		_data = new RadialColor[Size.Width * Size.Height];
		Array.Fill(_data, RadialColor.Black);
	}

	/// <inheritdoc/>
	public void Fill(RadialColor color)
	{
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
		var x = pnt.X + _currentOffset.X;
		var y = pnt.Y + _currentOffset.Y;

		if (_currentClip.HasValue && !_currentClip.Value.Contains(x, y))
			return;

		if (x < 0 || x >= Size.Width || y < 0 || y >= Size.Height)
			return;

		_data![y * Size.Width + x] = color;
		_isDirty = true;
	}

	/// <inheritdoc/> 
	public RadialColor GetPixel(Point pnt)
	{
		var x = pnt.X + _currentOffset.X;
		var y = pnt.Y + _currentOffset.Y;

		if (IsClipped(x, y))
			return RadialColor.Black;

		if (x < 0 || x >= Size.Width || y < 0 || y >= Size.Height)
			return RadialColor.Black;

		return _data![y * Size.Width + x];
	}

	/// <inheritdoc/> 
	public void RenderFilledRect(Rectangle rect, RadialColor color)
	{
		var x0 = rect.Left + _currentOffset.X;
		var y0 = rect.Top + _currentOffset.Y;
		var x1 = rect.Right + _currentOffset.X;
		var y1 = rect.Bottom + _currentOffset.Y;

		var clip = _currentClip ?? Bounds;

		x0 = Math.Max(x0, clip.Left);
		y0 = Math.Max(y0, clip.Top);
		x1 = Math.Min(x1, clip.Right);
		y1 = Math.Min(y1, clip.Bottom);

		for (var y = y0; y < y1; y++)
		{
			if (y < 0 || y >= Size.Height) continue;
			var rowOffset = y * Size.Width;

			for (var x = x0; x < x1; x++)
			{
				if (x < 0 || x >= Size.Width) continue;
				_data![rowOffset + x] = color;
			}
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void RenderRect(Rectangle rect, RadialColor color, int thickness = 1)
	{
		// Draw multiple concentric rectangles to achieve the desired thickness
		for (var i = 0; i < thickness; i++)
		{
			// Draw the four sides of the rectangle
			RenderHLine(new Point(rect.Left, rect.Top + i), rect.Width, color);  // Top
			RenderHLine(new Point(rect.Left, rect.Bottom - i - 1), rect.Width, color);  // Bottom
			RenderVLine(new Point(rect.Left + i, rect.Top), rect.Height, color);  // Left
			RenderVLine(new Point(rect.Right - i - 1, rect.Top), rect.Height, color);  // Right
		}
	}

	/// <inheritdoc/>
	public void RenderHLine(Point pnt, int len, RadialColor color)
	{
		var y = pnt.Y + _currentOffset.Y;
		var clip = _currentClip ?? Bounds;

		// ✅ clip Y
		if (y < clip.Top || y >= clip.Bottom)
			return;

		var xStart = pnt.X + _currentOffset.X;
		var xEnd = xStart + len;

		xStart = Math.Max(xStart, clip.Left);
		xEnd = Math.Min(xEnd, clip.Right);

		var rowOffset = y * Size.Width;
		for (var x = xStart; x < xEnd; x++)
		{
			if (x < 0 || x >= Size.Width) continue;
			_data![rowOffset + x] = color;
		}

		_isDirty = true;
	}

	/// <inheritdoc/>
	public void RenderVLine(Point pnt, int len, RadialColor color)
	{
		var x = pnt.X + _currentOffset.X;
		var clip = _currentClip ?? Bounds;

		// ✅ clip X
		if (x < clip.Left || x >= clip.Right)
			return;

		var yStart = pnt.Y + _currentOffset.Y;
		var yEnd = yStart + len;

		yStart = Math.Max(yStart, clip.Top);
		yEnd = Math.Min(yEnd, clip.Bottom);

		for (var y = yStart; y < yEnd; y++)
		{
			if (y < 0 || y >= Size.Height) continue;
			_data![y * Size.Width + x] = color;
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
		var cx = center.X + _currentOffset.X;
		var cy = center.Y + _currentOffset.Y;

		var clip = _currentClip ?? Bounds;

		int minX = Math.Max(cx - radius, clip.Left);
		int maxX = Math.Min(cx + radius, clip.Right - 1);
		int minY = Math.Max(cy - radius, clip.Top);
		int maxY = Math.Min(cy + radius, clip.Bottom - 1);

		for (var y = minY; y <= maxY; y++)
		{
			int rowOffset = y * Size.Width;
			for (var x = minX; x <= maxX; x++)
			{
				if (IsClipped(x, y)) continue;

				int dx = x - center.X;
				int dy = y - center.Y;
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

				// Draw pixel if the normalized distance is less than the threshold.
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
		var cx = center.X + _currentOffset.X;
		var cy = center.Y + _currentOffset.Y;

		var clip = _currentClip ?? Bounds;

		int minX = Math.Max(cx - radius, clip.Left);
		int maxX = Math.Min(cx + radius, clip.Right - 1);
		int minY = Math.Max(cy - radius, clip.Top);
		int maxY = Math.Min(cy + radius, clip.Bottom - 1);

		int radiusSquared = radius * radius;

		for (int y = minY; y <= maxY; y++)
		{
			if (y < 0 || y >= Size.Height) continue;
			int dy = y - cy;
			int dy2 = dy * dy;
			int rowOffset = y * Size.Width;

			for (int x = minX; x <= maxX; x++)
			{
				if (x < 0 || x >= Size.Width) continue;
				int dx = x - cx;

				if (dx * dx + dy2 <= radiusSquared)
					_data![rowOffset + x] = color;
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
		if (x < 0 || x >= Size.Width || y < 0 || y >= Size.Height)
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
			while (rightX < Size.Width - 1 && GetPixel(new Point(rightX + 1, py)) == targetColor)
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
		if (y < 0 || y >= Size.Height)
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

	/// <inheritdoc/> 
	public void Present()
	{
		if (_isDirty)
		{
			_bus.Publish(new AppFbWriteRect(0, 0, Size.Width, Size.Height, _data!, true));
			// _bus.Publish(new AppFbWriteSpan(0, 0, _data!, true));
			_isDirty = false;
		}
	}

	private bool IsClipped(int x, int y)
	{
		return _currentClip.HasValue && !_currentClip.Value.Contains(x, y);
	}

	#endregion
}