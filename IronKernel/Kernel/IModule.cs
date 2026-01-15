namespace IronKernel.Kernel;

public interface IModule : IAsyncDisposable
{
	string Name { get; }

	void Initialize(IKernel kernel);
	void Tick();
}
