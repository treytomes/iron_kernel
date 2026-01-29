using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

public sealed class MoveHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public MoveHandleMorph(Morph target)
		: base(target)
	{
		Size = new Size(8, 8);

		_icon = new ImageMorph(new Point(0, 0), "image.move_icon")
		{
			IsSelectable = false,
		};
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.MoveHandle;

	#endregion

	#region Methods

	public override void Draw(IRenderingContext rc)
	{
		if (StyleForHandle == null) return;

		// TODO: I don't like needing to interrogate child morphs for the IsHovered property.  I need a better way.
		var bg = IsHovered || _icon.IsHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(Position, Size), bg);

		_icon.Position = Position;
		_icon.Size = Size;
		_icon.Foreground = IsHovered || _icon.IsHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;

		base.Draw(rc);
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		var dx = e.Position.X - StartMouse.X;
		var dy = e.Position.Y - StartMouse.Y;

		if (dx != 0 || dy != 0)
		{
			if (TryGetWorld(out var world))
			{
				world.Commands.Submit(new MoveCommand(Target, dx, dy));
				StartMouse = e.Position;
			}
		}

		e.MarkHandled();
	}

	#endregion
}
