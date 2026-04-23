# Text Editing Consolidation Plan

## Background

Three morphs implement editing behavior independently:

- **`TextEditorMorph`** — full multi-line code editor; uses `TextDocument` (which wraps a `List<TextEditingCore>`)
- **`TextConsoleMorph`** — single-line REPL input with history; uses one `TextEditingCore` directly
- **`TextEditMorph`** — minimal inline field (inspector, dialogs); mutates a raw `string` directly

The lower two layers are well-separated: `TextEditingCore` is a single-line text buffer with cursor and word-boundary logic; `TextDocument` is a multi-line model that manages a list of `TextEditingCore` lines and cross-line operations. Neither has any UI or keyboard knowledge — that layering is correct and should not change.

The problem is at the Morph layer, where keyboard handling, selection, clipboard, and caret rendering are each reinvented.

---

## What Is Legitimately Different

Before listing the duplication, it is worth being precise about what should *not* be merged:

| Concern | Why it stays separate |
|---|---|
| `TextConsoleMorph` grid buffer + scroll model | The console renders a fixed cell grid and scrolls rows; the editor renders a line viewport. Fundamentally different display models. |
| `TextConsoleMorph` paste newline filtering | Console input is intentionally single-line; filtering `\n` on paste is correct behaviour, not a bug. |
| `TextConsoleMorph` Up/Down → history | History navigation is a console-specific feature unrelated to caret movement. |
| `TextEditorMorph` word-wrap visual row arithmetic | Only the editor has wrapping; this logic has no counterpart elsewhere. |
| `TextEditMorph` Enter/Escape commit/cancel | Inline fields have a commit lifecycle that editors do not. |

---

## Identified Duplication

### 1. Shift + movement pattern — inconsistent and repeated

`TextEditorMorph` uses `SelectionController.BeginIfNeeded` before movement and `Update` after:

```csharp
if (shift) _selection.BeginIfNeeded((line, column));
// ... movement ...
if (shift && moved) _selection.Update((_document.CaretLine, _document.CaretColumn));
else if (!shift) _selection.Clear();
```

`TextConsoleMorph` duplicates an alternate form — update *before and after* the move — separately in both `HandleHorizontalMove()` and `HandleHomeEnd()`:

```csharp
if (e.Modifiers.HasFlag(KeyModifier.Shift)) {
    _selection.Update(_editor.CursorIndex);
    _editor.Move(delta);
    _selection.Update(_editor.CursorIndex);
}
```

The two patterns behave slightly differently at the selection anchor. Both work but they should be the same.

### 2. Delete selection — same logic, different call sites

`TextEditorMorph` (`DeleteSelection`, lines 529–537) and `TextConsoleMorph` (lines 536–546) both retrieve the range from `SelectionController`, delete it from the backing store, and clear the selection. The only difference is which delete method they call (`_document.DeleteRangeAndSetCaret` vs `_editor.DeleteRange`).

### 3. Clipboard paste loop — copy-paste with one variation

Both editors iterate pasted text character by character and insert each one. `TextConsoleMorph` adds a newline filter that `TextEditorMorph` omits. The loop structure is otherwise identical.

### 4. Tab-width arithmetic repeated in `TextEditorMorph`

The expression `TabWidth - (col % TabWidth)` appears four times inside `TextEditorMorph` alone (drawing loop, `ComputeVisualColumn`, `VisualColumnToCaretIndex`, `GetVisualRowCount`). This should live once in a shared helper.

### 5. `TextEditMorph` uses raw `string` instead of `TextEditingCore`

Every keystroke allocates a new `string` via `string.Remove` / `string.Insert`. `TextEditingCore` already exists as a `StringBuilder`-backed single-line buffer with cursor, word movement, and deletion. `TextEditMorph` should use it.

### 6. `TextEditMorph` has no selection or clipboard support

This is a user-visible gap: you cannot select text in a text field (inspector values, dialogs). Adding selection and Ctrl+C/X/V would require `SelectionController<int>` and a `IClipboardService` — both already exist in `TextConsoleMorph` and could be adopted unchanged.

---

## Proposed Shared Layer: `LineEditingBehavior`

Rather than merging the three morphs (which have different display models and cannot share rendering), extract the *keyboard handling and selection logic* for single-line editing into a reusable helper:

