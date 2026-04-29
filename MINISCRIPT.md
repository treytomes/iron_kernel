# MINISCRIPT API

This document describes the **MiniScript API surface** provided by IronKernel’s `WorldScriptContext`.

It focuses on the **script‑facing API**:
- what MiniScript authors can call
- what objects they receive
- how those objects behave

This is **not** a MiniScript language reference.  
It documents the *host API* layered on top of MiniScript.

---

## Core Concepts

### Script World

MiniScript runs inside a **World context** owned by IronKernel.

Scripts **do not manipulate engine objects directly**.  
Instead, they interact through **handles** that represent live engine objects.

A handle is a MiniScript `map` that:
- represents a real engine object
- exposes instance‑style methods
- remains safe if the underlying object is destroyed

Conceptually, a handle looks like:

```miniscript
{
  "__isa": "Morph",
  "__id": 3,
  destroy: <function>,
  isAlive: <function>,
  props: <map>
}
```

Handles are:
- **Safe** — dead objects are detected
- **Stateful** — values persist across calls
- **Instance‑oriented** — methods are bound to `self`

---

## Script Projection Model

Engine objects expose a **projected property map** to MiniScript via `props`.

- Scripts mutate `props`
- Changes are **buffered**
- Changes are applied when the world commits script edits

This keeps:
- rendering deterministic
- engine invariants intact
- script execution safe

---

## Dialog & UI Intrinsics

These intrinsics provide **modal UI dialogs**.

All dialogs:
- pause script execution until the user responds
- never block the engine or UI thread
- return control cleanly to the script

### `alert(message)`

Display a modal alert dialog.

```miniscript
alert("Hello, world!")
print("This runs after OK")
```

Returns `null`.

---

### `prompt(message, defaultValue)`

Request text input from the user.

```miniscript
name = prompt("Enter your name:", "Player")
if name != null then
    print("Hello " + name)
end if
```

Returns:
- string if confirmed
- `null` if cancelled

---

### `confirm(message)`

Display a confirmation dialog.

```miniscript
if confirm("Delete all files?") then
    print("Confirmed")
end if
```

Returns `true` or `false`.

---

## Timing & Input

### `time`

Returns seconds elapsed since the kernel started, as a float. Backed by the engine's monotonic update clock — suitable for BPM timing, animation, and frame-rate-independent logic.

```miniscript
start = time
wait 1
print time - start   // ~1.0
```

---

### `wait(seconds=0)`

Suspend script execution for at least the given duration without blocking the engine or UI. Uses the continuation pattern — the engine keeps running while the script waits.

```miniscript
wait 0.5   // pause half a second
wait 0     // yield for one frame
```

---

### `mouse`

Returns a map with the current pointer state, sampled at the last input tick.

| Key | Description |
|-----|-------------|
| `mouse.x` | Pointer X in world coordinates |
| `mouse.y` | Pointer Y in world coordinates |
| `mouse.button` | `1` if primary button held, `0` otherwise |

```miniscript
if mouse.button then
    print "click at " + mouse.x + ", " + mouse.y
end if
```

---

## Console Intrinsics

### `input(prompt="")`

Read a line of text from the active console. Suspends script execution until the user presses Enter.

```miniscript
name = input("Your name: ")
print "Hello, " + name
```

Returns the entered string, or `null` if the console is closed.

---

### `cls`

Clear the active terminal output.

```miniscript
cls
```

---

### `inspect(value)`

Open the Inspector window for a value. Works with both Morph handles and plain MiniScript values.

```miniscript
inspect(myMorph)
inspect(42)
```

---

### `readText(path)`

Read a file as a UTF-8 string.

```miniscript
src = readText("sys://lib/listUtil.ms")
data = readText("file://save.json")
```

Returns the file contents as a string, or `null` and emits an error on failure.

---

### `writeText(path, content)`

Write a string to a `file://` path as UTF-8. Rejects `sys://` paths.

```miniscript
writeText "file://notes.txt", "hello world"
```

---

### `exists(path)`

Returns `1` if the path exists, `0` otherwise. Works with both `file://` and `sys://`.

```miniscript
if exists("file://save.json") then
    data = readText("file://save.json")
end if
```

---

## Editor & Script Execution

### `edit(filename)`

Open the built‑in editor and load a file.

```miniscript
edit("file://example.ms")
```

