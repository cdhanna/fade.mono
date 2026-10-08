"""Renders the scuba audition batch: underwater loops and one-shots, and a demo mix over the existing stems."""
import os
import sys

import numpy as np
from scipy.io import wavfile
from scipy.signal import fftconvolve

SR = 44100
OUT = sys.argv[1]
OLD = sys.argv[2]  # the stems that the game already uses, for the demo mix
rng = np.random.default_rng(21)
report = []
BREATH = 0 if "nobreath" in sys.argv else 1  # leave the breathing out of the demo mix


def band(f, lo, hi, slope=2):
    f = np.maximum(f, 1e-6)
    return 1 / np.sqrt(1 + (lo / f) ** (2 * slope)) / np.sqrt(1 + (f / hi) ** (2 * slope))


def shaped_noise(n, shape_fn):
    f = np.fft.rfftfreq(n, 1 / SR)
    spec = (rng.standard_normal(len(f)) + 1j * rng.standard_normal(len(f))) * shape_fn(f)
    spec[0] = 0
    x = np.fft.irfft(spec, n)
    return x / np.max(np.abs(x))


def muffle(x, cutoff, slope=2):
    """Take the top end off, the way that water does. it wraps around, so a loop stays a loop."""
    f = np.fft.rfftfreq(len(x), 1 / SR)
    gain = 1 / np.sqrt(1 + (f / cutoff) ** (2 * slope))
    if x.ndim == 1:
        return np.fft.irfft(np.fft.rfft(x) * gain, len(x))
    return np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * gain, len(x)) for c in range(2)], axis=1)


def lfo(n, cycles, phase=0.0):
    return np.sin(2 * np.pi * (cycles * np.arange(n) / n + phase))


def make_ir(seconds, lo=120, hi=2500):
    n = int(SR * seconds)
    t = np.arange(n) / SR
    chans = []
    for _ in range(2):
        x = shaped_noise(n, lambda f: band(f, lo, hi)) * np.exp(-6.9 * t / seconds)
        chans.append(x / np.sqrt(np.sum(x ** 2)))
    return np.stack(chans, axis=1)


def stereo(x):
    return x if x.ndim == 2 else np.stack([x, x], axis=1)


def pan(x, p):
    a = (p + 1) * np.pi / 4
    return np.stack([x * np.cos(a), x * np.sin(a)], axis=1)


def reverb(x, ir, wet, dry, circular=False):
    x = stereo(x)
    if circular:
        n = len(x)
        pad_ir = np.zeros((n, 2))
        pad_ir[:len(ir)] = ir
        w = np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * np.fft.rfft(pad_ir[:, c]), n) for c in range(2)], axis=1)
        return dry * x + wet * w
    w = np.stack([fftconvolve(x[:, c], ir[:, c]) for c in range(2)], axis=1)
    out = wet * w
    out[:len(x)] += dry * x
    return out


def fade_tail(x, seconds):
    n = int(SR * seconds)
    x = x.copy()
    x[-n:] *= np.linspace(1, 0, n)[:, None] ** 2
    return x


def save(name, x, peak_db=-3.0, loop=False):
    x = stereo(x)
    x = x / np.max(np.abs(x)) * 10 ** (peak_db / 20)
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (x * 32767).astype(np.int16))
    rms = 20 * np.log10(np.sqrt(np.mean(x ** 2)))
    seam = ""
    if loop:
        jump = np.max(np.abs(x[0] - x[-1]))
        typical = np.percentile(np.abs(np.diff(x, axis=0)), 99.9)
        seam = "seam ok" if jump <= typical * 1.5 else "SEAM CLICK (%.4f vs %.4f)" % (jump, typical)
    report.append("%-22s %6.1fs  rms %6.1f dB  %s" % (name, len(x) / SR, rms, seam))
    return x


def bubble(f0, dur, glide=1.2):
    """One bubble. it rings at a pitch that depends on how big it is, and the pitch creeps up as it rises."""
    n = int(SR * dur)
    t = np.arange(n) / SR
    f = f0 * glide ** (t / dur)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / (dur * 0.3)) * np.minimum(t / 0.002, 1)


