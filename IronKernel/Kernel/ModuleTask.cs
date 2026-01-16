namespace IronKernel.Kernel;

public sealed class ModuleTask
{
	public string Name { get; }
	public Task Task { get; }
	public ModuleTaskKind Kind { get; }
	public CancellationTokenSource WatchdogCts { get; }

	public volatile ModuleTaskState State;

	public ModuleTask(
		string name,
		Task task,
		ModuleTaskKind kind,
		CancellationTokenSource watchdogCts)
	{
		Name = name;
		Task = task;
		Kind = kind;
		WatchdogCts = watchdogCts;
		State = ModuleTaskState.Running;
	}
}
