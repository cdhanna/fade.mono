"""Makes the voices of the creatures that talk when a quest is taken. see THE QUEST TALK in
fish_headers_questtalk. Needs ffmpeg on the path.

    py -3 Tools/questtalk_voice_gen.py          from the Fade.MonoGame folder: samples, and into the game

There are five VOICES. each one is six sounds, one for each vowel, written into Assets/Fish/Audio as
voice-<voice>-<vowel>.mp3, which is what the game plays. every creature has one of the voices, and a
pitch of its own on top of it (qtLine in fish_routines_questtalk).

    squeak      a tiny bubble, high and quick. for the small ones.
    bubbly      the bubble pops that the game has already, pitched for the vowel. for the round ones.
    gruff       a low buzz, with the formants of the vowel, slow to rise. for the old and the big ones.
    bloop       two bubbles at once, at the first two formants of the vowel. for the long and the odd ones.
    clicky      the click of the crab, cut short, pitched for the vowel, with its formants. for the crabs.

and ../quest-voice-samples gets every creature saying its line, over the ambience, to judge by ear.
The pieces are the ones of the angler's voice (angler_voice_gen.py), which this borrows.
"""
import os
import random
import struct
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))

# the pieces of the angler's voice, without running it: it makes its own samples when it is run
_source = open(os.path.join(HERE, "angler_voice_gen.py"), encoding="utf-8").read()
_source = _source.replace("\nmain()\n", "\n")
angler = {"__file__": os.path.join(HERE, "angler_voice_gen.py"), "__name__": "angler_voice_gen"}
exec(compile(_source, "angler_voice_gen.py", "exec"), angler)

RATE = angler["RATE"]
NAMES = angler["NAMES"]
VOWELS = angler["VOWELS"]
VOWEL_PITCH = angler["VOWEL_PITCH"]
AUDIO = angler["AUDIO"]
SAMPLES = os.path.join(HERE, "..", "..", "quest-voice-samples")
normalize, lowpass, peaking, repitch, chirp = (angler[n] for n in ("normalize", "lowpass", "peaking", "repitch", "chirp"))


def squeak(name):
    base = 760 * 2 ** VOWEL_PITCH[name]
    return normalize(lowpass(chirp(base, base * 1.7, 0.055, 0.014), 5200))


def bubbly(name):
    return repitch(angler["style_recorded"](name), 0.15)


def gruff(name):
    # the buzz of the angler's "voice" style, a fifth lower, so slower and deeper
    return normalize(lowpass(repitch(angler["style_voice"](name), -0.6), 2000))


def bloop(name):
    return angler["style_bloop"](name)


_CLICK = None


