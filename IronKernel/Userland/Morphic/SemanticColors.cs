using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class SemanticColors
{
	// Core surfaces
	public required RadialColor Background { get; init; }
	public required RadialColor Surface { get; init; }
	public required RadialColor Border { get; init; }

	// Text
	public required RadialColor Text { get; init; }
	public required RadialColor SecondaryText { get; init; }
	public required RadialColor MutedText { get; init; }

	// Primary intent
	public required RadialColor Primary { get; init; }
	public required RadialColor PrimaryHover { get; init; }
	public required RadialColor PrimaryActive { get; init; }
	public required RadialColor PrimaryMuted { get; init; }

	// Success intent
	public required RadialColor Success { get; init; }
	public required RadialColor SuccessHover { get; init; }
	public required RadialColor SuccessActive { get; init; }
	public required RadialColor SuccessMuted { get; init; }

	// Danger intent
	public required RadialColor Danger { get; init; }
	public required RadialColor DangerHover { get; init; }
	public required RadialColor DangerActive { get; init; }
	public required RadialColor DangerMuted { get; init; }

	// Warning intent
	public required RadialColor Warning { get; init; }
	public required RadialColor WarningHover { get; init; }
	public required RadialColor WarningActive { get; init; }
	public required RadialColor WarningMuted { get; init; }

	// Info intent
	public required RadialColor Info { get; init; }
	public required RadialColor InfoHover { get; init; }
	public required RadialColor InfoActive { get; init; }
	public required RadialColor InfoMuted { get; init; }

	public static readonly SemanticColors DefaultSemantic = new()
	{
		// Core surfaces
		Background = RadialColor.Black,
		Surface = RadialColor.DarkGray,
		Border = RadialColor.Gray,

		// Text
		Text = RadialColor.White,
		SecondaryText = RadialColor.Yellow,
		MutedText = RadialColor.Gray,

		// Primary (focus / selected)
		Primary = RadialColor.Cyan,
		PrimaryHover = RadialColor.Cyan.Lerp(RadialColor.White, 0.35f),
		PrimaryActive = RadialColor.Cyan.Lerp(RadialColor.Black, 0.35f),
		PrimaryMuted = RadialColor.Cyan.Lerp(RadialColor.DarkGray, 0.6f),

		// Success (affirmative)
		Success = RadialColor.Green,
		SuccessHover = RadialColor.Green.Lerp(RadialColor.White, 0.35f),
		SuccessActive = RadialColor.Green.Lerp(RadialColor.Black, 0.35f),
		SuccessMuted = RadialColor.Green.Lerp(RadialColor.DarkGray, 0.6f),

		// Danger (destructive)
		Danger = RadialColor.Red,
		DangerHover = RadialColor.Red.Lerp(RadialColor.White, 0.35f),
		DangerActive = RadialColor.Red.Lerp(RadialColor.Black, 0.35f),
		DangerMuted = RadialColor.Red.Lerp(RadialColor.DarkGray, 0.6f),

		// Warning (caution)
		Warning = RadialColor.Orange,
		WarningHover = RadialColor.Orange.Lerp(RadialColor.White, 0.35f),
		WarningActive = RadialColor.Orange.Lerp(RadialColor.Black, 0.35f),
		WarningMuted = RadialColor.Orange.Lerp(RadialColor.DarkGray, 0.6f),

		// Info (informational)
		Info = RadialColor.Blue,
		InfoHover = RadialColor.Blue.Lerp(RadialColor.White, 0.35f),
		InfoActive = RadialColor.Blue.Lerp(RadialColor.Black, 0.35f),
		InfoMuted = RadialColor.Blue.Lerp(RadialColor.DarkGray, 0.6f),
	};
}