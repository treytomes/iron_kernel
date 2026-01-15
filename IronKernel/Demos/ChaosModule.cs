using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using IronKernel.Modules.Clock;
using Microsoft.Extensions.Logging;

public sealed class ChaosModule : IKernelModule
{
	private readonly ILogger<ChaosModule> _logger;
	private readonly IMessageBus _bus;

	private IDisposable? _subscription;

	public ChaosModule(
		ILogger<ChaosModule> logger,
		IMessageBus bus)
	{
		_logger = logger;
		_bus = bus;
	}

	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("ChaosModule started");

		// Uncomment ONE at a time during testing

		// 1. Crash during startup
		// throw new InvalidOperationException("Chaos: crash during StartAsync");

		_subscription = _bus.Subscribe<ChaosTrigger>(OnChaos);

		runtime.RunAsync(
			"DelayedCrash",
			async ct =>
			{
				await Task.Delay(2000, ct);
				throw new Exception("Chaos: background task crashed");
			},
			stoppingToken);

		return Task.CompletedTask;
	}

	private async Task OnChaos(ChaosTrigger msg, CancellationToken ct)
	{
		_logger.LogWarning("Chaos triggered: {Mode}", msg.Mode);

		switch (msg.Mode)
		{
			case "throw":
				// 3. Crash in message handler
				throw new Exception("Chaos: handler threw");

			case "slow":
				// 4. Starvation / slow handler
				await Task.Delay(TimeSpan.FromSeconds(10), ct);
				break;

			case "loop":
				// 5. Infinite loop ignoring cancellation
				while (true)
				{
					await Task.Delay(1000);
				}

			case "flood":
				// 6. Message flood
				for (int i = 0; i < 100_000; i++)
				{
					_bus.Publish(new Tick(DateTime.UtcNow));
				}
				break;

			case "cancel":
				// 7. Ignore cancellation explicitly
				await Task.Delay(TimeSpan.FromMinutes(5), CancellationToken.None);
				break;

			default:
				_logger.LogInformation("Unknown chaos mode");
				break;
		}
	}

	public ValueTask DisposeAsync()
	{
		_logger.LogInformation("ChaosModule disposed");
		_subscription?.Dispose();
		return ValueTask.CompletedTask;
	}
}
