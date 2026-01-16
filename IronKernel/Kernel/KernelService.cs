using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public sealed class KernelService : BackgroundService
{
	#region Fields

	private readonly IHostApplicationLifetime _appLifetime;
	private readonly ILogger<KernelService> _logger;
	private readonly IKernelState _state;
	private readonly IKernelMessageBus _bus;
	private readonly IServiceProvider _services;
	private readonly List<ModuleHost> _modules = new();
	private int _fatalFaulted;

	private readonly CancellationTokenSource _kernelCts = new();
	private readonly List<IDisposable> _subscriptions = new();

	#endregion

	#region Constructors

	public KernelService(
		ILogger<KernelService> logger,
		IServiceProvider services,
		IKernelState state,
		IKernelMessageBus bus,
		IHostApplicationLifetime appLifetime)
	{
		_logger = logger;
		_services = services;
		_state = state;
		_bus = bus;
		_appLifetime = appLifetime;
	}

	#endregion

	#region Execution

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(
				stoppingToken,
				_kernelCts.Token);

		_subscriptions.Add(_bus.SubscribeKernel<ModuleFaulted>(OnModuleFaulted));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleTaskSlow>(OnModuleTaskSlow));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleTaskHung>(OnModuleTaskHung));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleMessageFlooded>(OnModuleMessageFlooded));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleTaskAbandoned>(OnModuleTaskAbandoned));

		_logger.LogInformation("Kernel starting");
		_bus.Publish(new KernelStarting());

		try
		{
			foreach (var moduleType in DiscoverModules())
			{
				var host = await StartModuleAsync(
					moduleType,
					linkedCts.Token);

				_modules.Add(host);
			}

			_bus.Publish(new KernelStarted());

			// Kernel stays alive until cancelled (fault or host shutdown)
			await Task.Delay(Timeout.Infinite, linkedCts.Token);
		}
		catch (OperationCanceledException)
		{
			// Normal shutdown path
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Kernel crashed");
			throw;
		}
		finally
		{
			_logger.LogInformation("Kernel stopping");

			_bus.Publish(new KernelStopping());

			foreach (var module in _modules)
			{
				await StopModuleAsync(module);
			}

			_modules.Clear();

			_bus.Publish(new KernelStopped());

			DisposeSubscriptions();
		}
	}

	private IEnumerable<Type> DiscoverModules()
	{
		return _services
			.GetServices<IKernelModule>()
			.Select(m => m.GetType())
			.Distinct();
	}

	private async Task<ModuleHost> StartModuleAsync(Type moduleType, CancellationToken token)
	{
		var scope = _services.CreateScope();

		var module = scope.ServiceProvider
			.GetServices<IKernelModule>()
			.First(m => m.GetType() == moduleType);

		var runtime = new ModuleRuntime(
			moduleType,
			_bus,
			_logger);

		await module.StartAsync(
			_state,
			runtime,
			token);

		var host = new ModuleHost(
			moduleType,
			scope,
			module,
			runtime);

		_bus.Publish(new ModuleStarted(moduleType));

		return host;
	}

	private async Task StopModuleAsync(ModuleHost host)
	{
		try
		{
			await host.Runtime.WaitAllAsync();
		}
		catch { }

		host.Scope.Dispose();

		_bus.Publish(new ModuleStopped(host.ModuleType));
	}

	#endregion

	#region Fault Handling

	private Task OnModuleFaulted(ModuleFaulted fault, CancellationToken ct)
	{
		if (Interlocked.Exchange(ref _fatalFaulted, 1) != 0)
			return Task.CompletedTask;

		_logger.LogError(
			fault.Exception,
			"Module {Module} task '{Task}' faulted",
			fault.ModuleType.Name,
			fault.TaskName);

		// Default microkernel policy: fatal fault
		_kernelCts.Cancel();
		_appLifetime.StopApplication();

		return Task.CompletedTask;
	}

	private Task OnModuleTaskSlow(ModuleTaskSlow msg, CancellationToken _)
	{
		_logger.LogWarning(
			"Module {Module} task '{Task}' is slow ({Duration})",
			msg.ModuleType.Name,
			msg.TaskName,
			msg.Duration);

		return Task.CompletedTask;
	}

	private Task OnModuleTaskHung(ModuleTaskHung msg, CancellationToken _)
	{
		if (Interlocked.Exchange(ref _fatalFaulted, 1) != 0)
			return Task.CompletedTask;

		_logger.LogCritical(
			"Module {Module} task '{Task}' is hung after {Duration}",
			msg.ModuleType.Name,
			msg.TaskName,
			msg.Duration);

		// Hung task is unrecoverable in-process
		_kernelCts.Cancel();
		_appLifetime.StopApplication();

		return Task.CompletedTask;
	}

	private Task OnModuleMessageFlooded(ModuleMessageFlooded msg, CancellationToken _)
	{
		if (Interlocked.Exchange(ref _fatalFaulted, 1) != 0)
			return Task.CompletedTask;

		_logger.LogCritical(
			"Module {Module} flooded {Message} ({Count} messages in {Window})",
			msg.ModuleType.Name,
			msg.MessageType.Name,
			msg.Count,
			msg.Window);

		_kernelCts.Cancel();
		_appLifetime.StopApplication();

		return Task.CompletedTask;
	}

	private Task OnModuleTaskAbandoned(
		ModuleTaskAbandoned msg,
		CancellationToken _)
	{
		_logger.LogWarning(
			"Module {Module} task '{Task}' was abandoned during shutdown",
			msg.ModuleType.Name,
			msg.TaskName);

		// Default policy: informational, not fatal
		// Future policies could escalate or quarantine modules

		return Task.CompletedTask;
	}

	#endregion

	#region Shutdown

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Kernel stopping");
		_bus.Publish(new KernelStopping());

		// Modules will be disposed by DI container
		foreach (var module in _modules)
		{
			_bus.Publish(new ModuleStopped(module.GetType()));
		}

		_bus.Publish(new KernelStopped());

		DisposeSubscriptions();
		_kernelCts.Dispose();

		await base.StopAsync(cancellationToken);
	}

	private void DisposeSubscriptions()
	{
		foreach (var sub in _subscriptions)
		{
			sub.Dispose();
		}
		_subscriptions.Clear();
	}

	#endregion
}
