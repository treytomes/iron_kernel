using IronKernel.Logging;
using IronKernel.Kernel;
using IronKernel.Kernel.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using IronKernel.State;
using IronKernel.Kernel.Bus;
using IronKernel.Modules.Clock;

namespace IronKernel;

internal sealed class Program
{
	public static async Task<int> Main(string[] args)
	{
		return await BuildCommandLine()
			.InvokeAsync(args);
	}

	private static RootCommand BuildCommandLine()
	{
		var configFileOption = new Option<string>(
			name: "--config",
			description: "Path to the configuration file",
			getDefaultValue: () => "appsettings.json");

		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Enable debug mode");

		var root = new RootCommand("IronKernel Demo Host");
		root.AddOption(configFileOption);
		root.AddOption(debugOption);

		root.SetHandler(async (configFile, debug) =>
		{
			var props = new CommandLineProps
			{
				ConfigFile = configFile,
				Debug = debug
			};

			using var host = CreateHostBuilder(props).Build();

			Console.WriteLine("Starting IronKernel...");
			await host.RunAsync();
		}, configFileOption, debugOption);

		return root;
	}

	private static IHostBuilder CreateHostBuilder(CommandLineProps props)
	{
		return Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((ctx, config) =>
				ConfigureAppConfiguration(config, props))
			.ConfigureLogging((ctx, logging) =>
				ConfigureLogging(ctx, logging))
			.ConfigureServices((ctx, services) =>
				ConfigureServices(ctx, services));
	}

	private static void ConfigureAppConfiguration(
		IConfigurationBuilder config,
		CommandLineProps props)
	{
		config.Sources.Clear();
		config.SetBasePath(AppContext.BaseDirectory);

		config.AddJsonFile(
			props.ConfigFile,
			optional: false,
			reloadOnChange: false);

		var overrides = new Dictionary<string, string?>
		{
			["Debug"] = props.Debug.ToString()
		};

		config.AddInMemoryCollection(overrides);
	}

	private static void ConfigureLogging(
		HostBuilderContext ctx,
		ILoggingBuilder logging)
	{
		logging.ClearProviders();

		logging.AddConsole();

		var debug = ctx.Configuration.GetValue<bool>("Debug");
		var minLevel = debug ? LogLevel.Debug : LogLevel.Information;
		logging.SetMinimumLevel(minLevel);

		var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
		Directory.CreateDirectory(logDir);

		var logFile = Path.Combine(
			logDir,
			$"app-{DateTime.UtcNow:yyyy-MM-dd}.log");

		logging.AddProvider(
			new FileLoggerProvider(logFile, minLevel));
	}

	private static void ConfigureServices(
		HostBuilderContext ctx,
		IServiceCollection services)
	{
		// Configuration
		services.Configure<AppSettings>(ctx.Configuration);

		// Kernel infrastructure
		services.AddSingleton<IKernelState, KernelStateStore>();
		services.AddSingleton<KernelService>();
		services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<KernelService>());

		services.AddSingleton<IMessageBus, MessageBus>();

		services.AddSingleton<IKernelModule, ClockModule>();
		services.AddSingleton<IKernelModule, HelloModule>();
		services.AddSingleton<IKernelModule, ChaosModule>();
	}
}
