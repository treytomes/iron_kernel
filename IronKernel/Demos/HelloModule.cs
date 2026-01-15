using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using IronKernel.Modules.Clock;
using Microsoft.Extensions.Logging;

public sealed class HelloModule : IKernelModule
{
	private readonly ILogger<HelloModule> _logger;
	private readonly IMessageBus _bus;

	private readonly List<IDisposable> _subscriptions = new();

	public HelloModule(
		ILogger<HelloModule> logger,
		IMessageBus bus)
	{
		_logger = logger;
		_bus = bus;
	}

	public Task StartAsync(IKernelState state, IModuleRuntime runtime, CancellationToken stoppingToken)
	{
		_subscriptions.Add(_bus.Subscribe<Tick>(OnTick));

		_subscriptions.Add(_bus.Subscribe<KernelStarting>(OnKernelStarting));
		_subscriptions.Add(_bus.Subscribe<KernelStarted>(OnKernelStarted));
		_subscriptions.Add(_bus.Subscribe<KernelStopping>(OnKernelStopping));
		_subscriptions.Add(_bus.Subscribe<KernelStopped>(OnKernelStopped));

		_subscriptions.Add(_bus.Subscribe<ModuleStarted>(OnModuleStarted));
		_subscriptions.Add(_bus.Subscribe<ModuleStopped>(OnModuleStopped));

		_logger.LogInformation("HelloModule started");

		return Task.CompletedTask;
	}

	#region Message Handlers

	private Task OnTick(Tick tick, CancellationToken ct)
	{
		_logger.LogInformation("Hello! The time is {Time}", tick.UtcNow);
		return Task.CompletedTask;
	}

	private Task OnKernelStarting(KernelStarting _, CancellationToken ct)
	{
		_logger.LogInformation("Kernel is starting");
		return Task.CompletedTask;
	}

	private Task OnKernelStarted(KernelStarted _, CancellationToken ct)
	{
		_logger.LogInformation("Kernel has started");
		return Task.CompletedTask;
	}

	private Task OnKernelStopping(KernelStopping _, CancellationToken ct)
	{
		_logger.LogInformation("Kernel is stopping");
		return Task.CompletedTask;
	}

	private Task OnKernelStopped(KernelStopped _, CancellationToken ct)
	{
		_logger.LogInformation("Kernel has stopped");
		return Task.CompletedTask;
	}

	private Task OnModuleStarted(ModuleStarted msg, CancellationToken ct)
	{
		_logger.LogDebug("Module started: {Module}", msg.Module.Name);
		return Task.CompletedTask;
	}

	private Task OnModuleStopped(ModuleStopped msg, CancellationToken ct)
	{
		_logger.LogDebug("Module stopped: {Module}", msg.Module.Name);
		return Task.CompletedTask;
	}

	#endregion

	public ValueTask DisposeAsync()
	{
		foreach (var sub in _subscriptions)
		{
			sub.Dispose();
		}

		_subscriptions.Clear();

		_logger.LogInformation("HelloModule disposed");

		return ValueTask.CompletedTask;
	}
}
