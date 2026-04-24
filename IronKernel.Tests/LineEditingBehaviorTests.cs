using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;
using Userland.Services;

namespace IronKernel.Tests;

public class LineEditingBehaviorTests
{
    private static TextEditingCore MakeEditor(string text = "") =>
        new(NullLogger.Instance, text);

    // ── Selection state ───────────────────────────────────────────────────────

    [Fact]
    public void Initial_HasNoSelection()
    {
        var line = new LineEditingBehavior();
        Assert.False(line.HasSelection);
        Assert.Null(line.GetSelectionRange());
    }

    [Fact]
    public void BeginAndUpdate_CreatesSelection()
    {
        var line = new LineEditingBehavior();
        line.Begin(2);
        line.Update(7);
        Assert.True(line.HasSelection);
        Assert.Equal((2, 7), line.GetSelectionRange());
    }

    [Fact]
    public void BeginIfNeeded_WhenNoAnchor_SetsAnchor()
    {
        var line = new LineEditingBehavior();
        line.BeginIfNeeded(3);
        line.Update(8);
        Assert.True(line.HasSelection);
        Assert.Equal(3, line.GetSelectionRange()!.Value.start);
    }

    [Fact]
    public void BeginIfNeeded_WhenAnchorExists_DoesNotOverwrite()
    {
        var line = new LineEditingBehavior();
        line.Begin(1);
        line.Update(5);
        line.BeginIfNeeded(10); // should not move anchor
        line.Update(9);
        Assert.Equal(1, line.GetSelectionRange()!.Value.start);
    }

    [Fact]
    public void Clear_RemovesSelection()
    {
        var line = new LineEditingBehavior();
        line.Begin(0);
        line.Update(5);
        line.Clear();
        Assert.False(line.HasSelection);
        Assert.Null(line.GetSelectionRange());
    }

    // ── SelectAll ─────────────────────────────────────────────────────────────

    [Fact]
    public void SelectAll_SelectsEntireBuffer()
    {
        var editor = MakeEditor("hello");
        var line = new LineEditingBehavior();
        line.SelectAll(editor);
        Assert.True(line.HasSelection);
        Assert.Equal((0, 5), line.GetSelectionRange());
    }

    [Fact]
    public void SelectAll_EmptyBuffer_NoSelection()
    {
        var editor = MakeEditor();
        var line = new LineEditingBehavior();
        line.SelectAll(editor);
        Assert.False(line.HasSelection);
    }

    // ── DeleteSelection ───────────────────────────────────────────────────────

    [Fact]
    public void DeleteSelection_RemovesSelectedRange()
    {
        var editor = MakeEditor("hello world");
        var line = new LineEditingBehavior();
        line.Begin(6);
        line.Update(11);
        line.DeleteSelection(editor);
        Assert.Equal("hello ", editor.ToString());
        Assert.Equal(6, editor.CursorIndex);
    }

    [Fact]
    public void DeleteSelection_ReversedRange_StillDeletes()
    {
        var editor = MakeEditor("abcde");
        var line = new LineEditingBehavior();
        line.Begin(4);
        line.Update(1); // reversed
        line.DeleteSelection(editor);
        Assert.Equal("ae", editor.ToString());
    }

    [Fact]
    public void DeleteSelection_ClearsSelectionAfterwards()
    {
        var editor = MakeEditor("abc");
        var line = new LineEditingBehavior();
        line.Begin(0);
        line.Update(3);
        line.DeleteSelection(editor);
        Assert.False(line.HasSelection);
    }

    [Fact]
    public void DeleteSelection_WhenNoSelection_ReturnsFalse()
    {
        var editor = MakeEditor("abc");
        var line = new LineEditingBehavior();
        bool result = line.DeleteSelection(editor);
        Assert.False(result);
        Assert.Equal("abc", editor.ToString());
    }

    [Fact]
    public void DeleteSelection_WhenSelection_ReturnsTrue()
    {
        var editor = MakeEditor("abc");
        var line = new LineEditingBehavior();
        line.Begin(0);
        line.Update(2);
        bool result = line.DeleteSelection(editor);
        Assert.True(result);
    }

    // ── CopySelection ─────────────────────────────────────────────────────────

