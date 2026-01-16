namespace IronKernel.Modules.ApplicationHost;

public interface IApplicationContext
{
	IApplicationBus Bus { get; }
	IApplicationScheduler Scheduler { get; }
	IApplicationState State { get; }
}
