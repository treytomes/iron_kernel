namespace IronKernel.Modules.AssetLoader;

/// <summary>
/// Base interface for all resource loaders.
/// </summary>
public interface IResourceLoader
{
	/// <summary>
	/// Loads a resource from the specified path.
	/// </summary>
	/// <param name="resourceManager">The resource manager.</param>
	/// <param name="path">The path to the resource.</param>
	/// <returns>The loaded resource.</returns>
	object Load(IResourceManager resourceManager, string path);
}

/// <summary>
/// Interface for type-specific resource loaders.
/// </summary>
/// <typeparam name="T">The type of resource this loader handles.</typeparam>
public interface IResourceLoader<T> : IResourceLoader
{
	/// <summary>
	/// Loads a resource of the specified type from the given path.
	/// </summary>
	/// <param name="resourceManager">The resource manager.</param>
	/// <param name="path">The path to the resource.</param>
	/// <returns>The loaded resource.</returns>
	new T Load(IResourceManager resourceManager, string path);

	/// <summary>
	/// Default implementation to satisfy the base interface.
	/// </summary>
	/// <param name="resourceManager"></param>
	/// <param name="path"></param>
	/// <returns></returns>
	object IResourceLoader.Load(IResourceManager resourceManager, string path)
	{
		return Load(resourceManager, path) ?? throw new NullReferenceException("Null resource loaded.");
	}
}