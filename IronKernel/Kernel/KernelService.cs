using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IronKernel.Kernel;

public sealed class KernelService : BackgroundService, IKernel
{
	#region Fields

	private readonly ILogger<KernelService> _logger;
	private readonly List<IModule> _modules = new();

	#endregion

	#region Constructors

	public KernelService(ILogger<KernelService> logger)
	{
		_logger = logger;
	}

	#endregion

	#region Properties

	public ILogger Logger => _logger;

	#endregion

	#region Methods

	public void RegisterModule(IModule module)
	{
		_logger.LogInformation("Registering module: {Name}", module.Name);
		_modules.Add(module);
		module.Initialize(this);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Kernel started");

		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				foreach (var module in _modules)
				{
					try
					{
						module.Tick();
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Module {Name} threw during Tick.", module.Name);
					}
				}

				await Task.Delay(500, stoppingToken);
			}
		}
		catch (OperationCanceledException)
		{
			// Expected on shutdown.
		}
		finally
		{
			_logger.LogInformation("Kernel stopping");

			foreach (var module in _modules)
			{
				await module.DisposeAsync();
			}
		}
	}

	#endregion
}
