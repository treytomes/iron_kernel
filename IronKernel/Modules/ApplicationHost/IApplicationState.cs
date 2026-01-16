namespace IronKernel.Modules.ApplicationHost;

public interface IApplicationState
{
	/// <summary>
	/// Retrieve a value from application state.
	/// </summary>
	bool TryGet<T>(string key, out T value);

	/// <summary>
	/// Set or replace a value in application state.
	/// </summary>
	void Set<T>(string key, T value);

	/// <summary>
	/// Remove a value from application state.
	/// </summary>
	bool Remove(string key);
}
