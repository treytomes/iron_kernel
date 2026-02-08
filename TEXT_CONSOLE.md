# Text Consoles

This document describes the **input mechanics** implemented by `TextConsoleMorph` and intended to apply uniformly to all text consoles in the system (REPLs, logs with input, debug consoles, etc.).

This is **not** a rendering or styling document. It focuses strictly on **keyboard interaction, editing behavior, and command history semantics**.

---

## Core Model

Text consoles operate on a **buffered line‑editing model**:

- User input is accumulated in an **input buffer**
- The buffer is edited locally using familiar text‑editor semantics
- Input is **committed explicitly** (usually via Enter)
- Only committed input is sent to consumers (REPL, command processor, etc.)

This model mirrors traditional terminals and REPLs (e.g., shells, language consoles).

---

## Cursor Model

- The cursor is represented as a **linear index** into the input buffer
- Visual `(x, y)` cursor position is derived from:
  - input start position
  - column count
  - current input index
- All editing operations are index‑based, not grid‑based

**Invariant:**  
> The buffer is authoritative; the grid is a projection.

This guarantees correctness across line wrapping and resizing.

---

## Character Input

### Printable Characters
- Inserted at the current cursor position
- Buffer shifts to the right
- Cursor advances by one
- Display updates immediately

### Newline (`Enter`)
- Commits the current input buffer
- Input buffer is cleared
- Cursor advances to the next line
- A new input session begins

---

## Navigation Keys

### Left / Right Arrow
- Move cursor by one character
- Clamped to `[0, buffer length]`

### Home
- Move cursor to the **start of the input buffer**

### End
- Move cursor to the **end of the input buffer**

---

## Word Navigation

Word navigation is enabled using Control-modified arrow keys.

### Definition of a Word
A word character is:
- A letter
- A digit
- An underscore (`_`)

All other characters are considered separators.

### Ctrl + Left
- Move cursor to the **start of the previous word**

### Ctrl + Right
- Move cursor to the **start of the next word**

Word navigation operates purely on the buffer index and is independent of line wrapping.

---

## Editing Keys

### Backspace
- Deletes the character **before** the cursor
- Cursor moves left
- Buffer shifts left
- Trailing grid cells are cleared explicitly

### Delete
- Deletes the character **at** the cursor
- Cursor does not move
- Buffer shifts left
- Trailing grid cells are cleared explicitly

---

## Word Deletion

### Ctrl + Backspace
- Deletes the **previous word**
- Cursor moves to the start of the deleted word

### Ctrl + Delete
- Deletes the **next word**
- Cursor remains in place

Word deletion uses the same word definition as word navigation.

---

## Command History

Text consoles maintain a **local command history**.

### History Rules
- Only **committed lines** are stored
- History navigation is only active while editing a line
- History navigation does **not** mutate history
- Enter resets history navigation state

### History Index Model
- `0` → current live input (empty or partially edited)
- `1` → most recent command
- `2` → second most recent command
- etc.

---

## History Navigation Keys

### Up Arrow
- Move backward in history (older commands)
- Replaces the current input buffer with the selected command

### Down Arrow
- Move forward in history (newer commands)
- Moving past the newest command restores an empty input line

### Editing History Entries
- Once recalled, history entries can be edited freely
- Editing does not overwrite history
- Pressing Enter commits a new command

---

## Redraw Semantics

Editing operations follow this sequence:

1. Mutate the input buffer
2. Recompute cursor index
3. Redraw input characters
4. Explicitly clear trailing grid cells

This avoids stale characters and ensures visual consistency.

---

## Focus Behavior

- The console only accepts input when it has keyboard focus
- Cursor is rendered only when focused
- Input is ignored when unfocused

---

## Non-Goals

The following are intentionally **out of scope** for the base console:

- Text selection
- Clipboard integration
- Multi-line editing
- Scrollback beyond visible rows

These may be layered on later but are not part of the baseline contract.

---

## Summary

All text consoles adhere to the following principles:

- Buffered, index-based editing
- Explicit commit model
- Familiar navigation and editing keys
- Deterministic redraw behavior
- Simple, local command history

This provides a predictable, extensible foundation for REPLs, debug consoles, and future text-based tools.
