using IronKernel.Kernel;

namespace IronKernel.Modules.ApplicationHost;

internal sealed class ApplicationRuntime : IApplicationScheduler
{
	private readonly IModuleRuntime _kernelRuntime;

	public ApplicationRuntime(IModuleRuntime kernelRuntime)
	{
		_kernelRuntime = kernelRuntime;
	}

	public Task RunAsync(
		string name,
		ApplicationTaskKind kind,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken)
	{
		var moduleKind =
			kind == ApplicationTaskKind.Finite
				? ModuleTaskKind.Finite
				: ModuleTaskKind.Resident;

		return _kernelRuntime.RunAsync(
			$"App:{name}",
			moduleKind,
			work,
			stoppingToken);
	}
}
