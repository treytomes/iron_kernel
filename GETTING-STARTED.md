# Getting Started

IronKernel is a living retro computing platform written in C# (.NET 9.0). This document covers how to build, run, and orient yourself with the project.

---

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- A Linux desktop environment (the framebuffer output uses OpenTK/OpenGL)

---

## Build and Run

```bash
# Build the solution (Release)
./build.sh

# Publish kernel + userland (Debug — required for run.sh)
./publish.sh

# Run
./run.sh
```

`publish.sh` outputs to `publish/` (kernel) and `publish/userland/` (userland DLL).  
`run.sh` will fail with a clear message if you haven't published first.

To run tests:

```bash
dotnet test IronKernel.Tests/IronKernel.Tests.csproj
```

---

## Project Structure

```
IronKernel/         Kernel executable — modules, OpenTK window, message bus
Userland/           Userland DLL — Morphic UI, MiniScript runtime, applications
IronKernel.Common/  Shared contracts, interfaces, and message types
IronKernel.Tests/   Unit tests
ScriptConsole/      Headless script host for testing MiniScript outside the UI
www/                Project website source
```

---

## Architecture in Brief

The system is split into two layers:

**Kernel** — supervises modules that handle hardware-like concerns (window, framebuffer, input, filesystem, assets). Modules communicate only through a typed message bus; none talk to each other directly.

**Userland** — a single application DLL loaded at startup. It never touches OpenGL or the host OS directly. All host interaction goes through bus messages (syscalls and forwarded events).

Inside userland is a **Morphic UI system**: everything visible is a *Morph* — a live object with position, size, rendering behavior, and submorphs. The world is inspectable and modifiable at runtime via the Halo (Ctrl+Right-click any morph).

On top of Morphic runs a **MiniScript** scripting layer. Scripts interact with engine objects through handles (proxy maps), never direct C# references.

For deeper detail see:

- `CLAUDE.md` — architecture reference and design contracts (also used by Claude Code)
- `MORPHIC.md` — Morphic UI design rules
- `MINISCRIPT.md` — scripting layer and intrinsics
- `FILE_SYSTEM.md` — virtual filesystem layout and path conventions
- `ROADMAP.md` — planned direction

---

## Display

The framebuffer is **960×480 pixels** with a fixed 256-color palette (6 discrete intensity levels per RGB channel). This constraint is intentional — it defines the aesthetic and programming model of the system.

---

## First Things to Try

1. Launch the app with `./run.sh`
2. Click **Apps** in the toolbar to open the launcher
3. Open the **MiniScript REPL** and try `print "hello"`
4. Open the **Text Editor**, write a script, and save it to `file://hello.ms`
5. In the REPL, run `run "file://hello.ms"` to execute it
6. Ctrl+Right-click any morph to open the **Halo** and inspect it live
