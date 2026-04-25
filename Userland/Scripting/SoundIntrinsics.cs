using Miniscript;

namespace Userland.Scripting;

/// <summary>
/// MiniScript sound API modelled on the MiniMicro Sound class.
///
/// Globals:
///   Sound  — constructor: s = new Sound
///   noteFreq(midiNote)  — equal-temperament Hz from MIDI note number
///
/// Sound instance members (on the map returned by "new Sound"):
///   s.init(waveform, freq, dur, volume=1, attack=0.01, decay=0, sustain=1, release=0.05)
///   s.play()     — generate + play
///   s.stop()     — stop all current playback
///   s.setVolume(v)
///
/// Waveform constants (on the Sound map):
///   Sound.Sine / Sound.Triangle / Sound.Square / Sound.Sawtooth / Sound.Noise
/// </summary>
public static class SoundIntrinsics
{
    // Waveform constant values — match MiniMicro convention (0-based int)
    private const double WfSine = 0;
    private const double WfTriangle = 1;
    private const double WfSawtooth = 2;
    private const double WfSquare = 3;
    private const double WfNoise = 4;

    public static void Register()
    {
        RegisterSoundConstructor();
        RegisterNoteFreq();
    }

    // ------------------------------------------------------------------
    // Sound constructor + static constants
    // ------------------------------------------------------------------

    private static void RegisterSoundConstructor()
    {
        // The Sound intrinsic acts as both a namespace (Sound.Sine etc.)
        // and a constructor factory when called as "new Sound".
        var soundProto = BuildSoundProto();

        var soundIntrinsic = Intrinsic.Create("Sound");
        soundIntrinsic.code = (ctx, partial) => new Intrinsic.Result(soundProto);
    }

    /// <summary>
    /// Returns the Sound prototype map (with constants + constructor fields).
    /// Calling "new Sound" in MiniScript creates a shallow copy of this map.
    /// </summary>
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
            // Store synthesis params on the self map so play() can read them
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
            return new Intrinsic.Result(self);
        };
        proto["init"] = initIntrinsic.GetFunc();

        // s.play()
        var playIntrinsic = Intrinsic.Create("");
        playIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;

            var self = ctx.GetVar("self") as ValMap;
            if (self == null) return Intrinsic.Result.Null;

            // Read params stored by init(); fall back to sensible defaults
            double Get(string key, double def)
                => self.TryGetValue(new ValString(key), out var v) && v is ValNumber n ? n.value : def;

            var wfIndex   = (int)Get("_waveform", WfSine);
            var freq      = Get("_freq",      440.0);
            var duration  = Get("_duration",  1.0);
            var volume    = Get("_volume",    1.0);
            var attack    = Get("_attack",    0.01);
            var decay     = Get("_decay",     0.0);
            var sustain   = Get("_sustain",   1.0);
            var release   = Get("_release",   0.05);

            var wf = wfIndex switch
            {
                1 => SoundSynthesizer.Waveform.Triangle,
                2 => SoundSynthesizer.Waveform.Sawtooth,
                3 => SoundSynthesizer.Waveform.Square,
                4 => SoundSynthesizer.Waveform.Noise,
                _ => SoundSynthesizer.Waveform.Sine
            };

            var samples = SoundSynthesizer.Generate(wf, freq, duration, 22050,
                attack, decay, sustain, release, volume);

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

        // Direct asset playback: Sound.playAsset("sys://sounds/blipA4.wav") or bare key
        var assetIntrinsic = Intrinsic.Create("");
        assetIntrinsic.AddParam("assetKey", ValString.empty);
        assetIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;
            var key = ctx.GetVar("assetKey")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                return Intrinsic.Result.Null;
            var error = world.Sound.PlayAsset(key);
            if (error != null)
                ctx.interpreter.errorOutput?.Invoke($"Sound.playAsset: {error}", true);
            return Intrinsic.Result.Null;
        };
        proto["playAsset"] = assetIntrinsic.GetFunc();

        return proto;
    }

    // ------------------------------------------------------------------
    // noteFreq(midiNote) — global helper
    // ------------------------------------------------------------------

    private static void RegisterNoteFreq()
    {
        var nf = Intrinsic.Create("noteFreq");
        nf.AddParam("noteNumber", new ValNumber(69)); // A4 = 440 Hz
        nf.code = (ctx, partial) =>
        {
            var note = ctx.GetVar("noteNumber") is ValNumber n ? n.value : 69;
            var freq = 440.0 * Math.Pow(2.0, (note - 69.0) / 12.0);
            return new Intrinsic.Result(new ValNumber(freq));
        };
    }
}
