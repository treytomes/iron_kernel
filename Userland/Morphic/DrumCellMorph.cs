using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic;

/// <summary>
/// A single toggleable cell in the drum machine step sequencer grid.
/// </summary>
public sealed class DrumCellMorph : Morph
{
	public bool IsLit { get; private set; }
	public bool IsColumnHighlighted { get; private set; }

	// off / on / indicator-over-off / indicator-over-on
	private static readonly Color ColorOff         = new(0.35f, 0.35f, 0.35f);
	private static readonly Color ColorOn          = new(0.00f, 0.33f, 0.58f);
	private static readonly Color ColorIndicOff    = new(0.45f, 0.10f, 0.10f);
	private static readonly Color ColorIndicOn     = new(1.00f, 0.40f, 0.40f);

	// Brighter variants for alternating column groups (groups 1 and 3)
	private static readonly Color ColorOffBright      = new(0.45f, 0.45f, 0.45f);
	private static readonly Color ColorOnBright       = new(0.00f, 0.42f, 0.72f);
	private static readonly Color ColorIndicOffBright = new(0.55f, 0.15f, 0.15f);
	private static readonly Color ColorIndicOnBright  = new(1.00f, 0.55f, 0.55f);

	private bool IsBrightGroup => (Column / 4) % 2 == 1;

	public int Column { get; init; }

	public DrumCellMorph(Point position, Size size, int column)
	{
		Position = position;
		Size = size;
		Column = column;
		IsSelectable = true;
		ShouldClipToBounds = true;
	}

	public void SetLit(bool lit)
	{
		if (IsLit == lit) return;
		IsLit = lit;
		Invalidate();
	}

	public void SetHighlighted(bool highlighted)
	{
		if (IsColumnHighlighted == highlighted) return;
		IsColumnHighlighted = highlighted;
		Invalidate();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (e.Button != MouseButton.Left) return;
		SetLit(!IsLit);
		e.MarkHandled();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		var bright = IsBrightGroup;
		var fill = (IsColumnHighlighted, IsLit) switch
		{
			(true,  true)  => bright ? ColorIndicOnBright  : ColorIndicOn,
			(true,  false) => bright ? ColorIndicOffBright : ColorIndicOff,
			(false, true)  => bright ? ColorOnBright       : ColorOn,
			(false, false) => bright ? ColorOffBright      : ColorOff,
		};

		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), fill);
		rc.RenderRect(new Rectangle(Point.Empty, Size), Color.DarkerGray);
	}
}
