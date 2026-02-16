using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public abstract class HandleMorph : Morph
{
	#region Fields

	private readonly ImageMorph _icon;

	protected readonly Morph Target;
	protected Point StartMouse;
	protected Point StartPosition;
	protected Size StartSize;

	#endregion

	#region Constructors

	protected HandleMorph(Morph target)
	{
		Target = target;
		Visible = true;
		IsSelectable = false;

		_icon = new ImageMorph(Point.Empty, AssetUrl)
		{
			IsSelectable = false,
			Flags = RenderFlag,
		};
		AddMorph(_icon);
	}

	#endregion

	#region Properties

	public override bool WantsKeyboardFocus => false;
	public override bool IsGrabbable => false;
	protected abstract MorphicStyle.HandleStyle? StyleForHandle { get; }
	protected abstract string AssetUrl { get; }
	protected virtual RenderImage.RenderFlag RenderFlag => RenderImage.RenderFlag.None;

	#endregion

	#region Methods

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (!TryGetWorld(out var world)) return;

		StartMouse = e.Position;
		StartPosition = Target.Position;
		StartSize = Target.Size;

		world.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		if (TryGetWorld(out var world)) world.ReleasePointer(this);
		e.MarkHandled();
		base.OnPointerUp(e);
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);

		if (StyleForHandle == null) return;

		var bg = IsEffectivelyHovered
			? StyleForHandle.BackgroundHover
			: StyleForHandle.Background;

		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), bg);
		rc.RenderRect(new Rectangle(Point.Empty, Size), bg.Lerp(RadialColor.Black, 0.25f));

		_icon.Foreground = IsEffectivelyHovered
			? StyleForHandle.ForegroundHover
			: StyleForHandle.Foreground;
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Size = _icon.Size;
	}

	#endregion
}
