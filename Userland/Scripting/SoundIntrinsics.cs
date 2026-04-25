using Miniscript;

namespace Userland.Scripting;

/// <summary>
/// MiniScript sound API.  Exposes Sound.play, Sound.stop, Sound.setVolume,
/// and the global noteFreq() helper — a subset of the MiniMicro Sound API
/// sufficient for asset playback and procedural synthesis.
/// </summary>
public static class SoundIntrinsics
{
    public static void Register()
    {
        RegisterSoundMap();
        RegisterNoteFreq();
    }

    // ------------------------------------------------------------------
    // Sound map
    // ------------------------------------------------------------------

    private static void RegisterSoundMap()
    {
        var soundMap = new ValMap();

        // Sound.play("assetKey") — play a WAV asset by key or asset:// URL
        var playFunc = new ValFunction(new Function(null));
        var playIntrinsic = Intrinsic.Create("");
        playIntrinsic.AddParam("assetKey", ValString.empty);
        playIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;
            var key = ctx.GetVar("assetKey")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key)) return Intrinsic.Result.Null;
            world.Sound.PlayAsset(key);
            return Intrinsic.Result.Null;
        };
        soundMap["play"] = playIntrinsic.GetFunc();

        // Sound.stop()
        var stopIntrinsic = Intrinsic.Create("");
        stopIntrinsic.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is WorldScriptContext world)
                world.Sound.Stop();
            return Intrinsic.Result.Null;
        };
        soundMap["stop"] = stopIntrinsic.GetFunc();

        // Sound.setVolume(v) — 0.0..1.0
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
        soundMap["setVolume"] = volIntrinsic.GetFunc();

        // Expose Sound as a global
        var soundIntrinsic = Intrinsic.Create("Sound");
        soundIntrinsic.code = (ctx, partial) => new Intrinsic.Result(soundMap);
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
            // Equal-temperament: freq = 440 * 2^((note - 69) / 12)
            var freq = 440.0 * Math.Pow(2.0, (note - 69.0) / 12.0);
            return new Intrinsic.Result(new ValNumber(freq));
        };
    }
}
