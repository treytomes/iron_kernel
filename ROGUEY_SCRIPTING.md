# ROGUEY SCRIPTING

This document describes the **Roguey MiniScript API**, which builds on IronKernel’s morphic and MiniScript infrastructure to support a roguelike-style game. It focuses on **map tiles**, their creation from MiniScript, and how they integrate with the shared Morph/slot system.

This is a **game-level API**, layered on top of the core MiniScript and Morph APIs.

---

## Overview

Roguey extends the generic `Morph` scripting model with **tile-specific behavior**.  
Tiles are implemented as `MapTileMorph` instances, which:

- Inherit from `MiniScriptMorph`
- Store all gameplay-relevant state in **MiniScript slots**
- Render using a glyph-based tileset
- Are fully scriptable from the MiniScript REPL

All tile interaction in scripts happens through **handles**, just like Morph handles.

---

## Tile Handles

A Tile handle is a MiniScript map that represents a live `MapTileMorph`.

Conceptually:

```miniscript
{
  "__isa": "Morph",
  "__id": 42,
  get: <function>,
  set: <function>,
  has: <function>,
  delete: <function>,
  destroy: <function>,
  isAlive: <function>
}
```

Tile handles:
- Are safe to store and pass around
- Detect dead tiles automatically
- Expose instance-style methods (`t.set(...)`, `t.get(...)`)

---

## Creating Tiles

### `Tile.create([x, y])`

Create a new map tile at the given position.

```miniscript
t = Tile.create([10, 5])
```

Behavior:
- Creates a new `MapTileMorph`
- Sets its position to `(x, y)`
- Registers it with the world
- Returns a **fully bound tile handle**

Errors:
- Invalid arguments result in a MiniScript error message
- No engine crash or REPL hang

---

## Tile Properties (Slot-Backed)

All tile properties are backed by **MiniScript slots**.  
You access them using the standard Morph instance methods.

### `TileIndex`

Glyph index used for rendering.

```miniscript
t.set("TileIndex", 35)
index = t.get("TileIndex")
```

Default:
- `'.'` (ASCII dot)

---

### `ForegroundColor`

Glyph foreground color (`RadialColor`).

```miniscript
t.set("ForegroundColor", RadialColor.create(5, 5, 5))
```

Default:
- White

---

### `BackgroundColor`

Optional glyph background color (`RadialColor`).

```miniscript
t.set("BackgroundColor", RadialColor.create(0, 0, 2))
t.delete("BackgroundColor")   -- makes background transparent
```

Default:
- None (transparent)

---

### `BlocksMovement`

Whether the tile blocks movement.

```miniscript
t.set("BlocksMovement", true)
```

Default:
- `false`

---

### `BlocksVision`

Whether the tile blocks line of sight.

```miniscript
t.set("BlocksVision", true)
```

Default:
- `false`

---

### `TileTag`

Free-form string tag for gameplay logic.

```miniscript
t.set("TileTag", "wall")
```

Default:
- `"floor"`

---

## Tile Queries

### `Tile.isBlocked(tile)`

Check whether a tile blocks movement.

```miniscript
if Tile.isBlocked(t) then
    print("Can't walk here")
end
```

This is a convenience wrapper around the `BlocksMovement` slot.

---

## Using Tile Handles as Morphs

Tile handles support all standard Morph instance methods:

```miniscript
t.set("danger", true)

if t.has("danger") then
    print("Careful!")
end

t.delete("danger")
```

They also support lifetime checks:

```miniscript
if t.isAlive() then
    print("Tile still exists")
end
```

And destruction:

```miniscript
t.destroy()
```

---

## Rendering Model (Host-Side)

Rendering is handled entirely by `MapTileMorph` in C#:

- Tiles load a glyph set (`image.oem437_8`)
- Rendering uses:
  - `TileIndex`
  - `ForegroundColor`
  - `BackgroundColor`
- Scripts do **not** draw directly

This keeps rendering fast and deterministic.

---

## Error Handling

All Roguey intrinsics:

- Catch host exceptions
- Report errors via the MiniScript error stream
- Never crash or hang the REPL

Example:

```text
Tile.create error: expected [x,y]
```

---

## Design Principles

- **Slots are authoritative** for gameplay meaning
- **C# properties are adapters** for rendering and engine systems
- **World owns lifetime**, scripts hold references
- **Instance-style methods** via bound intrinsics (`ctx.self`)
- No inheritance or prototype magic in MiniScript

---

## Summary

The Roguey API provides:

- Scriptable, slot-backed map tiles
- Safe creation and destruction from MiniScript
- Instance-style scripting (`t.set(...)`)
- Clean separation of game logic and rendering
- A scalable foundation for roguelike mechanics

This API is designed to grow naturally toward:
- Tile maps
- Entities
- Items
- Turn systems
- AI logic

without changing the core scripting model.
