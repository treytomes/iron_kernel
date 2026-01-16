namespace IronKernel.Kernel;

internal sealed class ModuleTask(string name, Task task)
{
	public string Name => name;
	public Task Task => task;
}
