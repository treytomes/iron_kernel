using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace IronKernel.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
	#region Fields

	private readonly string _filePath;
	private readonly LogLevel _minLevel;
	private readonly BlockingCollection<string> _queue = new();
	private readonly Task _writerTask;
	private bool _disposed;

	#endregion

	#region Constructors

	public FileLoggerProvider(string filePath, LogLevel minLevel)
	{
		_filePath = filePath;
		_minLevel = minLevel;

		// Fire-and-forget writer loop
		_writerTask = Task.Run(async () =>
		{
			using var stream = new FileStream(
				_filePath,
				FileMode.Append,
				FileAccess.Write,
				FileShare.Read);

			using var writer = new StreamWriter(stream, Encoding.UTF8);

			foreach (string message in _queue.GetConsumingEnumerable())
			{
				await writer.WriteLineAsync(message);
				await writer.FlushAsync();
			}
		});
	}

	#endregion

	#region Methods

	public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _queue, _minLevel);

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_queue.CompleteAdding();
		_writerTask.Wait(2000);
	}

	#endregion
}
