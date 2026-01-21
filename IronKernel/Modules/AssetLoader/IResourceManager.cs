namespace IronKernel.Modules.AssetLoader;

/// <summary>
/// Defines the contract for a resource manager that loads and caches game resources.
/// </summary>
public interface IResourceManager
{
	/// <summary>
	/// Registers a resource loader for a specific resource type.
	/// </summary>
	/// <typeparam name="TResource">The type of resource to register a loader for.</typeparam>
	/// <typeparam name="TResourceLoader">The type of the resource loader.</typeparam>
	void Register<TResource, TResourceLoader>()
		where TResourceLoader : IResourceLoader<TResource>, new();

	void Register<T>(IResourceLoader<T> loader);

	/// <summary>
	/// Loads a resource from the specified relative path.
	/// </summary>
	/// <typeparam name="T">The type of resource to load.</typeparam>
	/// <param name="relativePath">The path to the resource, relative to the root path.</param>
	/// <returns>The loaded resource.</returns>
	T Load<T>(string relativePath);

	/// <summary>
	/// Unloads a specific resource from the cache.
	/// </summary>
	/// <typeparam name="T">The type of resource to unload.</typeparam>
	/// <param name="relativePath">The path to the resource, relative to the root path.</param>
	/// <returns>True if the resource was unloaded, false if it wasn't in the cache.</returns>
	bool Unload<T>(string relativePath);

	/// <summary>
	/// Clears all loaded resources from the cache.
	/// </summary>
	void ClearCache();
}