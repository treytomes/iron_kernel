namespace Userland.Morphic.Commands;

/// <summary>
/// Represents an executable, undoable intent.
/// Commands do not directly mutate global state unless explicitly executed.
/// </summary>
public interface ICommand
{
	/// <summary>
	/// Returns true if the command is currently valid for execution.
	/// </summary>
	bool CanExecute();

	/// <summary>
	/// Executes the command.
	/// </summary>
	void Execute();

	/// <summary>
	/// Can the command be undone?
	/// </summary>
	bool CanUndo();

	/// <summary>
	/// Reverts the effects of a previously executed command.
	/// </summary>
	void Undo();
}