    [Fact]
    public void CopySelection_WritesSelectedTextToClipboard()
    {
        var clipboard = new FakeClipboard();
        var editor = MakeEditor("hello world");
        var line = new LineEditingBehavior(clipboard);
        line.Begin(6);
        line.Update(11);
        line.CopySelection(editor);
        Assert.Equal("world", clipboard.Text);
    }

    [Fact]
    public void CopySelection_NoClipboard_DoesNotThrow()
    {
        var editor = MakeEditor("hello");
        var line = new LineEditingBehavior(); // no clipboard
        line.Begin(0);
        line.Update(5);
        line.CopySelection(editor); // should be a no-op
    }

    [Fact]
    public void CopySelection_NoSelection_DoesNotWriteClipboard()
    {
        var clipboard = new FakeClipboard();
        var editor = MakeEditor("hello");
        var line = new LineEditingBehavior(clipboard);
        line.CopySelection(editor);
        Assert.Null(clipboard.Text);
    }

    // ── CutSelection ──────────────────────────────────────────────────────────

    [Fact]
    public void CutSelection_CopiesAndDeletesSelection()
    {
        var clipboard = new FakeClipboard();
        var editor = MakeEditor("hello world");
        var line = new LineEditingBehavior(clipboard);
        line.Begin(6);
        line.Update(11);
        line.CutSelection(editor);
        Assert.Equal("world", clipboard.Text);
        Assert.Equal("hello ", editor.ToString());
        Assert.False(line.HasSelection);
    }

    // ── Normalize ─────────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_ValidRange_RetainsSelection()
    {
        var editor = MakeEditor("hello");
        var line = new LineEditingBehavior();
        line.Begin(1);
        line.Update(4);
        line.Normalize(editor);
        Assert.True(line.HasSelection);
    }

    [Fact]
    public void Normalize_AfterTextShrinks_ClearsSelection()
    {
        var editor = MakeEditor("hello");
        var line = new LineEditingBehavior();
        line.Begin(2);
        line.Update(5);
        editor.DeleteRange(3, 2); // buffer is now "hel" (length 3)
        line.Normalize(editor);   // caret (5) is now out of range
        Assert.False(line.HasSelection);
    }

    // ── PasteClipboard ────────────────────────────────────────────────────────

    [Fact]
    public async Task PasteClipboard_InsertsTextAtCursor()
    {
        var clipboard = new FakeClipboard("world");
        var editor = MakeEditor("hello ");
        var line = new LineEditingBehavior(clipboard);

        bool changed = false;
        // PasteClipboard is async void — we wait for the clipboard task to settle
        var tcs = new TaskCompletionSource();
        line.PasteClipboard(editor, () => { changed = true; tcs.TrySetResult(); });
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("hello world", editor.ToString());
        Assert.True(changed);
    }

    [Fact]
    public async Task PasteClipboard_ReplacesSelection()
    {
        var clipboard = new FakeClipboard("X");
        var editor = MakeEditor("hello");
        var line = new LineEditingBehavior(clipboard);
        line.Begin(1);
        line.Update(4); // select "ell"

        var tcs = new TaskCompletionSource();
        line.PasteClipboard(editor, () => tcs.TrySetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("hXo", editor.ToString());
    }

    [Fact]
    public async Task PasteClipboard_StripsNewlinesByDefault()
    {
        var clipboard = new FakeClipboard("a\nb\rc");
        var editor = MakeEditor();
        var line = new LineEditingBehavior(clipboard);

        var tcs = new TaskCompletionSource();
        line.PasteClipboard(editor, () => tcs.TrySetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("abc", editor.ToString());
    }

    [Fact]
    public async Task PasteClipboard_AllowNewlines_PreservesNewlines()
    {
        var clipboard = new FakeClipboard("a\nb");
        var editor = MakeEditor();
        var line = new LineEditingBehavior(clipboard, allowNewlines: true);

        var tcs = new TaskCompletionSource();
        line.PasteClipboard(editor, () => tcs.TrySetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("a\nb", editor.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeClipboard(string? initial = null) : IClipboardService
    {
        public string? Text { get; private set; } = initial;
        public void SetText(string text) => Text = text;
        public Task<string?> GetTextAsync() => Task.FromResult(Text);
    }
}
