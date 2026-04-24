using IronKernel.Common.ValueObjects;

namespace IronKernel.Tests;

public class ColorTests
{
    // ── Constructor and clamping ──────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsChannels()
    {
        var c = new Color(0.1f, 0.5f, 0.9f);
        Assert.Equal(0.1f, c.R);
        Assert.Equal(0.5f, c.G);
        Assert.Equal(0.9f, c.B);
    }

    [Fact]
    public void Constructor_ClampsAboveOne()
    {
        var c = new Color(2f, 3f, 4f);
        Assert.Equal(1f, c.R);
        Assert.Equal(1f, c.G);
        Assert.Equal(1f, c.B);
    }

    [Fact]
    public void Constructor_ClampsBelowZero()
    {
        var c = new Color(-1f, -0.5f, -2f);
        Assert.Equal(0f, c.R);
        Assert.Equal(0f, c.G);
        Assert.Equal(0f, c.B);
    }

    // ── Named constants ───────────────────────────────────────────────────────

    [Fact]
    public void Black_AllZero()
    {
        var c = Color.Black;
        Assert.Equal(0f, c.R);
        Assert.Equal(0f, c.G);
        Assert.Equal(0f, c.B);
    }

    [Fact]
    public void White_AllOne()
    {
        var c = Color.White;
        Assert.Equal(1f, c.R);
        Assert.Equal(1f, c.G);
        Assert.Equal(1f, c.B);
    }

    [Fact]
    public void Red_OnlyRedChannel()
    {
        var c = Color.Red;
        Assert.Equal(1f, c.R);
        Assert.Equal(0f, c.G);
        Assert.Equal(0f, c.B);
    }

    [Fact]
    public void Green_OnlyGreenChannel()
    {
        var c = Color.Green;
        Assert.Equal(0f, c.R);
        Assert.Equal(1f, c.G);
        Assert.Equal(0f, c.B);
    }

