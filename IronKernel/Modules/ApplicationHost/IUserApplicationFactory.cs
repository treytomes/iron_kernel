using IronKernel.Common;

namespace IronKernel.Modules.ApplicationHost;

public interface IUserApplicationFactory
{
	IUserApplication Create();
}