using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace IronKernel.Logging;

public sealed class FileLogger : ILogger
{
	#region Fields

	private readonly string _category;
	private readonly BlockingCollection<string> _queue;
	private readonly LogLevel _minLevel;

	#endregion

	#region Constructors

	public FileLogger(string category, BlockingCollection<string> queue, LogLevel minLevel)
	{
		_category = category;
		_queue = queue;
		_minLevel = minLevel;
	}

	#endregion

	#region Methods

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel)) return;

		var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

		var msg = $"{timestamp} [{logLevel}] {_category}: {formatter(state, exception)}";

		if (exception != null) msg += Environment.NewLine + exception;

		_queue.Add(msg);
	}

	#endregion
}
