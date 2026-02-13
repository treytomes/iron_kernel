using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic.Handles;

public sealed class DeleteHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public DeleteHandleMorph(Morph target)
		: base(target)
	{
		Size = new Size(8, 8);

		_icon = new ImageMorph(new Point(0, 0), "asset://image.delete_icon")
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

		// TODO: I don't like needing to interrogate child morphs for the IsHovered property.  I need a better way.
		var bg = IsEffectivelyHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		_icon.Position = new Point(0, 0);
		_icon.Size = Size;
		_icon.Foreground = IsEffectivelyHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;

		rc.RenderFilledRect(new Rectangle(new Point(0, 0), Size), bg);
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
