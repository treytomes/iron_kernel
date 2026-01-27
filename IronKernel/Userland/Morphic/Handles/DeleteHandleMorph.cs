using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

public sealed class DeleteHandleMorph : HandleMorph
{
	#region Fields

	private RenderImage? _image;

	#endregion

	#region Constructors

	public DeleteHandleMorph(Morph target)
		: base(target)
	{
		Size = new Size(8, 8);
	}

	#endregion

	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.DeleteHandle;

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		_ = LoadImageAsync(assets);
	}

	private async Task LoadImageAsync(IAssetService assets)
	{
		_image = await assets.LoadImageAsync("image.delete_icon");
		_image.Recolor(RadialColor.Black, null);
	}

	public override void Draw(IRenderingContext rc)
	{
		if (StyleForHandle == null) return;

		var bg = IsHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(Position, Size), bg);
		_image?.Render(rc, Position);
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		var owner = Target.Owner;
		if (owner == null) return;
		Target.MarkForDeletion();
		e.MarkHandled();
	}

	#endregion
}
