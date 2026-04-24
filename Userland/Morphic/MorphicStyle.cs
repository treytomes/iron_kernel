using System.Drawing;
using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

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
	public required Color HaloOutline { get; init; }
	public required Color SelectionTint { get; init; }

	// Labels
	public required Color LabelForegroundColor { get; init; }
	public required Color? LabelBackgroundColor { get; init; }

	// Buttons
	public required Color ButtonBackgroundColor { get; init; }
	public required Color ButtonHoverBackgroundColor { get; init; }
	public required Color ButtonActiveBackgroundColor { get; init; }
	public required Color ButtonForegroundColor { get; init; }
	public required Color ButtonDisabledBackgroundColor { get; init; }
	public required Color ButtonDisabledForegroundColor { get; init; }

	public required FontStyle DefaultFontStyle { get; init; }

	public sealed class HandleStyle
	{
		public required Color Background { get; init; }
		public required Color BackgroundHover { get; init; }
		public required Color Foreground { get; init; }
		public required Color ForegroundHover { get; init; }
	}

	public sealed class FontStyle
	{
		public required string Url { get; init; }
		public required Size TileSize { get; init; }
		public required int GlyphOffset { get; init; }
	}
}
