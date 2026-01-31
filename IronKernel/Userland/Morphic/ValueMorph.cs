using System.Drawing;

namespace IronKernel.Userland.Morphic;

/// <summary>
/// Base morph for displaying a runtime value inside an Inspector.
/// Concrete subclasses handle type-specific rendering and interaction.
/// </summary>
public class ValueMorph : Morph
{
	#region Fields

	/// <summary>
	/// The runtime value being represented.
	/// </summary>
	private Func<object?> _valueProvider;

	private object? _lastValue;
	protected readonly LabelMorph _label;

	#endregion

	#region Constructors

	public ValueMorph(Func<object?> valueProvider)
	{
		_valueProvider = valueProvider ?? throw new ArgumentNullException(nameof(valueProvider));
		_lastValue = _valueProvider();
		IsSelectable = true;

		_label = new LabelMorph()
		{
			IsSelectable = false,
			BackgroundColor = null
		};

		AddMorph(_label);
		UpdateDisplay();
	}

	#endregion

	#region Properties

	public virtual object? Value => _lastValue;

	#endregion

	#region Methods

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

		var current = _valueProvider();
		if (!Equals(current, _lastValue))
		{
			_lastValue = current;
			UpdateDisplay();
			InvalidateLayout();
		}
	}

	#region Layout

	protected override void UpdateLayout()
	{
		_label.Position = Point.Empty;
		Size = _label.Size;
	}

	#endregion

	#region Display

	/// <summary>
	/// Updates the visual representation of the value.
	/// Subclasses override for richer rendering.
	/// </summary>
	protected virtual void UpdateDisplay()
	{
		_label.Text = FormatValue(_lastValue);
	}

	protected virtual string FormatValue(object? value)
	{
		return value?.ToString() ?? "<null>";
	}

	#endregion

	#endregion
}