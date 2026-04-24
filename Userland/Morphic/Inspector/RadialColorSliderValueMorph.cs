using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Morphic.Layout;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic.Inspector;

public sealed class RadialColorSliderValueMorph : Morph, IValueContentMorph
{
	#region Fields
	private Color? _color;
	private readonly Action<Color?>? _setter;

	private readonly SliderWithEditorMorph _r;
	private readonly SliderWithEditorMorph _g;
	private readonly SliderWithEditorMorph _b;
	private readonly ButtonMorph _toggleButton;
	private readonly RadialColorSwatchMorph _swatch;
	private readonly HorizontalStackMorph _root;
	private readonly VerticalStackMorph _sliderStack;
	#endregion

	#region Construction
	public RadialColorSliderValueMorph(Action<Color?>? setter)
	{
		_setter = setter;
		IsSelectable = false;

		_root = new HorizontalStackMorph
		{
			Padding = 2,
			Spacing = 4
		};

		_sliderStack = new VerticalStackMorph
		{
			Padding = 0,
			Spacing = 2
		};

		_r = CreateChannelSlider("R:", v => OnChannelChanged(Channel.R, v));
		_g = CreateChannelSlider("G:", v => OnChannelChanged(Channel.G, v));
		_b = CreateChannelSlider("B:", v => OnChannelChanged(Channel.B, v));

		_sliderStack.AddMorph(_r);
		_sliderStack.AddMorph(_g);
		_sliderStack.AddMorph(_b);

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
		_root.AddMorph(_sliderStack);

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
	private static SliderWithEditorMorph CreateChannelSlider(string labelText, Action<float> setter)
	{
		return new SliderWithEditorMorph(labelText, 0, setter)
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
		Color? newColor = null;
		if (value is Color c)
			newColor = c;

		if (newColor == null)
		{
			if (_color == null)
				return;
			_color = null;
		}
		else
		{
			if (_color != null && newColor.Value.Equals(_color))
				return; // do not overwrite active edits
			_color = newColor;
		}

		if (_color != null)
		{
			_r.Value = MathF.Round(_color.Value.R * 5f);
			_g.Value = MathF.Round(_color.Value.G * 5f);
			_b.Value = MathF.Round(_color.Value.B * 5f);
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

		float v = value / 5f;

		var newColor = channel switch
		{
			Channel.R => _color.Value.WithR(v),
			Channel.G => _color.Value.WithG(v),
			Channel.B => _color.Value.WithB(v),
			_ => _color.Value
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
			_color = Color.Black;
			_setter?.Invoke(_color);
		}
		else
		{
			_color = null;
			_setter?.Invoke(null);
		}

		// Always update UI state explicitly
		UpdateEnabledState();

		// Force sliders to sync when becoming visible
		if (_color != null)
		{
			_r.Value = MathF.Round(_color.Value.R * 5f);
			_g.Value = MathF.Round(_color.Value.G * 5f);
			_b.Value = MathF.Round(_color.Value.B * 5f);
		}

		InvalidateLayout();
	}

	private void UpdateEnabledState()
	{
		bool hasValue = _color != null;

		_toggleButton.Text = hasValue ? "-" : "+";

		_sliderStack.Visible = hasValue;

		InvalidateLayout();
	}

	#endregion
}
