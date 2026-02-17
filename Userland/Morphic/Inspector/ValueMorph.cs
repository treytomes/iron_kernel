using System.Drawing;
using Userland.Morphic.Events;

namespace Userland.Morphic.Inspector;

/// <summary>
/// Base morph for displaying and editing a runtime value inside an Inspector.
/// Handles reconciliation between editor state and external value providers.
/// </summary>
public class ValueMorph : Morph
{
	#region Fields
	private readonly IInspectorFactory _inspectorFactory;
	private readonly Func<object?> _valueProvider;
	private readonly Action<object?>? _valueSetter;
	private readonly Type? _declaredType;

	private object? _lastValue;
	private object? _pendingCommit; // ✅ optimistic commit buffer

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

	#region Update
	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

		var current = _valueProvider();

		// ✅ Pending commit reconciliation:
		// Trust editor until provider agrees.
		if (_pendingCommit != null)
		{
			if (Equals(current, _pendingCommit))
			{
				// Provider caught up
				_pendingCommit = null;
			}
			else
			{
				// Still waiting — do not overwrite editor
				return;
			}
		}

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
			_valueProvider,
			v =>
			{
				// ✅ Optimistic commit
				_valueSetter?.Invoke(v);
				_lastValue = v;
				_pendingCommit = v;
			});
	}

	private Type? ResolveType(object? value)
	{
		// Declared type always wins if present
		return _declaredType ?? value?.GetType();
	}
	#endregion

	#region Input
	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		e.MarkHandled();
	}
	#endregion
}