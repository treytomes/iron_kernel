# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

IronKernel is a **living retro computing platform** written in C# (.NET 9.0). It's a self-contained system with a microkernel, a Morphic UI environment, and an embedded MiniScript scripting layer — designed to eventually be developed *from inside itself*.

The display is a fixed **960x480 framebuffer** with a 256-color palette (6 discrete intensity levels per RGB channel). This constraint is intentional and shapes all rendering decisions.

---

## Commands

```bash
./build.sh       # dotnet build IronKernel.sln -c Release
./publish.sh     # publishes kernel + userland to /publish (Debug config)
./run.sh         # dotnet publish/IronKernel.dll
```

There is no test suite. All verification is exploratory through the running application.

---

## Architecture

### Two-Layer Split

```
IronKernel (kernel process)
  └── supervised modules communicate via typed message bus
        |
        | bus bridge (typed syscalls + forwarded events)
        |
Userland (loaded DLL)
  └── Morphic UI system + MiniScript runtime
```

Userland never touches OpenGL, OpenTK, or the host OS directly. All host interaction goes through the kernel bus.

### Key Projects

- **IronKernel/** — kernel executable; hosts modules, runs the OpenTK window
- **Userland/** — user application DLL; loaded at startup via `--userland <path>`
- **IronKernel.Common/** — shared contracts, interfaces, and message types

### Kernel Modules

Each module implements `IKernelModule` and communicates only through `IKernelMessageBus`. Key modules:

- `ApplicationHostModule` — bridges kernel ↔ userland; forwards tick/input/resize/shutdown events; handles framebuffer writes and file I/O syscalls
- `FramebufferModule` — owns the pixel buffer, applies the palette
- `OpenTKHostModule` — owns the window and input polling
- `FileSystemModule`, `AssetLoaderModule`, `ClipboardModule`

The bus enforces subscription tiers (Kernel / Module / Application), flood detection (100k msg/s threshold), and task supervision with hung-task detection.

### Morphic UI (Userland)

Everything visible is a **Morph** — a live object with position (local to owner), size, rendering behavior, and submorphs. Key design rules from `MORPHIC.md`:

- `WorldMorph` is the root; it owns selection state, the halo, and command history
- Positions are **local to owner** — coordinate transforms stack through the hierarchy
- Rendering and hit-testing share the same traversal logic
- UI elements express intent via **commands** submitted to the world; they do not mutate state directly
- Undo/redo is global; drag gestures are grouped into transactions
- Colors are **semantic** (`Primary`, `Danger`, `Surface`, `MutedText`, etc.) via `MorphicStyle` — never hardcode palette values

### MiniScript Integration

Scripts run in a `WorldScriptContext` and interact with engine objects only through **handles** — MiniScript maps that proxy real objects. Scripts never hold direct C# references. Property changes are **buffered** and committed at a defined boundary to keep rendering deterministic.

File URLs use the scheme `file://` and asset URLs use `asset://`.

---

## Design Contracts

- **Composition over inheritance** for morphs
- **Commands, not direct mutation** for UI interactions
- **Handles, not direct references** for MiniScript ↔ engine boundary
- **Semantic colors, not raw palette values** for all visual styling
- Userland is isolated from the host — all kernel services accessed via bus messages

Violating these is a conscious architectural decision, not a shortcut.
