using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland;

namespace IronKernel.Morphic;

public sealed class BoxMorph : DraggableMorph
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

	public override void Draw(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Position, Size), FillColor);
		rc.RenderRect(new Rectangle(Position, Size), BorderColor);
		base.Draw(rc);
	}
}
