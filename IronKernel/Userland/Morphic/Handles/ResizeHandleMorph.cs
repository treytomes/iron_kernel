using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

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
		Size = new Size(6, 6);

		_icon = new ImageMorph(new Point(0, 0), "image.resize_icon")
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

	public override void Draw(IRenderingContext rc)
	{
		if (StyleForHandle == null) return;

		var bg = IsEffectivelyHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(Position, Size), bg);

		_icon.Position = Position;
		_icon.Size = Size;
		_icon.Foreground = IsEffectivelyHovered
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
				world.Commands.Submit(new ResizeCommand(Target, Kind, dx, dy));
				StartMouse = e.Position;
			}
		}

		e.MarkHandled();
	}

	#endregion
}