def crab_click():
    """the click of the crab, cut down to its first click"""
    global _CLICK
    if _CLICK is None:
        raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", os.path.join(AUDIO, "crab-click.mp3"),
                              "-ac", "1", "-ar", str(RATE), "-f", "s16le", "-"], capture_output=True, check=True).stdout
        values = [v / 32768 for v in struct.unpack("<%dh" % (len(raw) // 2), raw)]
        peak = max(abs(v) for v in values)
        start = next(i for i, v in enumerate(values) if abs(v) > peak * 0.3)
        start = max(0, start - int(RATE * 0.003))
        cut = values[start:start + int(RATE * 0.06)]
        fade = int(RATE * 0.012)
        for i in range(fade):
            cut[-1 - i] *= i / fade
        _CLICK = cut
    return _CLICK


def clicky(name):
    click = repitch(crab_click(), VOWEL_PITCH[name] - 0.2)
    for centre, loud, width in VOWELS[name][:2]:
        click = peaking(click, centre, 8 * loud, centre / (width * 2))
    return normalize(click)


VOICES = {"squeak": squeak, "bubbly": bubbly, "gruff": gruff, "bloop": bloop, "clicky": clicky}

# who says what, in which voice, at what pitch (hundredths of an octave). the same as setup_questtalk_lines.
CAST = [
    ("Sandy", "squeak", 20, "Roll me to the right edge? Only with swaps that pop!"),
    ("Old Barnacle", "gruff", -60, "So hungry... misses make krill. Fill a third of the board!"),
    ("Goldie", "squeak", 25, "My twin is lost! Bring the two ringed fish side by side?"),
    ("Professor Bass", "gruff", -10, "Pop the four kinds I show, in order. Out of turn? Start over."),
    ("Finn", "gruff", -35, "Pop near me, and I eat it. Ten popping swaps, if you dare."),
    ("Puff", "bubbly", 15, "Don't pop me! And keep the popping swaps coming, one by one."),
    ("Angela", "bloop", 20, "Keep the ringed fish safe. No swaps, and no pops on it!"),
    ("Glimmer", "squeak", 45, "Follow my light! Pop fish in the lit column as it moves."),
    ("Dash", "bloop", -20, "Keep it going! Every swap has to pop something."),
    ("Kelp", "bloop", -10, "Plant me with pops on the bottom row. Grow two of me up top!"),
    ("Ripple", "bloop", 10, "Pops leave a glow. Draw me a glowing path, edge to edge."),
    ("Grandpa Shell", "gruff", -25, "I step right after every swap... onto the fish I show."),
    ("Inky", "bubbly", 30, "I change with every swap. Pop me as each kind, in turn!"),
    ("Stella", "bubbly", 35, "Four ringed fish, in the shape of the stars. Careful now."),
    ("Slinky", "bloop", -30, "I'll follow you everywhere. Just don't cross my tail."),
    ("Snooze", "bloop", -15, "Shh... one kind is asleep. Have three groups waiting for it."),
    ("Echo", "bubbly", 40, "I copy your swaps, four moves later. Make mine pop!"),
    ("Thump", "bloop", 0, "Every fourth move, I pulse. Pop what I make!"),
    ("Chief Crab", "clicky", 10, "Stack the four kinds, top to bottom, in my column."),
    ("Captain Crab", "clicky", 5, "Ringed fish, sound off! Pop them in the order of the colors."),
    ("Crystal", "squeak", 45, "Lower me to the bottom, gently. A long fall, and I shatter!"),
    ("Mirror", "bloop", 20, "Make the marked row read the same from both ends."),
    ("Bobby", "bloop", 0, "Stay within three cells of me, and take me to the far corner!"),
    ("Wick", "gruff", -20, "Steps burn my light, and pops fill it. Light all three pearls!"),
    ("Mama Pearl", "bubbly", 15, "My eggs count down. Pop each one right as it hits zero!"),
    ("Breezy", "squeak", 30, "I see what falls next. Finish groups as the new fish land."),
    ("Skipper", "bloop", -10, "Each colored cell wants its own fish. Fill all four at once."),
    ("The Twins", "bubbly", 25, "We want to trade homes! Swap our ringed fish, no misses."),
    ("Pip", "squeak", 50, "I'm right behind you! Pop while I sit on each lit cell."),
    ("Starry", "bubbly", 35, "Carry us from the left edge to the right, three pops a trip!"),
    ("Wrangler Worm", "bloop", 0, "Walk a loop around the ringed fish, with four pops on the way."),
    ("Old Clam", "gruff", -30, "Warmer... colder... pop the fish on the buried cell."),
]


def main():
    random.seed(7)
    os.makedirs(SAMPLES, exist_ok=True)
    sets = {}
    for voice, make in VOICES.items():
        sets[voice] = {name: make(name) for name in NAMES}
        for name in NAMES:
            angler["to_mp3"](sets[voice][name], os.path.join(AUDIO, "voice-%s-%s.mp3" % (voice, name)))
        row = []
        for name in NAMES:
            row += [v * angler["VOICE_VOLUME"] for v in sets[voice][name]] + [0.0] * int(RATE * 0.25)
        angler["to_mp3"](row, os.path.join(SAMPLES, "vowels-%s.mp3" % voice))
        print(voice, "-> the game, and", SAMPLES)
    for n, (who, voice, pitch, line) in enumerate(CAST, 1):
        events, length = angler["speak"](line)
        events = [(when, sound, p + pitch / 100.0, vol) for when, sound, p, vol in events]
        out = os.path.join(SAMPLES, "%02d-%s.mp3" % (n, who.lower().replace(" ", "-")))
        angler["to_mp3"](angler["render"](events, length, sets[voice]), out, True)
    print(len(CAST), "lines ->", SAMPLES)


if __name__ == "__main__":
    main()
