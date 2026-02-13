namespace Userland.Morphic.Commands;

/// <summary>
/// Represents a group of commands that execute and undo atomically.
/// </summary>
public sealed class CommandTransaction : ICommand
{
	private readonly List<ICommand> _commands = new();

	public bool IsEmpty => _commands.Count == 0;

	/// <summary>
	/// Adds a command to the transaction.
	/// </summary>
	public void Add(ICommand command)
	{
		if (command.CanUndo()) _commands.Add(command);
	}

	/// <summary>
	/// Determines whether all commands can execute.
	/// </summary>
	public bool CanExecute()
	{
		// foreach (var command in _commands)
		// {
		// 	if (!command.CanExecute())
		// 		return false;
		// }
		// return true;

		// Already executed; nothing to do
		return true;
	}

	/// <summary>
	/// Executes all commands in order.
	/// </summary>
	public void Execute()
	{
		// foreach (var command in _commands)
		// 	command.Execute();

		// Intentionally empty.
		// Commands were executed at submission time.
	}

	public bool CanUndo() => true;

	/// <summary>
	/// Undoes all commands in reverse order.
	/// </summary>
	public void Undo()
	{
		for (int i = _commands.Count - 1; i >= 0; i--)
			_commands[i].Undo();
	}

	public override string? ToString()
	{
		return $"[{string.Join(',', _commands)}]";
	}
}