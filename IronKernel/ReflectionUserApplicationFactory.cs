using IronKernel.Common;
using IronKernel.Modules.ApplicationHost;
using Microsoft.Extensions.DependencyInjection;

namespace IronKernel;

public sealed class ReflectionUserApplicationFactory
	: IUserApplicationFactory
{
	private readonly Type _appType;
	private readonly IServiceProvider _services;

	public ReflectionUserApplicationFactory(
		Type appType,
		IServiceProvider services)
	{
		_appType = appType;
		_services = services;
	}

	public IUserApplication Create()
	{
		return (IUserApplication)ActivatorUtilities.CreateInstance(_services, _appType);
	}
}