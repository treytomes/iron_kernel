namespace IronKernel.Userland.Morphic.Commands;

/// <summary>
/// Queues commands for deferred execution and records
/// successfully executed commands into history.
/// </summary>
public sealed class CommandQueue
{
	private readonly Queue<ICommand> _queue = new();

	/// <summary>
	/// Enqueues a command for later execution.
	/// </summary>
	public void Enqueue(ICommand command)
	{
		_queue.Enqueue(command);
	}

	/// <summary>
	/// Executes all queued commands and records them into history.
	/// </summary>
	public void FlushAndRecord(CommandHistory history)
	{
		while (_queue.Count > 0)
		{
			var command = _queue.Dequeue();

			if (!command.CanExecute())
				continue;

			command.Execute();
			history.Record(command);
		}
	}

	/// <summary>
	/// Clears all queued commands without executing them.
	/// </summary>
	public void Clear()
	{
		_queue.Clear();
	}
}