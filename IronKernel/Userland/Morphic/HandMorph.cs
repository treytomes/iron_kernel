using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland;

namespace IronKernel.Userland.Morphic;

public sealed class HandMorph : Morph
{
	private Point _grabOffset;

	public Morph? GrabbedMorph { get; private set; }

	public void MoveTo(Point p)
	{
		Position = p;
	}

	public void Grab(Morph morph, Point at)
	{
		GrabbedMorph = morph;
		_grabOffset = new Point(
			at.X - morph.Position.X,
			at.Y - morph.Position.Y);
	}

	public void Release()
	{
		GrabbedMorph = null;
	}

	public void Update()
	{
		if (GrabbedMorph != null)
		{
			GrabbedMorph.Position = new Point(
				Position.X - _grabOffset.X,
				Position.Y - _grabOffset.Y);
		}
	}

	public override void Draw(IRenderingContext rc)
	{
		rc.RenderFilledCircle(Position, 2, RadialColor.Yellow);
	}
}
