# Getting Started

This document explains how to orient yourself with the Iron Kernel project, what you need to know first, and how to begin experimenting with the codebase.

Iron Kernel is a modular kernel and UI system focused on **live, scriptable objects**, a **morphic UI framework**, and a **safe embedded scripting environment** [3]. It is designed for exploration and evolution, not immediate production use.

---

## Who This Is For

Iron Kernel is currently best suited for:
- systems developers
- tool builders
- researchers interested in live systems and dynamic environments

You should be comfortable reading unfinished code and reasoning about architecture.

---

## Project Orientation

Before running anything, it’s important to understand what Iron Kernel is trying to achieve.

At a high level:
- The system is built around **live objects** that can be inspected and modified at runtime [3].
- The UI layer is morphic, meaning tools and interfaces are themselves objects within the system [3].
- Scripting is embedded and constrained to prioritize safety and runtime introspection [3].

The roadmap prioritizes **extensibility, live tooling, and runtime safety**, not fixed releases or short-term feature completeness [2].

---

## What to Read First

Before diving into the code, you should read:

- **Architecture**  
  A high-level overview of the system’s major subsystems and how they relate.

- **Design**  
  The constraints, invariants, and guiding principles that shape implementation decisions.

My opinion: understanding the design constraints early will save you a lot of confusion later.

---

## Running the Code

Exact build and run instructions are intentionally evolving and may change frequently.

General expectations:
- Expect partial subsystems.
- Expect rough edges.
- Expect to restart often.

If the repository includes scripts or build instructions, follow those first. If not, treat the codebase as an exploratory artifact rather than a turnkey system.

If you get stuck, reading the code is often more reliable than assuming missing documentation.

---

## Updates and Progress

Development notes, design changes, and external material (blog posts, videos) are tracked in the **Updates** section.

Updates are maintained as an append-only journal rather than a traditional changelog, reflecting how the system evolves over time.

---

## Contributing (Early Stage)

Contribution processes are still forming.

Before proposing changes:
1. Read the **Design** document carefully.
2. Understand the architectural boundaries.
3. Prefer small, exploratory changes over broad refactors.

Iron Kernel values clarity and structural integrity over speed.

---

## Expectations

Iron Kernel is an ongoing research and development effort.

You should expect:
- breaking changes
- shifting abstractions
- incomplete documentation

These are not bugs in the process; they are part of the exploration.

---

*If you’re here to understand how live, scriptable systems can be structured from the kernel up, you’re in the right place.*