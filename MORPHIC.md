# MORPHIC.md  
**The Morphic User Interface System**

## Overview

Morphic is the **primary user interface system** for IronKernel userland. It is a live, object‑centric UI inspired by classic Smalltalk Morphic systems, designed to be **inhabited rather than used**.

Morphic is not a widget toolkit.  
It is an environment.

All user interaction, tooling, inspection, and development occurs *inside* Morphic while the system is running. Windows, inspectors, buttons, and tools are themselves morphs—first‑class objects that can be selected, inspected, modified, and recomposed at runtime [1].

---

## Core Principles

### 1. Everything is a Morph

A **Morph** is a live object with:
- position and size (local to its owner)
- visual representation
- input behavior
- submorphs (children)

There is no separation between “model” and “view” at the UI level.  
If something exists visually, it exists as a Morph.

Examples:
- labels
- buttons
- windows
- inspectors
- halos
- handles
- the mouse cursor (Hand)

---

### 2. Live Object System

Morphic is designed for **liveness**:
- morphs can be inspected while running
- properties can be changed at runtime
- structure can be modified dynamically
- tools are built inside the same system they manipulate

There is no edit/compile/run cycle at the UI level.  
The UI *is* the running system.

---

### 3. Hierarchical Composition

Morphs form a **tree**:
- each morph has an owner (except the world)
- positions are **local to the owner**
- drawing and hit‑testing traverse the hierarchy

This allows:
- nested coordinate systems
- reusable components
- windows containing content
- inspectors containing property rows

---

### 4. The WorldMorph

`WorldMorph` is the **root morph**:
- defines world coordinate space
- owns global state (selection, halo, command history)
- routes input events
- executes deferred commands
- performs cleanup of deleted morphs

The world does **not** apply transforms.  
It is the root of all rendering and hit‑testing.

---

### 5. Rendering Model

Rendering is:
- software‑based
- framebuffer‑driven
- hierarchical

Each morph:
- pushes its local offset
- optionally pushes a clipping rectangle
- draws itself
- draws its submorphs
- restores rendering state

Rendering and hit‑testing share the same coordinate logic to ensure consistency.

---

## Input and Events

### Pointer Events

Morphic defines pointer events (down, move, up) that:
- carry a target morph
- can be marked as handled
- bubble up the owner chain if unhandled [2]

Event routing respects:
- pointer capture (for drags, resizing, buttons)
- z‑order (topmost morph wins)
- morph hierarchy

---

### Hover State

Each morph tracks:
- direct hover
- effective hover (itself or any descendant)

This enables:
- hover feedback on containers
- correct button and window behavior
- halo interaction without blocking content

---

## Selection and Halos

### Selection

Selection is:
- explicit
- global
- owned by the WorldMorph

Only one morph is selected at a time.

---

### HaloMorph

The **halo** is the structural manipulation interface:
- move
- resize
- delete
- inspect (future)

Halos are:
- dynamically attached to the selected morph
- implemented as morphs themselves
- non‑selectable but interactive

The halo is the *authoritative* mechanism for structural operations.

---

## Commands and Undo

Morphic uses a **command system**:
- user actions emit commands
- commands execute through the world
- undo/redo is global
- drag gestures are grouped into transactions

UI elements (buttons, handles) **do not mutate state directly**.  
They express intent by submitting commands.

This keeps:
- interaction reversible
- state changes explicit
- behavior consistent across tools

---

## Styling and Semantics

### MorphicStyle

`MorphicStyle` defines the visual language of the system:
- handle appearance
- halo colors
- label defaults
- button behavior
- semantic color roles

Styles are:
- centralized
- swappable
- inherited by morphs via the world

---

### Semantic Colors

Colors are defined by **meaning**, not appearance:
- Primary
- Success
- Danger
- Warning
- Info
- Background / Surface / Border
- Text / MutedText

This mirrors modern UI systems (e.g. Bootstrap), adapted to a constrained palette.

Morphs ask for *semantic intent*, not raw colors.

---

## Windows and Tools

### WindowMorph

A window is:
- a compositional container
- header + content
- styled semantically
- movable/resizable via the halo

Window chrome (close buttons, headers) is a **convenience**, not authority.

---

### Inspectors

Inspectors are specialized windows that:
- reflect live objects
- allow navigation of object graphs
- will eventually support editing

Inspectors are built *inside* Morphic, using the same primitives they inspect.

---

## The Hand

The Hand is the mouse cursor:
- drawn on top of everything
- tracks pointer position
- captures the pointer during interactions
- **does not participate in hit‑testing**

The Hand is visual, not semantic.

---

## What Morphic Is Not

Morphic is **not**:
- a retained‑mode widget toolkit
- an immediate‑mode UI
- a static layout system
- a thin abstraction over OS widgets

It is a **living object environment** [1].

---

## Long‑Term Vision

Morphic is the foundation for:
- a self‑hosting operating environment
- live development tools
- inspectors and debuggers
- games and applications built from inside the system

Eventually, the system should feel less like “an app” and more like **a computer you inhabit** [1].

---

*This document is a design contract.  
If future changes violate these principles, that should be a conscious decision.*

---