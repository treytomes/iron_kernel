namespace Userland.Morphic.Inspector;

public interface IInspectorFactory
{
	Morph GetInspectorFor(Type? contentType, Func<object?> valueProvider, Action<object?>? setter = null);
}