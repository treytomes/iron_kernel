using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic;

public sealed class SemanticColors
{
	// Core surfaces
	public required Color Background { get; init; }
	public required Color Surface { get; init; }
	public required Color Border { get; init; }

	// Text
	public required Color Text { get; init; }
	public required Color SecondaryText { get; init; }
	public required Color MutedText { get; init; }

	// Primary intent
	public required Color Primary { get; init; }
	public required Color PrimaryHover { get; init; }
	public required Color PrimaryActive { get; init; }
	public required Color PrimaryMuted { get; init; }

	// Success intent
	public required Color Success { get; init; }
	public required Color SuccessHover { get; init; }
	public required Color SuccessActive { get; init; }
	public required Color SuccessMuted { get; init; }

	// Danger intent
	public required Color Danger { get; init; }
	public required Color DangerHover { get; init; }
	public required Color DangerActive { get; init; }
	public required Color DangerMuted { get; init; }

	// Warning intent
	public required Color Warning { get; init; }
	public required Color WarningHover { get; init; }
	public required Color WarningActive { get; init; }
	public required Color WarningMuted { get; init; }

	// Info intent
	public required Color Info { get; init; }
	public required Color InfoHover { get; init; }
	public required Color InfoActive { get; init; }
	public required Color InfoMuted { get; init; }

	public static readonly SemanticColors DefaultSemantic = new()
	{
		// Core surfaces
		Background = Color.Black,
		Surface = Color.DarkGray,
		Border = Color.Gray,

		// Text
		Text = Color.White,
		SecondaryText = Color.Yellow,
		MutedText = Color.Gray,

		// Primary (focus / selected)
		Primary = Color.Cyan,
		PrimaryHover = Color.Cyan.Lerp(Color.White, 0.35f),
		PrimaryActive = Color.Cyan.Lerp(Color.Black, 0.35f),
		PrimaryMuted = Color.Cyan.Lerp(Color.DarkGray, 0.6f),

		// Success (affirmative)
		Success = Color.Green,
		SuccessHover = Color.Green.Lerp(Color.White, 0.35f),
		SuccessActive = Color.Green.Lerp(Color.Black, 0.35f),
		SuccessMuted = Color.Green.Lerp(Color.DarkGray, 0.6f),

		// Danger (destructive)
		Danger = Color.Red,
		DangerHover = Color.Red.Lerp(Color.White, 0.35f),
		DangerActive = Color.Red.Lerp(Color.Black, 0.35f),
		DangerMuted = Color.Red.Lerp(Color.DarkGray, 0.6f),

		// Warning (caution)
		Warning = Color.Orange,
		WarningHover = Color.Orange.Lerp(Color.White, 0.35f),
		WarningActive = Color.Orange.Lerp(Color.Black, 0.35f),
		WarningMuted = Color.Orange.Lerp(Color.DarkGray, 0.6f),

		// Info (informational)
		Info = Color.Blue,
		InfoHover = Color.Blue.Lerp(Color.White, 0.35f),
		InfoActive = Color.Blue.Lerp(Color.Black, 0.35f),
		InfoMuted = Color.Blue.Lerp(Color.DarkGray, 0.6f),
	};
}
