namespace IronKernel.Userland.Morphic.Inspector;

public interface IInspectorFactory
{
	Morph GetInspectorFor(Type? contentType);
}