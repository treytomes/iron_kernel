namespace IronKernel.Modules.ApplicationHost;

public interface IUserApplication
{
	Task RunAsync(
		IApplicationContext context,
		CancellationToken ct);
}
