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

    // Build a minimal valid 24-bit mono PCM WAV byte array in memory.
    private static byte[] BuildWav24(int[] samples24, int sampleRate = 44100, int channels = 1)
    {
        var dataBytes = samples24.Length * 3;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + dataBytes);
        bw.Write("WAVE".ToCharArray());

        bw.Write("fmt ".ToCharArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * 3);
        bw.Write((short)(channels * 3));
        bw.Write((short)24);

        bw.Write("data".ToCharArray());
        bw.Write(dataBytes);
        foreach (var s in samples24)
        {
            bw.Write((byte)(s & 0xFF));
            bw.Write((byte)((s >> 8) & 0xFF));
            bw.Write((byte)((s >> 16) & 0xFF));
        }

        return ms.ToArray();
    }

    [Fact]
    public void Load_24BitPcm_DownscalesToHighBits()
    {
        // 0x7FFFFF is the max positive 24-bit value → should become 0x7FFF (32767)
        // 0x800000 is min negative (sign-extended) → should become -32768 (short.MinValue)
        // 0x000000 → 0
        var samples24 = new[] { 0x7FFFFF, unchecked((int)0xFF800000), 0x000000 };
        var wav = WavLoader.Load(BuildWav24(samples24, sampleRate: 44100, channels: 1));

        Assert.Equal(1, wav.Channels);
        Assert.Equal(44100, wav.SampleRate);
        Assert.Equal(3, wav.Samples.Length);
        Assert.Equal((short)0x7FFF, wav.Samples[0]);
        Assert.Equal(short.MinValue, wav.Samples[1]);
        Assert.Equal((short)0, wav.Samples[2]);
    }

    [Fact]
    public void Load_24BitPcm_Stereo()
    {
        var samples24 = new[] { 0x100000, unchecked((int)0xFF000000) + 0x100000 };
        var wav = WavLoader.Load(BuildWav24(samples24, sampleRate: 44100, channels: 2));
        Assert.Equal(2, wav.Channels);
        Assert.Equal(2, wav.Samples.Length);
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

    [Fact]
    public void Load_RealWavFile_Bongo()
    {
        var path = Path.Combine(
            TestContext.RepoRoot, "IronKernel", "assets", "sys", "sounds", "bongo.wav");

        if (!File.Exists(path)) return;

        var wav = WavLoader.Load(File.ReadAllBytes(path));
        Assert.True(wav.Samples.Length > 0);
        Assert.True(wav.SampleRate > 0);
        Assert.True(wav.Channels is 1 or 2);
    }
}
