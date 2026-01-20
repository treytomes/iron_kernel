using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland;

namespace IronKernel.Morphic;

public sealed class BoxMorph : Morph
{
	public BoxMorph(Point position, Size size, RadialColor color)
	{
		Position = position;
		Size = size;
		Color = color;
	}

	public RadialColor Color { get; set; }

	public override void Draw(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Position, Size), Color);
		base.Draw(rc);
	}
}
