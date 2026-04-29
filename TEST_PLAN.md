# Test Plan

This document covers the features added and changed in the session that introduced
audio infrastructure, the `sys://` filesystem protocol, and the software synthesizer
(SoundModule, software synthesizer, MiniScript Sound class, sysdisk bundle, `sys://` protocol).

---

## 1. sys:// filesystem protocol

The `sys://` scheme maps to `assets/sys/` (read-only).
The `file://` scheme maps to the user's AppData storage (read/write).

### 1.1 Directory listing

Open a terminal and run:

```miniscript
dir "sys://"
```

Expected: lists `data`, `demo`, `fonts`, `help`, `lib`, `pics`, `sounds`,
`startup.ms`, etc.  No error.

```miniscript
dir "sys://sounds"
```

Expected: lists the ~50 WAV files from minimicro-sysdisk.

### 1.2 File existence check

`exists` and `readText` are **not currently exposed as MiniScript intrinsics** — they are internal C# extension methods on `IFileSystem`. These test cases require those intrinsics to be added before they can be run from the REPL.

When implemented, the expected behavior is:

```miniscript
// These should both return truthy without error
print exists("sys://sounds/blipA4.wav")
print exists("sys://lib/listUtil.ms")
```

### 1.3 Read a sys:// file

Requires a `readText` intrinsic (not yet exposed). When implemented:

```miniscript
src = readText("sys://lib/listUtil.ms")
print src[0:80]
```

Expected: prints the first 80 characters of `listUtil.ms`.

### 1.4 sys:// is read-only

`writeText` is **not currently a MiniScript intrinsic**. Use `del` to verify write rejection:

```miniscript
del "sys://sounds/blipA4.wav"
```

Expected: error output — `sys:// is read-only.`

### 1.5 Path traversal rejected

```miniscript
dir "sys://../etc"
```

Expected: error output containing `traversal`.

### 1.6 file:// round-trip

`readText` and `writeText` are not currently MiniScript intrinsics. This test requires them to be added. When implemented:

```miniscript
writeText "file://hello.txt", "hello world"
print readText("file://hello.txt")
del "file://hello.txt"
```

Expected: prints `hello world`, no errors.

---

## 2. Sound.playAsset — WAV playback

### 2.1 Explicit sys:// path

```miniscript
Sound.playAsset "sys://sounds/blipA4.wav"
```

Expected: audible blip.  No error output.

### 2.2 Bare relative path (file:// → sys:// fallback)

```miniscript
Sound.playAsset "sounds/blipA4.wav"
```

