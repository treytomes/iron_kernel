using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public sealed class ResizeHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public ResizeHandleMorph(Morph target, ResizeHandle kind)
		: base(target)
	{
		Kind = kind;

		_icon = new ImageMorph(Point.Empty, "asset://image.resize_icon")
		{
			IsSelectable = false,
			Flags = kind is ResizeHandle.TopLeft or ResizeHandle.BottomRight
				? RenderImage.RenderFlag.FlipVertical
				: RenderImage.RenderFlag.None
		};
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	public ResizeHandle Kind { get; }
	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.ResizeHandle;

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
				world.Commands.Submit(new ResizeCommand(Target, Kind, dx, dy));
				StartMouse = e.Position;
			}
		}

		e.MarkHandled();
	}

	#endregion
}
