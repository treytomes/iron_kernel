using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace Userland.Morphic;

public static class MorphicStyles
{
	public static readonly MorphicStyle Default = new()
	{
		// --- Semantic palette ---
		Semantic = SemanticColors.DefaultSemantic,

		// --- Handles ---
		MoveHandle = new()
		{
			Background = RadialColor.Orange,
			BackgroundHover = RadialColor.Orange.Lerp(RadialColor.Black, 0.35f),
			Foreground = RadialColor.Orange.Lerp(RadialColor.White, 0.5f),
			ForegroundHover = RadialColor.Orange.Lerp(RadialColor.White, 0.35f),
		},
		ResizeHandle = new()
		{
			Background = RadialColor.Cyan,
			BackgroundHover = RadialColor.Cyan.Lerp(RadialColor.Black, 0.35f),
			Foreground = RadialColor.Cyan.Lerp(RadialColor.White, 0.5f),
			ForegroundHover = RadialColor.Cyan.Lerp(RadialColor.White, 0.35f),
		},
		DeleteHandle = new()
		{
			Background = RadialColor.Red,
			BackgroundHover = RadialColor.Red.Lerp(RadialColor.Black, 0.35f),
			Foreground = RadialColor.Red.Lerp(RadialColor.White, 0.5f),
			ForegroundHover = RadialColor.Red.Lerp(RadialColor.White, 0.35f),
		},
		InspectHandle = new()
		{
			Background = RadialColor.Green,
			BackgroundHover = RadialColor.Green.Lerp(RadialColor.Black, 0.35f),
			Foreground = RadialColor.Green.Lerp(RadialColor.White, 0.5f),
			ForegroundHover = RadialColor.Green.Lerp(RadialColor.White, 0.35f),
		},
		DefaultFontStyle = new()
		{
			Url = "asset://image.screen_font_medium",
			GlyphOffset = 0,
			TileSize = new Size(13, 19),
		},
		// DefaultFontStyle = new()
		// {
		// 	Url = "asset://image.screen_font",
		// 	GlyphOffset = 0,
		// 	TileSize = new Size(16, 24),
		// },
		// DefaultFontStyle = new()
		// {
		// 	Url = "asset://image.my-font-light",
		// 	GlyphOffset = -32,
		// 	TileSize = new Size(5, 6),
		// },

		// --- Structural ---
		HaloOutline = RadialColor.Yellow,
		SelectionTint = RadialColor.Yellow.Lerp(RadialColor.White, 0.6f),

		// --- Labels ---
		LabelForegroundColor = RadialColor.White,
		LabelBackgroundColor = null,

		// --- Buttons (derived from semantic Primary) ---
		ButtonBackgroundColor = RadialColor.DarkGray,
		ButtonHoverBackgroundColor = RadialColor.Gray,
		ButtonActiveBackgroundColor = RadialColor.LightGray,
		ButtonForegroundColor = RadialColor.White,
		ButtonDisabledBackgroundColor = RadialColor.DarkGray.Lerp(RadialColor.Black, 0.4f),
		ButtonDisabledForegroundColor = RadialColor.Gray,
	};
}