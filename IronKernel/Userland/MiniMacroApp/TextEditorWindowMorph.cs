using System.Drawing;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Services;

namespace IronKernel.Userland.MiniMacro;

public sealed class TextEditorWindowMorph : WindowMorph
{
	private readonly TextDocument _doc = new(string.Empty);
	private readonly TextEditorMorph _editor;
	private readonly IWindowService _windowService;
	private readonly IFileSystem _fileSystem;

	public TextEditorWindowMorph(IWindowService windowService, IFileSystem fileSystem)
		: base(Point.Empty, new Size(640, 400), "Text Editor")
	{
		_editor = new TextEditorMorph(_doc);
		Content.AddMorph(_editor);

		_windowService = windowService;
		_fileSystem = fileSystem;
	}

	protected override void OnLoad(IAssetService assetService)
	{
		_windowService.PromptAsync("Filename:", "file://sample.ms")
			.ContinueWith(response =>
			{
				var filename = response.Result;
				if (!string.IsNullOrWhiteSpace(filename))
				{
					_windowService.ConfirmAsync($"Are you sure you want to load {filename}?")
						.ContinueWith(response =>
						{
							var result = response.Result;
							if (result)
							{
								_fileSystem.ReadTextAsync(filename).ContinueWith(response =>
								{
									var file = response.Result;
									_doc.SetText(file);
									_windowService.AlertAsync($"Loaded {filename}!");
								});
							}
							else
							{
								_windowService.AlertAsync("Operation cancelled.");
							}
						});
				}
			});
		base.OnLoad(assetService);
	}
}