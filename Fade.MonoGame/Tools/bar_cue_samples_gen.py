"""
Audition samples for a musical cue on the bar of the round (see THE CHARGE in fish_headers_charge):
the points of a combo land in the bar one after another, and each landing plays a note a step higher
up D minor, on the game's own instruments. when the bar takes the charge in, a chord answers.

    py Tools/bar_cue_samples_gen.py           from the Fade.MonoGame folder

It writes nothing into the game. It writes, into ../bar-cue-samples, three takes, each for a small,
a medium and a big combo, over the game's D minor chord and the water, with the fish popping:

    a-bells    a bell for every landing, up the scale, ringing between the top two once it is there.
               the chord is the bells of D minor, rolled.
    b-plucks   a pluck, but never two closer than 140 ms, so a fast run of landings is thinned to a
               ladder. the chord is a bell and a pluck under it.
    c-choir    the landings only tick, softly, on the pluck. a choir holds a note that steps up once
               a beat (520 ms) while the points keep landing, and it settles on the chord.

    d-shepard-plucks    the pluck as a Shepard tone: each note is the same note in four octaves at once,
                        the top one fading out and the bottom one fading in as it climbs, so the climb
                        never gets to the top. it steps up D minor, never two closer than 140 ms. the
                        chord is three of them, rolled: the note it got to, and a third and a fifth up.
    e-shepard-pentatonic the same, but up the five notes of D minor that never clash (D F G A C), so a
                        step is bigger, and every note sits well on the chord under it

and one more, f-shepard-round, which is a stretch of a round: five combos one after another, where the
climb picks up where the last one left it, so the whole round is one climb that never tops out.

and the same round again on other instruments, as round-<instrument>:

    the game's own:      bell, swell, choir, wail, and bell-pluck (the pluck with the bell under it)
    the game's sounds, tuned to D and played as notes: bubble (a bubble popping), bloop (the bloop
                         voice, on "oo"), and sonar (the ping)
    made here, so they sound made: marimba, kalimba, musicbox, and glass

the big combo fills the bar at the end, so it ends on the boom and the wave of bubbles instead of the
chord. Needs numpy, and ffmpeg on the path.
"""
import os

import numpy as np

from bar_climb_gen import SR, MUSIC, AUDIO, rng, reverb, load_mp3, fit, save as save_mp3

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "bar-cue-samples")
LAND_MS = 0.76      # SCORE_FLY_LAND_MS: the points land this long after their fish pops
SETTLE = 0.2        # CHARGE_SETTLE_MS
GLINT = 0.3         # CHARGE_GLINT_MS: the bar takes the charge in this long after it settles

SCALE = ["d4", "e4", "f4", "g4", "a4", "bb4", "c5", "d5", "e5", "f5", "g5", "a5", "bb5", "c6"]
CHORD = ["d4", "f4", "a4", "d5"]

# each combo: groups of fish that pop, as (when the group starts, how many fish, how far apart they pop)
COMBOS = {
    "1-small": [(0.5, 4, 0.05)],
    "2-medium": [(0.5, 5, 0.05), (1.4, 4, 0.05)],
    "3-big": [(0.5, 6, 0.05), (1.3, 7, 0.05), (2.1, 9, 0.05), (3.0, 12, 0.05)],
}

_cache = {}


def note(inst, name):
    key = (inst, name)
    if key not in _cache:
        _cache[key] = load_mp3(os.path.join(MUSIC, "%s-%s.mp3" % (inst, name)))
    return _cache[key]


def add(out, x, at, gain):
    i = int(at * SR)
    if i >= len(out):
        return
    j = min(len(out), i + len(x))
    out[i:j] += x[:j - i] * gain


def pops(combo):
    return sorted(start + k * gap for start, count, gap in combo for k in range(count))


