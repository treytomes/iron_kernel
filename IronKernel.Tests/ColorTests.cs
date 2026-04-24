using IronKernel.Common.ValueObjects;

namespace IronKernel.Tests;

public class ColorTests
{
    [Fact]
    public void Constructor_SetsChannels()
    {
        var c = new Color(1, 2, 3);
        Assert.Equal(1, c.Red);
        Assert.Equal(2, c.Green);
        Assert.Equal(3, c.Blue);
    }

    [Fact]
    public void Equals_SameChannels_ReturnsTrue()
    {
        var a = new Color(10, 20, 30);
        var b = new Color(10, 20, 30);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentChannels_ReturnsFalse()
    {
        var a = new Color(10, 20, 30);
        var b = new Color(10, 20, 31);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_SameChannels_SameHash()
    {
        var a = new Color(10, 20, 30);
        var b = new Color(10, 20, 30);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FromHSL_Grayscale_EqualChannels()
    {
        // s=0 means grayscale: all channels should be equal
        var c = Color.FromHSL(0, 0, 128);
        Assert.Equal(c.Red, c.Green);
        Assert.Equal(c.Green, c.Blue);
    }

    [Fact]
    public void FromHSL_Red_DominantRedChannel()
    {
        // Hue=0 (red), full saturation and mid lightness
        var c = Color.FromHSL(0, 255, 128);
        Assert.True(c.Red > c.Green, "Red channel should dominate for hue=0");
        Assert.True(c.Red > c.Blue, "Red channel should dominate for hue=0");
    }

    [Fact]
    public void FromHSL_Black_AllZero()
    {
        var c = Color.FromHSL(0, 0, 0);
        Assert.Equal(0, c.Red);
        Assert.Equal(0, c.Green);
        Assert.Equal(0, c.Blue);
    }

    [Fact]
    public void FromHSL_White_AllMax()
    {
        var c = Color.FromHSL(0, 0, 255);
        Assert.Equal(255, c.Red);
        Assert.Equal(255, c.Green);
        Assert.Equal(255, c.Blue);
    }

    [Fact]
    public void FromHSL_Green_DominantGreenChannel()
    {
        // Hue=85 ≈ 1/3 * 255, full saturation, mid lightness
        var c = Color.FromHSL(85, 255, 128);
        Assert.True(c.Green > c.Red);
        Assert.True(c.Green > c.Blue);
    }

    [Fact]
    public void FromHSL_Blue_DominantBlueChannel()
    {
        // Hue=170 ≈ 2/3 * 255
        var c = Color.FromHSL(170, 255, 128);
        Assert.True(c.Blue > c.Red);
        Assert.True(c.Blue > c.Green);
    }

    [Fact]
    public void FromHSL_HighLightness_UsesSumFormula()
    {
        // l > 0.5 → temp2 = (l + s) - (l * s) branch
        var c = Color.FromHSL(0, 200, 200);
        Assert.True(c.Red > 0);
    }

    [Theory]
    [InlineData(43,  255, 128)]  // tempR in (1/6, 1/2) → r = temp2
    [InlineData(128, 255, 128)]  // tempR in (1/2, 2/3) → r = temp1 + …
    [InlineData(200, 255, 128)]  // tempR > 2/3 → r = temp1
    public void FromHSL_AllHueSectors_ProduceValidRgb(float h, float s, float l)
    {
        var c = Color.FromHSL(h, s, l);
        // Just verify it doesn't throw and returns plausible byte values
        Assert.InRange(c.Red,   (byte)0, (byte)255);
        Assert.InRange(c.Green, (byte)0, (byte)255);
        Assert.InRange(c.Blue,  (byte)0, (byte)255);
    }
}
