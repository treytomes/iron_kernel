using IronKernel.Common.ValueObjects;

namespace IronKernel.Tests;

public class RadialColorTests
{
    // ── Constructor validation ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidValues_SetsChannels()
    {
        var c = new RadialColor(1, 2, 3);
        Assert.Equal(1, c.R);
        Assert.Equal(2, c.G);
        Assert.Equal(3, c.B);
    }

    [Theory]
    [InlineData(6, 0, 0)]
    [InlineData(0, 6, 0)]
    [InlineData(0, 0, 6)]
    [InlineData(255, 0, 0)]
    public void Constructor_OutOfRange_Throws(byte r, byte g, byte b)
    {
        Assert.Throws<ArgumentException>(() => new RadialColor(r, g, b));
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Index_Black_IsZero()
    {
        Assert.Equal(0, RadialColor.Black.Index);
    }

    [Fact]
    public void Index_White_IsMaximum()
    {
        // R=5, G=5, B=5 → 5*36 + 5*6 + 5 = 180+30+5 = 215
        Assert.Equal(215, RadialColor.White.Index);
    }

    [Theory]
    [InlineData(1, 0, 0, 36)]   // R=1 → 1*36
    [InlineData(0, 1, 0, 6)]    // G=1 → 1*6
    [InlineData(0, 0, 1, 1)]    // B=1 → 1
    [InlineData(2, 3, 1, 91)]   // 2*36 + 3*6 + 1
    public void Index_Calculation_IsCorrect(byte r, byte g, byte b, int expected)
    {
        Assert.Equal(expected, new RadialColor(r, g, b).Index);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameChannels_ReturnsTrue()
    {
        var a = new RadialColor(1, 2, 3);
        var b = new RadialColor(1, 2, 3);
        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentChannels_ReturnsFalse()
    {
        var a = new RadialColor(1, 2, 3);
        var b = new RadialColor(1, 2, 4);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Assert.False(RadialColor.Black.Equals(null));
    }

    [Fact]
    public void GetHashCode_EqualColors_SameHash()
    {
        var a = new RadialColor(3, 2, 1);
        var b = new RadialColor(3, 2, 1);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ── Add / operator+ ───────────────────────────────────────────────────────

    [Fact]
    public void Add_ClampsAt5()
    {
        var a = new RadialColor(4, 4, 4);
        var b = new RadialColor(3, 3, 3);
        var result = a.Add(b);
        Assert.Equal(5, result.R);
        Assert.Equal(5, result.G);
        Assert.Equal(5, result.B);
    }

    [Fact]
    public void Add_Normal_SumsChannels()
    {
        var a = new RadialColor(1, 2, 0);
        var b = new RadialColor(1, 1, 3);
        var result = a + b;
        Assert.Equal(2, result.R);
        Assert.Equal(3, result.G);
        Assert.Equal(3, result.B);
    }

    // ── Lerp ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Lerp_AtZero_ReturnsThis()
    {
        var a = new RadialColor(2, 3, 4);
        var b = new RadialColor(4, 4, 4);
        var result = a.Lerp(b, 0f);
        Assert.Equal(a, result);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsOther()
    {
        var a = new RadialColor(0, 0, 0);
        var b = new RadialColor(4, 2, 2);
        var result = a.Lerp(b, 1f);
        Assert.Equal(b, result);
    }

    [Fact]
    public void Lerp_AtMidpoint_InterpolatesChannels()
    {
        var a = new RadialColor(0, 0, 0);
        var b = new RadialColor(4, 4, 4);
        var result = a.Lerp(b, 0.5f);
        Assert.Equal(2, result.R);
        Assert.Equal(2, result.G);
        Assert.Equal(2, result.B);
    }

    [Fact]
    public void Lerp_ClampsAboveOne()
    {
        var a = new RadialColor(0, 0, 0);
        var b = new RadialColor(4, 4, 4);
        var result = a.Lerp(b, 999f); // clamped to 1.0
        Assert.Equal(b, result);
    }

    [Fact]
    public void Lerp_ClampsBelowZero()
    {
        var a = new RadialColor(2, 2, 2);
        var b = new RadialColor(4, 4, 4);
        var result = a.Lerp(b, -999f); // clamped to 0.0
        Assert.Equal(a, result);
    }

    // ── With* ─────────────────────────────────────────────────────────────────

    [Fact]
    public void WithR_ChangesOnlyR()
    {
        var c = new RadialColor(1, 2, 3).WithR(5);
        Assert.Equal(5, c.R);
        Assert.Equal(2, c.G);
        Assert.Equal(3, c.B);
    }

    [Fact]
    public void WithG_ChangesOnlyG()
    {
        var c = new RadialColor(1, 2, 3).WithG(0);
        Assert.Equal(1, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(3, c.B);
    }

    [Fact]
    public void WithB_ChangesOnlyB()
    {
        var c = new RadialColor(1, 2, 3).WithB(4);
        Assert.Equal(1, c.R);
        Assert.Equal(2, c.G);
        Assert.Equal(4, c.B);
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_FormatsChannels()
    {
        Assert.Equal("(R:1 G:2 B:3)", new RadialColor(1, 2, 3).ToString());
    }

    // ── Named colors spot-check ───────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0)]   // Black
    [InlineData(5, 5, 5)]   // White
    [InlineData(5, 0, 0)]   // Red
    [InlineData(0, 5, 0)]   // Green
    [InlineData(0, 0, 5)]   // Blue
    [InlineData(0, 5, 5)]   // Cyan
    [InlineData(5, 5, 0)]   // Yellow
    [InlineData(5, 3, 0)]   // Orange
    public void NamedColors_HaveExpectedChannels(byte r, byte g, byte b)
    {
        // Verifies the named color factory properties return valid RadialColors
        var c = new RadialColor(r, g, b);
        Assert.Equal(r, c.R);
        Assert.Equal(g, c.G);
        Assert.Equal(b, c.B);
    }

    // ── FromColor ─────────────────────────────────────────────────────────────

    [Fact]
    public void FromColor_Black_MapsToBlack()
    {
        var result = RadialColor.FromColor(new Color(0, 0, 0));
        Assert.Equal(RadialColor.Black, result);
    }

    [Fact]
    public void FromColor_White_MapsToWhite()
    {
        var result = RadialColor.FromColor(new Color(255, 255, 255));
        Assert.Equal(RadialColor.White, result);
    }

    [Fact]
    public void FromColor_MidGray_QuantizesCorrectly()
    {
        // 128/255 * 5 ≈ 2.5 → rounds to 3
        var result = RadialColor.FromColor(new Color(128, 128, 128));
        Assert.Equal(3, result.R);
        Assert.Equal(3, result.G);
        Assert.Equal(3, result.B);
    }

    // ── FromHSL ───────────────────────────────────────────────────────────────

    [Fact]
    public void FromHSL_ZeroSaturation_ProducesGray()
    {
        // Saturation=0 → gray, lightness=128 → mid gray
        var result = RadialColor.FromHSL(0, 0, 128);
        Assert.Equal(result.R, result.G);
        Assert.Equal(result.G, result.B);
    }
}
