using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic.Layout;

public abstract class ScrollThumbMorph : Morph
{
	#region Fields

	protected bool _dragging = false;
	protected int _dragOffset = 0;
	protected readonly Func<int> _getMaxScroll;
	protected readonly Action<int> _setScroll;

	#endregion

	#region Constructors

	public ScrollThumbMorph(
		Func<int> getMaxScroll,
		Action<int> setScroll
	) : base()
	{
		_getMaxScroll = getMaxScroll;
		_setScroll = setScroll;
		IsSelectable = true;
	}

	#endregion

	#region Properties

	public int Padding { get; set; } = 2;

	#endregion

	#region Methods

	protected RadialColor ResolveThumbColor()
	{
		var s = Style!.Semantic;

		if (!IsEnabled)
			return s.MutedText;

		if (_dragging)
			return s.PrimaryActive;

		if (IsEffectivelyHovered)
			return s.PrimaryHover;

		return s.Border;
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
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), ResolveThumbColor());
		base.DrawSelf(rc);
	}

	#endregion
}