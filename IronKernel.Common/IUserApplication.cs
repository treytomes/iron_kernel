namespace IronKernel.Common;

public interface IUserApplication
{
	Task RunAsync(
		IApplicationContext context,
		CancellationToken ct
	);
}
