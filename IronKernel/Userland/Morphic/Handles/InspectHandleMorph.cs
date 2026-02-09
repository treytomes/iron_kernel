using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;
using IronKernel.Userland.Morphic.Inspector;

namespace IronKernel.Userland.Morphic.Handles;

public sealed class InspectHandleMorph : HandleMorph
{
	#region Fields

	private readonly ImageMorph _icon;

	#endregion

	#region Constructors

	public InspectHandleMorph(Morph target)
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

		_icon.Position = new Point(0, 0);
		_icon.Size = Size;
		_icon.Foreground = IsEffectivelyHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;

		rc.RenderFilledRect(new Rectangle(new Point(0, 0), Size), bg);
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