```csharp
internal sealed class LineEditingBehavior
{
    // Owns the selection; delegates mutations to a provided TextEditingCore
    public LineEditingBehavior(
        TextEditingCore editor,
        IClipboardService clipboard,
        bool allowNewlines = false)

    // Called from the morph's OnKey override
    public bool HandleKey(KeyEvent e);

    // Returns selected text for rendering
    public (int start, int end)? GetSelectionRange();
    public void ClearSelection();
}
```

`TextConsoleMorph` and the upgraded `TextEditMorph` would both own a `LineEditingBehavior` instead of re-implementing selection and clipboard handling inline. `TextEditorMorph` is multi-line and does not fit this abstraction — it stays as-is.

---

## Recommended Work Items (ordered by risk and value)

### Phase 1 — Low risk, high value (self-contained fixes)

**1a. Consolidate tab-width arithmetic in `TextEditorMorph`**  
Extract `ComputeVisualColumn`, `VisualColumnToCaretIndex`, and `GetVisualRowCount` into a `static class TabCalculator` or static helpers on `TextEditingCore`. Eliminates the four copies of the modulo expression. No behaviour change.

**1b. Switch `TextEditMorph` from raw `string` to `TextEditingCore`**  
Drop `_text`, `_originalText`, and `_caretIndex`; introduce `_editor = new TextEditingCore()`. Wire `OnKey` to `_editor.Insert`, `_editor.Backspace`, `_editor.Delete`, `_editor.Move`, `_editor.MoveToStart`, `_editor.MoveToEnd`. `Refresh(object?)` sets `_editor` via `SetText`. This is a pure refactor — behaviour is identical, allocation profile improves.

### Phase 2 — Medium risk, visible user benefit

**2a. Add selection and clipboard to `TextEditMorph`**  
After 1b, add `SelectionController<int>` and a `IClipboardService` dependency. Wire Shift+Left/Right/Home/End and Ctrl+A/C/X/V using the same `BeginIfNeeded`/`Update` pattern from `TextEditorMorph`. Makes inspector text fields and prompt dialogs actually usable as text fields.

**2b. Align `TextConsoleMorph` shift+movement to use `BeginIfNeeded`**  
Replace the duplicated before/after update pattern in `HandleHorizontalMove` and `HandleHomeEnd` with the `BeginIfNeeded` + `Update` form. No user-visible behaviour change, but removes the inconsistency.

**2c. Extract `LineEditingBehavior`**  
Once 1b and 2a are done, `TextEditMorph` and (the updated) `TextConsoleMorph` share essentially the same selection + clipboard + key handling code. Extract it into `LineEditingBehavior` as described above. Both morphs become thin rendering wrappers.

### Phase 3 — Lower priority, diminishing returns

**3a. Add Ctrl+word movement to `TextConsoleMorph`**  
`TextEditingCore` already has `MoveWordLeft` / `MoveWordRight`; wiring them up is trivial. Currently missing from the console.

**3b. Add Ctrl+Backspace / Ctrl+Delete to `TextConsoleMorph`**  
Same story — `TextEditingCore` has `DeleteWordLeft` / `DeleteWordRight`; they just need to be called.

---

## What Not to Do

- **Do not merge `TextEditorMorph` into this abstraction.** It is multi-line, has a completely different scroll/viewport model, word-wrap, syntax highlighting, and a two-dimensional selection. Forcing it into `LineEditingBehavior` would add complexity without benefit.
- **Do not merge the three rendering paths.** Grid-cell rendering (console), character-by-character line rendering (editor), and label+caret rendering (field) serve different display models and should stay separate.
- **Do not add undo/redo to individual morphs.** The world-level `CommandHistory` is the right place for undo. Text mutations should eventually be wrapped in undoable commands, but that is a separate workstream.

---

## Risk Assessment

| Item | Risk | Benefit |
|---|---|---|
| 1a Tab arithmetic extraction | Low — pure refactor, no logic change | Removes 4 copies of same expression |
| 1b TextEditMorph → TextEditingCore | Low — same logical behaviour, better allocation | Foundation for 2a |
| 2a Selection + clipboard in TextEditMorph | Medium — new UI behaviour, needs testing | High — inspector/dialog fields become usable |
| 2b Align console shift pattern | Low — no behaviour change | Removes inconsistency |
| 2c Extract LineEditingBehavior | Medium — refactor across two morphs | Reduces future duplication risk |
| 3a/3b Console word movement | Low | Moderate — QoL for heavy REPL users |

Phase 1 items are safe to batch. Phase 2 items should be done in order since each builds on the previous. Phase 3 can be deferred indefinitely.
