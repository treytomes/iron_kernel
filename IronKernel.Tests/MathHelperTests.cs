using IronKernel.Common;

namespace IronKernel.Tests;

public class MathHelperTests
{
    [Theory]
    [InlineData(0f, 10f, 0f, 0f)]
    [InlineData(0f, 10f, 1f, 10f)]
    [InlineData(0f, 10f, 0.5f, 5f)]
    [InlineData(4f, 8f, 0.25f, 5f)]
    [InlineData(-10f, 10f, 0.5f, 0f)]
    public void Lerp_ReturnsCorrectValue(float start, float end, float t, float expected)
    {
        Assert.Equal(expected, MathHelper.Lerp(start, end, t), precision: 4);
    }

    [Theory]
    [InlineData(0.0, 1.0, 0f)]   // start of cycle
    [InlineData(0.25, 1.0, 0.5f)] // quarter cycle
    [InlineData(0.5, 1.0, 1f)]   // peak
    [InlineData(0.75, 1.0, 0.5f)] // three-quarter
    [InlineData(1.0, 1.0, 0f)]   // full cycle wraps to 0
    public void TriangleWave_ReturnsCorrectValue(double t, double freq, float expected)
    {
        Assert.Equal(expected, MathHelper.TriangleWave(t, freq), precision: 4);
    }

    [Fact]
    public void TriangleWave_OutputIsAlwaysBetweenZeroAndOne()
    {
        for (int i = 0; i < 100; i++)
        {
            float v = MathHelper.TriangleWave(i * 0.037, 1.0);
            Assert.InRange(v, 0f, 1f);
        }
    }
}