Returns `null`.

---

### `run(filename)`

Load, compile, and execute a MiniScript file.

```miniscript
run("file://example.ms")
print("This always runs")
```

Errors are reported to the REPL but **never hang the interpreter**.

---

### `import(name)`

Import a MiniScript module. The module is executed and its exported value is bound to a variable named after the module.

```miniscript
import "sys://lib/listUtil.ms"
// listUtil is now available as a variable
```

Resolves paths the same way as `run`. Adds `.ms` extension if not present.

---

## Morph API

`Morph` is the primary script‑visible object type.  
It represents a visual object in the world.

### Morph Lifetime

Every Morph handle provides:

#### `m.destroy()`

Destroy the morph.

```miniscript
m.destroy()
```

- Removes the morph from the world
- Invalidates the handle
- Safe to call multiple times

---

#### `m.isAlive()`

Check whether the morph still exists.

```miniscript
if m.isAlive() then
    print("Still alive")
end if
```

---

### Morph Properties (`props`)

All Morphs expose a `props` map.

Common properties:
- `position` → `[x, y]`
- `size` → `[width, height]`

```miniscript
m.props.position = [100, 50]
m.props.size = [64, 24]
```

Changes take effect when script edits are committed.

---

## Label API

`Label` is a Morph that displays text.

### Creating a Label

```miniscript
label = Label.create([20, 20], "Hello world")
```

Returns a **Label handle**.

---

### Label Properties

All properties are edited through `label.props`.

| Property | Type |
|--------|------|
| `text` | string |
| `position` | `[x, y]` |
| `foregroundColor` | `Color` |
| `backgroundColor` | `Color` |

Example:

```miniscript
label.props.text = "Updated text"
label.props.position = [100, 40]
label.props.foregroundColor = Color.create(5, 5, 0)
label.props.backgroundColor = Color.create(0, 0, 0)
```

---

## TileMap API (Roguey)

### Overview

A **TileMap** is a single Morph that renders a grid of tiles.

- Tiles are **data objects**, not Morphs
- Rendering is performed in bulk by the TileMap
- Scripts modify tile data, not rendering logic

This design scales to very large maps efficiently.

---

### Creating a TileMap

```miniscript
map = TileMap.create(
    [320, 240],          // viewport size (pixels)
    [64, 64],            // map size (tiles)
    "asset://tileset",   // tileset asset
    [8, 8]               // tile size (pixels)
)
```

Returns a **TileMap handle**.

---

### TileMap Properties

TileMap exposes common Morph properties via `props`.

Additional properties:
- `scrollOffset` → `[x, y]`

```miniscript
map.props.scrollOffset = [16, 32]
```

---

### Tile index shorthands

#### `map.setCell(x, y, index)`

Set the tile index at `(x, y)` directly. Out-of-bounds calls are silently ignored.

```miniscript
map.setCell 3, 5, 14   // set tile at (3,5) to index 14
```

#### `map.cell(x, y)`

Return the tile index at `(x, y)` as an integer. Returns `null` if out of bounds.

```miniscript
if map.cell(3, 5) == 14 then print "lit"
```

#### `map.fill(index)`

Set every tile's index to the given value in a single O(w×h) operation.

```miniscript
map.fill 0    // clear the map
map.fill 9    // fill with tile 9
```

---

### Accessing Tiles

#### `map.getTile(x, y)`

Retrieve the tile at `(x, y)`.

```miniscript
tile = map.getTile(10, 5)
```

Returns:
- a **TileInfo map** if in bounds
- `null` otherwise

---

## TileInfo API

A **Tile** is a projected data object representing one cell.

Tiles are **not Morphs** and cannot be destroyed independently.

### Tile Properties

All properties are edited directly on the returned tile map:

| Property | Description |
|--------|-------------|
| `tileIndex` | glyph index |
| `foregroundColor` | glyph foreground color |
| `backgroundColor` | glyph background color |
| `blocksMovement` | blocks movement |
| `blocksVision` | blocks vision |
| `tag` | free‑form string |

Example:

```miniscript
tile = map.getTile(3, 7)
tile.tileIndex = 176
tile.foregroundColor = Color.create(5, 5, 5)
tile.blocksMovement = true
tile.tag = "wall"
```

Changes are applied at the script commit boundary.

---

## Sound API

