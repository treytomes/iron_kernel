using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public static class MorphicStyles
{
	public static readonly MorphicStyle Default = new()
	{
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

		HaloOutline = RadialColor.Yellow,
		SelectionTint = RadialColor.Yellow.Lerp(RadialColor.White, 0.6f),

		LabelBackgroundColor = null,
		LabelForegroundColor = RadialColor.White,

		ButtonBackgroundColor = RadialColor.DarkGray,
		ButtonHoverBackgroundColor = RadialColor.Gray,
		ButtonActiveBackgroundColor = RadialColor.LightGray,
		ButtonForegroundColor = RadialColor.White,
		ButtonDisabledBackgroundColor = RadialColor.DarkSlateGray,
		ButtonDisabledForegroundColor = RadialColor.Gray,
	};
}
