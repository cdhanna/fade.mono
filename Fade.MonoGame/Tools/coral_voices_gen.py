"""Renders the voices that the music has for coral: a choir, a wail, and a scream from far away.

    python coral_voices_gen.py <folder to write the mp3 files to> [choir] [wail] [scream]

with no names after the folder, all three are made. with names, only those are, and the rest are left alone.

there is a fourth, "solo", which is only made when it is asked for by name: one woman calling, high and
clear, the way that the herding calls of the north are sung. the game does not use it. it is a trial.

the choir and the wail are instruments that a melody can be played on, so each has a sound for every
note, named like "choir-bb4". see THE MUSIC in fish_routines_music. the scream is a shot.

this needs nothing but python and ffmpeg. the voices are added up one sine at a time, which is slow,
so they are made at half of the sample rate, and ffmpeg does the echo of the room and the mp3.
"""
import array
import math
import os
import random
import subprocess
import sys
import tempfile
import wave

SR = 44100
SYNTH_SR = 22050
OUT = sys.argv[1]
TMP = tempfile.mkdtemp(prefix="coral_voices_")
ONLY = sys.argv[2:]
rng = random.Random(11)

NOTES = (("d4", 293.66), ("e4", 329.63), ("f4", 349.23), ("g4", 392.00), ("a4", 440.00), ("bb4", 466.16),
         ("c5", 523.25), ("d5", 587.33), ("e5", 659.25), ("f5", 698.46), ("g5", 783.99), ("a5", 880.00),
         ("bb5", 932.33), ("c6", 1046.50))

# what makes a voice sound like a vowel: the pitches that the mouth lets through, as (hz, how much, how wide)
AH_HIGH = ((800, 1.0, 130), (1150, 0.5, 150), (2900, 0.10, 300), (3900, 0.05, 300))
OO_HIGH = ((430, 1.0, 120), (850, 0.35, 140), (2800, 0.06, 300))
AH_LOW = ((620, 1.0, 110), (1050, 0.45, 130), (2500, 0.08, 280))
OH_LOW = ((480, 1.0, 100), (820, 0.50, 120), (2400, 0.05, 260))
AH_SCREAM = ((950, 1.0, 180), (1500, 0.8, 220), (2900, 0.5, 350), (3800, 0.25, 400))


def write_wav(path, left, right, rate):
    peak = max(max(abs(v) for v in left), max(abs(v) for v in right), 1e-9)
    scale = 0.7 * 32767 / peak
    frames = array.array("h")
    for l, r in zip(left, right):
        frames.append(int(l * scale))
        frames.append(int(r * scale))
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(frames.tobytes())


def read_wav(path):
    with wave.open(path, "rb") as w:
        frames = array.array("h")
        frames.frombytes(w.readframes(w.getnframes()))
    return frames


def make_ir(path, seconds, dark):
    """The echo of a big room: noise that dies away. `dark` is how much of the top is taken off, from 0 to 1."""
    n = int(SR * seconds)
    chans = []
    for _ in range(2):
        x = []
        low = 0.0
        slow = 0.0
        for i in range(n):
            v = rng.gauss(0, 1)
            low += (v - low) * (1 - dark)   # take the top off
            slow += (low - slow) * 0.02     # and the very bottom
            x.append((low - slow) * math.exp(-6.9 * i / n))
        energy = math.sqrt(sum(v * v for v in x))
        chans.append([v / energy for v in x])
    # this one is not scaled to a peak, because how loud the echo is depends on it
    frames = array.array("h")
    for l, r in zip(*chans):
        frames.append(int(max(-1, min(1, l * 8)) * 32767))
        frames.append(int(max(-1, min(1, r * 8)) * 32767))
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(frames.tobytes())


def harmonics(f0, formants, top, tilt):
    """How loud every harmonic of a voice is, for a vowel. the first one is the note itself."""
    amps = [0.0]
    h = 1
    while f0 * h <= top and h <= 24:
        f = f0 * h
        mouth = 0.10 + sum(a * math.exp(-0.5 * ((f - hz) / wide) ** 2) for hz, a, wide in formants)
        amps.append(mouth / h ** tilt)
        h += 1
    amps[1] = max(amps[1], 0.75)  # a high voice puts its weight on the note itself
    return amps


