using System.Drawing;
using IronKernel.Common;

namespace IronKernel.Userland;

public sealed class RenderImage
	: Image
{
	public RenderImage(Image image)
		: base(image.Width, image.Height, image.Data, 1)
	{
	}

	public void Render(IRenderingContext rc, Point position)
	{
		Render(rc, (int)position.X, (int)position.Y);
	}

	/// <summary>
	/// A value of 255 in either color indicates transparent.
	/// </summary>
	public void Render(IRenderingContext rc, int x, int y)
	{
		for (var dy = 0; dy < Height; dy++)
		{
			if (y + dy < 0)
			{
				continue;
			}
			if (y + dy >= rc.Height)
			{
				break;
			}
			for (var dx = 0; dx < Width; dx++)
			{
				if (x + dx < 0)
				{
					continue;
				}
				if (x + dx >= rc.Width)
				{
					break;
				}
				var color = GetPixel(dx, dy);
				if (color != null)
				{
					rc.SetPixel(new Point(x + dx, y + dy), color);
				}
			}
		}
	}
}