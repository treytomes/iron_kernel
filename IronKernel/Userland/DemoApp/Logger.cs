using Microsoft.Extensions.Logging;

internal sealed class SimpleConsoleLoggerProvider
	: ILoggerProvider
{
	public ILogger CreateLogger(string categoryName)
		=> new SimpleConsoleLogger(categoryName);

	public void Dispose() { }
}

internal sealed class SimpleConsoleLogger : ILogger
{
	private readonly string _category;

	public SimpleConsoleLogger(string category)
	{
		_category = category;
	}

	public IDisposable BeginScope<TState>(TState state)
		where TState : notnull
		=> NullScope.Instance;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		Console.WriteLine(
			$"[{logLevel}] {_category}: {formatter(state, exception)}");

		if (exception != null)
			Console.WriteLine(exception);
	}

	private sealed class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new();
		public void Dispose() { }
	}
}