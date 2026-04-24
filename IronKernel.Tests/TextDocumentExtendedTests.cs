using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;

namespace IronKernel.Tests;

/// <summary>
/// Covers TextDocument paths not exercised by TextDocumentTests:
/// word deletion, SetCaretLine, DeleteRangeAndSetCaret (multi-line),
/// TextMutated event, and MoveWordLeft/Right line-crossing.
/// </summary>
public class TextDocumentExtendedTests
{
    private static TextDocument Make(string? text = null) =>
        new(NullLogger.Instance, text);

    // ── Word deletion ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteWordLeft_WithinLine_DeletesPreviousWord()
    {
        var doc = Make("foo bar");
        // Caret starts at end ("bar" end)
        doc.DeleteWordLeft();
        Assert.Equal("foo ", doc.ToString());
    }

    [Fact]
    public void DeleteWordLeft_AtLineStart_MergesWithPreviousLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveDown();
        doc.MoveToLineStart();
        doc.DeleteWordLeft(); // merges lines since caret is at col 0
        Assert.Equal(1, doc.LineCount);
        Assert.Equal("foobar", doc.ToString());
    }

    [Fact]
    public void DeleteWordRight_WithinLine_DeletesNextWord()
    {
        var doc = Make("foo bar");
        doc.MoveToStart();
        doc.DeleteWordRight();
        Assert.Equal(" bar", doc.ToString());
    }

    [Fact]
    public void DeleteWordRight_AtLineEnd_MergesNextLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveToStart();
        doc.MoveToLineEnd(); // at end of "foo"
        doc.DeleteWordRight(); // merges lines since caret is at line end
        Assert.Equal(1, doc.LineCount);
        Assert.Equal("foobar", doc.ToString());
    }

    // ── SetCaretLine ──────────────────────────────────────────────────────────

    [Fact]
    public void SetCaretLine_MovesCaretToTargetLine()
    {
        var doc = Make("aaa\nbbb\nccc");
        doc.SetCaretLine(2);
        Assert.Equal(2, doc.CaretLine);
    }

    [Fact]
    public void SetCaretLine_ClampsAboveLineCount()
    {
        var doc = Make("aaa\nbbb");
        doc.SetCaretLine(99);
        Assert.Equal(1, doc.CaretLine);
    }

    [Fact]
    public void SetCaretLine_ClampsColumnToLineLength()
    {
        var doc = Make("hello\nhi");
        // Start at line 0 (constructor places caret at end of last line, so move up)
        doc.MoveToStart();
        doc.MoveToLineEnd(); // col 5 on "hello"
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(5, doc.CaretColumn);
        doc.SetCaretLine(1); // "hi" has length 2, so column must clamp to 2
        Assert.Equal(1, doc.CaretLine);
        Assert.Equal(2, doc.CaretColumn);
    }

    // ── DeleteRangeAndSetCaret (multi-line, crash regression) ─────────────────

    [Fact]
    public void DeleteRangeAndSetCaret_MultiLine_LeavesValidCaretLine()
    {
        // Regression: prior to fix, OnTextMutated fired with stale CaretLine
        // after a multi-line delete, causing ArgumentOutOfRangeException.
        var doc = Make("aaa\nbbb\nccc");
        doc.MoveToStart();
        doc.MoveToLineEnd(); // line 0, col 3

        // Select-all equivalent: delete everything from (0,0) to (2,3)
        doc.DeleteRangeAndSetCaret((0, 0), (2, 3));

        Assert.Equal(1, doc.LineCount);
        Assert.Equal("", doc.ToString());
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(0, doc.CaretColumn);
    }

    [Fact]
    public void DeleteRangeAndSetCaret_MultiLine_CaretAtStartPosition()
    {
        var doc = Make("hello\nworld\nend");
        doc.DeleteRangeAndSetCaret((0, 3), (1, 2));
        // "hel" + "rld\nend" → "helrld\nend"
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(3, doc.CaretColumn);
    }

    [Fact]
    public void DeleteRangeAndSetCaret_ReversedArgs_CaretAtMinPosition()
    {
        var doc = Make("hello\nworld");
        // reversed: end < start
        doc.DeleteRangeAndSetCaret((1, 3), (0, 2));
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(2, doc.CaretColumn);
    }

    // ── TextMutated event ─────────────────────────────────────────────────────

    [Fact]
    public void TextMutated_FiredOnInsert()
    {
        var doc = Make();
        bool fired = false;
        doc.TextMutated += () => fired = true;
        doc.InsertChar('x');
        Assert.True(fired);
    }

    [Fact]
    public void TextMutated_FiredOnBackspace()
    {
        var doc = Make("abc");
        bool fired = false;
        doc.TextMutated += () => fired = true;
        doc.Backspace();
        Assert.True(fired);
    }

    [Fact]
    public void TextMutated_FiredOnDeleteRange()
    {
        var doc = Make("hello");
        bool fired = false;
        doc.TextMutated += () => fired = true;
        doc.DeleteRange((0, 1), (0, 4));
        Assert.True(fired);
    }

    [Fact]
    public void Changed_NotFiredOnPureMovement()
    {
        // Changed IS fired on movement (cursor changed); TextMutated should not be.
        var doc = Make("hello");
        bool textMutated = false;
        doc.TextMutated += () => textMutated = true;
        doc.MoveLeft();
        doc.MoveRight();
        doc.MoveUp();
        doc.MoveDown();
        Assert.False(textMutated);
    }

    // ── MoveWordLeft/Right line-crossing ─────────────────────────────────────

    [Fact]
    public void MoveWordLeft_AtLineStart_CrossesToPreviousLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveDown();
        doc.MoveToLineStart(); // line 1, col 0
        doc.MoveWordLeft();
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(3, doc.CaretColumn); // end of "foo"
    }

    [Fact]
    public void MoveWordLeft_AtFirstLineStart_DoesNothing()
    {
        var doc = Make("hello");
        doc.MoveToStart();
        doc.MoveWordLeft();
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(0, doc.CaretColumn);
    }

    [Fact]
    public void MoveWordRight_AtLineEnd_CrossesToNextLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveToStart();
        doc.MoveToLineEnd(); // line 0, end of "foo"
        doc.MoveWordRight();
        Assert.Equal(1, doc.CaretLine);
    }

    [Fact]
    public void MoveWordRight_AtLastLineEnd_DoesNothing()
    {
        var doc = Make("hello");
        doc.MoveToEnd();
        doc.MoveWordRight();
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(5, doc.CaretColumn);
    }

    // ── InsertTab ─────────────────────────────────────────────────────────────

    [Fact]
    public void InsertTab_DefaultTabWidth_Inserts4Spaces()
    {
        var doc = Make();
        doc.InsertTab();
        Assert.Equal("    ", doc.ToString());
    }

    [Fact]
    public void InsertTab_CustomTabWidth_InsertsCorrectSpaces()
    {
        var doc = Make();
        doc.TabWidth = 2;
        doc.InsertTab();
        Assert.Equal("  ", doc.ToString());
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_MultiLine_JoinsWithNewlines()
    {
        var doc = Make("a\nb\nc");
        Assert.Equal("a\nb\nc", doc.ToString());
    }
}
