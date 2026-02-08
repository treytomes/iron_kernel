# MINISCRIPT API

This document describes the **MiniScript API surface** provided by IronKernel’s `WorldScriptContext`.  
It focuses on the **script-facing API**: what MiniScript authors can call, what objects they receive,
and how those objects behave.

This is **not** a MiniScript language reference. It documents the *host API* layered on top of MiniScript.

---

## Core Concepts

### Script World

MiniScript runs inside a **World context** owned by IronKernel.  
Scripts do not directly manipulate engine objects; instead they interact through **handles**.

A *handle* is a MiniScript `map` that represents a live engine object and exposes instance-style
methods.

Example handle structure (conceptual):

```miniscript
{
  "__isa": "Morph",
  "__id": 3,
  get: <function>,
  set: <function>,
  destroy: <function>,
  ...
}
```

Handles are:
- Safe (dead objects are detected)
- Stateful (slots persist)
- Instance-oriented (methods close over `self`)

---

## Morph API

`Morph` is the primary script-visible object type.  
It represents a visual object (a `MiniScriptMorph`) in the world.

### Creating Morphs

```miniscript
m = Morph.create([width, height])
m = Morph.create([x, y], [width, height])
```

Examples:

```miniscript
m = Morph.create([32, 16])
m = Morph.create([10, 20], [64, 24])
```

---

### Morph Instance Methods

All methods below are **instance methods** on a Morph handle.

#### `m.get(key)`

Retrieve a slot value.

```miniscript
hp = m.get("hp")
```

Returns `null` if the slot does not exist.

---

#### `m.set(key, value)`

Set or replace a slot value.

```miniscript
m.set("hp", 10)
m.set("name", "player")
```

---

#### `m.has(key)`

Check whether a slot exists.

```miniscript
if m.has("hp") then
    print("Has HP")
end
```

---

#### `m.delete(key)`

Delete a slot if it exists.

```miniscript
m.delete("hp")
```

Safe no-op if the slot is missing.

---

#### `m.destroy()`

Destroy the morph.

```miniscript
m.destroy()
```

- The morph is removed from the world.
- The handle becomes **dead**.
- Further calls on the handle are safe but do nothing.

---

#### `m.isAlive()`

Check whether the morph is still alive.

```miniscript
if m.isAlive() then
    print("Still exists")
end
```

---

## Morph Queries

### `Morph.findBySlot(key)`
### `Morph.findBySlot(key, value)`

Find all morphs that have a given slot, optionally matching a value.

```miniscript
enemies = Morph.findBySlot("faction", "enemy")
```

Returns a **list of Morph handles**.  
All returned handles:
- Are fully functional
- Have instance methods attached
- Respect lifetime rules

---

## Slot System

Slots are arbitrary key/value pairs stored on morphs.

- Keys are strings
- Values can be any MiniScript value:
  - numbers
  - strings
  - lists
  - maps
  - other handles

Slots are:
- Script-owned
- Persistent
- Safe across frames

---

## RadialColor API

`RadialColor` is a script-visible value type used for color data.

### Creating a RadialColor

```miniscript
c = RadialColor.create(r, g, b)
```

- Each component is an integer in the range `0–5`
- Invalid values produce a MiniScript error (not a crash)

Example:

```miniscript
c = RadialColor.create(5, 3, 1)
```

Invalid example:

```miniscript
c = RadialColor.create(10, 0, 0)
-- Error: RadialColor channel out of range (0–5)
```

### Using RadialColor

RadialColor values are passed into intrinsics that accept colors
(e.g. morph rendering, tiles, UI).

Internally they are represented as MiniScript maps, but scripts should
treat them as opaque values.

---

## Lifetime & Safety Rules

### Dead Handles

A handle becomes **dead** if:
- The morph is destroyed via script (`m.destroy()`)
- The morph is deleted via the UI (halo)

Dead handles:
- Return `false` from `isAlive()`
- Do not throw errors
- Do not resurrect objects
- Safely no-op on most operations

---

### Ownership Model

- **World owns objects**
- **Scripts hold references**
- Scripts never own lifetime

This prevents:
- dangling references
- crashes
- inconsistent state

---

## Error Handling

All API functions:
- Catch host exceptions
- Report errors via the MiniScript error stream
- Never hang or corrupt the interpreter

Example error output:

```text
Morph.create error: expected [x,y],[w,h]
RadialColor.create error: channel out of range
```

---

## Summary

This API provides:

- Instance-style scripting (`m.set(...)`)
- Explicit, predictable behavior
- Safe object lifetimes
- Clean separation between engine and script logic

It is intentionally simple, explicit, and deterministic—designed to scale
to tiles, entities, players, and game logic without surprises.
