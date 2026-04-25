namespace Userland.Scripting;

/// <summary>
/// Generates PCM float samples for a single voice: waveform + frequency + envelope.
/// Output is normalized to [-1, 1].
/// </summary>
public static class SoundSynthesizer
{
    public enum Waveform { Sine, Triangle, Square, Sawtooth, Noise }

    public static float[] Generate(
        Waveform waveform,
        double frequencyHz,
        double durationSeconds,
        int sampleRate = 22050,
        double attackSeconds = 0.01,
        double decaySeconds = 0.0,
        double sustainLevel = 1.0,
        double releaseSeconds = 0.05,
        double volume = 1.0)
    {
        var totalSamples = (int)(sampleRate * durationSeconds);
        if (totalSamples <= 0) return [];

        var samples = new float[totalSamples];
        var rng = new Random();

        var attackSamples = (int)(sampleRate * attackSeconds);
        var decaySamples = (int)(sampleRate * decaySeconds);
        var releaseSamples = (int)(sampleRate * releaseSeconds);
        // Sustain fills the gap between decay-end and release-start
        var releaseStart = Math.Max(0, totalSamples - releaseSamples);

        for (var i = 0; i < totalSamples; i++)
        {
            var t = (double)i / sampleRate;
            var raw = waveform switch
            {
                Waveform.Sine => Math.Sin(2 * Math.PI * frequencyHz * t),
                Waveform.Triangle => Triangle(frequencyHz, t),
                Waveform.Square => Math.Sign(Math.Sin(2 * Math.PI * frequencyHz * t)),
                Waveform.Sawtooth => Sawtooth(frequencyHz, t),
                Waveform.Noise => rng.NextDouble() * 2.0 - 1.0,
                _ => 0.0
            };

            var env = Envelope(i, totalSamples, attackSamples, decaySamples, sustainLevel, releaseStart, releaseSamples);
            samples[i] = (float)(raw * env * volume);
        }

        return samples;
    }

    private static double Triangle(double freq, double t)
    {
        var phase = (freq * t) % 1.0;
        return phase < 0.5 ? 4.0 * phase - 1.0 : 3.0 - 4.0 * phase;
    }

    private static double Sawtooth(double freq, double t)
    {
        var phase = (freq * t) % 1.0;
        return 2.0 * phase - 1.0;
    }

    private static double Envelope(
        int i, int total,
        int attackSamples, int decaySamples, double sustainLevel,
        int releaseStart, int releaseSamples)
    {
        if (i < attackSamples)
            return attackSamples > 0 ? (double)i / attackSamples : 1.0;

        var afterAttack = i - attackSamples;
        if (afterAttack < decaySamples)
            return decaySamples > 0
                ? 1.0 - (1.0 - sustainLevel) * ((double)afterAttack / decaySamples)
                : sustainLevel;

        if (i >= releaseStart)
        {
            var relPos = i - releaseStart;
            return releaseSamples > 0
                ? sustainLevel * (1.0 - (double)relPos / releaseSamples)
                : 0.0;
        }

        return sustainLevel;
    }
}
