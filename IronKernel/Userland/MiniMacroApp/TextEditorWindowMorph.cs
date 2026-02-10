using System.Drawing;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Services;

namespace IronKernel.Userland.MiniMacro;

public sealed class TextEditorWindowMorph : WindowMorph
{
	private readonly TextDocument _doc = new(string.Empty);
	private readonly TextEditorMorph _editor;
	private readonly IFileSystem _fileSystem;

	public TextEditorWindowMorph(IFileSystem fileSystem)
		: base(Point.Empty, new Size(640, 400), "Text Editor")
	{
		_editor = new TextEditorMorph(_doc);
		Content.AddMorph(_editor);

		_fileSystem = fileSystem;
	}

	protected override void OnLoad(IAssetService assetService)
	{
		_fileSystem.ReadTextAsync("file://sample.ms").ContinueWith(response =>
		{
			var file = response.Result;
			_doc.SetText(file);
		});
		base.OnLoad(assetService);
	}
}