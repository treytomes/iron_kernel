using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public interface IKernel
{
	ILogger Logger { get; }

	void RegisterModule(IModule module);
}
