namespace Userland.Morphic.Commands;

/// <summary>
/// Represents an intent to move a morph by a delta.
/// </summary>
public sealed class DeleteCommand : MorphCommand
{
	private Morph? _owner;
	private int _index;

	public DeleteCommand(Morph target)
		: base(target) { }

	public override void Execute()
	{
		_owner = Target.Owner;
		if (_owner == null) return;

		for (var n = 0; n < _owner.Submorphs.Count; n++)
		{
			if (_owner.Submorphs[n] == Target)
			{
				_index = n;
				break;
			}
		}

		_owner.RemoveMorph(Target);
	}

	public override void Undo()
	{
		if (_owner == null) return;
		_owner.AddMorph(Target, _index);
	}
}