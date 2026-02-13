using System.Drawing;
using IronKernel.Common;

namespace Userland.Gfx;

public sealed class RenderImage
	: Image, IImage<RenderImage>
{
	public RenderImage(Image image)
		: base(image.Size.Width, image.Size.Height, image.Data, 1)
	{
	}

	public void Render(IRenderingContext rc, Point position, RenderFlag flags = RenderFlag.None)
	{
		var x = position.X;
		var y = position.Y;

		var flipH = (flags & RenderFlag.FlipHorizontal) != 0;
		var flipV = (flags & RenderFlag.FlipVertical) != 0;

		for (var dy = 0; dy < Size.Height; dy++)
		{
			var dstY = y + dy;
			if (dstY < 0)
				continue;
			if (dstY >= rc.Size.Height)
				break;

			var sy = flipV ? (Size.Height - 1 - dy) : dy;

			for (var dx = 0; dx < Size.Width; dx++)
			{
				var dstX = x + dx;
				if (dstX < 0)
					continue;
				if (dstX >= rc.Size.Width)
					break;

				var sx = flipH ? (Size.Width - 1 - dx) : dx;

				var color = GetPixel(sx, sy);
				if (color != null)
				{
					rc.SetPixel(new Point(dstX, dstY), color);
				}
			}
		}
	}

	RenderImage IImage<RenderImage>.Crop(int x, int y, int width, int height)
	{
		return new RenderImage(Crop(x, y, width, height));
	}

	RenderImage IImage<RenderImage>.Scale(int factor)
	{
		return new RenderImage(Scale(factor));
	}

	[Flags]
	public enum RenderFlag
	{
		None = 0,
		FlipHorizontal = 1,
		FlipVertical = 2,
	}
}