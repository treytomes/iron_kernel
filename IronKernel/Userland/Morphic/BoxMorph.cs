using System.Drawing;
using IronKernel.Common.ValueObjects;

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

	public override void Draw(IMorphicCanvas canvas)
	{
		canvas.DrawRect(Position.X, Position.Y, Size.Width, Size.Height, Color);
		base.Draw(canvas);
	}
}
