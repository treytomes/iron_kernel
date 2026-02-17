using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic.Controls;

sealed class SliderThumbMorph : Morph
{
	private readonly Func<float> _getNormalized;
	private readonly Action<float> _setNormalized;
	private bool _dragging;

	public SliderThumbMorph(
		Func<float> getNormalized,
		Action<float> setNormalized)
	{
		_getNormalized = getNormalized;
		_setNormalized = setNormalized;

		IsSelectable = true;
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (e.Button == MouseButton.Left)
		{
			_dragging = true;
			GetWorld()?.CapturePointer(this);
			e.MarkHandled();
		}
		base.OnPointerDown(e);
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		_dragging = false;
		GetWorld()?.CapturePointer(null);
		e.MarkHandled();
		base.OnPointerUp(e);
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		if (!_dragging || Owner == null)
			return;

		var local = Owner.WorldToLocal(e.Position);

		float trackWidth = Owner.Size.Width - Size.Width;
		float x = local.X - Size.Width / 2f;
		float t = x / trackWidth;

		_setNormalized(Math.Clamp(t, 0f, 1f));
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			IsPressed ? s.PrimaryActive :
			IsEffectivelyHovered ? s.PrimaryHover :
			s.Border);
		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			IsPressed ? s.PrimaryActive :
			IsEffectivelyHovered ? s.PrimaryHover :
			s.Border.Lerp(RadialColor.Black, 0.75f));
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		if (Style == null) return;
		Size = Style.DefaultFontStyle.TileSize;
	}
}