def place(out, clip, at, wrap=False):
    """Add a stereo clip into a buffer, starting at a time in seconds. a loop wraps around its end."""
    start = int(SR * at)
    if wrap:
        idx = (start + np.arange(len(clip))) % len(out)
        out[idx] += clip
    else:
        clip = clip[:len(out) - start]
        out[start:start + len(clip)] += clip


def window(n, start, rise, hold, fall):
    """An envelope that comes up, holds, and goes away, wrapped around a loop of n samples."""
    t = (np.arange(n) / SR - start) % (n / SR)
    env = np.zeros(n)
    up = t < rise
    env[up] = np.sin(np.pi * t[up] / (2 * rise)) ** 2
    mid = (t >= rise) & (t < rise + hold)
    env[mid] = 1
    down = (t >= rise + hold) & (t < rise + hold + fall)
    env[down] = np.cos(np.pi * (t[down] - rise - hold) / (2 * fall)) ** 2
    return env


# ---------- loops ----------

def breath(seconds=24, breaths=4):
    """Breathing through a regulator: a hiss in, a pause, and a rush of big bubbles out."""
    n = SR * seconds
    cycle = seconds / breaths
    inhale_env = np.zeros(n)
    gurgle_env = np.zeros(n)
    out = np.zeros((n, 2))
    for b in range(breaths):
        t0 = b * cycle + rng.uniform(0, 0.4)
        inhale = rng.uniform(1.5, 1.9)
        exhale = rng.uniform(2.1, 2.7)
        inhale_env += window(n, t0, 0.25, inhale - 0.6, 0.35) * rng.uniform(0.8, 1.0)
        t1 = t0 + inhale + rng.uniform(0.5, 0.8)
        gurgle_env += window(n, t1, 0.12, exhale * 0.35, exhale * 0.65) * rng.uniform(0.8, 1.0)

        # the bubbles come thick and fast at the start of the breath out, and thin out
        for _ in range(int(rng.integers(90, 130))):
            at = t1 + exhale * rng.beta(1.2, 2.6)
            loud = rng.uniform(0.2, 1.0) * (1 - 0.6 * (at - t1) / exhale)
            clip = bubble(rng.uniform(120, 650), rng.uniform(0.04, 0.14)) * loud
            place(out, pan(clip, rng.uniform(-0.7, 0.7)), at, wrap=True)

    # the hiss of the air coming in. it is mostly one band, like air through a small valve.
    hiss = shaped_noise(n, lambda f: band(f, 700, 2600) + 1.5 * band(f, 1000, 1400, 4))
    out += pan(hiss * inhale_env, 0) * 1.0

    # the low churn of the water as the bubbles leave
    flutter = np.abs(shaped_noise(n, lambda f: band(f, 6, 28)))
    churn = shaped_noise(n, lambda f: band(f, 60, 380))
    out += stereo(churn * gurgle_env * (0.3 + 0.7 * flutter)) * 0.9

    return reverb(muffle(out, 2400), make_ir(1.2), wet=0.25, dry=0.9, circular=True)


def trickle(seconds=16):
    """A thin stream of small bubbles going by."""
    n = SR * seconds
    out = np.zeros((n, 2))
    density = 0.5 + 0.5 * lfo(n, 2, rng.random())  # they come in drifts
    for _ in range(seconds * 14):
        at = rng.uniform(0, seconds)
        if rng.random() > 0.25 + 0.75 * density[int(at * SR) % n]:
            continue
        clip = bubble(rng.uniform(700, 2600), rng.uniform(0.02, 0.06), glide=1.35) * rng.uniform(0.15, 1.0) ** 2
        place(out, pan(clip, rng.uniform(-0.9, 0.9)), at, wrap=True)
    return reverb(muffle(out, 3200), make_ir(1.5), wet=0.4, dry=0.8, circular=True)


