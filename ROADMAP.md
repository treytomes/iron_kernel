# ROADMAP

This document outlines the planned and aspirational directions for **IronKernel**, **Morphic**, and **MiniScript**. The roadmap emphasizes extensibility, live tooling, and runtime safety rather than fixed releases.

---

## Near‑Term Goals (Stabilization & Core Tooling)

### Morphic Core Stabilization
- Harden the Morph lifecycle (creation, destruction, ownership)
- Finalize input routing rules (pointer capture, keyboard focus, modal states)
- Reduce API churn for core morphs (ButtonMorph, LabelMorph, sliders, scrollbars)
- Improve consistency in layout morph behavior (stack, dock, scroll)

### Inspector Maturity
- Expand type‑specific editors (colors, ranges, vectors, enums)
- Better handling of nullable and optional values
- Clear visual distinction between:
  - computed values
  - overridden values
  - inherited/default values
- Improved inspector performance for large object graphs

### Semantic Styling
- Finalize `SemanticColors` vocabulary
- Ensure all core widgets resolve colors via styles
- Support theme switching at runtime
- Remove remaining hard‑coded visual constants

---

## Mid‑Term Goals (Expressiveness & Scripting)

### MiniScript Enhancements
- More complete standard library
- Better error reporting and stack traces
- Improved debugging hooks (step, inspect, trace)
- Safer and clearer handle introspection

### Script‑Driven UI
- First‑class support for creating Morphs from MiniScript
- Declarative UI construction helpers
- Script‑driven layout and animation
- Script callbacks for UI events (click, drag, focus)

### Live Editing Workflows
- Hot‑reload scripts without losing state
- Live script reattachment to existing objects
- Editable scripts within Morphic UI panels
- Script‑driven inspectors and tools

---

## Long‑Term Goals (Tools & Worlds)

### Tooling Ecosystem
- Domain‑specific inspectors (graphics, layout, data, behavior)
- Reusable editor panels built from Morphs
- In‑world debugging and visualization tools
- Scriptable tool palettes

### Persistence & Serialization
- Serialize Morph trees
- Persist world state safely
- Script‑controlled save/load
- Versioned data migration support

### World Composition
- Multiple worlds per kernel
- World switching and layering
- Sandboxed worlds for scripts
- Inter‑world communication via controlled APIs

---

## Experimental / Research Directions

### Advanced Layout
- Constraint‑based layout experiments
- Adaptive layouts based on content and context
- Visual layout debugging tools

### Animation & Transitions
- Declarative animation helpers
- Time‑based state transitions
- Semantic animations (hover, focus, activation)

### Interaction Models
- Alternative input models (gesture, pen, touch)
- Accessibility exploration
- Non‑traditional UI metaphors

---

## Non‑Goals (Explicitly Out of Scope)

- Traditional static GUI frameworks
- HTML/CSS‑style layout systems
- Exposing raw engine internals to scripts
- Compile‑time UI generation

---

## Guiding Principles (Ongoing)

- Runtime over compile‑time
- Explicit ownership and authority
- Safe scripting over raw power
- Tools are first‑class citizens
- UI is data, not code