def bed(n, combo):
    pad = fit(load_mp3(os.path.join(MUSIC, "loop-pad-dm.mp3")), n) * 0.45
    amb = fit(load_mp3(os.path.join(MUSIC, "loop-ambience.mp3")), n)
    out = 0.5 * (pad + amb)
    pop = load_mp3(os.path.join(AUDIO, "bubble-pop-2.mp3")) * 0.35
    for t in pops(combo):
        add(out, pop, t, rng.uniform(0.7, 1.0))
    return out


def chord(out, at, inst, gain, roll=0.07):
    for k, name in enumerate(CHORD):
        add(out, note(inst, name), at + k * roll, gain)


def ending(out, at, big):
    if big:
        add(out, load_mp3(os.path.join(AUDIO, "bar-charge-boom.mp3")), at, 0.8)
    return big


# the notes of D minor, and of its pentatonic, as the names of the clips of the pluck in the lower
# octave and the upper one, and how many semitones each is above D
SHEP_SCALE = [("d4", "d5", 0), ("e4", "e5", 2), ("f4", "f5", 3), ("g4", "g5", 5),
              ("a4", "a5", 7), ("bb4", "bb5", 8), ("c5", "c6", 10)]
SHEP_PENTA = [("d4", "d5", 0), ("f4", "f5", 3), ("g4", "g5", 5), ("a4", "a5", 7), ("c5", "c6", 10)]
SHEP_CENTER = 1.0   # the octave layer that is loudest: between the clip's two octaves
SHEP_SPREAD = 0.8   # how many octaves wide the hump is
_shep = {}


def speed(x, rate):
    """a sound played faster (higher and shorter) or slower (lower and longer)"""
    n = int(len(x) / rate)
    at = np.arange(n) * rate
    return np.stack([np.interp(at, np.arange(len(x)), x[:, c]) for c in range(2)], axis=1)


def shepard_pluck(step, scale):
    """the pluck as a Shepard tone: four octaves of it at once, weighted by a hump over how high each
    one is, so that each step up the scale fades the top one out a little and the bottom one in"""
    key = (step % len(scale), id(scale))
    if key in _shep:
        return _shep[key]
    low, high, semis = scale[step % len(scale)]
    a, b = note("pluck", low), note("pluck", high)
    layers = [(speed(a, 0.5), -1), (a, 0), (b, 1), (speed(b, 2.0), 2)]
    n = max(len(x) for x, _ in layers)
    out = np.zeros((n, 2))
    for x, k in layers:
        at = k + semis / 12
        w = np.exp(-0.5 * ((at - SHEP_CENTER) / SHEP_SPREAD) ** 2)
        out[:len(x)] += x * w
    _shep[key] = out
    return out


def shepard_run(cue, lands, step, scale, gain=0.26):
    """a Shepard pluck a step up for each landing, never two closer than 140 ms. it gives back the
    step that it got to, so that the next run can carry on from there."""
    gone = -1.0
    for t in lands:
        if t - gone >= 0.14:
            step += 1
            add(cue, shepard_pluck(step, scale), t, gain)
            gone = t
    return step


def shepard_chord(cue, at, step, scale, gain=0.2):
    """the note that it got to, and a third and a fifth above it, as Shepard plucks, rolled"""
    third = 2 if scale is SHEP_SCALE else 1
    for k, up in enumerate((0, third, third * 2)):
        add(cue, shepard_pluck(step + up, scale), at + k * 0.06, gain)


