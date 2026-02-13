using IronKernel.Userland.Gfx;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Inspector;
using System.Drawing;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

public sealed class CheckBoxMorph : Morph, IValueContentMorph
{
	#region Fields

	private bool? _checked;
	private readonly Action<bool>? _setter;

	private const int BoxSize = 6;
	private const int Padding = 1;

	#endregion

	#region Constructors

	public CheckBoxMorph(Action<bool>? setter = null)
	{
		_setter = setter;
		IsSelectable = true;
		Size = new(BoxSize + Padding * 2, BoxSize + Padding * 2);
	}

	#endregion

	#region Methods

	#region IValueContentMorph

	public void Refresh(object? value)
	{
		_checked = value as bool?;
		Invalidate();
	}

	#endregion

	#region Input

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (!IsEnabled)
			return;

		if (e.Button == MouseButton.Left)
		{
			_checked = _checked != true;
			_setter?.Invoke(_checked.Value);
			Invalidate();
		}

		base.OnPointerDown(e);
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null) return;

		var s = Style.Semantic;

		// Background
		rc.RenderFilledRect(
			new Rectangle(0, 0, Size.Width, Size.Height),
			s.Surface);

		// Border
		var border =
			!IsEnabled ? s.Border :
			IsPressed ? s.PrimaryActive :
			IsEffectivelyHovered ? s.PrimaryHover :
			s.Border;

		DrawBox(rc, border);

		// Check / indeterminate
		if (_checked == true)
		{
			DrawCheck(rc, ResolveCheckColor(s));
		}
		else if (_checked == null)
		{
			DrawIndeterminate(rc, s.SuccessMuted);
		}
	}

	private void DrawIndeterminate(IRenderingContext ctx, RadialColor color)
	{
		var y = Padding + BoxSize / 2;
		for (var x = Padding + 3; x < Padding + BoxSize - 3; x++)
		{
			ctx.SetPixel(new(x, y), color);
		}
	}

	private void DrawBox(IRenderingContext ctx, RadialColor color)
	{
		for (var x = Padding; x < Padding + BoxSize; x++)
			for (var y = Padding; y < Padding + BoxSize; y++)
			{
				// Border only
				if (x == Padding || y == Padding ||
					x == Padding + BoxSize - 1 ||
					y == Padding + BoxSize - 1)
				{
					ctx.SetPixel(new(x, y), color);
				}
			}
	}

	private void DrawCheck(IRenderingContext ctx, RadialColor color)
	{
		for (var x = Padding + 1; x < Padding + BoxSize - 1; x++)
			for (var y = Padding + 1; y < Padding + BoxSize - 1; y++)
			{
				ctx.SetPixel(new(x, y), color);
			}
	}

	private RadialColor ResolveCheckColor(SemanticColors s)
	{
		return
			!IsEnabled ? s.SuccessMuted :
			IsPressed ? s.SuccessActive :
			IsEffectivelyHovered ? s.SuccessHover :
			s.Success;
	}

	#endregion

	#endregion
}