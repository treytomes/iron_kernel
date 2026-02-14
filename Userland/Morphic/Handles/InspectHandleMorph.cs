using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Morphic.Inspector;

namespace Userland.Morphic.Halo;

public sealed class InspectHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public InspectHandleMorph(Morph target)
		: base(target)
	{
		_icon = new ImageMorph(Point.Empty, "asset://image.inspect_icon")
		{
			IsSelectable = false,
		};
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.InspectHandle;

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
		if (TryGetWorld(out var world))
		{
			world.ClearSelection();
			var inspector = new InspectorMorph(Target);
			world.AddMorph(inspector);
			inspector.CenterOnOwner();
			e.MarkHandled();
		}
	}

	#endregion
}
