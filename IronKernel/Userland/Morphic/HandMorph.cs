using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Morphic;

public sealed class HandMorph : Morph
{
	#region Fields

	private Point _grabOffset;
	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public HandMorph()
		: base()
	{
		_icon = new ImageMorph(new Point(0, 0), "image.mouse_cursor")
		{
			IsSelectable = false,
		};
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	public Morph? GrabbedMorph { get; private set; }

	#endregion

	#region Methods

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
		_icon.Position = Position;
		base.Draw(rc);
	}

	#endregion
}
