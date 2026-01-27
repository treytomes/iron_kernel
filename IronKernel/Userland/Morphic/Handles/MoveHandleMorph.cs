using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

public sealed class MoveHandleMorph : HandleMorph
{
	#region Fields

	private RenderImage? _image;

	#endregion

	#region Constructors

	public MoveHandleMorph(Morph target)
		: base(target)
	{
		Size = new Size(8, 8);
	}

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		_ = LoadImageAsync(assets);
	}

	private async Task LoadImageAsync(IAssetService assets)
	{
		_image = await assets.LoadImageAsync("image.move_icon");
		_image.Recolor(RadialColor.Black, null);
	}

	public override void Draw(IRenderingContext rc)
	{
		rc.RenderFilledRect(
			new Rectangle(Position, Size),
			IsHovered ? RadialColor.Orange : RadialColor.Orange.Lerp(RadialColor.White, 0.5f));
		_image?.Render(rc, Position);
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		var dx = e.Position.X - StartMouse.X;
		var dy = e.Position.Y - StartMouse.Y;

		Target.Position = new Point(
			StartPosition.X + dx,
			StartPosition.Y + dy);

		e.MarkHandled();
	}

	#endregion
}
