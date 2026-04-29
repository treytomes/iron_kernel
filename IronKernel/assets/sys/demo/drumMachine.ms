// Drum Machine — IronKernel MiniScript step sequencer

// ---------------------------------------------------------------
// Layout
// ---------------------------------------------------------------
rows       = 12
columns    = 16
cellW      = 48
cellH      = 32
padX       = 8
padY       = 8
labelH     = 20
measureGap = 4
vpW        = columns * cellW + 3 * measureGap
vpH        = rows * cellH

winH = padY + labelH + vpH + padY + 30
win  = Window.create([8, 8], [padX*2 + vpW, winH], "DRUM MACHINE")

title = Label.create([padX, padY], "Click cells to toggle  |  Escape to exit")
win.addMorph title

// ---------------------------------------------------------------
// Colors  (r, g, b as 0..1 floats)
// ---------------------------------------------------------------
// dim group (columns 0-3, 8-11)
COff      = [0.35, 0.35, 0.35]
COn       = [0.00, 0.33, 0.58]
CIndOff   = [0.45, 0.10, 0.10]
CIndOn    = [1.00, 0.40, 0.40]
// bright group (columns 4-7, 12-15)
COffB     = [0.45, 0.45, 0.45]
COnB      = [0.00, 0.42, 0.72]
CIndOffB  = [0.55, 0.15, 0.15]
CIndOnB   = [1.00, 0.55, 0.55]
CBorder   = [0.20, 0.20, 0.20]

cellColor = function(lit, highlighted, brightGroup)
    if highlighted then
        if lit then
            if brightGroup then return globals.CIndOnB else return globals.CIndOn
        else
            if brightGroup then return globals.CIndOffB else return globals.CIndOff
        end if
    else
        if lit then
            if brightGroup then return globals.COnB else return globals.COn
        else
            if brightGroup then return globals.COffB else return globals.COff
        end if
    end if
end function

// ---------------------------------------------------------------
// Build cell grid
// ---------------------------------------------------------------
// cells[row][col]  — Box handles
// litState[row][col] — bool
// bright[col]      — bool  (alternating measure groups)
cells    = []
litState = []
bright   = []

for c in range(0, columns - 1)
    bright.push (floor(c / 4) % 2 == 1)
end for

for r in range(0, rows - 1)
    row     = []
    rowLit  = []
    for c in range(0, columns - 1)
        cx   = padX + c * cellW + floor(c / 4) * measureGap
        cy   = padY + labelH + r * cellH
        cell = Box.create([cx, cy], [cellW - 1, cellH - 1])
        cell.setBorderColor CBorder
        cell.setColor cellColor(false, false, bright[c])
        win.addMorph cell
        row.push cell
        rowLit.push false
    end for
    cells.push row
    litState.push rowLit
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
        if prevCol >= 0 then
            globals.cells[r][prevCol].setColor cellColor(globals.litState[r][prevCol], false, globals.bright[prevCol])
        end if
        globals.cells[r][col].setColor cellColor(globals.litState[r][col], true, globals.bright[col])
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
    // Advance playback column
    if time >= next16th then
        lightColumn (litCol + 1) % columns
        for r in range(0, rows - 1)
            if litState[r][litCol] then playSound r, litCol
        end for
        next16th = next16th + 15 / bpm
    end if

    // Poll clicks and toggle state
    for r in range(0, rows - 1)
        for c in range(0, columns - 1)
            if cells[r][c].consumeClick then
                litState[r][c] = not litState[r][c]
                cells[r][c].setColor cellColor(litState[r][c], litCol == c, bright[c])
            end if
        end for
    end for

    wait 0
end while

if win.isAlive then win.destroy
