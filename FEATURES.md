# FEATURES

IronKernel is a modular kernel and UI system built around **live, scriptable objects**, a **morphic UI framework**, and a **safe embedded scripting environment** powered by MiniScript. Together, these systems enable dynamic tools, inspectors, editors, and interactive applications that can be modified at runtime.

---

## Core Capabilities

### Morphic UI Framework

IronKernel includes a **Morphic UI system** inspired by object‑oriented, compositional interfaces.

Key characteristics:
- UI is composed of **Morphs** (lightweight visual objects)
- Morphs can contain other morphs (tree structure)
- Layout is declarative via layout morphs (e.g. horizontal/vertical stacks, dock panels)
- Morphs are live objects: they can be moved, resized, shown/hidden, or replaced at runtime
- Visual appearance is driven by **styles and semantic colors**, not hardcoded values

Use cases:
- Inspectors
- Tool panels
- Editors
- Custom UI widgets
- Debug overlays

---

### Inspector System

IronKernel provides a **live Inspector** for runtime objects.

Features:
- Reflective inspection of objects and properties
- Automatic generation of editors based on value type
- Support for complex editors (sliders, color editors, text editors, etc.)
- Live reconciliation between UI and underlying object state
- Safe editing of both reference and value‑type properties

The inspector is designed to:
- Work continuously at runtime
- Allow both buffered and continuous editors
- Avoid destructive “snap‑back” during user interaction

---

### MiniScript Integration

MiniScript is embedded as a **first‑class runtime scripting environment**.

Key properties:
- Scripts run inside a **World context** owned by IronKernel [2]
- Scripts do not manipulate engine objects directly
- Instead, scripts interact with **handles** that represent live engine objects [2]

Handles:
- Are exposed to MiniScript as maps
- Represent real engine objects (e.g. Morphs)
- Provide instance‑style methods
- Detect dead or invalid objects safely
- Persist state across script calls

Conceptually, a script sees something like:

```miniscript
{
  "__isa": "Morph",
  "__id": 3,
  get: <function>,
  set: <function>,
  destroy: <function>
}
```

This design allows:
- Safe sandboxing of scripts
- Clear ownership boundaries
- Live interaction without exposing internal engine state [2]

---

### Script‑Driven UI and Behavior

By combining Morphic UI with MiniScript handles, IronKernel enables:

- Creating UI elements from scripts
- Modifying existing UI at runtime
- Responding to events via scripts
- Driving tools, inspectors, and behaviors without recompilation

Scripts can:
- Query and modify Morph properties through handles
- Create new Morphs and attach them to the world
- Control visibility, layout, and interaction logic
- Orchestrate higher‑level behaviors while the engine enforces safety [1][2]

---

### World‑Centric Architecture

IronKernel is built around the concept of a **World**:

- The World owns Morphs
- The World owns the MiniScript execution context
- The World mediates input, focus, and lifecycle
- Scripts and UI exist inside the same coherent runtime model

This allows:
- Live reconfiguration
- Introspection and debugging
- Runtime tool building
- Long‑running interactive systems

---

## What IronKernel Can Be Used For

IronKernel is particularly suited for:

- In‑engine tools and editors
- Scriptable UI systems
- Debug and inspection tooling
- Experimental UI and interaction research
- Embedded scripting environments
- Live‑coding or hot‑reloading workflows
- Domain‑specific editors (graphics, data, layout, behavior)

It is **not** focused on:
- Traditional static UI frameworks
- Compile‑time‑only tooling
- Heavyweight retained‑mode GUI systems

---

## Design Philosophy

- **Live objects over static state**
- **Composition over inheritance**
- **Runtime safety over raw access**
- **Semantic styling over hardcoded visuals**
- **Scripts as collaborators, not controllers**
