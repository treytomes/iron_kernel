
using IronKernel.Userland.Morphic.Commands;

namespace IronKernel.Userland.Morphic.ValueObjects;

public sealed record ToolbarItem(
	string Label,
	ICommand Command
);