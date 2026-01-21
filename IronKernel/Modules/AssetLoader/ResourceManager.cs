using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IronKernel.Modules.AssetLoader;

/// <summary>
/// Manages the loading and caching of game resources.
/// </summary>
class ResourceManager : IResourceManager, IDisposable
{
	#region Fields

	private readonly string _rootPath;
	private readonly Dictionary<Type, IResourceLoader> _resourceLoaders = new();
	private readonly Dictionary<string, object> _resources = new();
	private readonly ILogger<ResourceManager> _logger;
	private readonly object _lock = new();
	private bool _disposed;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceManager"/> class.
	/// </summary>
	/// <param name="settings">The application settings.</param>
	/// <param name="logger">The logger for this class.</param>
	public ResourceManager(IOptions<AppSettings> settings, ILogger<ResourceManager> logger)
	{
		if (settings == null)
		{
			throw new ArgumentNullException(nameof(settings));
		}
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_rootPath = settings.Value.AssetRoot;
		if (string.IsNullOrWhiteSpace(_rootPath))
		{
			throw new InvalidOperationException("Asset root path is not configured.");
		}

		// Ensure the root path exists
		if (!Directory.Exists(_rootPath))
		{
			_logger.LogWarning("Asset root directory does not exist: {RootPath}", _rootPath);
		}

		_logger.LogInformation("ResourceManager initialized with root path: {RootPath}", _rootPath);
	}

	#endregion

	#region IResourceManager Implementation

	/// <summary>
	/// Registers a resource loader for a specific resource type.
	/// </summary>
	/// <typeparam name="TResource">The type of resource to register a loader for.</typeparam>
	/// <typeparam name="TResourceLoader">The type of the resource loader.</typeparam>
	public void Register<TResource, TResourceLoader>()
		where TResourceLoader : IResourceLoader<TResource>, new()
	{
		lock (_lock)
		{
			var resourceType = typeof(TResource);
			if (_resourceLoaders.ContainsKey(resourceType))
			{
				_logger.LogWarning("Replacing existing resource loader for type {ResourceType}", resourceType.Name);
			}

			_resourceLoaders[resourceType] = new TResourceLoader();
			_logger.LogInformation("Registered resource loader {LoaderType} for resource type {ResourceType}", typeof(TResourceLoader).Name, resourceType.Name);
		}
	}

	public void Register<T>(IResourceLoader<T> loader)
	{
		if (loader == null)
		{
			throw new ArgumentNullException(nameof(loader));
		}

		lock (_lock)
		{
			var resourceType = typeof(T);
			if (_resourceLoaders.ContainsKey(resourceType))
			{
				_logger.LogWarning("Replacing existing resource loader for type {ResourceType}", resourceType.Name);
			}

			_resourceLoaders[resourceType] = loader;
			_logger.LogInformation("Registered resource loader {LoaderType} for resource type {ResourceType}", loader.GetType().Name, resourceType.Name);
		}
	}

	/// <summary>
	/// Loads a resource from the specified relative path.
	/// </summary>
	/// <typeparam name="T">The type of resource to load.</typeparam>
	/// <param name="relativePath">The path to the resource, relative to the root path.</param>
	/// <returns>The loaded resource.</returns>
	public T Load<T>(string relativePath)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			throw new ArgumentNullException(nameof(relativePath));
		}

		var key = $"{typeof(T).Name}.{relativePath}";

		lock (_lock)
		{
			// Check if the resource is already loaded
			if (_resources.TryGetValue(key, out var cachedResource))
			{
				_logger.LogDebug("Resource found in cache: {ResourceKey}", key);
				return (T)cachedResource;
			}

			// Resource not in cache, load it
			_logger.LogDebug("Loading resource: {ResourceKey}", key);
			var fullPath = GetResourcePath(relativePath);

			if (!File.Exists(fullPath))
			{
				_logger.LogError("Resource file not found: {FullPath}", fullPath);
				throw new FileNotFoundException($"Resource file not found: {fullPath}", fullPath);
			}

			var loader = GetResourceLoader<T>();

			try
			{
				var resource = loader.Load(this, fullPath);
				_resources.Add(key, resource);
				_logger.LogInformation("Successfully loaded resource: {ResourceKey}", key);
				return (T)resource;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load resource: {ResourceKey}", key);
				throw new InvalidOperationException($"Failed to load resource: {key}", ex);
			}
		}
	}

	/// <summary>
	/// Unloads a specific resource from the cache.
	/// </summary>
	/// <typeparam name="T">The type of resource to unload.</typeparam>
	/// <param name="relativePath">The path to the resource, relative to the root path.</param>
	/// <returns>True if the resource was unloaded, false if it wasn't in the cache.</returns>
	public bool Unload<T>(string relativePath)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			throw new ArgumentNullException(nameof(relativePath));
		}

		var key = $"{typeof(T).Name}.{relativePath}";

		lock (_lock)
		{
			if (_resources.TryGetValue(key, out var resource))
			{
				// Dispose the resource if it implements IDisposable
				if (resource is IDisposable disposable)
				{
					try
					{
						disposable.Dispose();
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error disposing resource: {ResourceKey}", key);
					}
				}

				_resources.Remove(key);
				_logger.LogInformation("Unloaded resource: {ResourceKey}", key);
				return true;
			}

			_logger.LogDebug("Resource not found for unloading: {ResourceKey}", key);
			return false;
		}
	}

	/// <summary>
	/// Clears all loaded resources from the cache.
	/// </summary>
	public void ClearCache()
	{
		lock (_lock)
		{
			_logger.LogInformation("Clearing resource cache containing {ResourceCount} items", _resources.Count);

			// Dispose any resources that implement IDisposable
			foreach (var resource in _resources.Values)
			{
				if (resource is IDisposable disposable)
				{
					try
					{
						disposable.Dispose();
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error disposing resource during cache clear");
					}
				}
			}

			_resources.Clear();
		}
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Gets the resource loader for the specified resource type.
	/// </summary>
	/// <typeparam name="T">The type of resource.</typeparam>
	/// <returns>The resource loader.</returns>
	private IResourceLoader GetResourceLoader<T>()
	{
		return GetResourceLoader(typeof(T));
	}

	/// <summary>
	/// Gets the resource loader for the specified resource type.
	/// </summary>
	/// <param name="type">The type of resource.</param>
	/// <returns>The resource loader.</returns>
	private IResourceLoader GetResourceLoader(Type type)
	{
		lock (_lock)
		{
			if (!_resourceLoaders.TryGetValue(type, out var loader))
			{
				_logger.LogError("No resource loader available for type: {ResourceType}", type.Name);
				throw new InvalidOperationException($"No resource loader registered for type: {type.Name}");
			}

			return loader;
		}
	}

	/// <summary>
	/// Gets the full path to a resource.
	/// </summary>
	/// <param name="relativePath">The path relative to the root path.</param>
	/// <returns>The full path to the resource.</returns>
	private string GetResourcePath(string relativePath)
	{
		return Path.Combine(_rootPath, relativePath);
	}

	#endregion

	#region IDisposable Implementation

	/// <summary>
	/// Disposes the resource manager and all loaded disposable resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes the resource manager and optionally disposes managed resources.
	/// </summary>
	/// <param name="disposing">Whether to dispose managed resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			_logger.LogInformation("Disposing ResourceManager");
			ClearCache();
		}

		_disposed = true;
	}

	#endregion
}