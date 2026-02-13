using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic;

public sealed class ChannelStepperMorph : Morph
{
	#region Constants

	private const int Padding = 1;

	#endregion

	#region Fields

	private byte _value;
	private readonly Action<byte> _onChanged;

	private readonly LabelMorph _labelMorph;
	private readonly LabelMorph _valueMorph;

	#endregion

	#region Constructors

	public ChannelStepperMorph(string label, byte initial, Action<byte> onChanged)
	{
		_value = initial;
		_onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));

		IsSelectable = true;

		_labelMorph = new LabelMorph
		{
			IsSelectable = false,
			Text = label,
			BackgroundColor = null
		};

		_valueMorph = new LabelMorph
		{
			IsSelectable = false,
			Text = initial.ToString(),
			BackgroundColor = null
		};

		AddMorph(_labelMorph);
		AddMorph(_valueMorph);

		InvalidateLayout();
	}

	#endregion

	#region Properties

	public byte Value
	{
		get => _value;
		set
		{
			var clamped = (byte)Math.Clamp(value, (byte)0, (byte)5);
			if (_value == clamped) return;

			_value = clamped;
			_valueMorph.Text = _value.ToString();

			Invalidate();
			InvalidateLayout();
		}
	}

	#endregion

	#region Methods

	#region Input

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (!IsEnabled)
			return;

		GetWorld()?.CapturePointer(this);

		if (e.Button == MouseButton.Left && _value < 5)
		{
			Value = (byte)(_value + 1);   // ← update local state
			_onChanged(Value);
		}
		else if (e.Button == MouseButton.Right && _value > 0)
		{
			Value = (byte)(_value - 1);   // ← update local state
			_onChanged(Value);
		}

		Invalidate();
		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		base.OnPointerUp(e);
		GetWorld()?.CapturePointer(null);
		Invalidate();
		e.MarkHandled();
	}

	protected override void OnPointerLeave()
	{
		base.OnPointerLeave();
		GetWorld()?.CapturePointer(null);
		Invalidate();
	}

	#endregion

	protected override void UpdateLayout()
	{
		_labelMorph.Position = new Point(Padding, Padding);
		_valueMorph.Position = new Point(
			_labelMorph.Size.Width + Padding * 2,
			Padding);

		var height = Math.Max(_labelMorph.Size.Height, _valueMorph.Size.Height);

		Size = new Size(
			_labelMorph.Size.Width + _valueMorph.Size.Width + Padding * 3,
			height + Padding * 2);

		base.UpdateLayout();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null) return;

		var s = Style.Semantic;

		var bg =
			!IsEnabled ? s.Surface :
			IsPressed ? s.PrimaryActive :
			IsEffectivelyHovered ? s.PrimaryHover :
			s.Surface;

		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), bg);
	}

	#endregion
}