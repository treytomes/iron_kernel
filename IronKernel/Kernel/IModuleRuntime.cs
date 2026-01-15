namespace IronKernel.Kernel;

public interface IModuleRuntime
{
	Task RunAsync(
		string name,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken);
}
