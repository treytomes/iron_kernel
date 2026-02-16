using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public sealed class DeleteHandleMorph(Morph target) : HandleMorph(target)
{
	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.DeleteHandle;
	protected override string AssetUrl => "asset://image.delete_icon";

	#endregion

	#region Methods

	public override void OnPointerUp(PointerUpEvent e)
	{
		if (TryGetWorld(out var world)) world.ReleasePointer(this);

		var owner = Target.Owner;
		if (owner == null) return;
		Target.MarkForDeletion();
		e.MarkHandled();
	}

	#endregion
}
