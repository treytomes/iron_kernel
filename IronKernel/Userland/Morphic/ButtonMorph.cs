using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

/// <summary>
/// A clickable Morphic button with depth, hover animation,
/// pressed feedback, and disabled state.
/// </summary>
public sealed class ButtonMorph : Morph
{
	#region Fields

	private readonly LabelMorph _label;

	// Animated hover factor [0..1]
	private float _hoverT;

	// Optional style overrides
	private RadialColor? _backgroundOverride;
	private RadialColor? _hoverBackgroundOverride;
	private RadialColor? _activeBackgroundOverride;
	private RadialColor? _foregroundOverride;
	private RadialColor? _disabledBackgroundOverride;
	private RadialColor? _disabledForegroundOverride;

	#endregion

	#region Constructors

	public ButtonMorph(Point position, Size size, string text)
	{
		Position = position;
		Size = size;
		IsSelectable = true;

		_label = new LabelMorph(position)
		{
			IsSelectable = true,
			Text = text,
			BackgroundColor = null
		};

		AddMorph(_label);
	}

	#endregion

	#region Command wiring

	public ICommand? Command { get; set; }

	public Action? OnClick
	{
		set => Command = value == null ? null : new ActionCommand(value);
	}

	public override bool IsEnabled
	{
		get => (Command?.CanExecute() ?? false) && base.IsEnabled;
		set => base.IsEnabled = value;
	}

	#endregion

	#region Style properties (overrides)

	public RadialColor? BackgroundColor
	{
		get => _backgroundOverride;
		set { _backgroundOverride = value; Invalidate(); }
	}

	public RadialColor? HoverBackgroundColor
	{
		get => _hoverBackgroundOverride;
		set { _hoverBackgroundOverride = value; Invalidate(); }
	}

	public RadialColor? ActiveBackgroundColor
	{
		get => _activeBackgroundOverride;
		set { _activeBackgroundOverride = value; Invalidate(); }
	}

	public RadialColor? ForegroundColor
	{
		get => _foregroundOverride;
		set { _foregroundOverride = value; Invalidate(); }
	}

	public RadialColor? DisabledBackgroundColor
	{
		get => _disabledBackgroundOverride;
		set { _disabledBackgroundOverride = value; Invalidate(); }
	}

	public RadialColor? DisabledForegroundColor
	{
		get => _disabledForegroundOverride;
		set { _disabledForegroundOverride = value; Invalidate(); }
	}

	public string Text
	{
		get => _label.Text;
		set => _label.Text = value;
	}

	#endregion

	#region Style resolution

	private RadialColor EffectiveBackground =>
		_backgroundOverride
		?? GetWorld()?.Style.ButtonBackgroundColor
		?? RadialColor.DarkGray;

	private RadialColor EffectiveHoverBackground =>
		_hoverBackgroundOverride
		?? GetWorld()?.Style.ButtonHoverBackgroundColor
		?? EffectiveBackground;

	private RadialColor EffectiveActiveBackground =>
		_activeBackgroundOverride
		?? GetWorld()?.Style.ButtonActiveBackgroundColor
		?? EffectiveBackground;

	private RadialColor EffectiveForeground =>
		_foregroundOverride
		?? GetWorld()?.Style.ButtonForegroundColor
		?? RadialColor.White;

	private RadialColor EffectiveDisabledBackground =>
		_disabledBackgroundOverride
		?? GetWorld()?.Style.ButtonDisabledBackgroundColor
		?? RadialColor.DarkGray;

	private RadialColor EffectiveDisabledForeground =>
		_disabledForegroundOverride
		?? GetWorld()?.Style.ButtonDisabledForegroundColor
		?? RadialColor.Gray;

	#endregion

	#region Update / animation

	public override void Update(double deltaTime)
	{
		var target = (IsEffectivelyHovered && IsEnabled) ? 1f : 0f;
		_hoverT = Lerp(_hoverT, target, 0.25f);
		Invalidate();

		base.Update(deltaTime);
	}

	private static float Lerp(float a, float b, float t)
		=> a + (b - a) * t;

	#endregion

	#region Drawing

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);

		// Resolve background
		var baseBg = !IsEnabled
			? EffectiveDisabledBackground
			: IsPressed
				? EffectiveActiveBackground
				: EffectiveBackground.Lerp(EffectiveHoverBackground, _hoverT);

		// Pressed offset (depth illusion)
		var offset = IsPressed ? new Point(1, 1) : Point.Empty;
		var body = new Rectangle(
			offset.X,
			offset.Y,
			Bounds.Width,
			Bounds.Height);

		// Optional drop shadow
		if (IsEnabled && !IsPressed)
		{
			var shadow = new Rectangle(
				2,
				2,
				Bounds.Width,
				Bounds.Height);
			rc.RenderFilledRect(shadow, RadialColor.DarkerGray);
		}

		// Body
		rc.RenderFilledRect(body, baseBg);

		// Bevel (classic button look)
		DrawBevel(rc, body, !IsPressed);

		// Outline
		rc.RenderRect(body, RadialColor.Black);

		// Label color
		_label.ForegroundColor = IsEnabled
			? EffectiveForeground
			: EffectiveDisabledForeground;
	}

	private void DrawBevel(IRenderingContext rc, Rectangle r, bool raised)
	{
		var light = raised ? RadialColor.White : RadialColor.DarkGray;
		var dark = raised ? RadialColor.DarkGray : RadialColor.White;

		rc.RenderLine(new Point(r.Left, r.Top), new Point(r.Right - 1, r.Top), light);
		rc.RenderLine(new Point(r.Left, r.Top), new Point(r.Left, r.Bottom - 1), light);

		rc.RenderLine(new Point(r.Left, r.Bottom - 1), new Point(r.Right - 1, r.Bottom - 1), dark);
		rc.RenderLine(new Point(r.Right - 1, r.Top), new Point(r.Right - 1, r.Bottom - 1), dark);
	}

	#endregion

	#region Pointer handling

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (IsPressed)
			GetWorld()?.CapturePointer(this);

		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		GetWorld()?.CapturePointer(null);

		if (IsPressed && IsEnabled && IsEffectivelyHovered)
		{
			if (TryGetWorld(out var world) && Command != null)
				world.Commands.Submit(Command);
		}

		e.MarkHandled();
		base.OnPointerUp(e);
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		var offset = IsPressed ? new Point(1, 1) : Point.Empty;
		_label.Position = new Point(
			(Size.Width - _label.Size.Width) / 2 + offset.X,
			(Size.Height - _label.Size.Height) / 2 + offset.Y);

		base.UpdateLayout();
	}

	#endregion
}