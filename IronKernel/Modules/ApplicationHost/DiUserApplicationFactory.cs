using IronKernel.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IronKernel.Modules.ApplicationHost;

public sealed class DiUserApplicationFactory<TApp>
	: IUserApplicationFactory
	where TApp : IUserApplication
{
	private readonly IServiceProvider _services;

	public DiUserApplicationFactory(IServiceProvider services)
	{
		_services = services;
	}

	public IUserApplication Create()
		=> _services.GetRequiredService<TApp>();
}