using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.FileSystem;
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

    private string _userRoot = string.Empty;
    private string _sysRoot = string.Empty;

    #endregion

    #region IKernelModule

    public Task StartAsync(IKernelState state, IModuleRuntime runtime, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SoundModule.");

        _sysRoot = Path.GetFullPath("assets/sys");
        _userRoot = Path.GetFullPath(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                nameof(IronKernel),
                _settings.UserFileRoot));

        if (!TryInitOpenAL())
        {
            _logger.LogWarning("OpenAL device unavailable — SoundModule will no-op.");
            return Task.CompletedTask;
        }

        _subscriptions.Add(_bus.Subscribe<SoundPlayAsset>(
            runtime, "SoundPlayAssetHandler",
            (msg, ct) =>
            {
                var error = PlayAsset(msg.Url);
                _bus.Publish(new SoundPlayAssetResult(msg.CorrelationID, error == null, error));
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

    // Returns null on success, an error string on failure.
    private string? PlayAsset(string url)
    {
        if (!_available) return null;

        string? diskPath = ResolveAudioPath(url);
        if (diskPath == null)
        {
            _logger.LogWarning("SoundModule: could not resolve audio path: {Url}", url);
            return $"Sound file not found: {url}";
        }

        try
        {
            var bytes = File.ReadAllBytes(diskPath);
            var wav = WavLoader.Load(bytes);
            PlayShort(wav.Samples, wav.SampleRate, wav.Channels);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SoundModule: failed to play asset {Url}", url);
            return ex.Message;
        }
    }

    /// <summary>
    /// Resolves an audio URL or bare filename to a disk path.
    /// Explicit sys:// and file:// URLs resolve directly.
    /// Bare filenames (no scheme) try file:// first, then sys://, so user
    /// files shadow system sounds.
    /// </summary>
    private string? ResolveAudioPath(string url)
    {
        if (url.StartsWith("sys://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            if (!VfsPath.TryResolve(url, _userRoot, _sysRoot, out var resolved, out var err))
            {
                _logger.LogWarning("SoundModule: path resolution failed for {Url}: {Err}", url, err);
                return null;
            }
            return File.Exists(resolved) ? resolved : null;
        }

        // Bare path: try user root first, then sys root.
        var normalized = url.Replace('/', Path.DirectorySeparatorChar)
                            .TrimStart(Path.DirectorySeparatorChar);
        var userPath = Path.GetFullPath(Path.Combine(_userRoot, normalized));
        if (File.Exists(userPath)) return userPath;

        var sysPath = Path.GetFullPath(Path.Combine(_sysRoot, normalized));
        if (File.Exists(sysPath)) return sysPath;

        return null;
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
