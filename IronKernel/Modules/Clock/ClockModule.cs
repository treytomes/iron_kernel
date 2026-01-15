using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;

namespace IronKernel.Modules.Clock;

public sealed class ClockModule : IKernelModule
{
	private readonly IMessageBus _bus;
	private PeriodicTimer? _timer;

	public ClockModule(IMessageBus bus)
	{
		_bus = bus;
	}

	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		runtime.RunAsync(
			"ClockLoop",
			async ct =>
			{
				while (!ct.IsCancellationRequested)
				{
					await Task.Delay(1000, ct);
					_bus.Publish(new Tick(DateTime.UtcNow));
				}
			},
			stoppingToken);

		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		_timer?.Dispose();
		return ValueTask.CompletedTask;
	}
}
