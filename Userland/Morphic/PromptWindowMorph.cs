using System.Drawing;
using Userland.Morphic.Commands;
using Userland.Morphic.Layout;
using Userland.Services;

namespace Userland.Morphic;

public sealed class PromptWindowMorph : WindowMorph
{
	#region Fields

	private readonly Action<string?> _onClose;
	private string _value;
	private readonly TextEditMorph _editor;

	#endregion

	#region Constructors

	public PromptWindowMorph(
		string message,
		string? defaultValue,
		Action<string?> onClose
	) : base(Point.Empty, new Size(420, 108), "Prompt")
	{
		Title = "Prompt";
		_value = defaultValue ?? string.Empty;
		_onClose = onClose;

		var hStack = new HorizontalStackMorph();
		hStack.AddMorph(new LabelMorph { Text = message });
		_editor = new TextEditMorph(_value, v => _value = v);
		_editor.OnCommit += OnCloseHandler;
		_editor.OnCancel += OnCloseHandler;
		hStack.AddMorph(_editor);

		var toolbar = new ToolbarMorph();
		toolbar.AddItem("OK", new ActionCommand(() => OnClose(_value)));
		toolbar.AddItem("Cancel", new ActionCommand(() => OnClose(null)));

		var vStack = new VerticalStackMorph();
		vStack.AddMorph(hStack);
		vStack.AddMorph(toolbar);
		Content.AddMorph(vStack);
	}

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assetService)
	{
		base.OnLoad(assetService);
		_editor.CaptureKeyboard();
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Content.Size = Content.Submorphs.First().Size;
		Size = new Size(Content.Size.Width + Padding, Content.Size.Height + Padding + HeaderHeight);
	}

	private void OnCloseHandler(object? sender, EventArgs e)
	{
		_editor.OnCommit -= OnCloseHandler;
		_editor.OnCancel -= OnCloseHandler;

		var value = _value;
		if (string.IsNullOrEmpty(value))
		{
			value = null;
		}
		OnClose(value);
	}

	private void OnClose(string? value)
	{
		Close();
		_onClose(value);
	}

	#endregion
}