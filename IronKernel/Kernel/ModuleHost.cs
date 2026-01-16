using IronKernel.Kernel;
using Microsoft.Extensions.DependencyInjection;

namespace IronKernel;

internal sealed class ModuleHost(
	Type moduleType,
	IServiceScope scope,
	IKernelModule module,
	ModuleRuntime runtime
)
{
	public Type ModuleType => moduleType;
	public IServiceScope Scope => scope;
	public IKernelModule Module => module;
	public ModuleRuntime Runtime => runtime;
}
