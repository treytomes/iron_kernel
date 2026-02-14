using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public sealed class DeleteHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public DeleteHandleMorph(Morph target)
		: base(target)
	{
		_icon = new ImageMorph(Point.Empty, "asset://image.delete_icon")
		{
			IsSelectable = false,
		};
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.DeleteHandle;

	#endregion

	#region Methods

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);

		if (StyleForHandle == null) return;

		var bg = IsEffectivelyHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		_icon.Foreground = IsEffectivelyHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;

		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), bg);
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Size = _icon.Size;
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		if (TryGetWorld(out var world)) world.ReleasePointer(this);

		var owner = Target.Owner;
		if (owner == null) return;
		Target.MarkForDeletion();
		e.MarkHandled();
	}

	#endregion
}
