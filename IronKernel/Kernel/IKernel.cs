using IronKernel.Kernel.State;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public interface IKernel
{
	ILogger Logger { get; }
	IKernelState State { get; }

	void RegisterModule(IKernelModule module);
}
