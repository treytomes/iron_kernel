using IronKernel.Kernel.State;

namespace IronKernel.Kernel;

public interface IKernelModule : IAsyncDisposable
{
	Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken);
}

