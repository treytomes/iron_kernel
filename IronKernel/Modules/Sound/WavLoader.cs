namespace IronKernel.Modules.Sound;

/// <summary>
/// Minimal WAV loader — supports 16-bit PCM, mono and stereo, any sample rate.
/// Returns interleaved 16-bit samples as a short[].
/// </summary>
internal static class WavLoader
{
    public sealed record WavData(short[] Samples, int SampleRate, int Channels);

    public static WavData Load(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);

        // RIFF header
        var riff = new string(br.ReadChars(4));
        if (riff != "RIFF") throw new InvalidDataException("Not a RIFF file.");
        br.ReadInt32(); // file size
        var wave = new string(br.ReadChars(4));
        if (wave != "WAVE") throw new InvalidDataException("Not a WAVE file.");

        int sampleRate = 0, channels = 0, bitsPerSample = 0;
        short[]? samples = null;

        while (ms.Position < ms.Length)
        {
            var chunkId = new string(br.ReadChars(4));
            var chunkSize = br.ReadInt32();
            var chunkStart = ms.Position;

            if (chunkId == "fmt ")
            {
                var audioFormat = br.ReadInt16(); // 1 = PCM
                if (audioFormat != 1)
                    throw new NotSupportedException($"WAV audio format {audioFormat} not supported (only PCM=1).");
                channels = br.ReadInt16();
                sampleRate = br.ReadInt32();
                br.ReadInt32(); // byte rate
                br.ReadInt16(); // block align
                bitsPerSample = br.ReadInt16();
                if (bitsPerSample != 16)
                    throw new NotSupportedException($"Only 16-bit WAV is supported (got {bitsPerSample}-bit).");
            }
            else if (chunkId == "data")
            {
                var sampleCount = chunkSize / 2;
                samples = new short[sampleCount];
                for (var i = 0; i < sampleCount; i++)
                    samples[i] = br.ReadInt16();
            }

            // Seek to next chunk (handles extra bytes in fmt chunk, etc.)
            ms.Position = chunkStart + chunkSize;
        }

        if (samples == null) throw new InvalidDataException("WAV file has no data chunk.");
        return new WavData(samples, sampleRate, channels);
    }
}
