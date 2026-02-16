using Userland.Gfx;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;

namespace Userland.Morphic.Halo;

public sealed class ResizeHandleMorph(Morph target, ResizeHandle kind) : HandleMorph(target)
{
	#region Properties

	public ResizeHandle Kind => kind;
	protected override MorphicStyle.HandleStyle? StyleForHandle => Style?.ResizeHandle;
	protected override string AssetUrl => "asset://image.resize_icon";
	protected override RenderImage.RenderFlag RenderFlag => Kind is ResizeHandle.TopLeft or ResizeHandle.BottomRight
		? RenderImage.RenderFlag.FlipVertical
		: RenderImage.RenderFlag.None;

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
				world.Commands.Submit(new ResizeCommand(Target, Kind, dx, dy));
				StartMouse = e.Position;
			}
		}

		e.MarkHandled();
	}

	#endregion
}
