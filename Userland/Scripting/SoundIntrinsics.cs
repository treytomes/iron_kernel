using Miniscript;

namespace Userland.Scripting;

/// <summary>
/// MiniScript sound API modelled on the MiniMicro Sound class.
///
/// Globals:
///   Sound       — constructor map: s = new Sound; also Sound.playAsset, Sound.Sine, etc.
///   noteFreq(n) — equal-temperament MIDI note → Hz
///
/// Sound instance members:
///   s.init(waveform, freq, dur, volume=1, attack=0.01, decay=0, sustain=1, release=0.05)
///   s.play([volume=1, pan=0, speed=1])
///   s.stop()
///   s.setVolume(v)
///
/// file.loadSound(path) returns the same kind of Sound instance, pre-loaded with PCM data.
/// Its .play([volume, pan, speed]) applies volume and speed scaling before sending to OpenAL.
/// </summary>
public static class SoundIntrinsics
{
    private const double WfSine     = 0;
    private const double WfTriangle = 1;
    private const double WfSawtooth = 2;
    private const double WfSquare   = 3;
    private const double WfNoise    = 4;

    public static void Register()
    {
        RegisterSoundConstructor();
        RegisterNoteFreq();
    }

    // ------------------------------------------------------------------
    // Path normalisation — convert MiniMicro-style /sys/... to sys://...
    // ------------------------------------------------------------------

    internal static string NormalizeSoundPath(string path)
    {
        // MiniMicro demos use /sys/... and /usr/... — map to our VFS schemes
        if (path.StartsWith("/sys/", StringComparison.OrdinalIgnoreCase))
            return "sys://" + path["/sys/".Length..];
        if (path.StartsWith("/usr/", StringComparison.OrdinalIgnoreCase))
            return "file://" + path["/usr/".Length..];
        return path;
    }

    // ------------------------------------------------------------------
    // Shared Sound instance factory
    // Used by both loadSound (pre-loaded PCM) and s.play (synthesizer).
    // ------------------------------------------------------------------

    /// <summary>
    /// Build a Sound map pre-loaded with decoded PCM samples.
    /// play(volume=1, pan=0, speed=1) applies volume/speed scaling before sending to OpenAL.
    /// pan is accepted but ignored (no stereo panning in this version).
    /// </summary>
    internal static ValMap BuildSoundInstance(WorldScriptContext world, float[] samples, int sampleRate)
    {
        var inst = new ValMap();
        // Store samples as a hidden field so play() can access them
        inst["_samples"]    = SamplesToValList(samples);
        inst["_sampleRate"] = new ValNumber(sampleRate);

        // play([volume=1, pan=0, speed=1])
        var playIntrinsic = Intrinsic.Create("");
        playIntrinsic.AddParam("volume", new ValNumber(1.0));
        playIntrinsic.AddParam("pan",    new ValNumber(0.0));
        playIntrinsic.AddParam("speed",  new ValNumber(1.0));
        playIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext w)
                return Intrinsic.Result.Null;

            var self = ctx.GetVar("self") as ValMap;
            if (self == null) return Intrinsic.Result.Null;

            if (!self.TryGetValue(new ValString("_samples"), out var rawSamples) ||
                !self.TryGetValue(new ValString("_sampleRate"), out var rawRate))
                return Intrinsic.Result.Null;

            var srcSamples = ValListToSamples(rawSamples as ValList);
            var srcRate    = rawRate is ValNumber rn ? (int)rn.value : 22050;

            var volume = ctx.GetVar("volume") is ValNumber vn ? (float)vn.value : 1f;
            var speed  = ctx.GetVar("speed")  is ValNumber sn ? (float)sn.value : 1f;

