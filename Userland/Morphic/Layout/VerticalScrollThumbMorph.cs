using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Layout;

public sealed class VerticalScrollThumbMorph : Morph
{
	private bool _dragging;
	private int _dragOffsetY;

	private readonly Func<int> _getMaxScroll;
	private readonly Func<int> _getViewportHeight;
	private readonly Action<int> _setScroll;

	public VerticalScrollThumbMorph(
		Func<int> getMaxScroll,
		Func<int> getViewportHeight,
		Action<int> setScroll)
	{
		_getMaxScroll = getMaxScroll;
		_getViewportHeight = getViewportHeight;
		_setScroll = setScroll;

		Size = new Size(10, 16);
		IsSelectable = true;
	}

	private int Padding => 2;
	public RadialColor FillColor => new RadialColor(0, 5, 0);

	protected override void UpdateLayout()
	{
		var style = Style ?? throw new NullReferenceException("Style is missing.");
		// Size = new Size(style.DefaultFontStyle.TileSize.Width + Padding * 2, style.DefaultFontStyle.TileSize.Height * 2 + Padding * 2);
		Size = new Size(style.DefaultFontStyle.TileSize.Width + Padding * 2, Size.Height);
		base.UpdateLayout();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		_dragging = true;
		_dragOffsetY = e.Position.Y - Position.Y;

		if (TryGetWorld(out var world))
			world.CapturePointer(this);

		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);
		if (!_dragging || Owner == null) return;

		var track = Owner;
		var newY = e.Position.Y - track.Position.Y - _dragOffsetY;

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

	public override void OnPointerUp(PointerUpEvent e)
	{
		base.OnPointerUp(e);

		_dragging = false;
		if (TryGetWorld(out var world))
			world.ReleasePointer(this);

		e.MarkHandled();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), FillColor);
		base.DrawSelf(rc);
	}
}