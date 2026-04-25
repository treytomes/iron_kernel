using IronKernel.Common;

namespace IronKernel.Modules.Sound.ValueObjects;

// Userland → kernel commands
public sealed record SoundPlayAsset(Guid CorrelationID, string Url) : Command(CorrelationID);
public sealed record SoundPlayAssetResult(Guid CorrelationID, bool Success, string? Error)
    : Response<bool>(CorrelationID, Success);

public sealed record SoundPlayPcm(float[] Samples, int SampleRate);
public sealed record SoundStop();
public sealed record SoundSetVolume(float Volume);