IronKernel provides a software synthesizer and WAV playback API modelled on MiniMicro's `Sound` class.

### `Sound` — the global sound object

`Sound` is a global map that doubles as a constructor.

#### Static methods / properties

| Name | Description |
|------|-------------|
| `Sound.playAsset(path)` | Fire-and-forget WAV playback |
| `Sound.setVolume(v)` | Set master volume (0–1) |
| `Sound.Sine` | Waveform constant (0) |
| `Sound.Triangle` | Waveform constant (1) |
| `Sound.Sawtooth` | Waveform constant (2) |
| `Sound.Square` | Waveform constant (3) |
| `Sound.Noise` | Waveform constant (4) |

#### `Sound.playAsset(path)`

Load and play a WAV file immediately. `path` can be:
- `sys://sounds/blipA4.wav` — explicit system path
- `file://mysound.wav` — user storage
- `sounds/blipA4.wav` — bare relative path (checks `file://` first, then `sys://`)

```miniscript
Sound.playAsset "sys://sounds/blipA4.wav"
Sound.playAsset "sounds/bonus.wav"   // bare path, sys:// fallback
```

---

### Creating a synthesizer voice

```miniscript
s = new Sound
```

Returns a Sound instance. Plays a 440 Hz sine for 1 second with default settings if `.play` is called immediately.

#### `s.init(waveform, freq, duration, volume=1, attack=0.01, decay=0, sustain=1, release=0.05)`

Configure the voice before playing.

```miniscript
s = new Sound
s.init Sound.Sine, 440, 0.5
s.play
```

```miniscript
// With envelope
s.init Sound.Triangle, 330, 1.0, 1.0, 0.3, 0.0, 1.0, 0.3
//                               vol  atk   dec  sus  rel
s.play
```

#### `s.play(volume=1, pan=0, speed=1)`

Generate and play the configured waveform. `volume` and `speed` scale the output; `pan` is accepted but currently ignored.

#### `s.stop()`

Stop the current sound immediately.

#### `s.setVolume(v)`

Set the master output volume (0.0–1.0).

---

### `noteFreq(midiNote)`

Convert a MIDI note number to a frequency in Hz using equal temperament.

```miniscript
print noteFreq(69)   // 440.0  (A4)
print noteFreq(60)   // ~261.6 (middle C)
print noteFreq(81)   // 880.0  (A5)
```

---

### `file.loadSound(path)`

Load a WAV file and return a Sound instance with pre-loaded PCM data. The returned object has `.play(volume=1, pan=0, speed=1)` and `.stop()` methods, identical to synthesized sounds.

```miniscript
snd = file.loadSound("sys://sounds/blipA4.wav")
snd.play
snd.play 0.5     // half volume
snd.play 1, 0, 2 // double speed
```

Paths are normalized the same way as `Sound.playAsset`.

---

## Help

### `help([fn])`

With no argument, prints the general reference (file protocols, filesystem commands, sound API, display API).

```miniscript
help
```

With a function reference, prints the function's docstring (the first string literal in the body).

```miniscript
myFunc = function(x)
    "Doubles x."
    return x * 2
end function
help @myFunc     // prints: Doubles x.
```

If the function has no docstring, prints `No help available for …`.

---

## Complete Example: Labels + TileMap

```miniscript
// create a label
label = Label.create([10,10], "Loading...")
label.props.foregroundColor = Color.create(5,5,0)

// create a tile map
map = TileMap.create(
    [320,240],
    [64,64],
    "asset://image.oem437_8",
    [8,8]
)

// fill the map
for y in range(0, 63)
    for x in range(0, 63)
        t = map.getTile(x, y)
        t.tileIndex = 46
        t.blocksMovement = false
        t.tag = "floor"
    end for
end for

label.props.text = "Ready"
```

---

## Lifetime & Safety Rules

- Dead handles never crash
- `isAlive()` detects invalid objects
- All intrinsics catch host exceptions
- Errors are reported to the MiniScript error stream
- The interpreter and REPL never hang

---

## Summary

This API provides:
- Instance‑style scripting via handles
- Safe object lifetimes
- Script‑projected state with explicit commits
- Efficient tile‑based rendering
- Clean separation between logic and rendering

It is designed to support:
- interactive tools
- UI scripting
- roguelike games
- procedural generation
- gameplay logic

…without exposing internal engine state or compromising stability.