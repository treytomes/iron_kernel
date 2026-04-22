# Design

Iron Kernel is guided by a small set of explicit design principles. These principles are treated as **invariants**: they constrain implementation choices and shape how the system is allowed to evolve.

This document explains those constraints and why they exist.

---

## Core Philosophy

Iron Kernel prioritizes **live systems** over static artifacts.

At every layer where it is feasible, the system is designed to support:
- inspection while running
- modification without restart
- tools that are part of the system, not external to it

This philosophy directly informs the kernel’s object model, scripting environment, and UI architecture.

---

## Live Objects

The system is built around **live objects**, not frozen state.

Objects:
- can be inspected while running
- can have properties changed at runtime
- can have their structure modified dynamically
- are manipulated using tools that exist inside the same system [1]

There is no strict edit → compile → run cycle at the UI level.  
The UI *is* the running system [1].

My opinion: this constraint is the most important one in the project. Everything else flows from it.

---

## Composition Over Inheritance

Iron Kernel favors **composition** rather than deep inheritance hierarchies.

This choice:
- reduces coupling
- improves runtime flexibility
- makes live reconfiguration tractable

Inheritance tends to harden structure over time. Composition allows structure to remain fluid, which is a requirement for live systems [2].

---

## Runtime Safety

Iron Kernel prioritizes **runtime safety over raw access** [2].

This does not mean the system is restrictive by default. It means:
- unsafe operations are explicit
- scripts are constrained collaborators, not omnipotent controllers
- inspection and modification are mediated, not arbitrary

Safety is treated as an enabler of liveness, not an obstacle.

---

## Scripts as Collaborators

Scripts in Iron Kernel are designed to **participate in the system**, not dominate it.

Key characteristics:
- scripts can be hot‑reloaded without losing state
- scripts can be reattached to existing live objects
- scripts can be edited from within the UI itself [3]

Scripts drive inspectors, tools, and behaviors, but they operate within defined boundaries [2][3].

My opinion: this is where Iron Kernel meaningfully diverges from many traditional embedded scripting models.

---

## Semantic Styling

Visual structure is driven by **semantics**, not hardcoded visuals [2].

This allows:
- UI components to evolve without rewriting logic
- tools to restyle themselves dynamically
- inspectors to adapt to the meaning of the objects they present

This is especially important in a morphic, live UI system.

---

## Tooling Inside the System

All major tools are intended to be:
- built from the same primitives as application objects
- inspectable and modifiable at runtime
- subject to the same constraints as everything else [1]

There is no privileged “external” tooling layer.

This is intentional.

---

## Non‑Goals

Iron Kernel explicitly does **not** prioritize:
- static predictability over liveness
- maximum performance at the expense of introspection
- sealed abstractions that cannot be examined or altered

Those tradeoffs are accepted in service of the project’s core goals.

---

## Design as Constraint

These principles are not aspirational guidelines.  
They are **constraints**.

When a design decision conflicts with these principles, the decision is wrong—even if it is convenient.

---

*Iron Kernel is designed to remain live, inspectable, and structurally honest, even as it grows.*