def sing(left, right, f0, seconds, amps, gain, pan, vibrato, sag_cents=0.0, growl=0.0, bend=None,
         amps_to=None, open_time=0.5):
    """Add one voice to the mix. it wobbles a little, the way that a throat does, and no two wobble alike.
    `sag_cents` is how far the pitch has sunk by the end, and `growl` is how rough the voice is.
    `bend` is a function of time that says how many times the pitch is multiplied, for a voice that slides.
    with `amps_to`, the mouth opens: the voice starts on the vowel of `amps`, and is on the vowel of
    `amps_to` after `open_time` seconds. a mouth that moves is most of what makes a tone into a voice."""
    if amps_to:
        size = max(len(amps), len(amps_to))
        amps = amps + [0.0] * (size - len(amps))
        amps_to = amps_to + [0.0] * (size - len(amps_to))
    shimmer_rate = 3.5 + 2.0 * rng.random()
    shimmer_phase = rng.random() * 6.2832
    n = int(SYNTH_SR * seconds)
    angle = (pan + 1) * math.pi / 4
    gl = gain * math.cos(angle)
    gr = gain * math.sin(angle)
    rate = vibrato[0] * (0.9 + 0.2 * rng.random())
    depth = vibrato[1] * (0.8 + 0.4 * rng.random())
    vib_phase = rng.random() * 6.2832
    drift_rate = 0.4 + 0.6 * rng.random()
    drift_phase = rng.random() * 6.2832
    growl_phase = rng.random() * 6.2832
    phase = rng.random() * 6.2832
    top = len(amps)
    step = 6.2832 / SYNTH_SR
    for i in range(n):
        t = i / SYNTH_SR
        f = f0 * (1 + depth * min(t / 0.7, 1) * math.sin(6.2832 * rate * t + vib_phase)
                  + 0.0015 * math.sin(6.2832 * drift_rate * t + drift_phase))
        if sag_cents:
            f *= 2 ** (-sag_cents * (t / seconds) ** 1.5 / 1200)
        if bend:
            f *= bend(t)
        phase += f * step
        c2 = 2 * math.cos(phase)
        s0 = 0.0
        s1 = math.sin(phase)
        v = amps[1] * s1
        for h in range(2, top):
            s0, s1 = s1, c2 * s1 - s0
            v += amps[h] * s1
        if amps_to:
            # the same again for the other vowel, and a mix of the two
            s0 = 0.0
            s1 = math.sin(phase)
            w = amps_to[1] * s1
            for h in range(2, top):
                s0, s1 = s1, c2 * s1 - s0
                w += amps_to[h] * s1
            m = min(t / open_time, 1.0)
            m = m * m * (3 - 2 * m)
            v = v * (1 - m) + w * m
            v *= 1 + 0.06 * math.sin(6.2832 * shimmer_rate * t + shimmer_phase)
        if growl:
            v *= 1 - growl * (0.5 + 0.5 * math.sin(6.2832 * 27 * t + growl_phase))
        left[i] += v * gl
        right[i] += v * gr


def shape(left, right, attack, hold_tau):
    """Fade the note in, and let it die away."""
    for i in range(len(left)):
        t = i / SYNTH_SR
        e = math.sin(math.pi * t / (2 * attack)) ** 2 if t < attack else math.exp(-(t - attack) / hold_tau)
        left[i] *= e
        right[i] *= e


def choir(f0):
    """Women singing "ah", together, with a few men an octave down underneath them. every voice starts
    with its mouth nearly shut, on "oo", and opens to "ah", and it has the bright ring that a trained
    voice has. the notes hold for a moment before they die away, and they do not hang about for long after that."""
    seconds = 5.5
    n = int(SYNTH_SR * seconds)
    left, right = [0.0] * n, [0.0] * n

    # a woman singing high opens her mouth to keep the first of these at or above the note
    first = max(820.0, f0 * 1.03)
    ah = ((first, 1.0, 110), (1200, 0.6, 130), (2850, 0.28, 220), (3900, 0.12, 280))
    oo = ((max(380.0, f0 * 1.0), 1.0, 90), (780, 0.3, 110), (2700, 0.05, 250))
    opened = harmonics(f0, ah, 7000, 1.15)
    shut = harmonics(f0, oo, 7000, 1.5)
    for cents, pan in ((-7, -0.7), (-3, 0.45), (0, -0.15), (2, 0.15), (5, -0.45), (8, 0.7)):
        sing(left, right, f0 * 2 ** (cents / 1200), seconds, shut, 1.0, pan, (5.4, 0.006),
             amps_to=opened, open_time=0.45 + 0.2 * rng.random())

    men_ah = ((640, 1.0, 100), (1080, 0.5, 120), (2500, 0.2, 220), (3300, 0.1, 260))
    men_oo = ((330, 1.0, 80), (800, 0.3, 100), (2300, 0.05, 240))
    low_opened = harmonics(f0 / 2, men_ah, 5000, 1.25)
    low_shut = harmonics(f0 / 2, men_oo, 5000, 1.5)
    for cents, pan in ((-5, 0.3), (4, -0.3)):
        sing(left, right, f0 / 2 * 2 ** (cents / 1200), seconds, low_shut, 0.28, pan, (5.0, 0.005),
             amps_to=low_opened, open_time=0.5 + 0.2 * rng.random())

    # it comes in gently, holds, and then dies away slowly
    for i in range(n):
        t = i / SYNTH_SR
        if t < 0.32:
            e = math.sin(math.pi * t / 0.64) ** 2
        elif t < 1.0:
            e = 1.0
        else:
            e = math.exp(-(t - 1.0) / 1.5)
        left[i] *= e
        right[i] *= e
    return left, right


