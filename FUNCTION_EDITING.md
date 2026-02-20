# Function Source Inspection & Editing

## Overview

This document describes the goal, design constraints, and implementation plan for enabling **source‑level inspection and editing of MiniScript functions** through IronKernel’s in‑world Inspector system.

The long‑term objective is to support a workflow similar to classic live systems (e.g. Smalltalk):

> Inspect → Navigate to Code → Edit → Rebind → Continue Running

This capability is foundational for interactive debugging, live scripting, and in‑world tooling, and aligns with IronKernel’s broader goals around improved debugging hooks and safer introspection [1].

---

## Problem Statement

MiniScript runtime values (`ValFunction`) currently contain executable code but **do not retain any information about the source code that created them**.

As a result:
- Inspecting a function only reveals a runtime object
- There is no way to navigate from a function value to its source
- Editing function behavior requires external file editing and re‑execution
- REPL‑defined functions have no durable source representation

This makes the Inspector less useful for behavior‑level exploration and live editing.

---

## Key Constraints

This project operates under several **non‑negotiable constraints**:

1. **MiniScript types must not be modified**
   - `ValFunction`, `Function`, and other MiniScript runtime types are treated as immutable external dependencies.

2. **Function call semantics must remain unchanged**
   - Functions must continue to be callable as normal MiniScript values.
   - No wrapper objects or proxy call logic may be introduced.

3. **Tooling metadata must not leak into script semantics**
   - Source information is for inspection and editing only.
   - Scripts should not see or manipulate source metadata.

Given these constraints, source information must live **outside** MiniScript values.

---

## Core Idea: Host‑Side Attached Metadata

Instead of embedding source information inside MiniScript values, we attach metadata **externally**, in the host runtime.

### Function Metadata Registry

A host‑owned registry maps runtime `Function` instances to source metadata:

- The mapping is identity‑based (reference equality)
- Metadata lifetime follows function lifetime
- Metadata is invisible to MiniScript code

Conceptually:

```
Function → FunctionMetadata
```

Where `FunctionMetadata` contains:
- Source document reference
- Start and end offsets (or line/column spans)
- Optional display name (for inspector UI)

A weak‑key structure (e.g. `ConditionalWeakTable`) ensures no memory leaks.

This approach mirrors how many real systems solve the problem:
- CLR PDB files
- JavaScript source maps
- Debug metadata tables in VMs

---

## Source Documents

To unify files, REPL input, and inspector‑authored code, all source is modeled as a **SourceDocument**.

A SourceDocument represents:
- A file
- A REPL session buffer
- An inspector‑created code fragment

Each document has:
- A stable identifier (e.g. `file://…`, `repl://…`, `inspector://…`)
- Full source text
- Editable or read‑only status

This makes the REPL a first‑class source provider rather than a special case.

---

## Inspector Behavior

### Inspecting a Function

When the Inspector encounters a `ValFunction`:

1. Resolve its underlying `Function`
2. Look up attached metadata in the registry
3. If metadata exists:
   - Open the corresponding SourceDocument
   - Navigate to the recorded span
4. If metadata does not exist:
   - Display a clear message:
     > “Function source not available (compiled without source metadata).”

The Inspector **never attempts to inspect function internals**.  
Navigation is always source‑oriented, not structural.

---

## Editing & Rebinding Model

Editing a function follows a **replace‑not‑mutate** rule:

1. User edits source text in a document editor
2. Source is recompiled
3. A new `Function` and `ValFunction` are created
4. The owning slot (e.g. in a `ValMap`) is replaced
5. New metadata is attached to the new function

The old function remains valid for any currently executing code.

This mirrors proven live‑coding models and avoids corrupting running interpreter state.

---

## Relationship to Existing Tooling

This project builds directly on existing infrastructure:

- **Inspector system**  
  Used as the entry point for navigation and editing [1].

- **TextEditorMorph / TextDocument**  
  Used to display and edit source documents inside the world [2].

- **Script execution model**  
  Already supports safe stopping, resetting, and re‑running of scripts.

No changes are required to MiniScript itself.

---

## Implementation Plan

### Phase 1 — Infrastructure
- Introduce `SourceDocument`
- Introduce `FunctionMetadataRegistry`
- Attach metadata during script compilation (file + REPL)

### Phase 2 — Inspector Integration
- Detect `ValFunction` during inspection
- Navigate to source instead of structural inspection
- Handle “source unavailable” cases gracefully

### Phase 3 — Editing & Rebinding
- Open source in `TextEditorMorph`
- Recompile on save
- Replace function values safely
- Reattach metadata

### Phase 4 — Tooling Refinement
- Better breadcrumbs (function → document)
- Display function signatures
- Optional read‑only mode for file‑backed code
- Error feedback inline in editor

---

## Non‑Goals (for Now)

- Reconstructing source from TAC or bytecode
- Mutating `Function` objects in place
- Allowing scripts to introspect source metadata
- Live patching of currently executing call frames

---

## Summary

This project enables **Smalltalk‑style function navigation and editing** within IronKernel while respecting strict constraints around MiniScript immutability and call semantics.

By using host‑side attached metadata and source documents, we gain:
- Powerful live tooling
- REPL‑safe editing
- No language changes
- A clear path toward richer debugging and inspection tools [1]

This design is intentionally incremental and future‑proof, supporting both immediate gains and long‑term tooling ambitions.
