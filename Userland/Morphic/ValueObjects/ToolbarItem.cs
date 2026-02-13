
using Userland.Morphic.Commands;

namespace Userland.Morphic.ValueObjects;

public sealed record ToolbarItem(
	string Label,
	ICommand Command
);