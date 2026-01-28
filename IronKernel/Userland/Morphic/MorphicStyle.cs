using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class MorphicStyle
{
	public required HandleStyle MoveHandle { get; init; }
	public required HandleStyle ResizeHandle { get; init; }
	public required HandleStyle DeleteHandle { get; init; }
	public required RadialColor HaloOutline { get; init; }
	public required RadialColor SelectionTint { get; init; }

	public sealed class HandleStyle
	{
		public required RadialColor Background { get; init; }
		public required RadialColor BackgroundHover { get; init; }
		public required RadialColor Foreground { get; init; }
		public required RadialColor ForegroundHover { get; init; }
	}
}
