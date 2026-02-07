using IronKernel.Common;

namespace IronKernel.Modules.ApplicationHost;

public sealed class ApplicationContext : IApplicationContext
{
	public IApplicationBus Bus { get; }
	public IApplicationScheduler Scheduler { get; }
	public IApplicationState State { get; }

	public ApplicationContext(
		IApplicationBus bus,
		IApplicationScheduler scheduler,
		IApplicationState state)
	{
		Bus = bus;
		Scheduler = scheduler;
		State = state;
	}
}