def deep(seconds=20):
    """The weight of the water: a low bed with a slow throb in it, and no hiss at all."""
    n = SR * seconds
    chans = []
    mono = shaped_noise(n, lambda f: band(f, 28, 130) / np.maximum(f, 20))
    for c in range(2):
        side = shaped_noise(n, lambda f: band(f, 28, 130) / np.maximum(f, 20))
        hum = shaped_noise(n, lambda f: band(f, 85, 100, 6)) * (0.5 + 0.5 * lfo(n, 3, 0.2 * c))
        chans.append(0.65 * mono + 0.35 * side + 0.25 * hum)
    x = np.stack(chans, axis=1)
    throb = 1 + 0.3 * lfo(n, 4, rng.random()) + 0.15 * lfo(n, 1, rng.random())
    return muffle(x * throb[:, None], 300)


def crackle(seconds=20):
    """The crackle of a reef: thousands of tiny snaps, far away."""
    n = SR * seconds
    out = np.zeros((n, 2))
    click_n = int(SR * 0.004)
    t = np.arange(click_n) / SR
    for _ in range(seconds * 30):
        click = rng.standard_normal(click_n) * np.exp(-t / 0.0007)
        loud = rng.uniform(0.05, 1.0) ** 3
        place(out, pan(click * loud, rng.uniform(-1, 1)), rng.uniform(0, seconds), wrap=True)
    f_shape = lambda x: muffle(x, 4500)
    out = f_shape(out)
    # take the bottom off too, so that it is only the snap
    f = np.fft.rfftfreq(n, 1 / SR)
    hp = 1 / np.sqrt(1 + (1200 / np.maximum(f, 1e-6)) ** 4)
    out = np.stack([np.fft.irfft(np.fft.rfft(out[:, c]) * hp, n) for c in range(2)], axis=1)
    return reverb(out, make_ir(2.0, lo=800, hi=5000), wet=0.5, dry=0.7, circular=True)


# ---------- one-shots ----------

def exhale_burst():
    seconds = 2.6
    out = np.zeros((int(SR * (seconds + 0.5)), 2))
    for _ in range(170):
        at = seconds * rng.beta(1.1, 2.4)
        clip = bubble(rng.uniform(110, 700), rng.uniform(0.04, 0.16)) * rng.uniform(0.2, 1.0) * (1 - 0.6 * at / seconds)
        place(out, pan(clip, rng.uniform(-0.8, 0.8)), at)
    n = len(out)
    churn = shaped_noise(n, lambda f: band(f, 60, 380)) * (0.3 + 0.7 * np.abs(shaped_noise(n, lambda f: band(f, 6, 28))))
    env = np.clip(1 - np.arange(n) / (SR * seconds), 0, 1) ** 1.5 * np.minimum(np.arange(n) / (SR * 0.08), 1)
    out += stereo(churn * env) * 0.9
    return fade_tail(reverb(muffle(out, 2400), make_ir(1.5), wet=0.3, dry=0.9), 0.6)


def tank_clink():
    """Something metal knocking against the tank. a dull ring, because of the water."""
    n = int(SR * 1.2)
    t = np.arange(n) / SR
    x = np.zeros(n)
    for f, a, tau in ((1130, 1.0, 0.22), (1790, 0.7, 0.14), (2810, 0.5, 0.08), (3940, 0.3, 0.05), (640, 0.4, 0.1)):
        x += a * np.sin(2 * np.pi * f * t + rng.random() * 6.28) * np.exp(-t / tau)
    x *= np.minimum(t / 0.0008, 1)
    out = np.zeros((int(SR * 1.6), 2))
    place(out, pan(x, -0.2), 0)
    place(out, pan(x * 0.5, 0.2), 0.19)  # it knocks twice
    return fade_tail(reverb(muffle(out, 3000), make_ir(2.5), wet=0.5, dry=0.8)[:int(SR * 4)], 1.5)


