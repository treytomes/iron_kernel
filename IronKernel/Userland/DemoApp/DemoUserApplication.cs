using IronKernel.Modules.ApplicationHost;
using IronKernel.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IronKernel.Userland.Services;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.DemoApp;

public sealed class DemoUserApplication(
	ILogger<DemoUserApplication> kernelLogger
) : IUserApplication
{
	private readonly ILogger<DemoUserApplication> _kernelLogger = kernelLogger;

	public async Task RunAsync(
		IApplicationContext context,
		CancellationToken stoppingToken)
	{
		_kernelLogger.LogInformation(
			"Starting DemoUserApplication with isolated service provider");

		using var services = BuildUserServiceProvider(context);
		using var scope = services.CreateScope();

		var root = scope.ServiceProvider
			.GetRequiredService<DemoAppRoot>();

		await root.RunAsync(stoppingToken);
	}

	private static ServiceProvider BuildUserServiceProvider(
		IApplicationContext context)
	{
		var services = new ServiceCollection();

		// ------------------------------------------------------------------
		// Logging (userland-owned, console-only)
		// ------------------------------------------------------------------
		services.AddLogging(b =>
		{
			b.ClearProviders();
			b.AddProvider(new SimpleConsoleLoggerProvider());
			b.SetMinimumLevel(LogLevel.Information);
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
		services.AddSingleton<RenderingContext>();
		services.AddSingleton<AssetService>();

		// ------------------------------------------------------------------
		// App root
		// ------------------------------------------------------------------
		services.AddSingleton<DemoAppRoot>();

		return services.BuildServiceProvider(
			new ServiceProviderOptions
			{
				ValidateScopes = true,
				ValidateOnBuild = true
			});
	}
}