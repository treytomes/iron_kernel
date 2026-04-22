using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;

namespace IronKernel.Tests;

public class TextDocumentTests
{
    private static TextDocument Make(string? text = null) =>
        new(NullLogger.Instance, text);

    // ── Initialization ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullText_OneEmptyLine()
    {
        var doc = Make();
        Assert.Equal(1, doc.LineCount);
        Assert.Equal("", doc.ToString());
    }

    [Fact]
    public void Constructor_SingleLine_OneLineNoNewline()
    {
        var doc = Make("hello");
        Assert.Equal(1, doc.LineCount);
        Assert.Equal("hello", doc.ToString());
    }

    [Fact]
    public void Constructor_MultiLine_CorrectLineCount()
    {
        var doc = Make("foo\nbar\nbaz");
        Assert.Equal(3, doc.LineCount);
        Assert.Equal("foo\nbar\nbaz", doc.ToString());
    }

    [Fact]
    public void SetText_ReplacesExistingContent()
    {
        var doc = Make("old");
        doc.SetText("new\nlines");
        Assert.Equal(2, doc.LineCount);
        Assert.Equal("new\nlines", doc.ToString());
    }

    // ── Insertion ─────────────────────────────────────────────────────────────

    [Fact]
    public void InsertChar_AppendsToCurrentLine()
    {
        var doc = Make("hi");
        doc.InsertChar('!');
        Assert.Equal("hi!", doc.ToString());
    }

    [Fact]
    public void InsertChar_Newline_SplitsLine()
    {
        var doc = Make("ab");
        doc.MoveToLineStart();
        doc.MoveRight(); // after 'a'
        doc.InsertChar('\n');
        Assert.Equal(2, doc.LineCount);
        Assert.Equal("a\nb", doc.ToString());
    }

    [Fact]
    public void InsertTab_ExpandsToSpaces()
    {
        var doc = Make();
        doc.TabWidth = 4;
        doc.InsertTab();
        Assert.Equal("    ", doc.ToString());
    }

    // ── Deletion ──────────────────────────────────────────────────────────────

    [Fact]
    public void Backspace_WithinLine_RemovesChar()
    {
        var doc = Make("abc");
        doc.Backspace();
        Assert.Equal("ab", doc.ToString());
    }