def boat_pass(seconds=14):
    """A boat going by overhead, a long way up. an engine drone and the beat of a propeller."""
    n = int(SR * seconds)
    t = np.arange(n) / SR
    u = t / seconds
    f = 58 * (1.03 - 0.06 * u)  # it drops a little in pitch as it goes by
    phase = 2 * np.pi * np.cumsum(f) / SR
    x = np.zeros(n)
    for h in range(1, 9):
        x += np.sin(h * phase + rng.random() * 6.28) / h ** 1.1
    prop = 0.6 + 0.4 * np.sin(2 * np.pi * 9.5 * t) ** 2
    wash = shaped_noise(n, lambda fr: band(fr, 80, 500)) * 0.5
    env = np.sin(np.pi * u) ** 2.5
    out = pan((x * prop + wash) * env, 0) * np.stack([np.cos(u * np.pi / 2), np.sin(u * np.pi / 2)], axis=1)  # it crosses over
    return fade_tail(reverb(muffle(out, 450), make_ir(3.0, hi=800), wet=0.4, dry=0.8)[:n], 2)


# ---------- render ----------

loops = {
    "loop_breath": save("loop_breath", breath(), loop=True),
    "loop_trickle": save("loop_trickle", trickle(), loop=True),
    "loop_deep": save("loop_deep", deep(), loop=True),
    "loop_crackle": save("loop_crackle", crackle(), loop=True),
}
shots = {
    "shot_exhale": save("shot_exhale", exhale_burst()),
    "shot_tank_clink": save("shot_tank_clink", tank_clink()),
    "shot_boat_pass": save("shot_boat_pass", boat_pass()),
}


def old(name):
    sr, x = wavfile.read(os.path.join(OLD, name + ".wav"))
    return x / 32768.0


def curve(n, points):
    t = np.arange(n) / SR
    return np.interp(t, [p[0] for p in points], [p[1] for p in points])[:, None]


def tiled(x, n):
    return np.tile(x, (n // len(x) + 1, 1))[:n]


def play(mix, instrument, beat, at, gain, bars):
    step = 0
    for bar in bars:
        for name in bar.split():
            if name != "-":
                clip = old("%s_%s" % (instrument, name))
                place(mix, clip * gain * rng.uniform(0.8, 1.0), at + step * beat)
            step += 1


n = SR * 80
mix = np.zeros((n, 2))
mix += tiled(loops["loop_deep"], n) * 0.7 * curve(n, ((0, 0), (5, 1), (74, 1), (80, 0)))
mix += tiled(loops["loop_breath"], n) * 0.3 * BREATH * curve(n, ((3, 0), (9, 1), (34, 1), (42, 0.25), (58, 0.25), (64, 1), (74, 1), (80, 0)))
mix += tiled(loops["loop_trickle"], n) * 0.22 * curve(n, ((10, 0), (20, 1), (50, 1), (60, 0.3), (80, 0)))
mix += tiled(loops["loop_crackle"], n) * 0.12 * curve(n, ((20, 0), (32, 1), (70, 1), (80, 0)))
mix += tiled(old("loop_pad_dm"), n) * 0.4 * curve(n, ((8, 0), (18, 1), (40, 1), (46, 0), (80, 0)))
mix += tiled(old("loop_pad_bb"), n) * 0.4 * curve(n, ((40, 0), (46, 1), (72, 1), (80, 0)))
place(mix, old("shot_whale_low") * 0.28, 14)
place(mix, shots["shot_tank_clink"] * 0.18, 36)
place(mix, shots["shot_boat_pass"] * 0.35, 44)
place(mix, old("shot_ping") * 0.18, 60)
place(mix, shots["shot_exhale"] * 0.25 * BREATH, 68)

# the first four bars of clair de lune, on the bell, and then the tumbling tune from the arabesque, on the pluck
play(mix, "note", 0.52, 20, 0.26, ("- C5 C6 - - - A5 - -", "- G5 A5 G5 - - - - -", "- F5 G5 F5 - A5 - - F5", "- E5 F5 E5 - - - - -"))
play(mix, "pluck", 0.26, 50, 0.26, ("F5 G5 D5 F5 C5 D5 A4 C5", "G4 A4 F4 A4 E4 - - -", "- - D4 - - - - -"))
save("demo_scuba_mix" if BREATH else "demo_scuba_no_breath", np.tanh(mix * 1.2), peak_db=-1)

print("\n".join(report))
