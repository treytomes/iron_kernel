using System.Drawing;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Gfx;

public sealed class RenderImage
	: Image, IImage<RenderImage>
{
	private Color?[]? _rowBuffer;

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

		_rowBuffer ??= new Color?[Size.Width];
		var buf = _rowBuffer;

		for (var dy = 0; dy < Size.Height; dy++)
		{
			var sy = flipV ? (Size.Height - 1 - dy) : dy;
			var srcRow = Data.AsSpan(sy * Size.Width, Size.Width);

			if (!flipH)
			{
				srcRow.CopyTo(buf);
			}
			else
			{
				for (var i = 0; i < Size.Width; i++)
					buf[i] = srcRow[Size.Width - 1 - i];
			}

			rc.RenderSpan(position.X, position.Y + dy, buf);
		}
	}

	/// <summary>
	/// Render with per-pixel tinting: opaque pixels are multiplied by fgColor, transparent pixels use bgColor.
	/// fgColor=White (default) reproduces the original image colors unchanged.
	/// </summary>
	public void Render(IRenderingContext rc, Point position, Color? fgColor, Color? bgColor)
	{
		_rowBuffer ??= new Color?[Size.Width];
		var buf = _rowBuffer;
		var w = Size.Width;

		for (var dy = 0; dy < Size.Height; dy++)
		{
			var srcRow = Data.AsSpan(dy * w, w);
			for (var dx = 0; dx < w; dx++)
			{
				var src = srcRow[dx];
				if (src.HasValue)
				{
					buf[dx] = fgColor.HasValue
						? new Color(src.Value.R * fgColor.Value.R,
						            src.Value.G * fgColor.Value.G,
						            src.Value.B * fgColor.Value.B)
						: src;
				}
				else
				{
					buf[dx] = bgColor;
				}
			}
			rc.RenderSpan(position.X, position.Y + dy, buf);
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
