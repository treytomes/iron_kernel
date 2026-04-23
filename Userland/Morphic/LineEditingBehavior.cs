using IronKernel.Common.ValueObjects;
using Userland.Services;

namespace Userland.Morphic;

/// <summary>
/// Owns selection state and clipboard operations for a single-line text field.
/// Each morph holds one instance and delegates selection/clipboard calls to it;
/// key routing stays in the morph so console-specific (history, Enter) and
/// field-specific (commit, cancel) behaviors remain separate.
/// </summary>
internal sealed class LineEditingBehavior
{
	private readonly SelectionController<int> _selection = new((a, b) => a.CompareTo(b));
	private readonly IClipboardService? _clipboard;
	private readonly bool _allowNewlines;

	public LineEditingBehavior(IClipboardService? clipboard = null, bool allowNewlines = false)
	{
		_clipboard = clipboard;
		_allowNewlines = allowNewlines;
	}

	public bool HasSelection => _selection.HasSelection;

	public (int start, int end)? GetSelectionRange()
		=> _selection.HasSelection ? _selection.GetRange() : null;

	public void Begin(int cursorIndex) => _selection.Begin(cursorIndex);
	public void BeginIfNeeded(int cursorIndex) => _selection.BeginIfNeeded(cursorIndex);
	public void Update(int cursorIndex) => _selection.Update(cursorIndex);
	public void Clear() => _selection.Clear();

	public void SelectAll(TextEditingCore editor)
	{
		_selection.Begin(0);
		_selection.Update(editor.Length);
	}

	public void Normalize(TextEditingCore editor)
		=> _selection.Normalize(i => i >= 0 && i <= editor.Length);

	/// <summary>
	/// Deletes the selected range from the editor and moves the cursor to the selection start.
	/// Returns true if a deletion occurred.
	/// </summary>
	public bool DeleteSelection(TextEditingCore editor)
	{
		if (!_selection.HasSelection)
			return false;

		var (start, end) = _selection.GetRange();
		editor.DeleteRange(start, end - start);
		editor.SetCursorIndex(start);
		_selection.Clear();
		return true;
	}

	/// <summary>Copies selected text to the clipboard.</summary>
	public void CopySelection(TextEditingCore editor)
	{
		if (_clipboard == null || !_selection.HasSelection)
			return;

		var (start, end) = _selection.GetRange();
		_clipboard.SetText(editor.GetSubstring(start, end - start));
	}

	/// <summary>Cuts selected text: copies then deletes.</summary>
	public void CutSelection(TextEditingCore editor)
	{
		CopySelection(editor);
		DeleteSelection(editor);
	}

	/// <summary>
	/// Pastes clipboard text into the editor at the cursor, replacing any selection.
	/// Strips newlines if <c>allowNewlines</c> is false (the default for single-line fields).
	/// </summary>
	public async void PasteClipboard(TextEditingCore editor, Action? onChanged = null)
	{
		if (_clipboard == null)
			return;

		var text = await _clipboard.GetTextAsync();
		if (string.IsNullOrEmpty(text))
			return;

		DeleteSelection(editor);

		foreach (char ch in text)
		{
			if (!_allowNewlines && (ch == '\n' || ch == '\r'))
				continue;
			editor.Insert(ch);
		}

		onChanged?.Invoke();
	}
}