def solo(f0):
    """One woman, calling out over water. a bright, open voice with almost no wobble to begin with. she
    slides up into the note from underneath, holds it straight, lets a little wobble in, and lets it
    fall away at the end. there is some breath in it."""
    seconds = 4.5
    n = int(SYNTH_SR * seconds)
    left, right = [0.0] * n, [0.0] * n

    first = max(880.0, f0 * 1.04)
    bright = ((first, 1.0, 90), (1450, 0.7, 110), (2950, 0.55, 170), (3850, 0.3, 220), (4900, 0.1, 300))
    shut = ((max(420.0, f0), 1.0, 90), (1900, 0.25, 140), (2800, 0.1, 220))
    opened = harmonics(f0, bright, 8500, 1.0)
    closed = harmonics(f0, shut, 8500, 1.4)
    flutter_a = rng.random() * 6.2832
    flutter_b = rng.random() * 6.2832

    def line(t):
        scoop = -170.0 * (1 - min(t / 0.16, 1.0)) ** 2
        fall = -90.0 * max(0.0, (t - 2.6) / (seconds - 2.6)) ** 2
        flutter = 4.0 * math.sin(6.2832 * 7.7 * t + flutter_a) + 3.0 * math.sin(6.2832 * 11.3 * t + flutter_b)
        late = 9.0 * max(0.0, min((t - 1.0) / 0.8, 1.0)) * math.sin(6.2832 * 5.6 * t)
        return 2 ** ((scoop + fall + flutter + late) / 1200)

    sing(left, right, f0, seconds, closed, 1.0, 0.0, (5.6, 0.0), bend=line, amps_to=opened, open_time=0.22)
    sing(left, right, f0 * 1.002, seconds, closed, 0.18, 0.25, (5.6, 0.0), bend=line, amps_to=opened, open_time=0.26)

    # breath: a hiss that follows the voice
    last = 0.0
    for i in range(n):
        t = i / SYNTH_SR
        if t < 0.07:
            e = math.sin(math.pi * t / 0.14) ** 2
        elif t < 1.1:
            e = 1.0
        else:
            e = math.exp(-(t - 1.1) / 1.1)
        noise = rng.gauss(0, 1)
        hiss = (noise - last) * 0.012
        last = noise
        left[i] = (left[i] + hiss) * e
        right[i] = (right[i] + hiss) * e
    return left, right


def wail(f0):
    """Voices in pain: an octave down, moaning "oh", out of tune with each other, and every note sinks as it goes."""
    seconds = 5.5
    n = int(SYNTH_SR * seconds)
    left, right = [0.0] * n, [0.0] * n
    low = harmonics(f0 / 2, OH_LOW, 3200, 1.3)
    for cents, pan, sag in ((-22, -0.7, 70), (-7, 0.4, 45), (9, -0.3, 60), (24, 0.7, 85)):
        sing(left, right, f0 / 2 * 2 ** (cents / 1200), seconds, low, 1.0, pan, (4.3, 0.012), sag_cents=sag, growl=0.22)
    high = harmonics(f0, OO_HIGH, 3200, 1.6)
    for cents, pan, sag in ((-15, 0.5, 55), (17, -0.5, 90)):
        sing(left, right, f0 * 2 ** (cents / 1200), seconds, high, 0.3, pan, (5.6, 0.016), sag_cents=sag)
    shape(left, right, 0.6, 1.9)
    return left, right


def scream():
    """One long cry: it climbs fast, shakes at the top, and falls away. the water takes the edge off of it later."""
    seconds = 2.8
    n = int(SYNTH_SR * seconds)
    left, right = [0.0] * n, [0.0] * n

    def contour(t):
        if t < 0.3:
            hz = 480 + (940 - 480) * math.sin(math.pi * t / 0.6) ** 2
        else:
            hz = 940 - (940 - 400) * ((t - 0.3) / (seconds - 0.3)) ** 1.6
        shake = 1 + 0.025 * math.sin(6.2832 * 7.3 * t) + 0.015 * math.sin(6.2832 * 11.9 * t + 1.0)
        return hz * shake / 700

    amps = harmonics(700, AH_SCREAM, 6000, 1.0)
    for cents, pan in ((-12, -0.3), (0, 0.0), (14, 0.3)):
        sing(left, right, 700 * 2 ** (cents / 1200), seconds, amps, 1.0, pan, (6.5, 0.01), growl=0.45, bend=contour)
    for i in range(n):
        t = i / SYNTH_SR
        e = min(t / 0.12, 1) * math.sin(math.pi * min(t / seconds, 1)) ** 0.6
        left[i] *= e
        right[i] *= e
    return left, right


