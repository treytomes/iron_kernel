using System.Drawing;
using Userland.Morphic.Events;

namespace Userland.Morphic.Layout;

public sealed class VerticalScrollThumbMorph(
	Func<int> getMaxScroll,
	Action<int> setScroll
) : ScrollThumbMorph(getMaxScroll, setScroll)
{
	protected override void UpdateLayout()
	{
		var style = Style ?? throw new NullReferenceException("Style is missing.");
		Size = new Size(style.DefaultFontStyle.TileSize.Width + Padding * 2, Size.Height);
		base.UpdateLayout();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		_dragging = true;
		_dragOffset = e.Position.Y - Position.Y;
		GetWorld()?.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);
		if (!_dragging || Owner == null) return;

		var track = Owner;
		var newY = e.Position.Y - track.Position.Y - _dragOffset;

		var maxThumbY = Math.Max(0, track.Size.Height - Size.Height);
		newY = Math.Clamp(newY, 0, maxThumbY);

		Position = new Point(Position.X, newY);

		var maxScroll = _getMaxScroll();
		if (maxScroll > 0 && maxThumbY > 0)
		{
			var scrollY = (int)((float)newY / maxThumbY * maxScroll);
			_setScroll(scrollY);
		}

		e.MarkHandled();
	}
}