namespace Userland.Morphic.Commands;

/// <summary>
/// Implemented by objects that can accept, execute,
/// and undo commands.
/// </summary>
public interface ICommandTarget
{
	/// <summary>
	/// Determines whether this target is willing to execute the command.
	/// </summary>
	bool CanExecute(ICommand command);

	/// <summary>
	/// Executes the command, applying internal rules and constraints.
	/// </summary>
	void Execute(ICommand command);

	/// <summary>
	/// Reverts the effects of the command.
	/// </summary>
	void Undo(ICommand command);
}