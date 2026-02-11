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

## Dialog & UI Intrinsics

These intrinsics provide **simple user interaction dialogs**, similar to JavaScript’s
`alert`, `prompt`, and `confirm`.

All dialogs:
- Are modal
- Pause script execution until the user responds
- Never block the engine or UI thread
- Return control cleanly to the script

---

### `alert(message)`

Display a modal alert dialog with a message and an **OK** button.

#### Parameters
- `message` (string): text to display

#### Returns
- `null`

#### Example

```miniscript
alert("Hello, world!")
print("This runs after the user clicks OK")
```

---

### `prompt(message, defaultValue)`

Display a modal prompt dialog requesting text input.

#### Parameters
- `message` (string): prompt text
- `defaultValue` (string, optional): initial value

#### Returns
- string: user input
- `null`: if the user cancels

#### Example

```miniscript
name = prompt("Enter your name:", "Player")
if name != null then
    print("Hello " + name)
else
    print("Prompt cancelled")
end
```

---

### `confirm(message)`

Display a modal confirmation dialog with **OK** and **Cancel** buttons.

#### Parameters
- `message` (string): confirmation text

#### Returns
- `true` if OK was clicked
- `false` if Cancel was clicked

#### Example

```miniscript
if confirm("Delete all files?") then
    print("Confirmed")
else
    print("Cancelled")
end
```

---

## Editor & Script Execution Intrinsics

These intrinsics integrate MiniScript with the built‑in text editor and script execution system.

They are **REPL-safe** and designed to work correctly from interactive sessions.

---

### `edit(filename)`

Open the built‑in text editor window and load a file for editing.

If the file exists, its contents are loaded.  
If it does not exist, an error is reported.

#### Parameters
- `filename` (string): file URL (e.g. `file://script.ms`)

#### Returns
- `null`

#### Example

```miniscript
edit("file://example.ms")
```

You can also call `edit` without immediately running the file, allowing inspection or modification.

---

### `run(filename)`

Load and execute a MiniScript file.

Behavior:
- Loads the file via the file system service
- Compiles and runs it in the current World interpreter
- Reports compile or runtime errors to the REPL
- Always returns control to the REPL

#### Parameters
- `filename` (string): file URL

#### Returns
- `null`

#### Example

```miniscript
run("file://example.ms")
```

#### Example with error handling

```miniscript
run("file://broken.ms")
print("This still runs even if there was a compile error")
```

Errors are printed to the REPL error stream but **never hang the interpreter**.

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

- The morph is removed from the world
- The handle becomes **dead**
- Further calls are safe but do nothing

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

---

## RadialColor API

### Creating a RadialColor

```miniscript
c = RadialColor.create(r, g, b)
```

- Each component is in the range `0–5`
- Invalid values produce a MiniScript error (not a crash)

Example:

```miniscript
c = RadialColor.create(5, 3, 1)
```

---

## Lifetime & Safety Rules

### Dead Handles

- Dead handles never crash
- `isAlive()` returns false
- Operations safely no-op

---

## Error Handling Guarantees

All intrinsics:

- Catch host exceptions
- Report errors via the MiniScript error stream
- Never hang the interpreter
- Never corrupt REPL state

This applies to:
- `alert`
- `prompt`
- `confirm`
- `edit`
- `run`
- All Morph and RadialColor APIs

---

## Summary

This API provides:

- Instance-style scripting
- Modal UI integration
- Script-controlled editing and execution
- Deterministic REPL behavior
- Strong safety guarantees

It is designed to support **interactive development**, **tooling**, and **game logic**
without hidden state or interpreter instability.

---
