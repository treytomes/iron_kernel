namespace IronKernel.Tests;

/// <summary>
/// Tests for equal-temperament MIDI note → frequency mapping.
/// Formula: 440 * 2^((note - 69) / 12)
/// </summary>
public class NoteFreqTests
{
    private static double NoteFreq(double note) =>
        440.0 * Math.Pow(2.0, (note - 69.0) / 12.0);

    [Theory]
    [InlineData(69, 440.0)]        // A4
    [InlineData(60, 261.626)]      // C4 (middle C)
    [InlineData(57, 220.0)]        // A3
    [InlineData(81, 880.0)]        // A5
    [InlineData(0,  8.176)]        // lowest MIDI note
    [InlineData(127, 12543.854)]   // highest MIDI note
    public void NoteFreq_KnownNotes_MatchEqualTemperament(double midiNote, double expectedHz)
    {
        var actual = NoteFreq(midiNote);
        Assert.Equal(expectedHz, actual, 0.5); // within 0.5 Hz
    }

    [Fact]
    public void NoteFreq_OctaveUp_DoublesFrequency()
    {
        var c4 = NoteFreq(60);
        var c5 = NoteFreq(72);
        Assert.Equal(c4 * 2, c5, 3);
    }

    [Fact]
    public void NoteFreq_OctaveDown_HalvesFrequency()
    {
        var a4 = NoteFreq(69);
        var a3 = NoteFreq(57);
        Assert.Equal(a4 / 2, a3, 3);
    }
}
