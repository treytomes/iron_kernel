namespace IronKernel.Userland.Morphic.Inspector;

public class InspectorFactory : IInspectorFactory
{
	public Morph GetInspectorFor(Type? contentType)
	{
		return new LabelMorph()
		{
			IsSelectable = false,
			BackgroundColor = null
		};
	}
}