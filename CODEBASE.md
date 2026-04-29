# Codebase Index

Quick reference for locating key files. See `CLAUDE.md` for architecture; see `TODO.md` for open work.

---

## Kernel (`IronKernel/`)

| File | Purpose |
|------|---------|
| `Program.cs` | Host builder, DI wiring, module registration |
| `AppSettings.cs` | Config model bound from `appsettings.json` |
| `appsettings.json` | Runtime config (window size, flood threshold, asset paths) |
| `Kernel/Bus/MessageBus.cs` | Typed pub/sub bus; flood detection; subscription tiers |
| `Kernel/KernelService.cs` | Supervises module lifecycles |
| `Kernel/ModuleRuntime.cs` | Per-module task runner; hung-task detection |
| `Modules/ApplicationHost/ApplicationHostModule.cs` | Kernel↔userland bridge; forwards tick/input/resize; handles file I/O syscalls |
| `Modules/ApplicationHost/ApplicationBusBridge.cs` | Translates App* messages to kernel File*/Asset* messages |
| `Modules/FileSystem/FileSystemModule.cs` | Sandbox enforcement (`TryResolvePath`), file read/write/delete/list |
| `Modules/Framebuffer/FramebufferModule.cs` | Pixel buffer; palette application |
| `Modules/OpenTKHost/OpenTKHostModule.cs` | OpenTK window; input polling |
| `Modules/AssetLoader/AssetLoaderModule.cs` | Read-only `asset://` resources |
| `Kernel/Messages.cs` | All kernel message types in one file |

---

## Shared Contracts (`IronKernel.Common/`)

| File | Purpose |
|------|---------|
| `ValueObjects/RadialColor.cs` | Fixed 3-channel / 6-level color used everywhere |
| `Messages/App*.cs` | Application-tier bus messages (input, file I/O, syscalls) |
| `Interfaces/IFileSystem.cs` | Userland file system service contract |
| `Interfaces/IWindowService.cs` | Alert/prompt/confirm dialog service contract |
| `Interfaces/IClipboardService.cs` | Clipboard service contract |

---

## Userland — Application (`Userland/MiniMacro/`)

| File | Purpose |
|------|---------|
| `MiniMacroAppRoot.cs` | Entry point; subscribes to kernel bus events; owns `WorldMorph`; `_updateLock` guards all input + update |
| `MiniMacroApplication.cs` | `IUserApplication` implementation; DI root for userland |
| `LauncherMorph.cs` | App launcher toolbar |
| `TextEditorWindowMorph.cs` | Text editor window; file open/save; dirty tracking via `TextMutated` |
| `MiniScriptReplMorph.cs` | MiniScript REPL; console I/O; per-interpreter `env.curdir` |

---

## Userland — Morphic (`Userland/Morphic/`)

| File | Purpose |
|------|---------|
| `Morph.cs` | Base class; position, size, submorph tree, event dispatch (`DispatchPointerDown` bubbles up) |
| `WorldMorph.cs` | Root morph; selection, halo, keyboard/pointer focus, command manager |
| `WindowMorph.cs` | Compositional window; header + content via `DockPanelMorph` |
| `WindowTitleBarMorph.cs` | Draggable title bar; captures pointer, submits `MoveCommand` deltas |
| `TextEditorMorph.cs` | Multi-line code editor; syntax highlighting; `WordWrap`; `EnsureCaretVisible` |
| `TextDocument.cs` | Document model; `TextMutated` (content only) vs `Changed` (content + cursor) |
| `TextConsoleMorph.cs` | Scrolling console with inline `input` support |
| `TextEditMorph.cs` | Single-line text field; used in inspector and dialogs |
| `MiniScriptHighlighter.cs` | Syntax coloring; `GetLineState` loop bound `i <= targetColumn` |
| `HandMorph.cs` | Drag ghost; owned by `WorldMorph` |
| **Commands/** | `MoveCommand`, `ResizeCommand`, `DeleteCommand`, `ActionCommand`, `WorldCommandManager` (undo/redo) |
| **Events/** | `PointerDownEvent`, `PointerMoveEvent`, `PointerUpEvent`, `PointerWheelEvent`, `KeyEvent` |
| **Handles/** | `HaloMorph`, `MoveHandleMorph`, `ResizeHandleMorph`, `DeleteHandleMorph`, `InspectHandleMorph` |
| **Inspector/** | `InspectorMorph`, `InspectorFactory`, `ValueMorph`, `PropertyRowMorph`, `PointValueMorph`, `SizeValueMorph`, `RadialColorSliderValueMorph` |
| **Layout/** | `DockPanelMorph`, `HorizontalStackMorph`, `VerticalStackMorph`, `ScrollPaneMorph`, `ContainerMorph` |

---

## Userland — Scripting (`Userland/Scripting/`)

| File | Purpose |
|------|---------|
| `FileSystemIntrinsics.cs` | `dir`, `mkdir`, `del`, `copy`, `move`, `cd`, `pwd`, `import`, `run`, `edit`, `file.loadSound`; `ResolvePath` collapses `.`/`..` before sending `file://` / `sys://` URLs to kernel |
| `SoundIntrinsics.cs` | `Sound` constructor + static API (`playAsset`, `setVolume`, waveform constants), `s.init`/`s.play`/`s.stop`, `noteFreq` |
| `IntrinsicRegistry.cs` | Top-level registration; wires all intrinsic modules; owns `help`, `decompile`, `cls` |
| `MorphIntrinsics.cs` | MiniScript intrinsics for creating/manipulating morphs |
| `WorldScriptContext.cs` | Bridges `WorldMorph` ↔ MiniScript interpreter; owns `PendingRunSource` |
| `IScriptHost.cs` | Minimal interface for the scripting host; implemented by `WorldScriptContext` and `ScriptConsole` harness |

---

## Userland — Services (`Userland/Services/`)

| File | Purpose |
|------|---------|
| `FileSystemService.cs` | Implements `IFileSystem`; wraps app-bus file messages |
| `WindowService.cs` | Implements `IWindowService`; creates alert/prompt/confirm windows |
| `ClipboardService.cs` | Implements `IClipboardService`; wraps app-bus clipboard messages |

---

## ScriptConsole (`ScriptConsole/`)

Headless MiniScript host for testing scripts outside the UI.

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point; bootstraps the interpreter with an in-memory filesystem |
| `ConsoleScriptHost.cs` | `IScriptHost` implementation backed by stdin/stdout |
| `InMemoryFileSystem.cs` | `IFileSystem` implementation for testing |

---

## Tests (`IronKernel.Tests/`)

Run with `dotnet test IronKernel.Tests/IronKernel.Tests.csproj`.