            var final = ApplyVolumeSpeed(srcSamples, volume, speed);
            var rate  = (int)(srcRate * speed);
            w.Sound.PlayPcm(final, rate);
            return Intrinsic.Result.Null;
        };
        inst["play"] = playIntrinsic.GetFunc();

        // stop()
        var stopIntrinsic = Intrinsic.Create("");
        stopIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is WorldScriptContext w2)
                w2.Sound.Stop();
            return Intrinsic.Result.Null;
        };
        inst["stop"] = stopIntrinsic.GetFunc();

        return inst;
    }

    // ------------------------------------------------------------------
    // Sound constructor + static constants
    // ------------------------------------------------------------------

    private static void RegisterSoundConstructor()
    {
        var soundProto = BuildSoundProto();
        var soundIntrinsic = Intrinsic.Create("Sound");
        soundIntrinsic.code = (ctx, partial) => new Intrinsic.Result(soundProto);
    }

    private static ValMap BuildSoundProto()
    {
        var proto = new ValMap();

        // Waveform constants
        proto["Sine"]     = new ValNumber(WfSine);
        proto["Triangle"] = new ValNumber(WfTriangle);
        proto["Sawtooth"] = new ValNumber(WfSawtooth);
        proto["Square"]   = new ValNumber(WfSquare);
        proto["Noise"]    = new ValNumber(WfNoise);

        // s.init(waveform, freq, duration, volume, attack, decay, sustain, release)
        var initIntrinsic = Intrinsic.Create("");
        initIntrinsic.AddParam("waveform", new ValNumber(WfSine));
        initIntrinsic.AddParam("freq",     new ValNumber(440));
        initIntrinsic.AddParam("duration", new ValNumber(1.0));
        initIntrinsic.AddParam("volume",   new ValNumber(1.0));
        initIntrinsic.AddParam("attack",   new ValNumber(0.01));
        initIntrinsic.AddParam("decay",    new ValNumber(0.0));
        initIntrinsic.AddParam("sustain",  new ValNumber(1.0));
        initIntrinsic.AddParam("release",  new ValNumber(0.05));
        initIntrinsic.code = (ctx, partial) =>
        {
            var self = ctx.GetVar("self") as ValMap;
            if (self == null) return Intrinsic.Result.Null;
            self["_waveform"] = ctx.GetVar("waveform") ?? new ValNumber(WfSine);
            self["_freq"]     = ctx.GetVar("freq")     ?? new ValNumber(440);
            self["_duration"] = ctx.GetVar("duration") ?? new ValNumber(1.0);
            self["_volume"]   = ctx.GetVar("volume")   ?? new ValNumber(1.0);
            self["_attack"]   = ctx.GetVar("attack")   ?? new ValNumber(0.01);
            self["_decay"]    = ctx.GetVar("decay")    ?? new ValNumber(0.0);
            self["_sustain"]  = ctx.GetVar("sustain")  ?? new ValNumber(1.0);
            self["_release"]  = ctx.GetVar("release")  ?? new ValNumber(0.05);
            // Clear any pre-loaded PCM so play() uses synthesis path
            self.map.Remove(new ValString("_samples"));
            self.map.Remove(new ValString("_sampleRate"));
            return new Intrinsic.Result(self);
        };
        proto["init"] = initIntrinsic.GetFunc();

        // s.play([volume=1, pan=0, speed=1])
        var playIntrinsic = Intrinsic.Create("");
        playIntrinsic.AddParam("volume", new ValNumber(1.0));
        playIntrinsic.AddParam("pan",    new ValNumber(0.0));
        playIntrinsic.AddParam("speed",  new ValNumber(1.0));
        playIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;

            var self = ctx.GetVar("self") as ValMap;
            if (self == null) return Intrinsic.Result.Null;

            var volume = ctx.GetVar("volume") is ValNumber vn ? (float)vn.value : 1f;
            var speed  = ctx.GetVar("speed")  is ValNumber sn ? (float)sn.value : 1f;

            // Pre-loaded PCM path (from file.loadSound)
            if (self.TryGetValue(new ValString("_samples"), out var rawSamples) && rawSamples is ValList)
            {
                var srcSamples = ValListToSamples(rawSamples as ValList);
                var srcRate    = self.TryGetValue(new ValString("_sampleRate"), out var rr) && rr is ValNumber rrn
                    ? (int)rrn.value : 22050;
                var final = ApplyVolumeSpeed(srcSamples, volume, speed);
                world.Sound.PlayPcm(final, (int)(srcRate * speed));
                return Intrinsic.Result.Null;
            }

            // Synthesizer path (from new Sound + s.init)
            double Get(string key, double def)
                => self.TryGetValue(new ValString(key), out var v) && v is ValNumber n ? n.value : def;

            var wfIndex  = (int)Get("_waveform", WfSine);
            var freq     = Get("_freq",     440.0);
            var duration = Get("_duration", 1.0);
            var svol     = Get("_volume",   1.0);
            var attack   = Get("_attack",   0.01);
            var decay    = Get("_decay",    0.0);
            var sustain  = Get("_sustain",  1.0);
            var release  = Get("_release",  0.05);

            var wf = wfIndex switch
            {
                1 => SoundSynthesizer.Waveform.Triangle,
                2 => SoundSynthesizer.Waveform.Sawtooth,
                3 => SoundSynthesizer.Waveform.Square,
                4 => SoundSynthesizer.Waveform.Noise,
                _ => SoundSynthesizer.Waveform.Sine
            };

            var samples = SoundSynthesizer.Generate(wf, freq * speed, duration, 22050,
                attack, decay, sustain, release, svol * volume);

            world.Sound.PlayPcm(samples, 22050);
            return Intrinsic.Result.Null;
        };
        proto["play"] = playIntrinsic.GetFunc();

        // s.stop()
        var stopIntrinsic = Intrinsic.Create("");
        stopIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is WorldScriptContext world)
                world.Sound.Stop();
            return Intrinsic.Result.Null;
        };
        proto["stop"] = stopIntrinsic.GetFunc();

        // s.setVolume(v)
        var volIntrinsic = Intrinsic.Create("");
        volIntrinsic.AddParam("volume", new ValNumber(1.0));
        volIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;
            var v = ctx.GetVar("volume") is ValNumber n ? (float)n.value : 1f;
            world.Sound.SetVolume(v);
            return Intrinsic.Result.Null;
        };
        proto["setVolume"] = volIntrinsic.GetFunc();

        // Sound.playAsset(path) — fire-and-forget convenience
        var assetIntrinsic = Intrinsic.Create("");
        assetIntrinsic.AddParam("path", ValString.empty);
        assetIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;
            var path = ctx.GetVar("path")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return Intrinsic.Result.Null;

            path = NormalizeSoundPath(path);
            var (samples, sampleRate, error) = world.Sound.LoadSound(path);
            if (error != null)
            {
                ctx.interpreter.errorOutput?.Invoke($"Sound.playAsset: {error}", true);
                return Intrinsic.Result.Null;
            }
            world.Sound.PlayPcm(samples!, sampleRate);
            return Intrinsic.Result.Null;
        };
        proto["playAsset"] = assetIntrinsic.GetFunc();

        return proto;
    }

    // ------------------------------------------------------------------
    // noteFreq(midiNote)
    // ------------------------------------------------------------------

    private static void RegisterNoteFreq()
    {
        var nf = Intrinsic.Create("noteFreq");
        nf.AddParam("noteNumber", new ValNumber(69));
        nf.code = (ctx, partial) =>
        {
            var note = ctx.GetVar("noteNumber") is ValNumber n ? n.value : 69;
            var freq = 440.0 * Math.Pow(2.0, (note - 69.0) / 12.0);
            return new Intrinsic.Result(new ValNumber(freq));
        };
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static ValList SamplesToValList(float[] samples)
    {
        var list = new ValList();
        list.values.Capacity = samples.Length;
        foreach (var s in samples)
            list.values.Add(new ValNumber(s));
        return list;
    }

    private static float[] ValListToSamples(ValList? list)
    {
        if (list == null) return [];
        var arr = new float[list.values.Count];
        for (var i = 0; i < list.values.Count; i++)
            arr[i] = list.values[i] is ValNumber n ? (float)n.value : 0f;
        return arr;
    }

    private static float[] ApplyVolumeSpeed(float[] samples, float volume, float speed)
    {
        if (Math.Abs(volume - 1f) < 0.001f && Math.Abs(speed - 1f) < 0.001f)
            return samples;

        // Speed != 1: resample by linear interpolation
        if (Math.Abs(speed - 1f) >= 0.001f)
        {
            var newLen = (int)(samples.Length / speed);
            var resampled = new float[newLen];
            for (var i = 0; i < newLen; i++)
            {
                var src = i * speed;
                var lo = (int)src;
                var hi = Math.Min(lo + 1, samples.Length - 1);
                var t = (float)(src - lo);
                resampled[i] = (samples[lo] * (1f - t) + samples[hi] * t) * volume;
            }
            return resampled;
        }

        // Volume only
        var scaled = new float[samples.Length];
        for (var i = 0; i < samples.Length; i++)
            scaled[i] = samples[i] * volume;
        return scaled;
    }
}
