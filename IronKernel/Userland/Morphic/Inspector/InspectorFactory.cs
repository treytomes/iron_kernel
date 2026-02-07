using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic.Inspector;

public class InspectorFactory : IInspectorFactory
{
	private readonly Action<object>? _navigate;

	public InspectorFactory(Action<object>? navigate = null)
	{
		_navigate = navigate;
	}

	public Morph GetInspectorFor(Type? declaredType, Func<object?> valueProvider, Action<object?>? setter = null)
	{
		if (declaredType == typeof(bool) || declaredType == typeof(bool?))
		{
			return new CheckBoxMorph(b =>
			{
				setter?.Invoke(b);
			});
		}

		if (declaredType == typeof(RadialColor))
		{
			return new RadialColorValueMorph(b =>
			{
				setter?.Invoke(b);
			});
		}

		if (declaredType == typeof(string))
		{
			return new TextEditMorph(
				Point.Empty,
				string.Empty,
				s => setter?.Invoke(s));
		}

		// Console.WriteLine($"Navigable type [{declaredType?.Name}]: {IsNavigableType(declaredType)}");
		if (_navigate != null && IsNavigableType(declaredType))
		{
			return new NavigableValueMorph(valueProvider, _navigate);
		}

		return new LabelMorph
		{
			IsSelectable = false,
			BackgroundColor = null
		};
	}

	private static bool IsNavigableType(Type? type)
	{
		if (type == null)
			return false;

		if (type.IsPrimitive)
			return false;

		if (type == typeof(string))
			return false;

		return !type.IsEnum;
	}
}