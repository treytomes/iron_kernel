using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

public sealed class ButtonMorph : Morph
{
	#region Fields

	private bool _isPressed;
	private LabelMorph _label;

	private RadialColor? _backgroundColorOverride;
	private RadialColor? _hoverBackgroundColorOverride;
	private RadialColor? _activeBackgroundColorOverride;
	private RadialColor? _foregroundColorOverride;
	private RadialColor? _disabledBackgroundColorOverride;
	private RadialColor? _disabledForegroundColorOverride;
	#endregion

	#region Constructors

	public ButtonMorph(Point position, Size size, string text)
	{
		Position = position;
		Size = size;
		IsSelectable = true;

		_label = new LabelMorph(position, "image.oem437_8", new Size(8, 8))
		{
			IsSelectable = false,
			Text = text,
			BackgroundColor = null,
			ForegroundColor = RadialColor.White,
		};
		AddMorph(_label);
	}

	#endregion

	#region Properties

	public Action? OnClick
	{
		get
		{
			return (Command as ActionCommand)?.ExecuteAction;
		}
		set
		{
			if (value == null)
			{
				Command = null;
			}
			else
			{
				Command = new ActionCommand(value);
			}
		}
	}

	public ICommand? Command { get; set; }

	public RadialColor? BackgroundColor
	{
		get => _backgroundColorOverride;
		set
		{
			_backgroundColorOverride = value;
			Invalidate();
		}
	}

	public RadialColor? HoverBackgroundColor
	{
		get => _hoverBackgroundColorOverride;
		set
		{
			_hoverBackgroundColorOverride = value;
			Invalidate();
		}
	}

	public RadialColor? ActiveBackgroundColor
	{
		get => _activeBackgroundColorOverride;
		set
		{
			_activeBackgroundColorOverride = value;
			Invalidate();
		}
	}

	public RadialColor? ForegroundColor
	{
		get => _foregroundColorOverride;
		set
		{
			_foregroundColorOverride = value;
			Invalidate();
		}
	}

	public RadialColor? DisabledBackgroundColor
	{
		get => _disabledBackgroundColorOverride;
		set
		{
			_disabledBackgroundColorOverride = value;
			Invalidate();
		}
	}

	public RadialColor? DisabledForegroundColor
	{
		get => _disabledForegroundColorOverride;
		set
		{
			_disabledForegroundColorOverride = value;
			Invalidate();
		}
	}

	private RadialColor EffectiveBackgroundColor =>
		_backgroundColorOverride
		?? GetWorld()?.Style.ButtonBackgroundColor
		?? RadialColor.White;

	private RadialColor? EffectiveHoverBackgroundColor =>
		_hoverBackgroundColorOverride
		?? GetWorld()?.Style.ButtonHoverBackgroundColor;

	private RadialColor? EffectiveActiveBackgroundColor =>
		_activeBackgroundColorOverride
		?? GetWorld()?.Style.ButtonActiveBackgroundColor;

	private RadialColor? EffectiveForegroundColor =>
		_foregroundColorOverride
		?? GetWorld()?.Style.ButtonForegroundColor;

	private RadialColor? EffectiveDisabledBackgroundColor =>
		_disabledBackgroundColorOverride
		?? GetWorld()?.Style.ButtonDisabledBackgroundColor;

	private RadialColor? EffectiveDisabledForegroundColor =>
		_disabledForegroundColorOverride
		?? GetWorld()?.Style.ButtonDisabledForegroundColor;

	public bool IsEnabled => Command?.CanExecute() ?? false;

	#endregion

	#region Methods

	public override void Draw(IRenderingContext rc)
	{
		var bg =
			!IsEnabled ? EffectiveDisabledBackgroundColor :
			_isPressed ? EffectiveActiveBackgroundColor :
			IsEffectivelyHovered ? EffectiveHoverBackgroundColor :
			EffectiveBackgroundColor;

		rc.RenderFilledRect(Bounds, bg!);
		rc.RenderRect(Bounds, RadialColor.Black);

		_label.ForegroundColor = IsEnabled
			? EffectiveForegroundColor
			: EffectiveDisabledForegroundColor;

		base.Draw(rc);
	}

	public override void Update(double deltaTime)
	{
		Invalidate();
		base.Update(deltaTime);
	}

	protected override void OnPointerEnter()
	{
		Invalidate();
	}

	protected override void OnPointerLeave()
	{
		_isPressed = false;
		Invalidate();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (!IsEnabled) return;

		_isPressed = true;
		GetWorld()?.CapturePointer(this);
		Invalidate();
		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		GetWorld()?.CapturePointer(null);

		if (!IsEnabled)
		{
			_isPressed = false;
			Invalidate();
			return;
		}

		if (_isPressed && IsEffectivelyHovered)
		{
			if (TryGetWorld(out var world) && Command != null)
			{
				world.Commands.Submit(Command);
			}
		}

		_isPressed = false;
		Invalidate();
		e.MarkHandled();
	}

	protected override void UpdateLayout()
	{
		_label.CenterOnOwner();
	}

	#endregion
}