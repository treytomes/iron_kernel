using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic;

public interface ISyntaxHighlighter
{
	/// <summary>
	/// Returns a color for the character at (line, column),
	/// or null to use the default text color.
	/// </summary>
	Color? GetForeground(
		TextDocument document,
		int line,
		int column);
}
