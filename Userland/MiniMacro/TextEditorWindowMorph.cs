using System.Drawing;
using Microsoft.Extensions.Logging;
using Userland.Morphic;
using Userland.Morphic.Commands;
using Userland.Morphic.Layout;
using Userland.Morphic.ValueObjects;
using Userland.Services;

namespace Userland.MiniMacro;

public sealed class TextEditorWindowMorph : WindowMorph
{
	#region Fields

	private readonly ILogger<TextEditorWindowMorph> _logger;
	private readonly TextDocument _doc;
	private readonly TextEditorMorph _editor;
	private readonly IWindowService _windowService;
	private readonly IFileSystem _fileSystem;

	private string? _filename;
	private bool _dirty;

	#endregion

	public TextEditorWindowMorph(
		ILogger<TextEditorWindowMorph> logger,
		IWindowService windowService,
		IFileSystem fileSystem,
		IClipboardService clipboard
	) : base(Point.Empty, new Size(640, 400), "Text Editor")
	{
		_logger = logger;
		_windowService = windowService;
		_fileSystem = fileSystem;

		_doc = new(_logger, string.Empty);
		_doc.Changed += () =>
		{
			if (!_dirty)
			{
				_dirty = true;
				UpdateTitle();
			}
		};

		var dock = new DockPanelMorph();

		var toolbar = BuildToolbar();
		dock.AddMorph(toolbar);
		dock.SetDock(toolbar, Dock.Top);

		_editor = new TextEditorMorph(_doc, clipboard);
		dock.AddMorph(_editor);
		dock.SetDock(_editor, Dock.Fill);

		Content.AddMorph(dock);

		UpdateTitle();
	}

	protected override void OnLoad(IAssetService assetService)
	{
		base.OnLoad(assetService);
		_editor.CaptureKeyboard();
	}

	protected override void UpdateLayout()
	{
		Content.Submorphs[0].Size = Content.Size;
		base.UpdateLayout();
	}

	#region Toolbar

	private ToolbarMorph BuildToolbar()
	{
		var toolbar = new ToolbarMorph();

		toolbar.AddItem("New", new ActionCommand(NewFile));
		toolbar.AddItem("Open", new ActionCommand(async () => await OpenFileAsync()));
		toolbar.AddItem("Save", new ActionCommand(async () => await SaveFileAsync()));
		toolbar.AddItem("Save As", new ActionCommand(async () => await SaveFileAsAsync()));

		return toolbar;
	}

	#endregion

	#region Commands

	private void NewFile()
	{
		_doc.SetText(string.Empty);
		_filename = null;
		_dirty = false;
		UpdateTitle();
	}

	private async Task OpenFileAsync()
	{
		var filename = await _windowService.PromptAsync(
			"Open file:",
			_filename ?? "file://");
		if (string.IsNullOrWhiteSpace(filename)) return;
		await OpenFileAsync(filename);
	}

	public async Task OpenFileAsync(string? filename)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			await _windowService.AlertAsync("You must provide a filename.");
			return;
		}

		try
		{
			var text = await _fileSystem.ReadTextAsync(filename);
			_doc.SetText(text);
			_filename = filename;
			_dirty = false;
			UpdateTitle();
		}
		catch
		{
			await _windowService.AlertAsync($"File not found:\n{filename}");
		}
	}

	public async void OpenFile(string? filename)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			await _windowService.AlertAsync("You must provide a filename.");
			return;
		}

		try
		{
			var text = _fileSystem.ReadText(filename);
			_doc.SetText(text);
			_filename = filename;
			_dirty = false;
			UpdateTitle();
		}
		catch
		{
			await _windowService.AlertAsync($"File not found:\n{filename}");
		}
	}

	private async Task SaveFileAsync()
	{
		if (string.IsNullOrWhiteSpace(_filename))
		{
			await SaveFileAsAsync();
			return;
		}

		await SaveToFilenameAsync(_filename);
	}

	private async Task SaveFileAsAsync()
	{
		var filename = await _windowService.PromptAsync(
			"Save file as:",
			_filename ?? "file://");

		if (string.IsNullOrWhiteSpace(filename))
			return;

		await SaveToFilenameAsync(filename);
	}

	private async Task SaveToFilenameAsync(string filename)
	{
		try
		{
			await _fileSystem.WriteTextAsync(filename, _doc.ToString());
			_filename = filename;
			_dirty = false;
			UpdateTitle();
			await _windowService.AlertAsync($"Saved {filename}");
		}
		catch (Exception ex)
		{
			await _windowService.AlertAsync(
				$"Failed to save file:\n{ex.Message}");
		}
	}

	#endregion

	#region Title management

	private void UpdateTitle()
	{
		var name = string.IsNullOrWhiteSpace(_filename)
			? "Untitled"
			: _filename;

		if (_dirty)
			name += "*";

		Title = $"Text Editor - {name}";
	}

	#endregion
}