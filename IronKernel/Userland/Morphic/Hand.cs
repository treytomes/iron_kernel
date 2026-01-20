using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Morphic;

public sealed class HandMorph : Morph
{
	public HandMorph()
	{
		Size = new Size(1, 1);
	}

	public override void Draw(IMorphicCanvas canvas)
	{
		canvas.DrawPixel(Position.X, Position.Y, RadialColor.White);
	}
}
