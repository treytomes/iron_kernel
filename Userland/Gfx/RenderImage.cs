using System.Drawing;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;

namespace Userland.Gfx;

public sealed class RenderImage
	: Image, IImage<RenderImage>
{
	private RadialColor?[]? _flipBuffer;

	public RenderImage(Image image)
		: base(image.Size.Width, image.Size.Height, image.Data, 1)
	{
	}

	public RenderImage(int width, int height)
		: base(width, height)
	{
	}

	public void Render(IRenderingContext rc, Point position, RenderFlag flags = RenderFlag.None)
	{
		var flipH = (flags & RenderFlag.FlipHorizontal) != 0;
		var flipV = (flags & RenderFlag.FlipVertical) != 0;

		if (!flipH)
		{
			// Fast path: each source row is a contiguous slice of Data.
			for (var dy = 0; dy < Size.Height; dy++)
			{
				var sy = flipV ? (Size.Height - 1 - dy) : dy;
				rc.RenderSpan(position.X, position.Y + dy, Data.AsSpan(sy * Size.Width, Size.Width));
			}
		}
		else
		{
			// Flip-horizontal: reverse each row into a cached buffer (allocated once).
			_flipBuffer ??= new RadialColor?[Size.Width];
			var tmp = _flipBuffer;
			for (var dy = 0; dy < Size.Height; dy++)
			{
				var sy = flipV ? (Size.Height - 1 - dy) : dy;
				var srcRow = Data.AsSpan(sy * Size.Width, Size.Width);
				for (var i = 0; i < Size.Width; i++)
					tmp[i] = srcRow[Size.Width - 1 - i];
				rc.RenderSpan(position.X, position.Y + dy, tmp);
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