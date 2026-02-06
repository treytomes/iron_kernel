using System.Drawing;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Inspector;

/// <summary>
/// Base morph for displaying a runtime value inside an Inspector.
/// Concrete subclasses handle type-specific rendering and interaction.
/// </summary>
public class ValueMorph : Morph
{
	#region Fields

	private readonly IInspectorFactory _inspectorFactory;
	private readonly Func<object?> _valueProvider;
	private readonly Action<object?>? _valueSetter;

	private readonly Type? _declaredType;

	private object? _lastValue;
	protected Morph _content;

	#endregion

	#region Constructors

	public ValueMorph(
		IInspectorFactory inspectorFactory,
		Func<object?> valueProvider,
		Action<object?>? valueSetter = null,
		Type? declaredType = null)
	{
		_inspectorFactory = inspectorFactory
			?? throw new ArgumentNullException(nameof(inspectorFactory));

		_valueProvider = valueProvider
			?? throw new ArgumentNullException(nameof(valueProvider));

		_valueSetter = valueSetter;
		_declaredType = declaredType;

		_lastValue = _valueProvider();
		IsSelectable = true;

		_content = CreateContent(_lastValue);
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

		var previousType = ResolveType(_lastValue);
		var currentType = ResolveType(current);

		// If the resolved type changes, rebuild the content morph
		if (!Equals(previousType, currentType))
		{
			RemoveMorph(_content);
			_content = CreateContent(current);
			AddMorph(_content);
			InvalidateLayout();
		}

		// If the value changes, refresh display
		if (!Equals(current, _lastValue))
		{
			_lastValue = current;
			UpdateDisplay();
			InvalidateLayout();
		}
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		_content.Position = Point.Empty;
		Size = _content.Size;

		base.UpdateLayout();
	}

	#endregion

	#region Display

	protected virtual void UpdateDisplay()
	{
		if (_content is IValueContentMorph refreshable)
		{
			refreshable.Refresh(_lastValue);
		}
	}

	#endregion

	#region Helpers

	private Morph CreateContent(object? value)
	{
		var type = ResolveType(value);

		return _inspectorFactory.GetInspectorFor(
			type,
			v =>
			{
				_valueSetter?.Invoke(v);
				_lastValue = v;
			});
	}

	private Type? ResolveType(object? value)
	{
		// Declared type always wins if present
		return _declaredType ?? value?.GetType();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		e.MarkHandled();
	}

	#endregion
}