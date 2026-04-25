namespace Userland.Services;

public interface ISoundService
{
    /// <summary>Play a WAV asset. Returns null on success, an error message on failure.</summary>
    string? PlayAsset(string assetKey);

    /// <summary>Play raw PCM float samples (synthesizer output).</summary>
    void PlayPcm(float[] samples, int sampleRate);

    /// <summary>Stop all currently playing sounds.</summary>
    void Stop();

    /// <summary>Set master volume (0.0–1.0).</summary>
    void SetVolume(float volume);
}
