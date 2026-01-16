namespace IronKernel.Kernel;

public interface IModuleRuntime
{
	Task RunAsync(
		string name,
		ModuleTaskKind kind,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken);

	void RunDetached(
		string name,
		ModuleTaskKind kind,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken);
}
