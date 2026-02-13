using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic.Layout;

public sealed class HorizontalScrollThumbMorph : Morph
{
	private bool _dragging;
	private int _dragOffsetX;
	private readonly Func<int> _getMaxScroll;
	private readonly Action<int> _setScroll;

	public HorizontalScrollThumbMorph(
		Func<int> getMaxScroll,
		Action<int> setScroll)
	{
		_getMaxScroll = getMaxScroll;
		_setScroll = setScroll;

		Size = new Size(16, 10);
		IsSelectable = true;
	}

	private int Padding => 2;
	public RadialColor FillColor => new RadialColor(0, 5, 0);

	protected override void UpdateLayout()
	{
		var style = Style ?? throw new NullReferenceException("Style is missing.");
		// Size = new Size(style.DefaultFontStyle.TileSize.Width * 2 + Padding * 2, style.DefaultFontStyle.TileSize.Height + Padding * 2);
		Size = new Size(Size.Width, style.DefaultFontStyle.TileSize.Height + Padding * 2);
		base.UpdateLayout();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		_dragging = true;
		_dragOffsetX = e.Position.X - Position.X;
		GetWorld()?.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);
		if (!_dragging) return;

		var track = Owner!;
		var newX = e.Position.X - track.Position.X - _dragOffsetX;

		var maxThumbX = track.Size.Width - Size.Width;
		newX = Math.Clamp(newX, 0, maxThumbX);

		Position = new Point(newX, Position.Y);

		// Map thumb position → scroll offset
		var maxScroll = _getMaxScroll();
		if (maxThumbX > 0)
		{
			var scrollX = (int)((float)newX / maxThumbX * maxScroll);
			_setScroll(scrollX);
		}

		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		base.OnPointerUp(e);
		_dragging = false;
		GetWorld()?.CapturePointer(null);
		e.MarkHandled();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), FillColor);
		base.DrawSelf(rc);
	}
}