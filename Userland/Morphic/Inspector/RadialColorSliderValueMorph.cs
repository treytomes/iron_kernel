using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Morphic.Layout;

namespace Userland.Morphic.Inspector;

public sealed class RadialColorSliderValueMorph : Morph, IValueContentMorph
{
	#region Fields
	private RadialColor? _color;
	private readonly Action<RadialColor?>? _setter;

	private readonly SliderWithEditorMorph _r;
	private readonly SliderWithEditorMorph _g;
	private readonly SliderWithEditorMorph _b;
	private readonly ButtonMorph _toggleButton;
	private readonly RadialColorSwatchMorph _swatch;
	private readonly HorizontalStackMorph _root;
	#endregion

	#region Construction
	public RadialColorSliderValueMorph(Action<RadialColor?>? setter)
	{
		_setter = setter;
		IsSelectable = false;

		_root = new HorizontalStackMorph
		{
			Padding = 2,
			Spacing = 4
		};

		var sliderStack = new VerticalStackMorph
		{
			Padding = 0,
			Spacing = 2
		};

		_r = CreateChannelSlider(v => OnChannelChanged(Channel.R, v));
		_g = CreateChannelSlider(v => OnChannelChanged(Channel.G, v));
		_b = CreateChannelSlider(v => OnChannelChanged(Channel.B, v));

		sliderStack.AddMorph(_r);
		sliderStack.AddMorph(_g);
		sliderStack.AddMorph(_b);

		_toggleButton = new ButtonMorph(
			Point.Empty,
			new Size(8, 8),
			"+")
		{
			OnClick = OnToggleNull
		};

		_swatch = new RadialColorSwatchMorph(() => _color);

		_root.AddMorph(_swatch);
		_root.AddMorph(_toggleButton);
		_root.AddMorph(sliderStack);

		AddMorph(_root);

		UpdateEnabledState();
	}
	#endregion

	#region Layout
	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		// Adopt the computed size of the root layout morph
		Size = _root.Size;
	}
	#endregion

	#region Helpers
	private static SliderWithEditorMorph CreateChannelSlider(Action<float> setter)
	{
		return new SliderWithEditorMorph(0, setter)
		{
			Min = 0,
			Max = 5,
			Step = 1
		};
	}
	#endregion

	#region IValueContentMorph
	public void Refresh(object? value)
	{
		if (value is not RadialColor newColor)
		{
			if (_color == null)
				return;
			_color = null;
		}
		else
		{
			if (_color != null && newColor.Equals(_color))
				return; // ✅ do not overwrite active edits
			_color = newColor;
		}

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
	}
	#endregion

	#region Channel updates
	private enum Channel { R, G, B }

	private void OnChannelChanged(Channel channel, float value)
	{
		if (_color == null)
			return;

		byte v = (byte)value;

		var newColor = channel switch
		{
			Channel.R => _color.WithR(v),
			Channel.G => _color.WithG(v),
			Channel.B => _color.WithB(v),
			_ => _color
		};

		if (newColor.Equals(_color))
			return;

		_color = newColor;
		_setter?.Invoke(newColor);
		Invalidate();
	}
	#endregion

	#region Null toggle
	private void OnToggleNull()
	{
		if (_color == null)
		{
			_color = RadialColor.Black;
			_setter?.Invoke(_color);
		}
		else
		{
			_color = null;
			_setter?.Invoke(null);
		}

		Refresh(_color);
	}

	private void UpdateEnabledState()
	{
		bool hasValue = _color != null;

		_r.IsEnabled = hasValue;
		_g.IsEnabled = hasValue;
		_b.IsEnabled = hasValue;

		_toggleButton.Text = hasValue ? "-" : "+";
	}
	#endregion
}