    [Fact]
    public void Blue_OnlyBlueChannel()
    {
        var c = Color.Blue;
        Assert.Equal(0f, c.R);
        Assert.Equal(0f, c.G);
        Assert.Equal(1f, c.B);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameChannels_ReturnsTrue()
    {
        var a = new Color(0.2f, 0.4f, 0.6f);
        var b = new Color(0.2f, 0.4f, 0.6f);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentChannels_ReturnsFalse()
    {
        var a = new Color(0.2f, 0.4f, 0.6f);
        var b = new Color(0.2f, 0.4f, 0.7f);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_SameChannels_SameHash()
    {
        var a = new Color(0.1f, 0.2f, 0.3f);
        var b = new Color(0.1f, 0.2f, 0.3f);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ── Lerp ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Lerp_AtZero_ReturnsOriginal()
    {
        var a = Color.Red;
        var b = Color.Blue;
        Assert.Equal(a, a.Lerp(b, 0f));
    }

    [Fact]
    public void Lerp_AtOne_ReturnsOther()
    {
        var a = Color.Red;
        var b = Color.Blue;
        Assert.Equal(b, a.Lerp(b, 1f));
    }

    [Fact]
    public void Lerp_AtMidpoint_AveragesChannels()
    {
        var a = new Color(0f, 0f, 0f);
        var b = new Color(1f, 1f, 1f);
        var mid = a.Lerp(b, 0.5f);
        Assert.Equal(0.5f, mid.R);
        Assert.Equal(0.5f, mid.G);
        Assert.Equal(0.5f, mid.B);
    }

    [Fact]
    public void Lerp_ClampsBelowZero()
    {
        var a = Color.Black;
        var b = Color.White;
        var result = a.Lerp(b, -1f);
        Assert.Equal(Color.Black, result);
    }

    [Fact]
    public void Lerp_ClampsAboveOne()
    {
        var a = Color.Black;
        var b = Color.White;
        var result = a.Lerp(b, 2f);
        Assert.Equal(Color.White, result);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_SaturatesAtOne()
    {
        var a = new Color(0.8f, 0.8f, 0.8f);
        var b = new Color(0.5f, 0.5f, 0.5f);
        var result = a + b;
        Assert.Equal(1f, result.R);
        Assert.Equal(1f, result.G);
        Assert.Equal(1f, result.B);
    }

    [Fact]
    public void Add_BelowSaturation_SumsChannels()
    {
        var a = new Color(0.1f, 0.2f, 0.3f);
        var b = new Color(0.1f, 0.1f, 0.1f);
        var result = a + b;
        Assert.Equal(0.2f, result.R, precision: 5);
        Assert.Equal(0.3f, result.G, precision: 5);
        Assert.Equal(0.4f, result.B, precision: 5);
    }

    // ── With* builders ────────────────────────────────────────────────────────

    [Fact]
    public void WithR_ReplacesRedChannel()
    {
        var c = new Color(0.1f, 0.2f, 0.3f).WithR(0.9f);
        Assert.Equal(0.9f, c.R);
        Assert.Equal(0.2f, c.G);
        Assert.Equal(0.3f, c.B);
    }

    [Fact]
    public void WithG_ReplacesGreenChannel()
    {
        var c = new Color(0.1f, 0.2f, 0.3f).WithG(0.8f);
        Assert.Equal(0.1f, c.R);
        Assert.Equal(0.8f, c.G);
        Assert.Equal(0.3f, c.B);
    }

    [Fact]
    public void WithB_ReplacesBlueChannel()
    {
        var c = new Color(0.1f, 0.2f, 0.3f).WithB(0.7f);
        Assert.Equal(0.1f, c.R);
        Assert.Equal(0.2f, c.G);
        Assert.Equal(0.7f, c.B);
    }

    // ── FromHSL ───────────────────────────────────────────────────────────────

    [Fact]
    public void FromHSL_Grayscale_EqualChannels()
    {
        var c = Color.FromHSL(0f, 0f, 0.5f);
        Assert.Equal(c.R, c.G);
        Assert.Equal(c.G, c.B);
    }

    [Fact]
    public void FromHSL_Black_AllZero()
    {
        var c = Color.FromHSL(0f, 0f, 0f);
        Assert.Equal(0f, c.R, precision: 5);
        Assert.Equal(0f, c.G, precision: 5);
        Assert.Equal(0f, c.B, precision: 5);
    }

    [Fact]
    public void FromHSL_White_AllOne()
    {
        var c = Color.FromHSL(0f, 0f, 1f);
        Assert.Equal(1f, c.R, precision: 5);
        Assert.Equal(1f, c.G, precision: 5);
        Assert.Equal(1f, c.B, precision: 5);
    }

    [Fact]
    public void FromHSL_Red_DominantRedChannel()
    {
        var c = Color.FromHSL(0f, 1f, 0.5f);
        Assert.True(c.R > c.G, "Red channel should dominate for hue=0");
        Assert.True(c.R > c.B, "Red channel should dominate for hue=0");
    }

    [Fact]
    public void FromHSL_Green_DominantGreenChannel()
    {
        var c = Color.FromHSL(1f / 3f, 1f, 0.5f);
        Assert.True(c.G > c.R);
        Assert.True(c.G > c.B);
    }

    [Fact]
    public void FromHSL_Blue_DominantBlueChannel()
    {
        var c = Color.FromHSL(2f / 3f, 1f, 0.5f);
        Assert.True(c.B > c.R);
        Assert.True(c.B > c.G);
    }

    [Theory]
    [InlineData(1f / 6f, 1f, 0.5f)]
    [InlineData(0.5f,    1f, 0.5f)]
    [InlineData(5f / 6f, 1f, 0.5f)]
    public void FromHSL_AllHueSectors_ProduceValidRange(float h, float s, float l)
    {
        var c = Color.FromHSL(h, s, l);
        Assert.InRange(c.R, 0f, 1f);
        Assert.InRange(c.G, 0f, 1f);
        Assert.InRange(c.B, 0f, 1f);
    }
}