def take(style, combo, big):
    lands = [t + LAND_MS for t in pops(combo)]
    last = lands[-1]
    end_at = last if big else last + SETTLE + GLINT
    n = int(SR * (end_at + 5.0))
    cue = np.zeros((n, 2))

    if style == "a-bells":
        for k, t in enumerate(lands):
            # once it is at the top, it rings between the top two notes, and does not hammer one
            at = k if k < len(SCALE) else len(SCALE) - 1 - (k % 2)
            add(cue, note("note", SCALE[at]), t, 0.28)
        if not big:
            chord(cue, end_at, "note", 0.32)
    elif style == "b-plucks":
        step, gone = 0, -1.0
        for t in lands:
            if t - gone >= 0.14:
                add(cue, note("pluck", SCALE[min(step, len(SCALE) - 1)]), t, 0.4)
                step, gone = step + 1, t
        if not big:
            chord(cue, end_at, "note", 0.25, roll=0.05)
            add(cue, note("pluck", "d4"), end_at, 0.35)
    elif style in ("d-shepard-plucks", "e-shepard-pentatonic"):
        scale = SHEP_SCALE if style == "d-shepard-plucks" else SHEP_PENTA
        step = shepard_run(cue, lands, -1, scale)
        if not big:
            shepard_chord(cue, end_at, step, scale)
    else:
        for t in lands:
            add(cue, note("pluck", "d5"), t, 0.08)
        step, at = 0, lands[0]
        while at <= last:
            add(cue, note("choir", SCALE[min(step, len(SCALE) - 1)]), at, 0.3)
            step, at = step + 2, at + 0.52
        if not big:
            for name in ("d4", "a4", "f5"):
                add(cue, note("choir", name), end_at, 0.22)

    ending(cue, end_at, big)
    cue = reverb(cue, wet=0.25, dry=0.9)[:n]
    return bed(n, combo) + cue


D4 = 293.66


def tone_at(x, freq_from, freq_to):
    """a recording played at another pitch: faster for higher, slower for lower"""
    return speed(x, freq_to / freq_from)


def pitch_of(x):
    """roughly the pitch of a short sound: the loudest frequency in its first fifth of a second"""
    mono = x[:, 0] + x[:, 1]
    start = int(np.argmax(np.abs(mono) > 0.2 * np.max(np.abs(mono))))
    seg = mono[start:start + int(SR * 0.2)] * np.hanning(min(len(mono) - start, int(SR * 0.2)))
    spec = np.abs(np.fft.rfft(seg, 1 << 16))
    fr = np.fft.rfftfreq(1 << 16, 1 / SR)
    band = (fr > 90) & (fr < 3000)
    return fr[band][np.argmax(spec[band])]


def synth(kind, f):
    """an instrument made here, at f Hz. they are plain, and sound made."""
    n = int(SR * 2.2)
    t = np.arange(n) / SR
    if kind == "marimba":
        x = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.45) + 0.35 * np.sin(2 * np.pi * 3.93 * f * t) * np.exp(-t / 0.06)
    elif kind == "kalimba":
        x = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.7) + 0.25 * np.sin(2 * np.pi * 5.4 * f * t) * np.exp(-t / 0.05)
        x += 0.15 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t / 0.3)
    elif kind == "musicbox":
        x = (np.sin(2 * np.pi * f * t) + 0.4 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t / 0.4)
             + 0.2 * np.sin(2 * np.pi * 4.2 * f * t) * np.exp(-t / 0.1)) * np.exp(-t / 0.9)
    else:  # glass: slow to come in, with a slow wobble, like a wet finger on a glass
        x = (np.sin(2 * np.pi * f * t) + 0.2 * np.sin(2 * np.pi * 2.01 * f * t)) * np.minimum(t / 0.06, 1) * np.exp(-t / 1.1)
        x *= 1 + 0.15 * np.sin(2 * np.pi * 5 * t)
    x *= np.minimum(t / 0.003, 1)
    return np.stack([x, x], axis=1) * 0.5


_tuned = {}


def source(inst):
    """a recording of the game, as (the sound, its pitch), for the instruments that are one sound played as notes"""
    if inst not in _tuned:
        path = {"bubble": os.path.join(AUDIO, "bubble-pop-2.mp3"),
                "bloop": os.path.join(AUDIO, "voice-bloop-oo.mp3"),
                "sonar": os.path.join(MUSIC, "shot-ping.mp3")}[inst]
        x = load_mp3(path)
        if inst == "sonar":
            x = x[:int(SR * 1.6)]  # only the first ping
            x[-int(SR * 0.3):] *= np.linspace(1, 0, int(SR * 0.3))[:, None]
        _tuned[inst] = (x, pitch_of(x))
    return _tuned[inst]