Expected: audible blip (resolved via sys:// fallback).  No error output.

### 2.3 Other bundled sounds

Try a few to confirm the sysdisk bundle is intact:

```miniscript
Sound.playAsset "sounds/bonus.wav"
Sound.playAsset "sounds/pop.wav"
Sound.playAsset "sounds/fanfare.wav"
```

### 2.4 User-file shadow

Write a WAV to user storage with the same relative path and confirm it takes
priority over the sys:// version:

```miniscript
// Craft a minimal WAV and write it (or copy an existing one)
// Then:
Sound.playAsset "sounds/blipA4.wav"
```

Expected: the user's file plays, not the system one.

### 2.5 Error: file not found

```miniscript
Sound.playAsset "sounds/doesnotexist.wav"
```

Expected: error output — `Sound.playAsset: Sound file not found: sounds/doesnotexist.wav`

### 2.6 Error: explicit bad sys:// path

```miniscript
Sound.playAsset "sys://sounds/ghost.wav"
```

Expected: error output — `Sound.playAsset: Sound file not found: …`

### 2.7 Error: path traversal attempt

```miniscript
Sound.playAsset "sys://../secret"
```

Expected: error output containing `traversal` or `resolution failed`.

### 2.8 Empty argument (silent no-op)

```miniscript
Sound.playAsset ""
```

Expected: silent — no error, no crash.

---

## 3. Software synthesizer (s.init / s.play)

### 3.1 Basic sine tone

```miniscript
s = new Sound
s.init Sound.Sine, 440, 0.5
s.play
```

Expected: audible 440 Hz sine for ~0.5 seconds.

### 3.2 noteFreq helper

```miniscript
s = new Sound
s.init Sound.Sine, noteFreq(69), 0.5   // A4 = 440 Hz
s.play
```

Expected: same tone as above.

```miniscript
print noteFreq(69)   // should print ~440
print noteFreq(60)   // should print ~261.6 (middle C)
print noteFreq(81)   // should print ~880 (A5)
```

### 3.3 All waveforms

```miniscript
for wf in [Sound.Sine, Sound.Triangle, Sound.Sawtooth, Sound.Square, Sound.Noise]
  s = new Sound
  s.init wf, 330, 0.3
  s.play
  wait 0.4
end for
```

Expected: five audibly distinct timbres in sequence.

### 3.4 Volume scaling

```miniscript
s = new Sound
s.init Sound.Sine, 440, 0.5, 0.2   // volume = 0.2
s.play
```

Expected: same pitch but noticeably quieter than the default volume-1 case.

### 3.5 Attack and release envelope

```miniscript
s = new Sound
s.init Sound.Sine, 440, 1.0, 1.0, 0.3, 0.0, 1.0, 0.3
//                           ^attack=0.3s           ^release=0.3s
s.play
```

Expected: tone fades in over ~0.3 s and fades out over the last ~0.3 s.

### 3.6 s.stop

```miniscript
s = new Sound
s.init Sound.Sine, 200, 5.0   // 5 second note
s.play
wait 1
s.stop
```

Expected: sound stops after ~1 second.

### 3.7 Chord (simultaneous voices)

```miniscript
s1 = new Sound
s2 = new Sound
s3 = new Sound
s1.init Sound.Sine, noteFreq(60), 1.0   // C4
s2.init Sound.Sine, noteFreq(64), 1.0   // E4
s3.init Sound.Sine, noteFreq(67), 1.0   // G4
s1.play
s2.play
s3.play
```

Expected: audible C major triad.

### 3.8 Defaults (play without init)

```miniscript
s = new Sound
s.play
```

Expected: 1-second 440 Hz sine with default envelope.  No crash.

---

## 4. setVolume (master volume)

```miniscript
Sound.setVolume 0.1
Sound.playAsset "sounds/blipA4.wav"
wait 0.5
Sound.setVolume 1.0
Sound.playAsset "sounds/blipA4.wav"
```

Expected: first blip is much quieter than the second.

---

## 5. help intrinsic

### 5.1 No argument — general reference

```miniscript
help
```

Expected: prints the protocol and API reference to the terminal, including:
- `FILE PROTOCOLS` section listing `file://`, `sys://`, `asset://`
- `FILESYSTEM` section listing `dir`, `cd`, `pwd`, etc.
- `SOUND` section listing `Sound.playAsset`, `new Sound`, `noteFreq`, waveform constants

### 5.2 Function argument — docstring

```miniscript
myFunc = function(x)
  "Doubles x."
  return x * 2
end function
help @myFunc
```

Expected: prints the function name and its docstring `Doubles x.`

### 5.3 Function with no docstring

```miniscript
bare = function(x)
  return x + 1
end function
help @bare
```

Expected: prints `No help available for …`

### 5.4 Non-function argument falls back to general help

```miniscript
help 42
```

Expected: prints the same general reference as `help` with no argument.

---

## 6. Filesystem intrinsics — regression

These should continue working as before.

```miniscript
// Create, list, navigate, delete
mkdir "file://testdir"
dir "file://"            // should show testdir
cd "testdir"
pwd                      // should show file://testdir
cd
pwd                      // should show file://
del "file://testdir"
dir "file://"            // testdir should be gone
```

```miniscript
// Copy and move — requires readText/writeText/exists intrinsics (not yet exposed)
writeText "file://a.txt", "content"
copy "file://a.txt", "file://b.txt"
print readText("file://b.txt")   // content
move "file://b.txt", "file://c.txt"
print exists("file://b.txt")     // 0
print readText("file://c.txt")   // content
del "file://a.txt"
del "file://c.txt"
```

---

## 7. Image assets — regression

The `asset://image.*` subsystem was not changed, but confirm it still works:

- Launch the kernel and verify the halo appears on a selected morph with all
  icons (move, resize, inspect, delete) rendering correctly.
- Open a terminal window; confirm the screen font renders correctly.

---

## 8. Unit test suite

All 292 automated tests should continue to pass:

```bash
dotnet test IronKernel.Tests/IronKernel.Tests.csproj
```

Key test classes introduced in this session:

| File | What it covers |
|---|---|
| `WavLoaderTests.cs` | 16-bit PCM mono/stereo parse, invalid RIFF throws, real-file smoke test |
| `NoteFreqTests.cs` | Equal-temperament Hz for A4/C4/A3/A5/MIDI 0/127; octave doubling/halving |
| `SysProtocolTests.cs` | sys:// → sysRoot, file:// → userRoot, root-only URL, traversal rejected (both schemes), unknown scheme, nested path |
| `SoundSynthesizerTests.cs` | Output length, unit range, zero-duration empty, RMS by waveform, volume scaling, attack/release/sustain envelope edges, octave-frequency invariance |

---

## Known limitations / not tested in this plan

- **OGG streaming** (#164) — not yet implemented; assigned @trey.
- **sys:// `import` / `run`** — the scripting intrinsics pass `sys://` paths
  straight through to the filesystem module.  A script at `sys://lib/listUtil.ms`
  should be importable as `import "sys://lib/listUtil.ms"`.  This is untested
  and may need a follow-up.
- **Bare-key audio** (e.g. `Sound.playAsset "blipa4"`) — the old `asset://sound.*`
  registry is removed.  Bare keys that aren't relative paths (no `/`) will fail
  with a "not found" error.  Any existing scripts using that convention need to be
  updated to `Sound.playAsset "sounds/blipA4.wav"` or the sys:// form.
