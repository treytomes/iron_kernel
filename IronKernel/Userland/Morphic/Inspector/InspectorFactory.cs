using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic.Inspector;

public class InspectorFactory : IInspectorFactory
{
	public Morph GetInspectorFor(Type? contentType, Action<object?>? setter = null)
	{
		if (contentType == typeof(bool) || contentType == typeof(bool?))
		{
			return new CheckBoxMorph(b =>
			{
				setter?.Invoke(b);
			});
		}

		if (contentType == typeof(RadialColor))
			return new RadialColorValueMorph();

		return new LabelMorph
		{
			IsSelectable = false,
			BackgroundColor = null
		};
	}
}