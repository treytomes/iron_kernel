# ROGUEY SCRIPTING

This document describes the **Roguey MiniScript API**, which builds on IronKernel’s morphic and MiniScript infrastructure to support a roguelike-style game.

It focuses on **tile maps**, **tile data**, and how they are created, accessed, and mutated from MiniScript.

This is a **game-level API**, layered on top of the core MiniScript and Morph APIs.

---

## Overview

Roguey uses a **data-driven tile map model**:

- A **TileMap** is a single Morph that renders a grid of tiles.
- Individual tiles are **data objects** (`TileInfo`), not Morphs.
- MiniScript operates on **handles** that reference tile data.
- Rendering is performed in bulk by the `TileMapMorph`, not per tile.

This design avoids per-tile Morph overhead and scales to large maps.

---

## TileMap

A **TileMap** represents a 2D grid of tiles rendered using a glyph tileset.

### Creating a TileMap

```miniscript
map = TileMap.create(
    [viewportWidth, viewportHeight],  -- pixels
    [mapWidth, mapHeight],             -- tiles
    "asset://image.oem437_8",                  -- tileset asset id
    [tileWidth, tileHeight]            -- pixels per tile
)
```

Example:

```miniscript
map = TileMap.create(
    [320, 240],
    [256, 256],
    "asset://image.oem437_8",
    [8, 8]
)
```

Behavior:
- Creates a `TileMapMorph`
- Allocates a 2D array of `TileInfo`
- Loads the glyph set defined by the tileset
- Registers the TileMap with the world
- Returns a **TileMap handle**

---

### TileMap Instance Methods

#### `map.getTile(x, y)`

Retrieve a handle to the tile at `(x, y)`.

```miniscript
tile = map.getTile(10, 5)
```

- Returns `null` if `(x, y)` is out of bounds
- Returned object is a **Tile handle**, not a Morph

---

## Tile (TileInfo)

A **Tile** is a data-backed object representing one cell in a tile map.

Tiles are not Morphs and cannot be moved or destroyed independently.
They are accessed and modified through the TileMap.

Conceptually:

```miniscript
{
  "__isa": "Tile",
  get: <function>,
  set: <function>
}
```

---

### Tile Instance Methods

#### `tile.get(key)`

Retrieve a tile property.

```miniscript
index = tile.get("TileIndex")
```

#### `tile.set(key, value)`

Set a tile property.

```miniscript
tile.set("TileIndex", 176)
```

All changes immediately affect rendering.

---

### Tile Properties

All tile properties map directly to fields on `TileInfo`.

#### `TileIndex`

Glyph index used for rendering.

```miniscript
tile.set("TileIndex", 176)
```

---

#### `ForegroundColor`

Glyph foreground color (`RadialColor`).

```miniscript
tile.set("ForegroundColor", RadialColor.create(5, 5, 5))
```

Default:
- White

---

#### `BackgroundColor`

Glyph background color (`RadialColor`).

```miniscript
tile.set("BackgroundColor", RadialColor.create(0, 0, 2))
```

Default:
- Black

---

#### `BlocksMovement`

Whether the tile blocks movement.

```miniscript
tile.set("BlocksMovement", true)
```

---

#### `BlocksVision`

Whether the tile blocks line of sight.

```miniscript
tile.set("BlocksVision", true)
```

---

#### `Tag`

Free-form string tag for gameplay logic.

```miniscript
tile.set("Tag", "wall")
```

---

## Random Tile Generation

MiniScript provides `rnd()` for random numbers in `[0,1)`.

To generate a random integer in a half-open range `[min, max)`:

```miniscript
randInt = function(min, max)
    return floor(min + rnd() * (max - min))
end
```

---

## Complete Example: Creating and Filling a Tile Map

This example replaces the earlier C# nested loop with pure MiniScript.

```miniscript
// --- configuration ---
viewportSize = [320, 240]
mapSize = [256, 256]
tileSize = [16, 24]
tileSet = "asset://image.screen_font"

// --- create the tile map ---
map = TileMap.create(
    viewportSize,
    mapSize,
    tileSet,
    tileSize
)

// --- helper: random integer in [min, max) ---
randInt = function(min, max)
    return floor(min + rnd * (max - min))
end function

// --- populate the map ---
for y in range(0, mapSize[1] - 1)
    for x in range(0, mapSize[0] - 1)
        tile = map.getTile(x, y)
		tile.set("TileIndex", randInt(65, 70))
		tile.set("BlocksMovement", false)
		tile.set("BlocksVision", false)
		tile.set("Tag", "floor")
    end for
end for
```

This script:
- Allocates a large tile map
- Iterates over all tiles
- Assigns random glyphs
- Sets gameplay flags
- Does **not** create any per-tile Morphs

---

## Rendering Model (Host-Side)

Rendering is handled entirely by `TileMapMorph` in C#:

- A single Morph renders all visible tiles
- Tiles are culled to the viewport
- Rendering cost depends on **viewport size**, not map size
- Scripts never draw directly

This design allows maps with tens or hundreds of thousands of tiles.

---

## Error Handling

All Roguey intrinsics:
- Catch host exceptions
- Report errors via the MiniScript error stream
- Never crash or hang the REPL

Example:

```text
TileMap.create error: expected tileSize [w,h]
```

---

## Design Principles

- **Tiles are data**, not UI objects
- **TileMap is the renderer**
- **MiniScript mutates data**, not Morph trees
- **World owns lifetime**, scripts hold references
- **Instance-style methods** via bound intrinsics (`ctx.self`)
- No prototype chains or inheritance tricks in MiniScript

---

## Summary

The updated Roguey API provides:

- Efficient, scalable tile maps
- Data-backed tile access from MiniScript
- Fast bulk rendering
- Clear separation of logic and rendering
- A solid foundation for:
  - procedural generation
  - pathfinding
  - FOV
  - AI and gameplay systems

This model replaces the earlier per-tile Morph approach and is the recommended way to build roguelike maps in Roguey.
