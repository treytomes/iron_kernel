namespace Userland.Morphic.Commands;

public class ActionCommand : ICommand
{
	#region Constructors

	public ActionCommand(Action execute, Action? undo = null, Func<bool>? canExecute = null)
	{
		ExecuteAction = execute;
		CanExecutePredicate = canExecute;
		UndoAction = undo;
	}

	#endregion

	#region Properties

	public Action ExecuteAction { get; }
	public Func<bool>? CanExecutePredicate;
	public Action? UndoAction { get; }

	#endregion

	#region Methods

	public bool CanExecute()
	{
		return CanExecutePredicate?.Invoke() ?? true;
	}

	public void Execute()
	{
		ExecuteAction?.Invoke();
	}

	public bool CanUndo()
	{
		return UndoAction != null;
	}

	public void Undo()
	{
		UndoAction?.Invoke();
	}

	#endregion
}