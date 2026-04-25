using Userland.Scripting;

namespace IronKernel.Tests;

public class SoundSynthesizerTests
{
    private const int SampleRate = 22050;

    // ── helpers ──────────────────────────────────────────────────────────────

    private static float[] Gen(
        SoundSynthesizer.Waveform wf,
        double freq = 440.0,
        double dur = 0.1,
        double attack = 0.0,
        double decay = 0.0,
        double sustain = 1.0,
        double release = 0.0,
        double volume = 1.0)
        => SoundSynthesizer.Generate(wf, freq, dur, SampleRate, attack, decay, sustain, release, volume);

    private static double Rms(float[] s)
        => Math.Sqrt(s.Average(x => (double)x * x));

    // ── basic output shape ───────────────────────────────────────────────────

    [Theory]
    [InlineData(SoundSynthesizer.Waveform.Sine)]
    [InlineData(SoundSynthesizer.Waveform.Triangle)]
    [InlineData(SoundSynthesizer.Waveform.Square)]
    [InlineData(SoundSynthesizer.Waveform.Sawtooth)]
    [InlineData(SoundSynthesizer.Waveform.Noise)]
    public void Output_Length_MatchesDuration(SoundSynthesizer.Waveform wf)
    {
        var s = Gen(wf, 440, 0.5);
        Assert.Equal((int)(SampleRate * 0.5), s.Length);
    }

    [Theory]
    [InlineData(SoundSynthesizer.Waveform.Sine)]
    [InlineData(SoundSynthesizer.Waveform.Triangle)]
    [InlineData(SoundSynthesizer.Waveform.Square)]
    [InlineData(SoundSynthesizer.Waveform.Sawtooth)]
    [InlineData(SoundSynthesizer.Waveform.Noise)]
    public void Samples_Within_Unit_Range(SoundSynthesizer.Waveform wf)
    {
        var s = Gen(wf, 440, 0.1);
        Assert.All(s, v => Assert.InRange(v, -1.01f, 1.01f));
    }

    [Fact]
    public void ZeroDuration_Returns_Empty()
    {
        var s = SoundSynthesizer.Generate(SoundSynthesizer.Waveform.Sine, 440, 0);
        Assert.Empty(s);
    }

    // ── waveform energy ──────────────────────────────────────────────────────

    [Fact]
    public void Sine_HasExpectedRms()
    {
        var s = Gen(SoundSynthesizer.Waveform.Sine, 440, 1.0, release: 0);
        // RMS of a full sine = 1/√2 ≈ 0.707 (no envelope fade)
        Assert.InRange(Rms(s), 0.65, 0.75);
    }

    [Fact]
    public void Square_HasHigherRms_ThanSine()
    {
        var sine = Rms(Gen(SoundSynthesizer.Waveform.Sine, 440, 0.5, release: 0));
        var square = Rms(Gen(SoundSynthesizer.Waveform.Square, 440, 0.5, release: 0));
        Assert.True(square > sine, $"square RMS {square:F4} should be > sine RMS {sine:F4}");
    }

    // ── volume scaling ───────────────────────────────────────────────────────

    [Fact]
    public void Volume_ScalesOutput()
    {
        var full = Rms(Gen(SoundSynthesizer.Waveform.Sine, 440, 0.5, volume: 1.0, release: 0));
        var half = Rms(Gen(SoundSynthesizer.Waveform.Sine, 440, 0.5, volume: 0.5, release: 0));
        Assert.InRange(half / full, 0.48, 0.52);
    }

    // ── envelope ─────────────────────────────────────────────────────────────

    [Fact]
    public void Attack_FirstSample_NearZero()
    {
        var s = Gen(SoundSynthesizer.Waveform.Sine, 440, 0.2, attack: 0.1, release: 0);
        Assert.InRange(s[0], -0.01f, 0.01f);
    }

    [Fact]
    public void Release_LastSample_NearZero()
    {
        var s = Gen(SoundSynthesizer.Waveform.Sine, 440, 0.2, attack: 0.0, release: 0.05, sustain: 1.0);
        Assert.InRange(s[^1], -0.01f, 0.01f);
    }

    [Fact]
    public void Sustain_ReducesMidAmp()
    {
        var full = Rms(Gen(SoundSynthesizer.Waveform.Sine, 440, 0.1, sustain: 1.0, release: 0));
        var half = Rms(Gen(SoundSynthesizer.Waveform.Sine, 440, 0.1, sustain: 0.5, release: 0));
        Assert.True(half < full, $"sustain=0.5 RMS {half:F4} should be less than sustain=1.0 RMS {full:F4}");
    }

    // ── frequency ────────────────────────────────────────────────────────────

    [Fact]
    public void Sine_OctaveDouble_SameEnergy()
    {
        // Different frequencies, same duration — energy should be comparable
        var a4 = Rms(Gen(SoundSynthesizer.Waveform.Sine, 440, 0.5, release: 0));
        var a5 = Rms(Gen(SoundSynthesizer.Waveform.Sine, 880, 0.5, release: 0));
        Assert.InRange(a5 / a4, 0.9, 1.1);
    }
}
