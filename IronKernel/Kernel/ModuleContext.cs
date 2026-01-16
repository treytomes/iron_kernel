namespace IronKernel.Kernel;

internal static class ModuleContext
{
	public static readonly AsyncLocal<Type?> CurrentModule = new();
}
