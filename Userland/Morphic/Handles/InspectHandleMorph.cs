using Userland.Morphic.Events;
using Userland.Morphic.Inspector;

namespace Userland.Morphic.Halo;

public sealed class InspectHandleMorph(Morph target) : HandleMorph(target)
{
	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.InspectHandle;
	protected override string AssetUrl => "asset://image.inspect_icon";

	#endregion

	#region Methods

	public override void OnPointerUp(PointerUpEvent e)
	{
		base.OnPointerUp(e);
		if (TryGetWorld(out var world))
		{
			world.ClearSelection();
			var inspector = new InspectorMorph(Target, world.ColorDepth);
			world.AddMorph(inspector);
			inspector.CenterOnOwner();
		}
	}

	#endregion
}
