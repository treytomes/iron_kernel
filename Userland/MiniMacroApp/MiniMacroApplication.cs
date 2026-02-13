using IronKernel.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IronKernel.Userland.Services;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic;

namespace IronKernel.Userland.MiniMacro;

public sealed class MiniMacroApplication(
// ILogger<MiniMacroApplication> kernelLogger
) : IUserApplication, IDisposable
{
	#region Constants

	private const LogLevel MIN_LOG_LEVEL = LogLevel.Trace;

	#endregion

	#region Fields

	// private readonly ILogger<MiniMacroApplication> _kernelLogger = kernelLogger;
	private ServiceProvider? _services;
	private IServiceScope? _scope;

	#endregion

	#region Methods

	public async Task RunAsync(IApplicationContext context, CancellationToken stoppingToken)
	{
		// _kernelLogger.LogInformation($"Starting {nameof(MiniMacroApplication)}.");

		_services = BuildUserServiceProvider(context);
		_scope = _services.CreateScope();

		var root = _scope.ServiceProvider.GetRequiredService<MiniMacroRoot>();

		await root.RunAsync(stoppingToken);
	}

	private static ServiceProvider BuildUserServiceProvider(IApplicationContext context)
	{
		var services = new ServiceCollection();

		// ------------------------------------------------------------------
		// Logging (userland-owned, console-only)
		// ------------------------------------------------------------------
		services.AddLogging(b =>
		{
			b.ClearProviders();
			b.AddProvider(new SimpleConsoleLoggerProvider());
			b.SetMinimumLevel(MIN_LOG_LEVEL);
		});

		// ------------------------------------------------------------------
		// Kernel bridge (explicit, minimal)
		// ------------------------------------------------------------------
		services.AddSingleton(context);
		services.AddSingleton(context.Bus);

		// ------------------------------------------------------------------
		// Userland services
		// ------------------------------------------------------------------
		services.AddSingleton<IFileSystem, FileSystemService>();
		services.AddSingleton<IRenderingContext, RenderingContext>();
		services.AddSingleton<IAssetService, AssetService>();
		services.AddSingleton<IWindowService, WindowService>();
		services.AddSingleton<IClipboardService, ClipboardService>();

		services.AddSingleton<WorldMorph>(sp =>
		{
			var rc = sp.GetRequiredService<IRenderingContext>();
			var assets = sp.GetRequiredService<IAssetService>();
			return new WorldMorph(rc.Size, assets, sp);
		});

		// ------------------------------------------------------------------
		// Launcher apps
		// ------------------------------------------------------------------
		services.AddTransient<DummyReplMorph>();
		services.AddTransient<MiniScriptReplMorph>();
		services.AddTransient<TextEditorWindowMorph>();

		// ------------------------------------------------------------------
		// App root
		// ------------------------------------------------------------------
		services.AddSingleton<MiniMacroRoot>();

		return services.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateScopes = true,
			ValidateOnBuild = true
		});
	}

	public void Dispose()
	{
		_scope?.Dispose();
		_services?.Dispose();
	}

	#endregion
}