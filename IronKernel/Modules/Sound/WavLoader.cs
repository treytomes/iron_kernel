namespace IronKernel.Modules.Sound;

/// <summary>
/// Minimal WAV loader — supports 16-bit and 24-bit PCM, mono and stereo, any sample rate.
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

        while (ms.Length - ms.Position >= 8)
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
                if (bitsPerSample != 16 && bitsPerSample != 24)
                    throw new NotSupportedException($"Only 16-bit and 24-bit PCM WAV are supported (got {bitsPerSample}-bit).");
            }
            else if (chunkId == "data")
            {
                var bytesPerSample = bitsPerSample / 8;
                var sampleCount = chunkSize / bytesPerSample;
                samples = new short[sampleCount];
                if (bitsPerSample == 16)
                {
                    for (var i = 0; i < sampleCount; i++)
                        samples[i] = br.ReadInt16();
                }
                else // 24-bit: read 3 bytes, take high 16 bits
                {
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var b0 = br.ReadByte();
                        var b1 = br.ReadByte();
                        var b2 = br.ReadByte();
                        var val24 = (int)(b0 | ((uint)b1 << 8) | ((uint)b2 << 16));
                        if ((val24 & 0x800000) != 0) val24 |= unchecked((int)0xFF000000); // sign-extend
                        samples[i] = (short)(val24 >> 8);
                    }
                }
            }

            // Seek to next chunk (handles extra bytes in fmt chunk, etc.)
            ms.Position = chunkStart + chunkSize;
        }

        if (samples == null) throw new InvalidDataException("WAV file has no data chunk.");
        return new WavData(samples, sampleRate, channels);
    }
}
