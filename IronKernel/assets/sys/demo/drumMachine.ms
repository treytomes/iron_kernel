// Drum Machine — IronKernel port of the MiniMicro sysdisk demo

// ---------------------------------------------------------------
// Layout
// ---------------------------------------------------------------
rows    = 12
columns = 16
cellW   = 48
cellH   = 32
padX    = 8
padY    = 8
labelH  = 20
measureGap = 4
vpW     = columns * cellW + 3 * measureGap
vpH     = rows    * cellH

// winH: content area needs padY+labelH+vpH+padY; add 30 for title bar + dock chrome overhead
winH = padY + labelH + vpH + padY + 30
win = Window.create([8, 8], [padX*2 + vpW, winH], "DRUM MACHINE")

title = Label.create([padX, padY], "Click cells to toggle  |  Escape to exit")
win.addMorph title

// ---------------------------------------------------------------
// Build cell grid
// ---------------------------------------------------------------
// cells[row][col] — DrumCellMorph handles
cells = []
for r in range(0, rows - 1)
    row = []
    for c in range(0, columns - 1)
        cx = padX + c * cellW + floor(c / 4) * measureGap
        cy = padY + labelH + r * cellH
        cell = DrumCell.create([cx, cy], [cellW - 1, cellH - 1], c)
        win.addMorph cell
        row.push cell
    end for
    cells.push row
end for

// ---------------------------------------------------------------
// Load sounds
// ---------------------------------------------------------------
instrument     = file.loadSound("sys://sounds/elecGuitarC4.wav")
instrumentFreq = noteFreq(60)

chords = [
    [60, 64, 67, 72],
    [60, 65, 68, 72],
    [62, 67, 71, 74],
    [60, 64, 67, 72]]

sounds = []
sounds.push instrument
sounds.push instrument
sounds.push instrument
sounds.push instrument
sounds.push file.loadSound("sys://sounds/hit.wav")
sounds.push file.loadSound("sys://sounds/hihat.wav")
sounds.push file.loadSound("sys://sounds/hihat2.wav")
sounds.push file.loadSound("sys://sounds/snare.wav")
sounds.push file.loadSound("sys://sounds/snare2.wav")
sounds.push file.loadSound("sys://sounds/bongo.wav")
sounds.push file.loadSound("sys://sounds/aminPluck.wav")
sounds.push file.loadSound("sys://sounds/woodblock.wav")

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------
litCol = -1

lightColumn = function(col)
    prevCol = globals.litCol
    globals.litCol = col
    for r in range(0, globals.rows - 1)
        if prevCol >= 0 then globals.cells[r][prevCol].setHighlighted false
        globals.cells[r][col].setHighlighted true
    end for
end function

playSound = function(row, col)
    if row > 3 then
        globals.sounds[row].play
    else
        speed = noteFreq(globals.chords[floor(col / 4)][row]) / globals.instrumentFreq
        globals.sounds[row].play 1, 0, speed
    end if
end function

lightColumn 0

// ---------------------------------------------------------------
// Main loop
// ---------------------------------------------------------------
bpm      = 90
next16th = time

while win.isAlive and not Keyboard.isDown("Escape")
    if time >= next16th then
        lightColumn (litCol + 1) % columns
        for r in range(0, rows - 1)
            if cells[r][litCol].isLit then playSound r, litCol
        end for
        next16th = next16th + 15 / bpm
    end if
    wait 0
end while

if win.isAlive then win.destroy
