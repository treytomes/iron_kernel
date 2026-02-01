using System.Drawing;

namespace IronKernel.Userland.Morphic.Inspector;

/// <summary>
/// Base morph for displaying a runtime value inside an Inspector.
/// Concrete subclasses handle type-specific rendering and interaction.
/// </summary>
public class ValueMorph : Morph
{
	#region Fields

	private InspectorFactory _inspectorFactory;

	/// <summary>
	/// The runtime value being represented.
	/// </summary>
	private Func<object?> _valueProvider;

	private object? _lastValue;
	protected Morph _content;

	#endregion

	#region Constructors

	public ValueMorph(InspectorFactory inspectorFactory, Func<object?> valueProvider)
	{
		_inspectorFactory = inspectorFactory;
		_valueProvider = valueProvider ?? throw new ArgumentNullException(nameof(valueProvider));
		_lastValue = _valueProvider();
		IsSelectable = true;

		_content = _inspectorFactory.GetInspectorFor(_lastValue?.GetType());

		AddMorph(_content);
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
		if (!Equals(current?.GetType(), _lastValue?.GetType()))
		{
			RemoveMorph(_content);
			_content = _inspectorFactory.GetInspectorFor(current?.GetType());
			AddMorph(_content);
		}
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
		_content.Position = Point.Empty;
		Size = _content.Size;
	}

	#endregion

	#region Display

	/// <summary>
	/// Updates the visual representation of the value.
	/// Subclasses override for richer rendering.
	/// </summary>
	protected virtual void UpdateDisplay()
	{
		if (_content is IValueContentMorph refreshable)
		{
			refreshable.Refresh(_lastValue);
		}
	}

	#endregion

	#endregion
}