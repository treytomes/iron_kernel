using System.Drawing;
using IronKernel.Userland.Morphic;

namespace IronKernel.Userland.DemoApp;

public sealed class TextEditorWindowMorph : WindowMorph
{
	private readonly TextEditorMorph _editor;

	public TextEditorWindowMorph()
		: base(Point.Empty, new Size(640, 400), "Text Editor")
	{
		_editor = new TextEditorMorph(new TextDocument(string.Empty));
		Content.AddMorph(_editor);
	}
}