using IronKernel.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace IronKernel;

internal sealed class Program
{
	private IServiceProvider HostServices = null!;

	public static async Task<int> Main(params string[] args)
	{
		return await new Program().BootstrapAsync(args);
	}

	private async Task<int> BootstrapAsync(string[] args)
	{
		// Define command-line options.
		var configFileOption = new Option<string>(
			name: "--config",
			description: "Path to the configuration file",
			getDefaultValue: () => "appsettings.json");

		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Enable debug mode");

		// Create root command.
		var rootCommand = new RootCommand("AI NPC Example");
		rootCommand.AddOption(configFileOption);
		rootCommand.AddOption(debugOption);

		// Set handler for processing the command.
		rootCommand.SetHandler(async (configFile, debug) =>
		{
			try
			{
				var props = new CommandLineProps()
				{
					ConfigFile = configFile,
					Debug = debug,
				};

				// Build host with DI container.
				using var host = CreateHostBuilder(props).Build();
				await host.StartAsync();

				HostServices = host.Services;

				// Start the app.
				await StartAsync(props);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error starting the app: {ex.Message}");
				Environment.Exit(1);
			}
		},
		configFileOption, debugOption);

		// Parse the command line.
		return await rootCommand.InvokeAsync(args);
	}

	private async Task StartAsync(CommandLineProps props)
	{
		Console.WriteLine("Starting IronKernel...");

		if (HostServices == null) throw new NullReferenceException("Host services are not initialized.");

		using var scope = HostServices.CreateScope();
		var kernel = scope.ServiceProvider.GetRequiredService<Kernel.KernelService>();

		// Register demo module
		kernel.RegisterModule(new Demos.HelloModule());

		// Keep the process alive
		await Task.Delay(Timeout.Infinite);
	}

	private IHostBuilder CreateHostBuilder(CommandLineProps props)
	{
		return Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((hostContext, config) => ConfigureAppConfiguration(config, props))
			.ConfigureLogging(ConfigureLogging)
			.ConfigureServices(ConfigureServices);
	}

	private void ConfigureAppConfiguration(IConfigurationBuilder config, CommandLineProps props)
	{
		config.Sources.Clear();
		// config.SetBasePath(Directory.GetCurrentDirectory());
		config.SetBasePath(AppContext.BaseDirectory);
		config.AddJsonFile(props.ConfigFile, optional: false, reloadOnChange: false);

		// Add command line overrides.
		var commandLineConfig = new Dictionary<string, string?>();
		if (props.Debug)
		{
			commandLineConfig["Debug"] = "true";
		}

		config.AddInMemoryCollection(commandLineConfig);
	}

	private void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
	{
		logging.ClearProviders();

		// Disable console logging.
		// logging.AddConsole();

		// Set minimum log level based on debug setting.
		var debugEnabled = hostContext.Configuration.GetValue<bool>("Debug");
		var minLevel = debugEnabled ? LogLevel.Debug : LogLevel.Information;
		logging.SetMinimumLevel(minLevel);

		// Create log directory.
		var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
		Directory.CreateDirectory(logDir);

		// Timestamped log file (daily rotation).
		var logFile = Path.Combine(logDir, $"app-{DateTime.Now:yyyy-MM-dd}.log");

		// Add file logger.
		logging.AddProvider(new FileLoggerProvider(logFile, minLevel));
	}

	private void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
	{
		// Register configuration.
		services.Configure<AppSettings>(hostContext.Configuration);

		services.AddSingleton<HttpClient>();

		// Kernel
		services.AddSingleton<Kernel.KernelService>();
		services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<Kernel.KernelService>());
	}
}