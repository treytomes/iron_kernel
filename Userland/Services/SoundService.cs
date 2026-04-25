using IronKernel.Common;
using IronKernel.Common.ValueObjects;

namespace Userland.Services;

public sealed class SoundService(IApplicationBus bus) : ISoundService
{
    private readonly IApplicationBus _bus = bus;

    public void PlayAsset(string assetKey)
    {
        var url = assetKey.StartsWith("asset://", StringComparison.OrdinalIgnoreCase)
            ? assetKey
            : $"asset://sound.{assetKey}";
        _bus.Publish(new AppSoundPlayAsset(url));
    }

    public void PlayPcm(float[] samples, int sampleRate) =>
        _bus.Publish(new AppSoundPlayPcm(samples, sampleRate));

    public void Stop() =>
        _bus.Publish(new AppSoundStop());

    public void SetVolume(float volume) =>
        _bus.Publish(new AppSoundSetVolume(volume));
}
