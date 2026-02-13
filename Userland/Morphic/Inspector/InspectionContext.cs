namespace Userland.Morphic.Inspector;

/// <summary>
/// Represents a single inspection state in the Inspector navigation stack.
/// </summary>
internal sealed record InspectionContext(
	object Target,
	string Label,
	Action<object>? Commit = null
);