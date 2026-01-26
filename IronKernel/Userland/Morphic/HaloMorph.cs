using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Handles;

namespace IronKernel.Userland.Morphic;

public sealed class HaloMorph : Morph
{
	private readonly Morph _target;

	public HaloMorph(Morph target)
	{
		_target = target;
		Visible = true;

		AddMorph(new ResizeHandleMorph(target, ResizeHandle.TopLeft));
		AddMorph(new ResizeHandleMorph(target, ResizeHandle.TopRight));
		AddMorph(new ResizeHandleMorph(target, ResizeHandle.BottomLeft));
		AddMorph(new ResizeHandleMorph(target, ResizeHandle.BottomRight));

		AddMorph(new MoveHandleMorph(target)
		{
			Position = new Point(
				target.Position.X + target.Size.Width / 2 - 6,
				target.Position.Y - 16
			)
		});
	}

	public override bool IsSelectable => false;

	public override void Draw(IRenderingContext rc)
	{
		UpdateFromTarget();

		rc.RenderRect(
			new Rectangle(Position, Size),
			RadialColor.Yellow,
			1);

		base.Draw(rc);
	}

	private void UpdateFromTarget()
	{
		Position = _target.Position;
		Size = _target.Size;

		LayoutHandles();
	}

	private void LayoutHandles()
	{
		int hs = 3;

		foreach (var m in Submorphs.OfType<ResizeHandleMorph>())
		{
			m.Position = m.Kind switch
			{
				ResizeHandle.TopLeft =>
					new Point(Position.X - hs, Position.Y - hs),

				ResizeHandle.TopRight =>
					new Point(Position.X + Size.Width - hs, Position.Y - hs),

				ResizeHandle.BottomLeft =>
					new Point(Position.X - hs, Position.Y + Size.Height - hs),

				ResizeHandle.BottomRight =>
					new Point(Position.X + Size.Width - hs, Position.Y + Size.Height - hs),

				_ => m.Position
			};
		}

		Submorphs.OfType<MoveHandleMorph>().Single().Position = new Point(
			Position.X + Size.Width / 2 - 6,
			Position.Y - 16
		);
	}
}
