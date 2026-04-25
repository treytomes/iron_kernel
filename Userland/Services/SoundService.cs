using IronKernel.Common;
using IronKernel.Common.ValueObjects;

namespace Userland.Services;

public sealed class SoundService(IApplicationBus bus) : ISoundService
{
    private readonly IApplicationBus _bus = bus;

    public string? PlayAsset(string path)
    {
        var result = _bus.CommandAsync<AppSoundPlayAsset, AppSoundPlayAssetResult>(
            id => new AppSoundPlayAsset(id, path))
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return result.Success ? null : result.Error;
    }

    public void PlayPcm(float[] samples, int sampleRate) =>
        _bus.Publish(new AppSoundPlayPcm(samples, sampleRate));

    public void Stop() =>
        _bus.Publish(new AppSoundStop());

    public void SetVolume(float volume) =>
        _bus.Publish(new AppSoundSetVolume(volume));
}
