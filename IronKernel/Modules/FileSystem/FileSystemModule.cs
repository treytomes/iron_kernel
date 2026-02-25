using System.ComponentModel.DataAnnotations;
using IronKernel.Common.ValueObjects;
using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.FileSystem.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.FileSystem;

internal sealed class FileSystemModule(
	AppSettings settings,
	IMessageBus bus,
	ILogger<FileSystemModule> logger
) : IKernelModule
{
	#region Fields

	private readonly AppSettings _settings = settings;
	private readonly IMessageBus _bus = bus;
	private readonly ILogger<FileSystemModule> _logger = logger;

	private string _userRoot = null!;

	#endregion

	#region Methods

	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		_userRoot = InitializeUserRoot();

		_logger.LogInformation(
			"FileSystemModule initialized at {UserRoot}",
			_userRoot);

		_bus.Subscribe<DirectoryCreateCommand>(
			runtime,
			"DirectoryWriteHandler",
			HandleDirCreateAsync);

		_bus.Subscribe<FileExistsQuery>(
			runtime,
			"FileExistsHandler",
			HandleExistsAsync);

		_bus.Subscribe<FileReadQuery>(
			runtime,
			"FileReadHandler",
			HandleReadAsync);

		_bus.Subscribe<FileWriteCommand>(
			runtime,
			"FileWriteHandler",
			HandleWriteAsync);

		_bus.Subscribe<FileDeleteCommand>(
			runtime,
			"FileDeleteHandler",
			HandleDeleteAsync);

		_bus.Subscribe<DirectoryListQuery>(
			runtime,
			"DirectoryListHandler",
			HandleDirectoryListAsync);

		return Task.CompletedTask;
	}

	#region Handlers

	private Task HandleDirCreateAsync(DirectoryCreateCommand msg, CancellationToken ct)
	{
		if (!TryResolvePath(msg.Url, out var path, out var error))
		{
			_logger.LogWarning("Exists denied for {Url}: {Error}", msg.Url, error);
			_bus.Publish(new DirectoryCreateResult(
				msg.CorrelationID,
				msg.Url,
				false,
				error));
		}

		var exists = Directory.Exists(path);
		if (exists)
		{
			_bus.Publish(new DirectoryCreateResult(
				msg.CorrelationID,
				msg.Url,
				false,
				"The directory already exists."));
			return Task.CompletedTask;
		}

		var newDir = Directory.CreateDirectory(path);
		_bus.Publish(new DirectoryCreateResult(
			msg.CorrelationID,
			msg.Url,
			newDir.Exists,
			null));
		return Task.CompletedTask;
	}

	private Task HandleExistsAsync(FileExistsQuery msg, CancellationToken ct)
	{
		if (!TryResolvePath(msg.Url, out var path, out var error))
		{
			_logger.LogWarning("Exists denied for {Url}: {Error}", msg.Url, error);
			_bus.Publish(new FileExistsResponse(
				msg.CorrelationID,
				msg.Url,
				false));
			return Task.CompletedTask;
		}

		// Console.WriteLine($"Does it exist? {path}, {File.Exists(path)}, {Directory.Exists(path)}, {File.Exists(path) || Directory.Exists(path)}");

		_bus.Publish(new FileExistsResponse(
			msg.CorrelationID,
			msg.Url,
			File.Exists(path) || Directory.Exists(path)));
		return Task.CompletedTask;
	}

	private Task HandleReadAsync(FileReadQuery msg, CancellationToken ct)
	{
		if (!TryResolvePath(msg.Url, out var path, out var error))
		{
			_logger.LogWarning("Read denied for {Url}: {Error}", msg.Url, error);
			_bus.Publish(new FileReadResponse(
				msg.CorrelationID,
				msg.Url,
				null,
				string.Empty));
			return Task.CompletedTask;
		}

		if (!File.Exists(path))
		{
			_logger.LogWarning("File not found: {Path}", path);
			_bus.Publish(new FileReadResponse(
				msg.CorrelationID,
				msg.Url,
				null,
				string.Empty));
			return Task.CompletedTask;
		}

		byte[] data = File.ReadAllBytes(path);
		string? mime = GuessMimeType(path);

		_bus.Publish(new FileReadResponse(
			msg.CorrelationID,
			msg.Url,
			data,
			mime));

		return Task.CompletedTask;
	}

	private Task HandleWriteAsync(FileWriteCommand msg, CancellationToken ct)
	{
		if (!TryResolvePath(msg.Url, out var path, out var error))
		{
			_bus.Publish(new FileWriteResult(
				msg.CorrelationID,
				msg.Url,
				false,
				error));
			return Task.CompletedTask;
		}

		try
		{
			Directory.CreateDirectory(
				Path.GetDirectoryName(path)!);

			File.WriteAllBytes(path, msg.Data);
			_logger.LogInformation($"File write succeeded: {path}");

			_bus.Publish(new FileWriteResult(
				msg.CorrelationID,
				msg.Url,
				true,
				null));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "File write failed: {Path}", path);

			_bus.Publish(new FileWriteResult(
				msg.CorrelationID,
				msg.Url,
				false,
				ex.Message));
		}

		return Task.CompletedTask;
	}

	private Task HandleDeleteAsync(FileDeleteCommand msg, CancellationToken ct)
	{
		if (!TryResolvePath(msg.Url, out var path, out var error))
		{
			_bus.Publish(new FileDeleteResult(
				msg.CorrelationID,
				msg.Url,
				false,
				error));
			return Task.CompletedTask;
		}

		try
		{
			var isFile = File.Exists(path);
			var isDir = !isFile && Directory.Exists(path);

			if (isDir)
			{
				Directory.Delete(path);
				_bus.Publish(new FileDeleteResult(
					msg.CorrelationID,
					msg.Url,
					true,
					null));
			}
			else if (isFile)
			{
				File.Delete(path);
				_bus.Publish(new FileDeleteResult(
					msg.CorrelationID,
					msg.Url,
					true,
					null));
			}
			else
			{
				throw new FileNotFoundException("File does not exist.", msg.Url);
			}
		}
		catch (Exception ex)
		{
			_bus.Publish(new FileDeleteResult(
				msg.CorrelationID,
				msg.Url,
				false,
				ex.Message));
		}

		return Task.CompletedTask;
	}

	private Task HandleDirectoryListAsync(DirectoryListQuery msg, CancellationToken ct)
	{
		if (!TryResolvePath(msg.Url, out var path, out var error))
		{
			_logger.LogWarning("Directory list denied: {Error}", error);
			_bus.Publish(new DirectoryListResponse(
				msg.CorrelationID,
				msg.Url,
				[]));
			return Task.CompletedTask;
		}

		if (!Directory.Exists(path))
		{
			_logger.LogWarning("Directory not found: {Path}", path);
			_bus.Publish(new DirectoryListResponse(
				msg.CorrelationID,
				msg.Url,
				[]));
			return Task.CompletedTask;
		}

		var entries = new List<DirectoryEntry>();

		try
		{
			foreach (var dir in Directory.GetDirectories(path))
			{
				var info = new DirectoryInfo(dir);
				entries.Add(new DirectoryEntry(
					info.Name,
					IsDirectory: true,
					Size: null,
					LastModified: info.LastWriteTimeUtc
				));
			}

			foreach (var file in Directory.GetFiles(path))
			{
				var info = new FileInfo(file);
				entries.Add(new DirectoryEntry(
					info.Name,
					IsDirectory: false,
					Size: info.Length,
					LastModified: info.LastWriteTimeUtc
				));
			}

			_bus.Publish(new DirectoryListResponse(
				msg.CorrelationID,
				msg.Url,
				entries));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to list directory: {Path}", path);
			_bus.Publish(new DirectoryListResponse(
				msg.CorrelationID,
				msg.Url,
				[]));
		}

		return Task.CompletedTask;
	}

	#endregion

	#region Path Resolution

	private string InitializeUserRoot()
	{
		var baseDir =
			Environment.GetFolderPath(
				Environment.SpecialFolder.ApplicationData);

		var appDir = Path.Combine(baseDir, nameof(IronKernel));
		var userDir = Path.Combine(appDir, _settings.UserFileRoot);

		Directory.CreateDirectory(userDir);
		return Path.GetFullPath(userDir);
	}

	private bool TryResolvePath(string url, out string fullPath, out string? error)
	{
		fullPath = string.Empty;
		error = null;

		if (!url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
		{
			error = "Unsupported URL scheme.";
			return false;
		}

		var relative = url["file://".Length..]
			.Replace('/', Path.DirectorySeparatorChar)
			.TrimStart(Path.DirectorySeparatorChar);

		if (relative.Contains(".."))
		{
			error = "Path traversal is not allowed.";
			return false;
		}

		fullPath = Path.GetFullPath(
			Path.Combine(_userRoot, relative));

		if (!fullPath.StartsWith(_userRoot, StringComparison.Ordinal))
		{
			error = "Resolved path escapes user root.";
			return false;
		}

		return true;
	}

	#endregion

	#region Utilities

	private static string? GuessMimeType(string path)
	{
		var ext = Path.GetExtension(path).ToLowerInvariant();
		return ext switch
		{
			".txt" => "text/plain",
			".json" => "application/json",
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".bin" => "application/octet-stream",
			_ => null
		};
	}

	public ValueTask DisposeAsync()
	{
		_logger.LogInformation("FileSystem disposed.");
		return ValueTask.CompletedTask;
	}

	#endregion

	#endregion
}