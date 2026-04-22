# TODO

## Bugs

- [ ] String slicing instabilities across `TextEditorMorph`, `TextEditingCore`, and `TextConsoleMorph` — can silently corrupt user data
- [ ] Cursor falls out of view when typing on the last row of the text editor
- [ ] Console doesn't scroll when typing on the bottom row causes a wrap
- [ ] Mouse wheel should engage the vertical scrollbar

## Text Editor

- [ ] Source lines should not wrap — wrapping looks wrong in a code editor
- [ ] Page Up / Page Down
- [ ] Ctrl+A then Up arrow should move cursor to selection start
- [ ] Ctrl+A then Down arrow should move cursor to selection end
- [ ] Shift+Tab should reverse-indent
- [ ] Ctrl+X on a row should cut the entire row into the clipboard
- [ ] Ctrl+S to save (without the alert dialog)

## REPL / Console

- [ ] `dir` intrinsic to list directory contents
- [ ] `mkdir` intrinsic to create directories
- [ ] `del` intrinsic to delete files or directories
- [ ] `input` intrinsic to gather input in the REPL
- [ ] Per-interpreter current-directory tracking (ties into Interpreter Process concept)

## Inspector

- [ ] Inline editors for `Point` and `Size` types

## Kernel / Architecture

- [ ] Remove `WorldMorph`'s own `Interpreter` once the Process Manager is in place (see also: circular reference in `WorldScriptContext.cs:30`)
- [ ] An exception in a single morph should log an error and pause that morph, not crash all of userland
- [ ] `FileSystemIntrinsics.cs` handles `..` path traversal in userland — clarify whether this duplicates or bypasses the kernel's own enforcement (per `FILE_SYSTEM.md`)
- [ ] `PendingRunSource` pattern in `WorldScriptContext` is fragile shared mutable state between frames — replace with a command or bus message
- [ ] `MessageBus.cs` uses `Console.Error.WriteLine` (lines 53, 268, 315) — should use `ILogger`
- [ ] Flood detection threshold (100,000 msg/s) is hardcoded in `MessageBus.cs` — make configurable

## Morphic

- [ ] Null-submorph `Debug.Assert` calls are commented out in `Morph.cs` (lines 185, 190, 365) — re-enable in Debug builds to determine if the tree is ever actually corrupted

## Code Cleanup

- [ ] Remove debug `Console.WriteLine` in `TextEditorMorph.cs:556` (fires in rendering path for every wrapped line)
- [ ] Remove debug `Console.WriteLine` in `MiniScriptReplMorph.cs:175`
- [ ] Move shader uniform-not-found warnings in `ShaderProgram.cs` from `Console.WriteLine` to `ILogger`
- [ ] Delete or implement the unused method flagged in `TextEditingCore.cs:99`
- [ ] Clean up the large commented-out block in `MorphIntrinsics.cs` (lines 33–84)
- [ ] Resolve `WorldCommandManager.cs:53` TODO about transaction stacking behavior

## Documentation

- [ ] Expand or resolve `CONSIDERATIONS.md` — the color abstraction idea is worth either a proper design doc or a decision to close it
- [ ] `getting-started.md` intentionally omits build instructions; consider linking to `CLAUDE.md` or `build.sh` now that those exist
