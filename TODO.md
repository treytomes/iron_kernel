# TODO

## Text Editor (keyboard shortcuts)

- [x] Ctrl+S to save (without the alert dialog)
- [x] Ctrl+X on a row should cut the entire row into the clipboard
- [x] Page Up / Page Down
- [x] Shift+Tab should reverse-indent
- [x] Ctrl+A then Up arrow should move cursor to selection start
- [x] Ctrl+A then Down arrow should move cursor to selection end

## Bugs

- [x] String slicing instabilities across `TextEditorMorph`, `TextEditingCore`, and `TextConsoleMorph` — can silently corrupt user data
- [x] Cursor falls out of view when typing on the last row of the text editor
- [x] Console doesn't scroll when typing on the bottom row causes a wrap
- [x] Mouse wheel should engage the vertical scrollbar

## Text Editor

- [x] Syntax highlighter colors closing string quotes as plain text — `GetLineState` in `MiniScriptHighlighter.cs` loops `i <= targetColumn`, so the closing `"` toggles `inString` back to false before the caller checks it; loop bound should be `i < targetColumn`
- [x] Source lines should not wrap — `WordWrap` property on `TextEditorMorph` and `TextEditorWindowMorph`, default `false`
- [ ] Research consolidating text editing features — `TextEditorMorph`, `TextConsoleMorph`, and `TextEditMorph` each implement overlapping editing behaviors (cursor movement, selection, clipboard); investigate whether a shared editing layer would reduce duplication and bring feature parity across all three

## REPL / Console

- [x] `dir` intrinsic to list directory contents
- [x] `mkdir` intrinsic to create directories
- [x] `del` intrinsic to delete files or directories
- [x] `input` intrinsic to gather input in the REPL
- [x] Per-interpreter current-directory tracking — `env.curdir` is stored as a global on each `Interpreter` instance via `EnsureEnv`; each REPL window already has independent state

## Inspector

- [x] Sort properties alphabetically — `BuildStandardPropertyList` in `InspectorMorph.cs` uses `GetProperties()` which returns properties in declaration order
- [x] Inline editors for `Point` and `Size` types

## Dialog Service

- [ ] File picker dialog — `IWindowService.PromptAsync` currently shows a plain text input; open/save operations in the text editor need a proper file browser dialog that shows `dir()` output and lets the user navigate and select

## Kernel / Architecture

- [ ] Remove `WorldMorph`'s own `Interpreter` once the Process Manager is in place (see also: circular reference in `WorldScriptContext.cs:30`)
- [ ] An exception in a single morph should log an error and pause that morph, not crash all of userland
- [ ] `FileSystemIntrinsics.cs` handles `..` path traversal in userland — clarify whether this duplicates or bypasses the kernel's own enforcement (per `FILE_SYSTEM.md`)
- [ ] `PendingRunSource` pattern in `WorldScriptContext` is fragile shared mutable state between frames — replace with a command or bus message
- [x] `MessageBus.cs` uses `Console.Error.WriteLine` (lines 53, 268, 315) — should use `ILogger`
- [ ] Flood detection threshold (100,000 msg/s) is hardcoded in `MessageBus.cs` — make configurable

## Windows

- [ ] Drag `WindowMorph` by its title bar — pointer down on the header begins a move gesture; drag updates `Position` relative to owner; pointer up commits

## Morphic

- [x] Null-submorph `Debug.Assert` calls are commented out in `Morph.cs` (lines 185, 190, 365) — re-enable in Debug builds to determine if the tree is ever actually corrupted

## Code Cleanup

- [x] Remove debug `Console.WriteLine` in `TextEditorMorph.cs:556` (fires in rendering path for every wrapped line)
- [x] Remove debug `Console.WriteLine` in `MiniScriptReplMorph.cs:175`
- [x] Move shader uniform-not-found warnings in `ShaderProgram.cs` from `Console.WriteLine` to `ILogger`
- [x] Delete or implement the unused method flagged in `TextEditingCore.cs:99`
- [x] Clean up the large commented-out block in `MorphIntrinsics.cs` (lines 33–84)
- [x] Resolve `WorldCommandManager.cs:53` TODO about transaction stacking behavior

## Rendering / Color

- [ ] Abstract color representation out of userland — currently `RadialColor` is a fixed 3-channel / 6-level value tied to the framebuffer palette; userland colors could be 3 floating-point channels clamped at render time, so changing the framebuffer color depth wouldn't require a userland rewrite

## Documentation

- [x] Write `GETTING-STARTED.md` at the repo root with build and run instructions
- [x] Remove `CONSIDERATIONS.md` — content moved to TODO
