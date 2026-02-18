using System.Drawing;
using Userland.Morphic.Events;

namespace Userland.Morphic.Layout;

public sealed class HorizontalScrollThumbMorph(
	Func<int> getMaxScroll,
	Action<int> setScroll
) : ScrollThumbMorph(getMaxScroll, setScroll)
{
	protected override void UpdateLayout()
	{
		var style = Style ?? throw new NullReferenceException("Style is missing.");
		Size = new Size(Size.Width, style.DefaultFontStyle.TileSize.Height + Padding * 2);
		base.UpdateLayout();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		_dragging = true;
		_dragOffset = e.Position.X - Position.X;
		GetWorld()?.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);
		if (!_dragging || Owner == null) return;

		var track = Owner;
		var newX = e.Position.X - track.Position.X - _dragOffset;

		var maxThumbX = Math.Max(0, track.Size.Width - Size.Width);
		newX = Math.Clamp(newX, 0, maxThumbX);

		Position = new Point(newX, Position.Y);

		var maxScroll = _getMaxScroll();
		if (maxScroll > 0 && maxThumbX > 0)
		{
			var scrollX = (int)((float)newX / maxThumbX * maxScroll);
			_setScroll(scrollX);
		}

		e.MarkHandled();
	}
}