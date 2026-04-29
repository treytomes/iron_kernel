using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic;

public sealed class BoxMorph : Morph
{
	private bool _clicked;

	public BoxMorph(Point position, Size size)
	{
		Position = position;
		Size = size;
		FillColor = Color.DarkGray;
		BorderColor = Color.DarkerGray;
		IsSelectable = true;
		ShouldClipToBounds = true;
	}

	public Color FillColor { get; set; }
	public Color BorderColor { get; set; }

	/// <summary>
	/// Returns true and clears the flag if a left-click occurred since the last call.
	/// </summary>
	public bool ConsumeClick()
	{
		if (!_clicked) return false;
		_clicked = false;
		return true;
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (e.Button != MouseButton.Left) return;
		_clicked = true;
		e.MarkHandled();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), FillColor);
		rc.RenderRect(new Rectangle(Point.Empty, Size), BorderColor);
	}
}
