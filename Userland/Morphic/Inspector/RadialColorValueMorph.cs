using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Morphic.Inspector;

public sealed class RadialColorValueMorph : Morph, IValueContentMorph
{
	#region Constants

	private const int SwatchSize = 6;
	private const int Padding = 1;

	#endregion

	#region Fields

	private RadialColor? _color;
	private readonly Action<RadialColor?>? _setter;

	private readonly ChannelStepperMorph _r;
	private readonly ChannelStepperMorph _g;
	private readonly ChannelStepperMorph _b;
	private readonly ButtonMorph _toggleButton;

	#endregion

	#region Constructor

	public RadialColorValueMorph(Action<RadialColor?>? setter)
	{
		_setter = setter;
		IsSelectable = false;

		_r = new ChannelStepperMorph("R", 0, OnRChanged);
		_g = new ChannelStepperMorph("G", 0, OnGChanged);
		_b = new ChannelStepperMorph("B", 0, OnBChanged);

		_toggleButton = new ButtonMorph(
			position: Point.Empty,
			size: new Size(8, 8),
			text: "+"
		);
		_toggleButton.OnClick = OnToggleNull;

		AddMorph(_toggleButton);
		AddMorph(_r);
		AddMorph(_g);
		AddMorph(_b);

		UpdateEnabledState();
	}

	#endregion

	#region IValueContentMorph

	public void Refresh(object? value)
	{
		_color = value as RadialColor;

		if (_color != null)
		{
			_r.Value = _color.R;
			_g.Value = _color.G;
			_b.Value = _color.B;
		}
		else
		{
			_r.Value = 0;
			_g.Value = 0;
			_b.Value = 0;
		}

		UpdateEnabledState();
		Invalidate();
		InvalidateLayout();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		var x = SwatchSize + Padding * 2;

		_toggleButton.Position = new Point(x, Padding);
		x += _toggleButton.Size.Width + Padding;

		foreach (var stepper in new[] { _r, _g, _b })
		{
			stepper.Position = new Point(x, Padding);
			x += stepper.Size.Width + Padding;
		}

		var height = Math.Max(
			SwatchSize,
			_r.Size.Height);

		Size = new Size(
			x + Padding,
			height + Padding * 2);

		base.UpdateLayout();
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;

		// Background
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			s.Surface);

		// Swatch border
		rc.RenderFilledRect(
			new Rectangle(Padding, Padding, SwatchSize, SwatchSize),
			s.Border);

		// Swatch fill
		if (_color != null)
		{
			for (var x = 1; x < SwatchSize - 1; x++)
				for (var y = 1; y < SwatchSize - 1; y++)
				{
					rc.SetPixel(
						new Point(Padding + x, Padding + y),
						_color);
				}
		}
		else
		{
			// Muted fill for null state
			for (var x = 1; x < SwatchSize - 1; x++)
				for (var y = 1; y < SwatchSize - 1; y++)
				{
					rc.SetPixel(
						new Point(Padding + x, Padding + y),
						s.MutedText);
				}
		}
	}

	#endregion

	#region Channel updates

	private void OnRChanged(byte r)
	{
		if (_color == null) return;
		UpdateColor(_color.WithR(r));
	}

	private void OnGChanged(byte g)
	{
		if (_color == null) return;
		UpdateColor(_color.WithG(g));
	}

	private void OnBChanged(byte b)
	{
		if (_color == null) return;
		UpdateColor(_color.WithB(b));
	}

	private void UpdateColor(RadialColor newColor)
	{
		if (newColor.Equals(_color))
			return;

		_color = newColor;
		_setter?.Invoke(newColor);
		Invalidate();
	}

	#endregion

	#region Null toggle logic

	private void OnToggleNull()
	{
		if (_color == null)
		{
			Refresh(RadialColor.Black);
			_setter?.Invoke(_color);
		}
		else
		{
			Refresh(null);
			_setter?.Invoke(null);
		}

		UpdateEnabledState();
		Invalidate();
	}

	private void UpdateEnabledState()
	{
		var hasValue = _color != null;

		_r.IsEnabled = hasValue;
		_g.IsEnabled = hasValue;
		_b.IsEnabled = hasValue;

		_toggleButton.Text = hasValue ? "-" : "+";
	}

	#endregion
}