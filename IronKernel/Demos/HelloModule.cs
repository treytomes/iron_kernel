using IronKernel.Kernel;
using Microsoft.Extensions.Logging;

namespace IronKernel.Demos;

public sealed class HelloModule : IModule
{
	public string Name => "HelloDemo";

	private int _counter;

	public void Initialize(IKernel kernel)
	{
		kernel.Logger.LogInformation("HelloModule initialized");
	}

	public void Tick()
	{
		_counter++;
		Console.WriteLine($"Hello from kernel tick {_counter}");
	}

	public ValueTask DisposeAsync()
	{
		Console.WriteLine("HelloModule disposed");
		return ValueTask.CompletedTask;
	}
}
