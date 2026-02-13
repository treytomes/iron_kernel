namespace IronKernel.Common;

public interface IApplicationScheduler
{
	/// <summary>
	/// Run a supervised task.
	/// </summary>
	Task RunAsync(
		string name,
		ApplicationTaskKind kind,
		Func<CancellationToken, Task> work,
		CancellationToken stoppingToken);
}
