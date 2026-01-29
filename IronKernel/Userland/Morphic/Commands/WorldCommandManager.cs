namespace IronKernel.Userland.Morphic.Commands;

/// <summary>
/// Central authority for deferred command execution,
/// transactions, and global undo/redo.
/// </summary>
public sealed class WorldCommandManager
{
	private readonly CommandQueue _queue = new();
	private readonly CommandHistory _history = new();

	private CommandTransaction? _activeTransaction;

	public bool CanUndo => _history.CanUndo;
	public bool CanRedo => _history.CanRedo;

	/// <summary>
	/// Submits a command for deferred execution.
	/// </summary>
	public void Submit(ICommand command)
	{
		if (!command.CanExecute())
			return;

		if (_activeTransaction != null)
		{
			command.Execute();
			_activeTransaction.Add(command);
			return;
		}

		_queue.Enqueue(command);
	}

	/// <summary>
	/// Executes all queued commands.
	/// Called by the World during its update/layout phase.
	/// </summary>
	public void Flush()
	{
		_queue.FlushAndRecord(_history);
	}

	/// <summary>
	/// Begins a new command transaction.
	/// </summary>
	public void BeginTransaction()
	{
		if (_activeTransaction != null)
			throw new InvalidOperationException("Transaction already active.");

		_activeTransaction = new CommandTransaction();
	}

	/// <summary>
	/// Commits the active transaction for deferred execution.
	/// </summary>
	public void CommitTransaction()
	{
		if (_activeTransaction == null)
			return;

		if (!_activeTransaction.IsEmpty)
		{
			_history.Record(_activeTransaction);
		}
		_activeTransaction = null;
	}

	/// <summary>
	/// Cancels the active transaction without executing it.
	/// </summary>
	public void CancelTransaction()
	{
		_activeTransaction = null;
	}

	/// <summary>
	/// Undoes the most recently executed command or transaction.
	/// </summary>
	public void Undo()
	{
		Console.WriteLine("Undo");
		_history.Undo();
	}

	/// <summary>
	/// Redoes the most recently undone command or transaction.
	/// </summary>
	public void Redo()
	{
		Console.WriteLine("Redo");
		_history.Redo();
	}
}