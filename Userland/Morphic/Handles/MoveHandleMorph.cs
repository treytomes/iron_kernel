using Userland.Morphic.Commands;
using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public sealed class MoveHandleMorph(Morph target) : HandleMorph(target)
{
	#region Properties

	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.MoveHandle;
	protected override string AssetUrl => "asset://image.move_icon";

	#endregion

	#region Methods

	public override void OnPointerMove(PointerMoveEvent e)
	{
		var dx = e.Position.X - StartMouse.X;
		var dy = e.Position.Y - StartMouse.Y;

		if (dx != 0 || dy != 0)
		{
			if (TryGetWorld(out var world))
			{
				world.Commands.Submit(new MoveCommand(Target, dx, dy));
				StartMouse = e.Position;
			}
		}

		e.MarkHandled();
	}

	#endregion
}
