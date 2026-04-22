# TODO

## Bugs

- [x] String slicing instabilities across `TextEditorMorph`, `TextEditingCore`, and `TextConsoleMorph` — can silently corrupt user data
- [x] Cursor falls out of view when typing on the last row of the text editor
- [x] Console doesn't scroll when typing on the bottom row causes a wrap
- [x] Mouse wheel should engage the vertical scrollbar

## Text Editor

- [x] Syntax highlighter colors closing string quotes as plain text — `GetLineState` in `MiniScriptHighlighter.cs` loops `i <= targetColumn`, so the closing `"` toggles `inString` back to false before the caller checks it; loop bound should be `i < targetColumn`
- [ ] Source lines should not wrap — wrapping looks wrong in a code editor
- [ ] Page Up / Page Down
- [ ] Ctrl+A then Up arrow should move cursor to selection start
- [ ] Ctrl+A then Down arrow should move cursor to selection end
- [ ] Shift+Tab should reverse-indent
- [ ] Ctrl+X on a row should cut the entire row into the clipboard
- [ ] Ctrl+S to save (without the alert dialog)

## REPL / Console

- [x] `dir` intrinsic to list directory contents
- [x] `mkdir` intrinsic to create directories
- [x] `del` intrinsic to delete files or directories
- [x] `input` intrinsic to gather input in the REPL
- [x] Per-interpreter current-directory tracking — `env.curdir` is stored as a global on each `Interpreter` instance via `EnsureEnv`; each REPL window already has independent state

## Inspector

- [ ] Inline editors for `Point` and `Size` types

## Dialog Service

- [ ] File picker dialog — `IWindowService.PromptAsync` currently shows a plain text input; open/save operations in the text editor need a proper file browser dialog that shows `dir()` output and lets the user navigate and select

## Kernel / Architecture

- [ ] Remove `WorldMorph`'s own `Interpreter` once the Process Manager is in place (see also: circular reference in `WorldScriptContext.cs:30`)
- [ ] An exception in a single morph should log an error and pause that morph, not crash all of userland
- [ ] `FileSystemIntrinsics.cs` handles `..` path traversal in userland — clarify whether this duplicates or bypasses the kernel's own enforcement (per `FILE_SYSTEM.md`)
- [ ] `PendingRunSource` pattern in `WorldScriptContext` is fragile shared mutable state between frames — replace with a command or bus message
- [x] `MessageBus.cs` uses `Console.Error.WriteLine` (lines 53, 268, 315) — should use `ILogger`
- [ ] Flood detection threshold (100,000 msg/s) is hardcoded in `MessageBus.cs` — make configurable

## Morphic

- [x] Null-submorph `Debug.Assert` calls are commented out in `Morph.cs` (lines 185, 190, 365) — re-enable in Debug builds to determine if the tree is ever actually corrupted

## Code Cleanup

- [x] Remove debug `Console.WriteLine` in `TextEditorMorph.cs:556` (fires in rendering path for every wrapped line)
- [x] Remove debug `Console.WriteLine` in `MiniScriptReplMorph.cs:175`
- [x] Move shader uniform-not-found warnings in `ShaderProgram.cs` from `Console.WriteLine` to `ILogger`
- [x] Delete or implement the unused method flagged in `TextEditingCore.cs:99`
- [x] Clean up the large commented-out block in `MorphIntrinsics.cs` (lines 33–84)
- [x] Resolve `WorldCommandManager.cs:53` TODO about transaction stacking behavior

## Documentation

- [ ] Expand or resolve `CONSIDERATIONS.md` — the color abstraction idea is worth either a proper design doc or a decision to close it
- [ ] `getting-started.md` intentionally omits build instructions; consider linking to `CLAUDE.md` or `build.sh` now that those exist
