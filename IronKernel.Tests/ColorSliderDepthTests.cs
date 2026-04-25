using System.Reflection;
using IronKernel.Common.ValueObjects;
using Userland.Morphic;
using Userland.Morphic.Inspector;

namespace IronKernel.Tests;

public class ColorSliderDepthTests
{
    private static SliderWithEditorMorph GetSlider(ColorSliderValueMorph morph, string fieldName)
    {
        var field = typeof(ColorSliderValueMorph)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found.");
        return (SliderWithEditorMorph)field.GetValue(morph)!;
    }

    [Theory]
    [InlineData(6, 5f)]
    [InlineData(16, 15f)]
    [InlineData(2, 1f)]
    public void ChannelSliders_MaxMatchesColorDepthMinusOne(int colorDepth, float expectedMax)
    {
        var morph = new ColorSliderValueMorph(setter: null, colorDepth: colorDepth);

        foreach (var field in new[] { "_r", "_g", "_b" })
        {
            var slider = GetSlider(morph, field);
            Assert.Equal(expectedMax, slider.Max);
        }
    }

    [Fact]
    public void InspectorFactory_ColorDepth16_CreatesColorSliderWithCorrectRange()
    {
        var factory = new InspectorFactory(navigate: null, colorDepth: 16);

        var content = factory.GetInspectorFor(
            typeof(Color?),
            () => (Color?)Color.Red,
            setter: null
        );

        Assert.IsType<ColorSliderValueMorph>(content);
        var sliderMorph = (ColorSliderValueMorph)content;

        foreach (var field in new[] { "_r", "_g", "_b" })
        {
            var slider = GetSlider(sliderMorph, field);
            Assert.Equal(15f, slider.Max);
        }
    }

    [Fact]
    public void InspectorFactory_DefaultColorDepth_UsesDepth6()
    {
        var factory = new InspectorFactory();

        var content = factory.GetInspectorFor(
            typeof(Color?),
            () => (Color?)Color.Red,
            setter: null
        );

        Assert.IsType<ColorSliderValueMorph>(content);
        var sliderMorph = (ColorSliderValueMorph)content;

        foreach (var field in new[] { "_r", "_g", "_b" })
        {
            var slider = GetSlider(sliderMorph, field);
            Assert.Equal(5f, slider.Max);
        }
    }
}
