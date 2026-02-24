# TODO

* Mouse wheel should engage vertical scrollbar.
* WorldMorph doesn't need it's own Interpreter.  Remove it after the Process Manager is in place.
* An exception in a single morph should log an error and "pause" the morph, but shouldn't crash the entire userland.

* In the console, if typing on bottom row and wrapping occurs, scroll the window.
* There are a lot of instabilities in string slicing between the text editor, text editor core, and console.
* Inspector changes:
	* Inline editors for Point and Size.
* Text Editor changes:
	* Source lines need to not wrap around to the next line.  It just doesn't look right.
	* Ctrl+S to save.
	* Page Up / Page Down
	* Ctrl+A, then up arrow should move cursor to selection start.
	* Ctrl+A, then down arrow should move cursor to selection bottom.
	* Shift+Tab should reverse-indent.
	* The alert dialog on save is unnecessary.
	* Ctrl+X on a row should cut the row into the copy buffer.
	* Typing on the last row of the text editor is causing the cursor to fall out of view.
* Some kind of "current directory" tracking per Interpreter instance.
	* This might tie into the idea of an "Interpreter Process"
* REPL changes:
	* A `dir` intrinsic to list directory contents.
	* `mkdir` to make directories.
	* `del` to delete either files or directories.
	* An `input` intrinsic to gather input in the REPL.
* Common dialog changes:
	* When opening text editor or REPL, keyboard focus should be immediate.
	* When opening a prompt, immediate keyboard focus on text box.
	* When committing text on a prompt, accept and close the prompt.
	* The inspector should count as a common dialog.