CLIPS = {"bell": "note", "swell": "swell", "choir": "choir", "wail": "wail"}


def shepard_note(inst, step, scale):
    """one note of the climb on an instrument: the same note in four octaves at once, weighted by the hump"""
    key = (inst, step % len(scale), id(scale))
    if key in _shep:
        return _shep[key]
    low, high, semis = scale[step % len(scale)]
    layers = []
    if inst in CLIPS or inst == "bell-pluck":
        names = ["note", "pluck"] if inst == "bell-pluck" else [CLIPS[inst]]
        for name in names:
            a, b = note(name, low), note(name, high)
            g = 0.6 if name == "note" and inst == "bell-pluck" else 1.0
            layers += [(speed(a, 0.5) * g, -1), (a * g, 0), (b * g, 1), (speed(b, 2.0) * g, 2)]
    elif inst in ("bubble", "bloop", "sonar"):
        x, f0 = source(inst)
        for k in (-1, 0, 1, 2):
            layers.append((tone_at(x, f0, D4 * 2 ** (k + semis / 12)), k))
    else:
        for k in (-1, 0, 1, 2):
            layers.append((synth(inst, D4 * 2 ** (k + semis / 12)), k))
    n = max(len(x) for x, _ in layers)
    out = np.zeros((n, 2))
    for x, k in layers:
        at = k + semis / 12
        out[:len(x)] += x * np.exp(-0.5 * ((at - SHEP_CENTER) / SHEP_SPREAD) ** 2)
    # every instrument about as loud as the pluck
    out *= 0.6 / max(1e-6, np.max(np.abs(out)))
    _shep[key] = out
    return out


def round_take(inst="pluck"):
    """five combos one after another, with the climb carried from one to the next, the last one filling the bar"""
    combos = [[(0.5, 4, 0.05)], [(3.0, 5, 0.05), (3.9, 3, 0.05)], [(6.2, 4, 0.05)],
              [(8.6, 6, 0.05), (9.4, 7, 0.05)], [(12.2, 7, 0.05), (13.0, 9, 0.05), (13.9, 12, 0.05)]]
    every = [g for c in combos for g in c]
    lands_all = [t + LAND_MS for t in pops(every)]
    n = int(SR * (lands_all[-1] + 5.0))
    cue = np.zeros((n, 2))
    step = -1
    for k, c in enumerate(combos):
        lands = [t + LAND_MS for t in pops(c)]
        if inst == "pluck":
            step = shepard_run(cue, lands, step, SHEP_SCALE)
            if k < len(combos) - 1:
                shepard_chord(cue, lands[-1] + SETTLE + GLINT, step, SHEP_SCALE)
        else:
            gone = -1.0
            for t in lands:
                if t - gone >= 0.14:
                    step += 1
                    add(cue, shepard_note(inst, step, SHEP_SCALE), t, 0.42)
                    gone = t
            if k < len(combos) - 1:
                for j, up in enumerate((0, 2, 4)):
                    add(cue, shepard_note(inst, step + up, SHEP_SCALE), lands[-1] + SETTLE + GLINT + j * 0.06, 0.3)
        if k == len(combos) - 1:
            ending(cue, lands[-1], True)
    cue = reverb(cue, wet=0.25, dry=0.9)[:n]
    return bed(n, every) + cue


def main():
    global OUT
    import bar_climb_gen
    bar_climb_gen.OUT = OUT
    for style in ("a-bells", "b-plucks", "c-choir", "d-shepard-plucks", "e-shepard-pentatonic"):
        for size, combo in COMBOS.items():
            save_mp3("%s-%s" % (style, size), take(style, combo, size == "3-big"))
    save_mp3("f-shepard-round", round_take())
    for inst in ("bell", "swell", "choir", "wail", "bell-pluck", "bubble", "bloop", "sonar",
                 "marimba", "kalimba", "musicbox", "glass"):
        save_mp3("round-" + inst, round_take(inst))
        print("  round-" + inst)
    print("wrote", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
