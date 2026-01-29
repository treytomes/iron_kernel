namespace IronKernel.Userland.Morphic.Commands;

/// <summary>
/// Base class for commands that target a specific Morph.
/// This class represents intent, not policy.
/// </summary>
public abstract class MorphCommand : ICommand
{
	/// <summary>
	/// The morph this command is targeting.
	/// </summary>
	public Morph Target { get; }

	protected MorphCommand(Morph target)
	{
		Target = target ?? throw new ArgumentNullException(nameof(target));
	}

	public virtual bool CanExecute()
		=> Target is ICommandTarget commandTarget
		   && commandTarget.CanExecute(this);

	public virtual void Execute()
	{
		if (Target is ICommandTarget commandTarget)
			commandTarget.Execute(this);
	}

	public virtual void Undo()
	{
		if (Target is ICommandTarget commandTarget)
			commandTarget.Undo(this);
	}
}