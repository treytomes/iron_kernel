using System.Drawing;
using IronKernel.Userland.Gfx;
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

		var pos = StartPosition;
		var size = StartSize;

		switch (Kind)
		{
			case ResizeHandle.TopLeft:
				pos = new Point(pos.X + dx, pos.Y + dy);
				size = new Size(size.Width - dx, size.Height - dy);
				break;

			case ResizeHandle.TopRight:
				pos = new Point(pos.X, pos.Y + dy);
				size = new Size(size.Width + dx, size.Height - dy);
				break;

			case ResizeHandle.BottomLeft:
				pos = new Point(pos.X + dx, pos.Y);
				size = new Size(size.Width - dx, size.Height + dy);
				break;

			case ResizeHandle.BottomRight:
				size = new Size(size.Width + dx, size.Height + dy);
				break;
		}

		Target.Position = pos;
		Target.Size = new Size(
			Math.Max(1, size.Width),
			Math.Max(1, size.Height));

		e.MarkHandled();
	}

	#endregion
}
