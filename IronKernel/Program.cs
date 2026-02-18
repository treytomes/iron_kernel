using IronKernel.Common;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Logging;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Modules.AssetLoader;
using IronKernel.Modules.Framebuffer;
using IronKernel.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTK.Mathematics;
using System.CommandLine;
using System.Reflection;

namespace IronKernel;

internal sealed class Program
{
	public static async Task<int> Main(string[] args)
	{
		return await BuildCommandLine().InvokeAsync(args);
	}

	private static RootCommand BuildCommandLine()
	{
		var userlandPathOption = new Option<string>(
			name: "--userland",
			description: "Path to the userland assembly",
			getDefaultValue: () =>
			{
				return Path.Combine(
					AppContext.BaseDirectory,
					"userland",
					"Userland.dll");
			}
		);

		var configFileOption = new Option<string>(
			name: "--config",
			description: "Path to the configuration file",
			getDefaultValue: () => "appsettings.json");

		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Enable debug mode");

		var root = new RootCommand("IronKernel Host");
		root.AddOption(userlandPathOption);
		root.AddOption(configFileOption);
		root.AddOption(debugOption);

		root.SetHandler(async (userlandPath, configFile, debug) =>
		{
			var props = new CommandLineProps
			{
				UserlandPath = userlandPath,
				ConfigFile = configFile,
				Debug = debug
			};

			using var host = CreateHostBuilder(props).Build();
			var logger = host.Services.GetRequiredService<ILogger<Program>>();
			var kernel = host.Services.GetRequiredService<KernelService>();
			var cts = new CancellationTokenSource();
			var ct = cts.Token;

			Console.CancelKeyPress += (sender, e) =>
			{
				e.Cancel = true;        // prevent hard process kill
				cts.Cancel();           // signal kernel shutdown
			};

			Console.WriteLine($"Starting {nameof(IronKernel)}...");
			await kernel.StartAsync(cts.Token);
		}, userlandPathOption, configFileOption, debugOption);

		return root;
	}

	private static IHostBuilder CreateHostBuilder(CommandLineProps props)
	{
		return Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((ctx, config) => ConfigureAppConfiguration(config, props))
			.ConfigureLogging(ConfigureLogging)
			.ConfigureServices((ctx, services) => ConfigureServices(ctx, services, props.UserlandPath));
	}

	private static void ConfigureAppConfiguration(IConfigurationBuilder config, CommandLineProps props)
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

	private static void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder logging)
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

	private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services, string userlandPath)
	{
		// Configuration
		services.Configure<AppSettings>(ctx.Configuration);
		services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

		// Kernel infrastructure
		services.AddSingleton<IKernelState, KernelStateStore>();
		services.AddSingleton<KernelService>();

		services.AddSingleton<IKernelMessageBus, MessageBus>();
		services.AddSingleton<IMessageBus>(sp => sp.GetRequiredService<IKernelMessageBus>());

		// Register IVirtualDisplay as a factory.
		services.AddSingleton<IVirtualDisplay>(serviceProvider =>
		{
			// Get window settings from configuration.
			var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

			// Get or create window.
			var windowSize = new Vector2i(
				appSettings.Window.Width,
				appSettings.Window.Height
			);

			// Create virtual display settings.
			var virtualDisplaySettings = new AppSettings.VirtualDisplaySettings
			{
				Width = appSettings.VirtualDisplay.Width,
				Height = appSettings.VirtualDisplay.Height,
				VertexShaderPath = appSettings.VirtualDisplay.VertexShaderPath,
				FragmentShaderPath = appSettings.VirtualDisplay.FragmentShaderPath
			};

			// Create and return the virtual display.
			return new VirtualDisplay(windowSize, virtualDisplaySettings);
		});

		services.AddSingleton<IResourceManager, ResourceManager>();

		var kernelAssembly = Assembly.GetExecutingAssembly();

		var moduleTypes = kernelAssembly
			.GetTypes()
			.Where(t =>
				typeof(IKernelModule).IsAssignableFrom(t) &&
				t.IsClass &&
				!t.IsAbstract)
			.ToList();

		foreach (var moduleType in moduleTypes)
		{
			services.AddSingleton(typeof(IKernelModule), moduleType);
		}

		var userlandDir = Path.GetDirectoryName(userlandPath)!;
		var alc = new UserlandLoadContext(userlandPath);
		var userlandAssembly = alc.LoadFromAssemblyPath(userlandPath);

		var appTypes = userlandAssembly
			.GetTypes()
			.Where(t =>
				typeof(IUserApplication).IsAssignableFrom(t) &&
				!t.IsAbstract &&
				t.IsClass)
			.ToList();

		var appType = appTypes.Single();

		services.AddSingleton(typeof(IUserApplication), appType);
		services.AddSingleton<IUserApplicationFactory>(sp => new ReflectionUserApplicationFactory(appType, sp));
	}
}
