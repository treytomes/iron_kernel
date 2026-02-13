using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;

namespace Userland.Morphic.Handles;

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

		_icon = new ImageMorph(new Point(0, 0), "asset://image.move_icon")
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

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);

		if (StyleForHandle == null) return;

		// TODO: I don't like needing to interrogate child morphs for the IsHovered property.  I need a better way.
		var bg = IsEffectivelyHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(new Point(0, 0), Size), bg);

		_icon.Position = new Point(0, 0);
		_icon.Size = Size;
		_icon.Foreground = IsEffectivelyHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;
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
