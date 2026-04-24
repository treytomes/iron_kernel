using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;

namespace IronKernel.Tests;

public class MiniScriptHighlighterTests
{
    private static MiniScriptHighlighter Make() => new();

    private static TextDocument Doc(string text) =>
        new(NullLogger.Instance, text);

    private RadialColor? Color(string line, int col) =>
        Make().GetForeground(Doc(line), 0, col);

    // ── Out-of-bounds ─────────────────────────────────────────────────────────

    [Fact]
    public void NegativeColumn_ReturnsNull()
    {
        Assert.Null(Color("hello", -1));
    }

    [Fact]
    public void ColumnPastEnd_ReturnsNull()
    {
        Assert.Null(Color("hi", 99));
    }

    // ── Plain text ────────────────────────────────────────────────────────────

    [Fact]
    public void PlainIdentifier_ReturnsNull()
    {
        Assert.Null(Color("x", 0));
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
        Assert.Equal(RadialColor.Yellow, Color(keyword, 0));
    }

    [Fact]
    public void NonKeyword_Identifier_ReturnsNull()
    {
        Assert.Null(Color("foo", 0));
    }

    [Fact]
    public void Keyword_AsPartOfLongerWord_ReturnsNull()
    {
        // "iffy" contains "if" but is not a keyword
        Assert.Null(Color("iffy", 0));
    }

    // ── String literals ───────────────────────────────────────────────────────

    [Fact]
    public void InsideString_ReturnsOrange()
    {
        // "hello" — column 2 is inside the string
        Assert.Equal(RadialColor.Orange, Color("\"hello\"", 2));
    }

    [Fact]
    public void OpeningQuote_ReturnsNull()
    {
        // The opening quote itself at col 0 is not yet inside the string
        // (GetLineState only flips inString for i < targetColumn)
        Assert.Null(Color("\"hello\"", 0));
    }

    [Fact]
    public void ClosingQuote_ReturnsOrange()
    {
        // The closing quote at col 6 — string was opened at 0, and
        // GetLineState loops i < targetColumn so at col 6, only columns
        // 0..5 are processed. Col 0 toggles inString=true, no other quote
        // before col 6, so inString=true at the closing quote.
        Assert.Equal(RadialColor.Orange, Color("\"hello\"", 6));
    }

    [Fact]
    public void AfterClosedString_ReturnsNull()
    {
        // "hi" x — 'x' at col 5 is outside the string
        Assert.Null(Color("\"hi\" x", 5));
    }

    // ── Line comments ─────────────────────────────────────────────────────────

    [Fact]
    public void InsideLineComment_ReturnsGreen()
    {
        Assert.Equal(RadialColor.Green, Color("// comment", 3));
    }

    [Fact]
    public void SlashSlash_FirstChar_ReturnsGreen()
    {
        Assert.Equal(RadialColor.Green, Color("//x", 2));
    }

    [Fact]
    public void CommentAfterCode_CodePortionUnaffected()
    {
        // "x // note" — 'x' at col 0 is plain text
        Assert.Null(Color("x // note", 0));
    }

    [Fact]
    public void CommentAfterCode_CommentPortionIsGreen()
    {
        // 'n' at col 5 is inside the comment
        Assert.Equal(RadialColor.Green, Color("x // note", 5));
    }

    // ── Numbers ───────────────────────────────────────────────────────────────

    [Fact]
    public void Integer_ReturnsCyan()
    {
        Assert.Equal(RadialColor.Cyan, Color("42", 0));
    }

    [Fact]
    public void Float_ReturnsCyan()
    {
        Assert.Equal(RadialColor.Cyan, Color("3.14", 0));
    }

    [Fact]
    public void DigitInsideIdentifier_ReturnsNull()
    {
        // "foo2bar" — the '2' at col 3 is part of an identifier
        Assert.Null(Color("foo2bar", 3));
    }

    [Fact]
    public void NumberInExpression_ReturnsCyan()
    {
        // "x = 5" — '5' at col 4
        Assert.Equal(RadialColor.Cyan, Color("x = 5", 4));
    }
}
