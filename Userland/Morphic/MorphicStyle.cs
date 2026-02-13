using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace Userland.Morphic;

public sealed class MorphicStyle
{
	// Semantic palette (new)
	public required SemanticColors Semantic { get; init; }

	// Handles
	public required HandleStyle MoveHandle { get; init; }
	public required HandleStyle ResizeHandle { get; init; }
	public required HandleStyle DeleteHandle { get; init; }
	public required HandleStyle InspectHandle { get; init; }

	// Structural UI
	public required RadialColor HaloOutline { get; init; }
	public required RadialColor SelectionTint { get; init; }

	// Labels
	public required RadialColor LabelForegroundColor { get; init; }
	public required RadialColor? LabelBackgroundColor { get; init; }

	// Buttons
	public required RadialColor ButtonBackgroundColor { get; init; }
	public required RadialColor ButtonHoverBackgroundColor { get; init; }
	public required RadialColor ButtonActiveBackgroundColor { get; init; }
	public required RadialColor ButtonForegroundColor { get; init; }
	public required RadialColor ButtonDisabledBackgroundColor { get; init; }
	public required RadialColor ButtonDisabledForegroundColor { get; init; }

	public required FontStyle DefaultFontStyle { get; init; }

	public sealed class HandleStyle
	{
		public required RadialColor Background { get; init; }
		public required RadialColor BackgroundHover { get; init; }
		public required RadialColor Foreground { get; init; }
		public required RadialColor ForegroundHover { get; init; }
	}

	public sealed class FontStyle
	{
		public required string Url { get; init; }
		public required Size TileSize { get; init; }
		public required int GlyphOffset { get; init; }
	}
}