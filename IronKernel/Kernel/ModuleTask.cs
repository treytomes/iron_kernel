namespace IronKernel.Kernel;

internal sealed record ModuleTask(
	string Name,
	Task Task,
	CancellationTokenSource WatchdogCts
);
