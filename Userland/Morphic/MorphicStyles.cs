using System.Drawing;
using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

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
			Background = Color.Orange,
			BackgroundHover = Color.Orange.Lerp(Color.Black, 0.35f),
			Foreground = Color.Orange.Lerp(Color.White, 0.5f),
			ForegroundHover = Color.Orange.Lerp(Color.White, 0.35f),
		},
		ResizeHandle = new()
		{
			Background = Color.Cyan,
			BackgroundHover = Color.Cyan.Lerp(Color.Black, 0.35f),
			Foreground = Color.Cyan.Lerp(Color.White, 0.5f),
			ForegroundHover = Color.Cyan.Lerp(Color.White, 0.35f),
		},
		DeleteHandle = new()
		{
			Background = Color.Red,
			BackgroundHover = Color.Red.Lerp(Color.Black, 0.35f),
			Foreground = Color.Red.Lerp(Color.White, 0.5f),
			ForegroundHover = Color.Red.Lerp(Color.White, 0.35f),
		},
		InspectHandle = new()
		{
			Background = Color.Green,
			BackgroundHover = Color.Green.Lerp(Color.Black, 0.35f),
			Foreground = Color.Green.Lerp(Color.White, 0.5f),
			ForegroundHover = Color.Green.Lerp(Color.White, 0.35f),
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
		HaloOutline = Color.Yellow,
		SelectionTint = Color.Yellow.Lerp(Color.White, 0.6f),

		// --- Labels ---
		LabelForegroundColor = Color.White,
		LabelBackgroundColor = null,

		// --- Buttons (derived from semantic Primary) ---
		ButtonBackgroundColor = Color.DarkGray,
		ButtonHoverBackgroundColor = Color.Gray,
		ButtonActiveBackgroundColor = Color.LightGray,
		ButtonForegroundColor = Color.White,
		ButtonDisabledBackgroundColor = Color.DarkGray.Lerp(Color.Black, 0.4f),
		ButtonDisabledForegroundColor = Color.Gray,
	};
}
