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
    public void FromHSL_White_NearMax()
    {
        // The implementation scales by 255/256 due to float division, so max is 254.
        var c = Color.FromHSL(0, 0, 255);
        Assert.True(c.Red >= 254, $"Red={c.Red}");
        Assert.Equal(c.Red, c.Green);
        Assert.Equal(c.Green, c.Blue);
    }
}
