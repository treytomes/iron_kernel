using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic.Inspector;

public class InspectorFactory : IInspectorFactory
{
	public Morph GetInspectorFor(Type? declaredType, Action<object?>? setter = null)
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

		return new LabelMorph
		{
			IsSelectable = false,
			BackgroundColor = null
		};
	}
}