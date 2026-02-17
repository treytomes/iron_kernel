using System.Drawing;
using Userland.Morphic.Controls;
using Userland.Morphic.Inspector;
using Userland.Morphic.Layout;

namespace Userland.Morphic;

public sealed class SliderWithEditorMorph : HorizontalStackMorph, IValueContentMorph
{
	#region Fields
	private float _value;
	private readonly Action<float>? _setter;
	private readonly SliderTrackMorph _track;
	private readonly TextEditMorph _editor;
	private bool _suppressEditorCallback;
	#endregion

	#region Configuration
	private float _min = 0f;
	private float _max = 1f;
	private float _step = 0f;

	public float Min
	{
		get => _min;
		set
		{
			_min = value;
			ClampCurrentValue();
			PropagateRange();
		}
	}

	public float Max
	{
		get => _max;
		set
		{
			_max = value;
			ClampCurrentValue();
			PropagateRange();
		}
	}

	public float Step
	{
		get => _step;
		set
		{
			_step = value;
			ClampCurrentValue();
			PropagateRange();
		}
	}
	#endregion

	#region Value
	public float Value
	{
		get => _value;
		set => SetValue(value, fromUser: false);
	}
	#endregion

	#region Construction
	public SliderWithEditorMorph(
		float initialValue,
		Action<float>? setter)
	{
		_value = initialValue;
		_setter = setter;

		_editor = new TextEditMorph(
			position: Point.Empty,
			initialText: initialValue.ToString("0.##"),
			setter: OnEditorCommitted,
			validator: ValidateText);

		_track = new SliderTrackMorph(
			getNormalized: () => ValueToNormalized(_value),
			setNormalized: t =>
			{
				SetValue(NormalizedToValue(t), fromUser: true);
			})
		{
			Min = Min,
			Max = Max,
			Step = Step
		};

		AddMorph(_editor);
		AddMorph(_track);
	}
	#endregion

	#region IValueContentMorph
	public void Refresh(object? value)
	{
		if (value is not float f)
			return;

		SetValue(f, fromUser: false);
	}
	#endregion

	#region Editor callbacks
	private void OnEditorCommitted(string text)
	{
		if (_suppressEditorCallback)
			return;

		if (float.TryParse(text, out var v))
			SetValue(v, fromUser: true);
	}

	private bool ValidateText(string text)
	{
		if (!float.TryParse(text, out var v))
			return false;

		return v >= Min && v <= Max;
	}
	#endregion

	#region Value helpers
	private void SetValue(float v, bool fromUser)
	{
		var newValue = ClampAndSnap(v);
		if (Math.Abs(newValue - _value) < float.Epsilon)
			return;

		_value = newValue;

		// Always update editor text
		_suppressEditorCallback = true;
		_editor.Refresh(_value);
		_suppressEditorCallback = false;

		// Only notify external consumer when user initiated
		if (fromUser)
		{
			_setter?.Invoke(_value);
		}

		InvalidateLayout();
	}

	private float ClampAndSnap(float v)
	{
		v = Math.Clamp(v, Min, Max);
		if (Step > 0f)
			v = MathF.Round(v / Step) * Step;
		return v;
	}

	private float ValueToNormalized(float v)
	{
		if (Max <= Min)
			return 0f;

		return (v - Min) / (Max - Min);
	}

	private float NormalizedToValue(float t)
	{
		return Min + t * (Max - Min);
	}

	private void ClampCurrentValue()
	{
		SetValue(_value, fromUser: false);
	}

	private void PropagateRange()
	{
		_track.Min = _min;
		_track.Max = _max;
		_track.Step = _step;
		InvalidateLayout();
	}

	#endregion
}