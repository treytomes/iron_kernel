using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;
using Color = IronKernel.Common.ValueObjects.Color;

namespace IronKernel.Tests;

public class MiniScriptHighlighterTests
{
    private static MiniScriptHighlighter Make() => new();

    private static TextDocument Doc(string text) =>
        new(NullLogger.Instance, text);

    private Color? GetColor(string line, int col) =>
        Make().GetForeground(Doc(line), 0, col);

    // ── Out-of-bounds ─────────────────────────────────────────────────────────

    [Fact]
    public void NegativeColumn_ReturnsNull()
    {
        Assert.Null(GetColor("hello", -1));
    }

    [Fact]
    public void ColumnPastEnd_ReturnsNull()
    {
        Assert.Null(GetColor("hi", 99));
    }

    // ── Plain text ────────────────────────────────────────────────────────────

    [Fact]
    public void PlainIdentifier_ReturnsNull()
    {
        Assert.Null(GetColor("x", 0));
    }

    // ── Keywords ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("if")]
    [InlineData("else")]
    [InlineData("then")]
    [InlineData("for")]
    [InlineData("while")]
    [InlineData("function")]
    [InlineData("return")]
    [InlineData("end")]
    public void Keyword_ReturnsYellow(string keyword)
    {
        // Check the first character of each keyword
        Assert.Equal(Color.Yellow, GetColor(keyword, 0));
    }

    [Fact]
    public void NonKeyword_Identifier_ReturnsNull()
    {
        Assert.Null(GetColor("foo", 0));
    }

    [Fact]
    public void Keyword_AsPartOfLongerWord_ReturnsNull()
    {
        // "iffy" contains "if" but is not a keyword
        Assert.Null(GetColor("iffy", 0));
    }

    // ── String literals ───────────────────────────────────────────────────────

    [Fact]
    public void InsideString_ReturnsOrange()
    {
        // "hello" — column 2 is inside the string
        Assert.Equal(Color.Orange, GetColor("\"hello\"", 2));
    }

    [Fact]
    public void OpeningQuote_ReturnsNull()
    {
        // The opening quote itself at col 0 is not yet inside the string
        // (GetLineState only flips inString for i < targetColumn)
        Assert.Null(GetColor("\"hello\"", 0));
    }

    [Fact]
    public void ClosingQuote_ReturnsOrange()
    {
        // The closing quote at col 6 — string was opened at 0, and
        // GetLineState loops i < targetColumn so at col 6, only columns
        // 0..5 are processed. Col 0 toggles inString=true, no other quote
        // before col 6, so inString=true at the closing quote.
        Assert.Equal(Color.Orange, GetColor("\"hello\"", 6));
    }

    [Fact]
    public void AfterClosedString_ReturnsNull()
    {
        // "hi" x — 'x' at col 5 is outside the string
        Assert.Null(GetColor("\"hi\" x", 5));
    }

    // ── Line comments ─────────────────────────────────────────────────────────

    [Fact]
    public void InsideLineComment_ReturnsGreen()
    {
        Assert.Equal(Color.Green, GetColor("// comment", 3));
    }

    [Fact]
    public void SlashSlash_FirstChar_ReturnsGreen()
    {
        Assert.Equal(Color.Green, GetColor("//x", 2));
    }

    [Fact]
    public void CommentAfterCode_CodePortionUnaffected()
    {
        // "x // note" — 'x' at col 0 is plain text
        Assert.Null(GetColor("x // note", 0));
    }

    [Fact]
    public void CommentAfterCode_CommentPortionIsGreen()
    {
        // 'n' at col 5 is inside the comment
        Assert.Equal(Color.Green, GetColor("x // note", 5));
    }

    // ── Numbers ───────────────────────────────────────────────────────────────

    [Fact]
    public void Integer_ReturnsCyan()
    {
        Assert.Equal(Color.Cyan, GetColor("42", 0));
    }

    [Fact]
    public void Float_ReturnsCyan()
    {
        Assert.Equal(Color.Cyan, GetColor("3.14", 0));
    }

    [Fact]
    public void DigitInsideIdentifier_ReturnsNull()
    {
        // "foo2bar" — the '2' at col 3 is part of an identifier
        Assert.Null(GetColor("foo2bar", 3));
    }

    [Fact]
    public void NumberInExpression_ReturnsCyan()
    {
        // "x = 5" — '5' at col 4
        Assert.Equal(Color.Cyan, GetColor("x = 5", 4));
    }
}
