namespace IronKernel.Modules.Sound.ValueObjects;

// Userland → kernel commands
public sealed record SoundPlayAsset(string Url);
public sealed record SoundPlayPcm(float[] Samples, int SampleRate);
public sealed record SoundStop();
public sealed record SoundSetVolume(float Volume);
