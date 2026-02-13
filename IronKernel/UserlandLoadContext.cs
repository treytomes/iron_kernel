using System.Reflection;
using System.Runtime.Loader;

namespace IronKernel;

sealed class UserlandLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver _resolver;

	public UserlandLoadContext(string mainAssemblyPath)
	{
		_resolver = new AssemblyDependencyResolver(mainAssemblyPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		// 🔑 Force shared contract assemblies to unify
		if (assemblyName.Name == "IronKernel.Common" ||
			assemblyName.Name!.StartsWith("Microsoft.Extensions."))
		{
			return null; // force default ALC
		}

		var path = _resolver.ResolveAssemblyToPath(assemblyName);
		if (path != null)
			return LoadFromAssemblyPath(path);

		return null;
	}
}
