using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

public sealed class ResizeHandleMorph : HandleMorph
{
	#region Fields

	private RenderImage? _image;

	#endregion

	#region Constructors

	public ResizeHandleMorph(Morph target, ResizeHandle kind)
		: base(target)
	{
		Kind = kind;
		Size = new Size(6, 6);
	}

	#endregion

	#region Properties

	public ResizeHandle Kind { get; }
	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.ResizeHandle;

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		_ = LoadImageAsync(assets);
	}

	private async Task LoadImageAsync(IAssetService assets)
	{
		_image = await assets.LoadImageAsync("image.resize_icon");
		_image.Recolor(RadialColor.Black, null);
		_image.Recolor(RadialColor.White, StyleForHandle?.Foreground);
	}

	public override void Draw(IRenderingContext rc)
	{
		if (StyleForHandle == null) return;

		var bg = IsHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(Position, Size), bg);

		var flipVertical = Kind == ResizeHandle.TopLeft || Kind == ResizeHandle.BottomRight;
		var flags = flipVertical ? RenderImage.RenderFlag.FlipVertical : RenderImage.RenderFlag.None;

		_image?.Render(rc, Position, flags);
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
