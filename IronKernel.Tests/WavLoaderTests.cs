using IronKernel.Modules.Sound;

namespace IronKernel.Tests;

public class WavLoaderTests
{
    // Build a minimal valid 16-bit mono PCM WAV byte array in memory.
    private static byte[] BuildWav(short[] samples, int sampleRate = 44100, int channels = 1)
    {
        var dataBytes = samples.Length * 2;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + dataBytes);       // file size - 8
        bw.Write("WAVE".ToCharArray());

        // fmt chunk
        bw.Write("fmt ".ToCharArray());
        bw.Write(16);                   // chunk size
        bw.Write((short)1);             // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * 2); // byte rate
        bw.Write((short)(channels * 2));     // block align
        bw.Write((short)16);                 // bits per sample

        // data chunk
        bw.Write("data".ToCharArray());
        bw.Write(dataBytes);
        foreach (var s in samples) bw.Write(s);

        return ms.ToArray();
    }

    [Fact]
    public void Load_MonoPcm_ReturnsSamplesAndMetadata()
    {
        var samples = new short[] { 0, 100, -100, 32767, -32768 };
        var wav = WavLoader.Load(BuildWav(samples, sampleRate: 22050, channels: 1));

        Assert.Equal(1, wav.Channels);
        Assert.Equal(22050, wav.SampleRate);
        Assert.Equal(samples.Length, wav.Samples.Length);
        Assert.Equal(samples, wav.Samples);
    }

    [Fact]
    public void Load_StereoPcm_ReturnsInterleavedSamples()
    {
        // Stereo: L, R, L, R
        var samples = new short[] { 100, -100, 200, -200 };
        var wav = WavLoader.Load(BuildWav(samples, sampleRate: 44100, channels: 2));

        Assert.Equal(2, wav.Channels);
        Assert.Equal(44100, wav.SampleRate);
        Assert.Equal(4, wav.Samples.Length);
    }

    [Fact]
    public void Load_InvalidRiff_Throws()
    {
        var bad = new byte[44];
        "NOTW"u8.CopyTo(bad);
        Assert.Throws<InvalidDataException>(() => WavLoader.Load(bad));
    }

    [Fact]
    public void Load_RealWavFile_BlipA4()
    {
        var path = Path.Combine(
            TestContext.RepoRoot, "IronKernel", "assets", "sounds", "blipA4.wav");

        if (!File.Exists(path))
        {
            // Skip gracefully if assets not present in CI.
            return;
        }

        var wav = WavLoader.Load(File.ReadAllBytes(path));
        Assert.True(wav.Samples.Length > 0);
        Assert.True(wav.SampleRate > 0);
        Assert.True(wav.Channels is 1 or 2);
    }
}
