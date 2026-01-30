using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Morphic;

public sealed class BoxMorph : Morph
{
	public BoxMorph(Point position, Size size)
	{
		Position = position;
		Size = size;
		FillColor = RadialColor.DarkGray;
		BorderColor = RadialColor.Gray;
	}

	public RadialColor FillColor { get; set; }
	public RadialColor BorderColor { get; set; }

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);
		rc.RenderFilledRect(new Rectangle(new Point(0, 0), Size), FillColor);
		rc.RenderRect(new Rectangle(new Point(0, 0), Size), BorderColor);
	}
}
