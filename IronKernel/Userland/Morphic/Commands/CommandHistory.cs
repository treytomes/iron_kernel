namespace IronKernel.Userland.Morphic.Commands;

/// <summary>
/// Maintains global undo/redo history.
/// </summary>
public sealed class CommandHistory
{
	private readonly Stack<ICommand> _undoStack = new();
	private readonly Stack<ICommand> _redoStack = new();

	public bool CanUndo => _undoStack.Count > 0;
	public bool CanRedo => _redoStack.Count > 0;

	/// <summary>
	/// Records a successfully executed command.
	/// </summary>
	public void Record(ICommand command)
	{
		_undoStack.Push(command);
		_redoStack.Clear();
	}

	public void Record(IEnumerable<ICommand> commands)
	{
		foreach (var cmd in commands) _undoStack.Push(cmd);
		_redoStack.Clear();
	}

	/// <summary>
	/// Undoes the most recent command.
	/// </summary>
	public void Undo()
	{
		if (_undoStack.Count == 0) return;

		var command = _undoStack.Pop();
		Console.WriteLine($"Undo: {command}");
		command.Undo();
		_redoStack.Push(command);
	}

	/// <summary>
	/// Redoes the most recently undone command.
	/// </summary>
	public void Redo()
	{
		if (_redoStack.Count == 0) return;

		var command = _redoStack.Pop();
		Console.WriteLine($"Redo: {command}");
		command.Execute();
		_undoStack.Push(command);
	}
}