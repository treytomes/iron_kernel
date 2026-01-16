namespace IronKernel.Kernel;

public sealed class ModuleTask
{
	public string Name { get; }
	public Task Task { get; set; } = null!;
	public ModuleTaskKind Kind { get; }
	public CancellationTokenSource WatchdogCts { get; }

	public volatile ModuleTaskState State;

	public ModuleTask(
		string name,
		ModuleTaskKind kind,
		CancellationTokenSource watchdogCts
	)
	{
		Name = name;
		Kind = kind;
		WatchdogCts = watchdogCts;
		State = ModuleTaskState.Running;
	}
}
