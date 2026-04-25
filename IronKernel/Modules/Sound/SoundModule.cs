using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.Sound.ValueObjects;
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;

namespace IronKernel.Modules.Sound;

internal sealed class SoundModule(
    IMessageBus bus,
    ILogger<SoundModule> logger,
    AppSettings settings
) : IKernelModule
{
    #region Fields

    private readonly IMessageBus _bus = bus;
    private readonly ILogger<SoundModule> _logger = logger;
    private readonly AppSettings _settings = settings;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly List<int> _activeSources = new();
    private readonly object _sourceLock = new();

    private ALDevice _device;
    private ALContext _context;
    private bool _available = false;

    #endregion

    #region IKernelModule

    public Task StartAsync(IKernelState state, IModuleRuntime runtime, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SoundModule.");

        if (!TryInitOpenAL())
        {
            _logger.LogWarning("OpenAL device unavailable — SoundModule will no-op.");
            return Task.CompletedTask;
        }

        _subscriptions.Add(_bus.Subscribe<SoundPlayAsset>(
            runtime, "SoundPlayAssetHandler",
            (msg, ct) =>
            {
                PlayAsset(msg.Url);
                return Task.CompletedTask;
            }));

        _subscriptions.Add(_bus.Subscribe<SoundPlayPcm>(
            runtime, "SoundPlayPcmHandler",
            (msg, ct) =>
            {
                PlayPcm(msg.Samples, msg.SampleRate);
                return Task.CompletedTask;
            }));

        _subscriptions.Add(_bus.Subscribe<SoundStop>(
            runtime, "SoundStopHandler",
            (msg, ct) =>
            {
                StopAll();
                return Task.CompletedTask;
            }));

        _subscriptions.Add(_bus.Subscribe<SoundSetVolume>(
            runtime, "SoundSetVolumeHandler",
            (msg, ct) =>
            {
                AL.Listener(ALListenerf.Gain, Math.Clamp(msg.Volume, 0f, 1f));
                return Task.CompletedTask;
            }));

        _logger.LogInformation("SoundModule ready.");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing SoundModule.");

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        if (_available)
        {
            StopAll();
            lock (_sourceLock)
            {
                foreach (var src in _activeSources) AL.DeleteSource(src);
                _activeSources.Clear();
            }

            ALC.DestroyContext(_context);
            ALC.CloseDevice(_device);
            _available = false;
        }

        return ValueTask.CompletedTask;
    }

    #endregion

    #region OpenAL init

    private bool TryInitOpenAL()
    {
        try
        {
            _device = ALC.OpenDevice(null);
            if (_device == ALDevice.Null)
            {
                _logger.LogWarning("ALC.OpenDevice returned null.");
                return false;
            }

            _context = ALC.CreateContext(_device, (int[]?)null);
            ALC.MakeContextCurrent(_context);
            _available = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAL initialization failed.");
            return false;
        }
    }

    #endregion

    #region Playback

    private void PlayAsset(string url)
    {
        if (!_available) return;

        var assetRoot = _settings.AssetRoot;

        // url is "asset://sound.NAME" → look up in settings, then load from disk
        if (!url.StartsWith("asset://sound.", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("SoundModule: unsupported asset URL scheme: {Url}", url);
            return;
        }

        var key = url["asset://sound.".Length..].Trim().ToLowerInvariant();

        if (!_settings.Assets.Sound.TryGetValue(key, out var relativePath))
        {
            _logger.LogWarning("SoundModule: unknown sound asset key '{Key}'", key);
            return;
        }

        var fullPath = Path.Combine(assetRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("SoundModule: sound file not found: {Path}", fullPath);
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(fullPath);
            var wav = WavLoader.Load(bytes);
            PlayShort(wav.Samples, wav.SampleRate, wav.Channels);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SoundModule: failed to play asset {Url}", url);
        }
    }

    private void PlayPcm(float[] samples, int sampleRate)
    {
        if (!_available) return;

        // Convert float[-1,1] to short
        var shorts = new short[samples.Length];
        for (var i = 0; i < samples.Length; i++)
            shorts[i] = (short)Math.Clamp((int)(samples[i] * 32767f), short.MinValue, short.MaxValue);

        PlayShort(shorts, sampleRate, 1);
    }

    private void PlayShort(short[] samples, int sampleRate, int channels)
    {
        var format = channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;

        var buffer = AL.GenBuffer();
        AL.BufferData(buffer, format, samples, sampleRate);

        var source = AL.GenSource();
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.SourcePlay(source);

        lock (_sourceLock)
            _activeSources.Add(source);

        // Schedule cleanup on a background thread once playback finishes.
        var capturedSource = source;
        var capturedBuffer = buffer;
        _ = Task.Run(async () =>
        {
            // Poll until the source finishes. Max wait = 30 s.
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                AL.GetSource(capturedSource, ALGetSourcei.SourceState, out var state);
                if ((ALSourceState)state != ALSourceState.Playing) break;
            }

            AL.DeleteSource(capturedSource);
            AL.DeleteBuffer(capturedBuffer);
            lock (_sourceLock) _activeSources.Remove(capturedSource);
        });
    }

    private void StopAll()
    {
        if (!_available) return;
        lock (_sourceLock)
        {
            foreach (var src in _activeSources)
                AL.SourceStop(src);
        }
    }

    #endregion
}
