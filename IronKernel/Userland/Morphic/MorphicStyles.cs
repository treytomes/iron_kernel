using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public static class MorphicStyles
{
	public static readonly MorphicStyle Default = new()
	{
		// --- Semantic palette ---
		Semantic = new SemanticColors
		{
			Background = RadialColor.Black,
			Surface = RadialColor.DarkGray,
			Border = RadialColor.Gray,

			Text = RadialColor.White,
			MutedText = RadialColor.Gray,

			Primary = RadialColor.Cyan,
			PrimaryHover = RadialColor.Cyan.Lerp(RadialColor.White, 0.35f),
			PrimaryActive = RadialColor.Cyan.Lerp(RadialColor.Black, 0.35f),
			PrimaryMuted = RadialColor.Cyan.Lerp(RadialColor.DarkGray, 0.6f),

			Success = RadialColor.Green,
			SuccessHover = RadialColor.Green.Lerp(RadialColor.White, 0.35f),
			SuccessActive = RadialColor.Green.Lerp(RadialColor.Black, 0.35f),
			SuccessMuted = RadialColor.Green.Lerp(RadialColor.DarkGray, 0.6f),

			Danger = RadialColor.Red,
			DangerHover = RadialColor.Red.Lerp(RadialColor.White, 0.35f),
			DangerActive = RadialColor.Red.Lerp(RadialColor.Black, 0.35f),
			DangerMuted = RadialColor.Red.Lerp(RadialColor.DarkGray, 0.6f),

			Warning = RadialColor.Orange,
			WarningHover = RadialColor.Orange.Lerp(RadialColor.White, 0.35f),
			WarningActive = RadialColor.Orange.Lerp(RadialColor.Black, 0.35f),
			WarningMuted = RadialColor.Orange.Lerp(RadialColor.DarkGray, 0.6f),

			Info = RadialColor.Blue,
			InfoHover = RadialColor.Blue.Lerp(RadialColor.White, 0.35f),
			InfoActive = RadialColor.Blue.Lerp(RadialColor.Black, 0.35f),
			InfoMuted = RadialColor.Blue.Lerp(RadialColor.DarkGray, 0.6f),
		},

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
		// DefaultFontStyle = new()
		// {
		// 	AssetId = "image.oem437_8",
		// 	GlyphOffset = 0,
		// 	TileSize = new Size(8, 8),
		// },
		DefaultFontStyle = new()
		{
			AssetId = "image.my-font-light",
			GlyphOffset = -32,
			TileSize = new Size(5, 6),
		},

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