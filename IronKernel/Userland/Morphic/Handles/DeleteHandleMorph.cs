using System.Drawing;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

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

		_icon = new ImageMorph("image.delete_icon");
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.DeleteHandle;

	#endregion

	#region Methods

	public override void Draw(IRenderingContext rc)
	{
		if (StyleForHandle == null) return;

		// TODO: I don't like needing to interrogate child morphs for the IsHovered property.  I need a better way.
		var bg = IsHovered || _icon.IsHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		_icon.Position = Position;
		_icon.Size = Size;
		_icon.Foreground = IsHovered || _icon.IsHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;

		rc.RenderFilledRect(new Rectangle(Position, Size), bg);

		base.Draw(rc);
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
