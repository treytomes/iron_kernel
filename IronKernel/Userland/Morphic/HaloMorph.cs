using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland;

namespace IronKernel.Morphic;

public sealed class HaloMorph : Morph
{
	#region Constants

	private const int HANDLE_SIZE = 6;

	#endregion

	#region Fields

	private readonly Morph _target;

	private bool _resizing;
	private Point _startMouse;
	private Size _startSize;
	private Point _startPosition;

	private ResizeHandle _activeHandle = ResizeHandle.None;
	private ResizeHandle _hoverHandle = ResizeHandle.None;

	#endregion

	#region Constructors

	public HaloMorph(Morph target)
	{
		_target = target;
		Visible = true;
		UpdateFromTarget();
	}

	#endregion

	#region Properties

	public override bool WantsKeyboardFocus => false;
	public override bool IsSelectable => false;

	#endregion

	#region Methods

	public override void Draw(IRenderingContext rc)
	{
		UpdateFromTarget();

		// Outline
		rc.RenderRect(
			new Rectangle(Position, Size),
			RadialColor.Yellow,
			thickness: 1);

		DrawHandles(rc);
	}

	private void UpdateFromTarget()
	{
		Position = _target.Position;
		Size = _target.Size;
	}

	private void DrawHandles(IRenderingContext rc)
	{
		foreach (var (handle, rect) in EnumerateHandleRects())
		{
			var color =
				handle == _activeHandle ? RadialColor.Red :
				handle == _hoverHandle ? RadialColor.Cyan :
				RadialColor.White;

			rc.RenderFilledRect(rect, color);
		}
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		var handle = HandleHitTest(e.Position);
		if (handle == ResizeHandle.None)
			return;

		_activeHandle = handle;
		_hoverHandle = ResizeHandle.None;

		BeginResize(e);
		(GetWorld() as WorldMorph)?.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		if (_resizing)
		{
			ApplyResize(e.Position);
			e.MarkHandled();
			return;
		}

		var hover = HandleHitTest(e.Position);
		if (_hoverHandle != hover)
		{
			_hoverHandle = hover;
			Invalidate();
		}

		if (_hoverHandle != ResizeHandle.None)
			e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		if (!_resizing)
			return;

		_resizing = false;
		_activeHandle = ResizeHandle.None;
		e.MarkHandled();
		(GetWorld() as WorldMorph)?.ReleasePointer(this);
	}

	public override bool ContainsPoint(Point p)
	{
		foreach (var (_, rect) in EnumerateHandleRects())
		{
			if (rect.Contains(p))
				return true;
		}

		return false;
	}

	private void BeginResize(PointerDownEvent e)
	{
		_resizing = true;
		_startMouse = e.Position;
		_startSize = _target.Size;
		_startPosition = _target.Position;
	}

	private void ApplyResize(Point mouse)
	{
		int dx = mouse.X - _startMouse.X;
		int dy = mouse.Y - _startMouse.Y;

		var pos = _startPosition;
		var size = _startSize;

		switch (_activeHandle)
		{
			case ResizeHandle.TopLeft:
				pos = new Point(pos.X + dx, pos.Y + dy);
				size = new Size(size.Width - dx, size.Height - dy);
				break;

			case ResizeHandle.TopRight:
				pos = new Point(pos.X, pos.Y + dy);
				size = new Size(size.Width + dx, size.Height - dy);
				break;

			case ResizeHandle.BottomLeft:
				pos = new Point(pos.X + dx, pos.Y);
				size = new Size(size.Width - dx, size.Height + dy);
				break;

			case ResizeHandle.BottomRight:
				size = new Size(size.Width + dx, size.Height + dy);
				break;
		}

		size = new Size(
			Math.Max(1, size.Width),
			Math.Max(1, size.Height));

		_target.Position = pos;
		_target.Size = size;
	}

	private ResizeHandle HandleHitTest(Point p)
	{
		foreach (var (handle, rect) in EnumerateHandleRects())
		{
			if (rect.Contains(p))
				return handle;
		}

		return ResizeHandle.None;
	}

	private IEnumerable<(ResizeHandle handle, Rectangle rect)> EnumerateHandleRects()
	{
		int hs = HANDLE_SIZE / 2;

		yield return (
			ResizeHandle.TopLeft,
			new Rectangle(
				Position.X - hs,
				Position.Y - hs,
				HANDLE_SIZE,
				HANDLE_SIZE));

		yield return (
			ResizeHandle.TopRight,
			new Rectangle(
				Position.X + Size.Width - hs,
				Position.Y - hs,
				HANDLE_SIZE,
				HANDLE_SIZE));

		yield return (
			ResizeHandle.BottomLeft,
			new Rectangle(
				Position.X - hs,
				Position.Y + Size.Height - hs,
				HANDLE_SIZE,
				HANDLE_SIZE));

		yield return (
			ResizeHandle.BottomRight,
			new Rectangle(
				Position.X + Size.Width - hs,
				Position.Y + Size.Height - hs,
				HANDLE_SIZE,
				HANDLE_SIZE));
	}

	#endregion

	private enum ResizeHandle
	{
		None,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}
}