    [Fact]
    public void Backspace_AtLineStart_MergesWithPreviousLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveDown(); // go to line 1 ("bar")
        doc.MoveToLineStart();
        doc.Backspace();
        Assert.Equal(1, doc.LineCount);
        Assert.Equal("foobar", doc.ToString());
    }

    [Fact]
    public void Backspace_AtFirstLineStart_DoesNothing()
    {
        var doc = Make("abc");
        doc.MoveToStart();
        doc.Backspace();
        Assert.Equal("abc", doc.ToString());
        Assert.Equal(1, doc.LineCount);
    }

    [Fact]
    public void Delete_WithinLine_RemovesChar()
    {
        var doc = Make("abc");
        doc.MoveToLineStart();
        doc.Delete();
        Assert.Equal("bc", doc.ToString());
    }

    [Fact]
    public void Delete_AtLineEnd_MergesNextLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveToStart();
        doc.MoveToLineEnd(); // at end of "foo"
        doc.Delete();
        Assert.Equal(1, doc.LineCount);
        Assert.Equal("foobar", doc.ToString());
    }

    [Fact]
    public void Delete_AtLastLineEnd_DoesNothing()
    {
        var doc = Make("abc");
        doc.Delete();
        Assert.Equal("abc", doc.ToString());
    }

    // ── DeleteRange ───────────────────────────────────────────────────────────

    [Fact]
    public void DeleteRange_SingleLine_RemovesCorrectChars()
    {
        var doc = Make("hello world");
        doc.DeleteRange((0, 5), (0, 11));
        Assert.Equal("hello", doc.ToString());
    }

    [Fact]
    public void DeleteRange_MultiLine_CollapsesMidSection()
    {
        var doc = Make("aaa\nbbb\nccc");
        doc.DeleteRange((0, 2), (2, 1)); // keep "aa" from line 0, "cc" from line 2
        Assert.Equal("aacc", doc.ToString());
        Assert.Equal(1, doc.LineCount);
    }

    [Fact]
    public void DeleteRange_ReversedArguments_StillWorks()
    {
        var doc = Make("hello world");
        doc.DeleteRange((0, 11), (0, 5)); // reversed
        Assert.Equal("hello", doc.ToString());
    }

    [Fact]
    public void DeleteRangeAndSetCaret_PlacesCaretAtStart()
    {
        var doc = Make("hello world");
        doc.DeleteRangeAndSetCaret((0, 6), (0, 11));
        Assert.Equal("hello ", doc.ToString());
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(6, doc.CaretColumn);
    }

    // ── Cursor movement (horizontal) ─────────────────────────────────────────

    [Fact]
    public void MoveLeft_AtLineStart_MovesToEndOfPreviousLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveDown();
        doc.MoveToLineStart();
        doc.MoveLeft();
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(3, doc.CaretColumn); // end of "foo"
    }

    [Fact]
    public void MoveRight_AtLineEnd_MovesToStartOfNextLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveToStart();
        doc.MoveToLineEnd(); // at end of "foo"
        doc.MoveRight();
        Assert.Equal(1, doc.CaretLine);
        Assert.Equal(0, doc.CaretColumn);
    }

    [Fact]
    public void MoveToStart_GoesToFirstLineFirstColumn()
    {
        var doc = Make("foo\nbar");
        doc.MoveToStart();
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(0, doc.CaretColumn);
    }

    [Fact]
    public void MoveToEnd_GoesToLastLineEnd()
    {
        var doc = Make("foo\nbar");
        doc.MoveToEnd();
        Assert.Equal(1, doc.CaretLine);
        Assert.Equal(3, doc.CaretColumn);
    }

    // ── Cursor movement (vertical) ────────────────────────────────────────────

    [Fact]
    public void MoveUp_FromSecondLine_MovesToFirstLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveDown();
        doc.MoveUp();
        Assert.Equal(0, doc.CaretLine);
    }

    [Fact]
    public void MoveUp_AtFirstLine_DoesNothing()
    {
        var doc = Make("foo");
        doc.MoveUp();
        Assert.Equal(0, doc.CaretLine);
    }

    [Fact]
    public void MoveDown_AtLastLine_DoesNothing()
    {
        var doc = Make("foo");
        doc.MoveDown();
        Assert.Equal(0, doc.CaretLine);
    }

    [Fact]
    public void MoveUpDown_PreservesDesiredColumn()
    {
        // "hello" (5 chars), "hi" (2 chars), "hello again" (11 chars)
        var doc = Make("hello\nhi\nhello again");
        doc.MoveToStart();
        doc.MoveToLineEnd(); // col 5 on "hello"

        doc.MoveDown(); // short line: col clamped to 2
        Assert.Equal(2, doc.CaretColumn);

        doc.MoveDown(); // longer line: desired column (5) restored
        Assert.Equal(5, doc.CaretColumn);
    }

    // ── Word navigation ───────────────────────────────────────────────────────

    [Fact]
    public void MoveWordRight_WrapsToNextLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveToStart();
        doc.MoveWordRight(); // to end of "foo"
        doc.MoveWordRight(); // wraps to line 1
        Assert.Equal(1, doc.CaretLine);
    }

    [Fact]
    public void MoveWordLeft_WrapsToEndOfPreviousLine()
    {
        var doc = Make("foo\nbar");
        doc.MoveDown();
        doc.MoveToLineStart();
        doc.MoveWordLeft();
        Assert.Equal(0, doc.CaretLine);
        Assert.Equal(3, doc.CaretColumn); // end of "foo"
    }

    // ── Changed event ─────────────────────────────────────────────────────────

    [Fact]
    public void ChangedEvent_FiredOnInsertion()
    {
        var doc = Make();
        bool fired = false;
        doc.Changed += () => fired = true;
        doc.InsertChar('x');
        Assert.True(fired);
    }
}
