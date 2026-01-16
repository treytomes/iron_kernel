using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public sealed class KernelService
{
	private readonly ILogger<KernelService> _logger;
	private readonly IServiceProvider _services;
	private readonly IKernelState _state;
	private readonly IKernelMessageBus _bus;
	private readonly IHostApplicationLifetime _lifetime;

	private readonly List<ModuleHost> _modules = new();
	private readonly List<IDisposable> _subscriptions = new();

	private readonly CancellationTokenSource _kernelCts = new();
	private int _shutdownRequested;
	private volatile bool _isShuttingDown;
	private int _fatalFaulted;

	public KernelService(
		ILogger<KernelService> logger,
		IServiceProvider services,
		IKernelState state,
		IKernelMessageBus bus,
		IHostApplicationLifetime lifetime)
	{
		_logger = logger;
		_services = services;
		_state = state;
		_bus = bus;
		_lifetime = lifetime;
	}

	/* ============================================================
     * Public entry point
     * ============================================================ */

	public async Task StartAsync(CancellationToken externalToken)
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(
				externalToken,
				_kernelCts.Token);

		HookShutdownSignals(externalToken);
		SubscribeKernelEvents();

		_logger.LogInformation("Kernel starting");
		_bus.Publish(new KernelStarting());

		try
		{
			foreach (var moduleType in DiscoverModules())
			{
				var host = await StartModuleAsync(moduleType, linkedCts.Token);
				_modules.Add(host);
			}

			_bus.Publish(new KernelStarted());

			var primaries = _modules
				.Select(m => m.Module)
				.OfType<IPrimaryKernelModule>()
				.ToList();

			if (primaries.Count > 1)
				throw new InvalidOperationException(
					$"Expected at most one primary module, found {primaries.Count}");

			var primary = primaries.SingleOrDefault();

			if (primary != null)
			{
				_logger.LogInformation(
					"Transferring control to primary module {Module}",
					primary.GetType().Name);

				// Blocks until the primary module exits
				primary.Run();
			}
			else
			{
				await Task.Delay(Timeout.Infinite, linkedCts.Token);
			}
		}
		catch (OperationCanceledException)
		{
			// expected during shutdown
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Kernel crashed");
			throw;
		}
		finally
		{
			await ShutdownAsync("Kernel exit");
		}
	}

	/* ============================================================
     * Shutdown orchestration
     * ============================================================ */

	private void HookShutdownSignals(CancellationToken externalToken)
	{
		Console.CancelKeyPress += (_, e) =>
		{
			e.Cancel = true;
			RequestShutdown("Ctrl+C");
		};

		_lifetime.ApplicationStopping.Register(() =>
			RequestShutdown("Host lifetime stopping"));

		externalToken.Register(() =>
			RequestShutdown("External cancellation"));
	}

	private void RequestShutdown(string reason)
	{
		if (Interlocked.Exchange(ref _shutdownRequested, 1) != 0)
			return;

		_logger.LogWarning("Kernel shutdown requested: {Reason}", reason);

		_bus.Publish(new KernelShutdownRequested(reason));
		_kernelCts.Cancel();
	}

	private async Task ShutdownAsync(string reason)
	{
		_logger.LogInformation("Kernel stopping: {Reason}", reason);

		_bus.Publish(new KernelStopping());
		_isShuttingDown = true;

		foreach (var module in _modules)
		{
			try
			{
				await module.Runtime.WaitAllAsync();
			}
			catch { }

			module.Scope.Dispose();
			_bus.Publish(new ModuleStopped(module.ModuleType));
		}

		_modules.Clear();

		_bus.Publish(new KernelStopped());

		DisposeSubscriptions();
		_kernelCts.Dispose();
	}

	/* ============================================================
     * Module lifecycle
     * ============================================================ */

	private IEnumerable<Type> DiscoverModules() =>
		_services
			.GetServices<IKernelModule>()
			.Select(m => m.GetType())
			.Distinct();

	private async Task<ModuleHost> StartModuleAsync(
		Type moduleType,
		CancellationToken token)
	{
		var scope = _services.CreateScope();

		var module = scope.ServiceProvider
			.GetServices<IKernelModule>()
			.First(m => m.GetType() == moduleType);

		var runtime = new ModuleRuntime(
			moduleType,
			_bus,
			_logger);

		await module.StartAsync(_state, runtime, token);

		_bus.Publish(new ModuleStarted(moduleType));

		return new ModuleHost(
			moduleType,
			scope,
			module,
			runtime);
	}

	/* ============================================================
     * Kernel supervision (THIS is the microkernel)
     * ============================================================ */

	private void SubscribeKernelEvents()
	{
		_subscriptions.Add(_bus.SubscribeKernel<ModuleFaulted>(OnModuleFaulted));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleTaskSlow>(OnModuleTaskSlow));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleTaskHung>(OnModuleTaskHung));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleMessageFlooded>(OnModuleMessageFlooded));
		_subscriptions.Add(_bus.SubscribeKernel<ModuleTaskAbandoned>(OnModuleTaskAbandoned));
	}

	private Task OnModuleFaulted(ModuleFaulted msg, CancellationToken _)
	{
		if (Interlocked.Exchange(ref _fatalFaulted, 1) != 0)
			return Task.CompletedTask;

		_logger.LogError(
			msg.Exception,
			"Module {Module} task '{Task}' faulted",
			msg.ModuleType.Name,
			msg.TaskName);

		RequestShutdown($"Module fault: {msg.ModuleType.Name}");
		_lifetime.StopApplication();

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
			"Module {Module} task '{Task}' hung after {Duration}",
			msg.ModuleType.Name,
			msg.TaskName,
			msg.Duration);

		RequestShutdown($"Hung task in {msg.ModuleType.Name}");
		_lifetime.StopApplication();

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

		RequestShutdown($"Message flood in {msg.ModuleType.Name}");
		_lifetime.StopApplication();

		return Task.CompletedTask;
	}

	private Task OnModuleTaskAbandoned(ModuleTaskAbandoned msg, CancellationToken _)
	{
		if (_isShuttingDown)
		{
			_logger.LogInformation(
				"Module {Module} task '{Task}' abandoned during shutdown",
				msg.ModuleType.Name,
				msg.TaskName);
		}
		else
		{
			_logger.LogWarning(
				"Module {Module} task '{Task}' abandoned unexpectedly",
				msg.ModuleType.Name,
				msg.TaskName);
		}

		return Task.CompletedTask;
	}

	/* ============================================================
     * Cleanup
     * ============================================================ */

	private void DisposeSubscriptions()
	{
		foreach (var sub in _subscriptions)
			sub.Dispose();

		_subscriptions.Clear();
	}
}
