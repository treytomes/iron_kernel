using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public sealed class MoveHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public MoveHandleMorph(Morph target)
		: base(target)
	{
		_icon = new ImageMorph(Point.Empty, "asset://image.move_icon")
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

		var bg = IsEffectivelyHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), bg);

		_icon.Foreground = IsEffectivelyHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Size = _icon.Size;
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
