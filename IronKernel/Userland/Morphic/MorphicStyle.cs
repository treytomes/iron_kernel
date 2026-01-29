using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class MorphicStyle
{
	public required HandleStyle MoveHandle { get; init; }
	public required HandleStyle ResizeHandle { get; init; }
	public required HandleStyle DeleteHandle { get; init; }
	public required RadialColor HaloOutline { get; init; }
	public required RadialColor SelectionTint { get; init; }

	public required RadialColor LabelForegroundColor { get; init; }
	public required RadialColor? LabelBackgroundColor { get; init; }

	public required RadialColor ButtonBackgroundColor { get; init; }
	public required RadialColor ButtonHoverBackgroundColor { get; init; }
	public required RadialColor ButtonActiveBackgroundColor { get; init; }
	public required RadialColor ButtonForegroundColor { get; init; }
	public required RadialColor ButtonDisabledBackgroundColor { get; init; }
	public required RadialColor ButtonDisabledForegroundColor { get; init; }

	public sealed class HandleStyle
	{
		public required RadialColor Background { get; init; }
		public required RadialColor BackgroundHover { get; init; }
		public required RadialColor Foreground { get; init; }
		public required RadialColor ForegroundHover { get; init; }
	}
}
