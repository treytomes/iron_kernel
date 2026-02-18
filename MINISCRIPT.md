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