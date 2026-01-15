namespace IronKernel.Kernel.State;

public interface IKernelState
{
	T Get<T>(string key, T defaultValue = default!);
	void Set<T>(string key, T value) where T : notnull;
	bool Contains(string key);
	T? Update<T>(string key, Func<T, T> updater, T defaultValue = default!) where T : notnull;
}