def room(name, left, right, ir, total, tail, wet, dry, muffle=0):
    """Put a voice in the room, and write it out. `muffle` is where the water cuts the top off, in hz, or 0."""
    raw = os.path.join(TMP, name + "-raw.wav")
    out = os.path.join(TMP, name + ".wav")
    pad = [0.0] * int(SYNTH_SR * total - len(left) + 1)
    write_wav(raw, left + pad, right + pad, SYNTH_SR)
    cut = ("lowpass=f=%d,lowpass=f=%d," % (muffle, muffle)) if muffle else ""
    # afir's "dry" is how much of the sound goes into the echo, and not how much of it comes out untouched,
    # so the untouched sound is mixed back in by hand. its "wet" undoes the 8 that make_ir multiplied by.
    # everything is turned down at the end, to keep it from clipping. it is leveled afterwards anyway.
    graph = ("[0]aresample=%d,asplit[d][s];[s][1]afir=dry=1:wet=0.125:gtype=-1:irnorm=-1[w];"
             "[d][w]amix=inputs=2:weights=%g %g:normalize=0,%svolume=0.35,atrim=0:%g,afade=t=out:st=%g:d=%g:curve=qua"
             % (SR, dry, wet, cut, total, total - tail, tail))
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", raw, "-i", ir, "-filter_complex", graph, out], check=True)
    return read_wav(out)


def rms(frames):
    return math.sqrt(sum(v * v for v in frames) / len(frames))


def save(name, frames, gain):
    """Write the mp3. `gain` is what the sound is multiplied by, to bring it to the level that it should have."""
    wav = os.path.join(TMP, name + "-out.wav")
    scaled = array.array("h", (int(max(-32767, min(32767, v * gain))) for v in frames))
    with wave.open(wav, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(scaled.tobytes())
    mp3 = os.path.join(OUT, name + ".mp3")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", wav, "-codec:a", "libmp3lame", "-q:a", "5", mp3], check=True)
    level = 20 * math.log10(max(rms(scaled), 1) / 32767)
    print("%-14s %5.1fs  rms %6.1f dB" % (name, len(scaled) / 2 / SR, level))


ir_long = os.path.join(TMP, "ir-long.wav")
ir_dark = os.path.join(TMP, "ir-dark.wav")
make_ir(ir_long, 3.2, 0.55)
make_ir(ir_dark, 4.5, 0.85)

# an instrument. the echo colors every pitch, so the notes are leveled by how loud they are on the
# whole, and then all of them are turned down together until the loudest peak is at -3 dB.
# each one starts from the same dice every time, so making one again does not change it.
ir_far = os.path.join(TMP, "ir-far.wav")
make_ir(ir_far, 5.0, 0.4)

for instrument, make, ir, total, tail, wet, dry, muffle, seed in (
        ("choir", choir, ir_long, 7.0, 2.5, 0.5, 0.7, 0, 23),
        ("wail", wail, ir_dark, 7.5, 3.0, 0.8, 0.45, 1300, 29),
        ("solo", solo, ir_far, 7.5, 3.5, 0.55, 0.75, 0, 37)):
    if ONLY and instrument not in ONLY:
        continue
    if instrument == "solo" and not ONLY:
        continue
    rng = random.Random(seed)
    raw = []
    for name, f0 in NOTES:
        left, right = make(f0)
        raw.append(room("%s-%s" % (instrument, name), left, right, ir, total, tail, wet, dry, muffle))
    gains = [1 / max(rms(x), 1) for x in raw]
    peak = max(max(abs(v) for v in x) * g for x, g in zip(raw, gains))
    for (name, _), x, g in zip(NOTES, raw, gains):
        save("%s-%s" % (instrument, name), x, g / peak * 32767 * 10 ** (-3 / 20))

if not ONLY or "scream" in ONLY:
    rng = random.Random(31)
    left, right = scream()
    far = room("shot-scream", left, right, ir_dark, 7.5, 3.5, 1.0, 0.25, 650)
    save("shot-scream", far, 32767 * 10 ** (-3 / 20) / max(abs(v) for v in far))
