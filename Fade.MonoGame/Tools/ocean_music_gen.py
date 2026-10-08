"""Renders the ocean audition batch: seamless loops, one-shots, melodic notes and a demo mix."""
import os
import sys

import numpy as np
from scipy.io import wavfile
from scipy.signal import fftconvolve

SR = 44100
OUT = sys.argv[1]
rng = np.random.default_rng(7)
report = []


# ---------- helpers ----------

def band(f, lo, hi, slope=2):
    f = np.maximum(f, 1e-6)
    return 1 / np.sqrt(1 + (lo / f) ** (2 * slope)) / np.sqrt(1 + (f / hi) ** (2 * slope))


def shaped_noise(n, shape_fn):
    """Noise built in the frequency domain, so it is periodic over n samples (a seamless loop)."""
    f = np.fft.rfftfreq(n, 1 / SR)
    spec = (rng.standard_normal(len(f)) + 1j * rng.standard_normal(len(f))) * shape_fn(f)
    spec[0] = 0
    x = np.fft.irfft(spec, n)
    return x / np.max(np.abs(x))


def lfo(n, cycles, phase=0.0):
    """Sine with a whole number of cycles over n samples, so it loops."""
    return np.sin(2 * np.pi * (cycles * np.arange(n) / n + phase))


def uni(n, cycles, phase=0.0):
    return 0.5 + 0.5 * lfo(n, cycles, phase)


def make_ir(seconds, lo=150, hi=5000):
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
    """p from -1 (left) to 1 (right), equal power."""
    a = (p + 1) * np.pi / 4
    return np.stack([x * np.cos(a), x * np.sin(a)], axis=1)


def reverb(x, ir, wet=0.5, dry=0.6, circular=False):
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


def save(name, x, peak_db=-3.0, loop=False, ref_peak=None):
    x = stereo(x)
    x = x / (ref_peak or np.max(np.abs(x))) * 10 ** (peak_db / 20)
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (x * 32767).astype(np.int16))
    rms = 20 * np.log10(np.sqrt(np.mean(x ** 2)))
    seam = ""
    if loop:
        jump = np.max(np.abs(x[0] - x[-1]))
        typical = np.percentile(np.abs(np.diff(x, axis=0)), 99)
        seam = "seam ok" if jump <= typical * 1.5 else "SEAM CLICK (%.4f vs %.4f)" % (jump, typical)
    report.append("%-22s %6.1fs  rms %6.1f dB  %s" % (name, len(x) / SR, rms, seam))
    return x


# ---------- loops ----------

def rumble(seconds=20):
    n = SR * seconds
    shape = lambda f: band(f, 35, 200) / np.maximum(f, 20)
    mono = shaped_noise(n, shape)
    chans = [0.6 * mono + 0.4 * shaped_noise(n, shape) for _ in range(2)]
    x = np.stack(chans, axis=1)
    amp = 1 + 0.25 * lfo(n, 1, rng.random()) + 0.15 * lfo(n, 3, rng.random())
    return x * amp[:, None]


def wash(seconds=24):
    n = SR * seconds
    chans = []
    for c in range(2):
        dark = shaped_noise(n, lambda f: band(f, 150, 900))
        bright = shaped_noise(n, lambda f: band(f, 600, 3500) / np.sqrt(np.maximum(f, 20)))
        swell = 0.6 * uni(n, 3, 0.05 * c) ** 2 + 0.4 * uni(n, 5, 0.3 + 0.05 * c) ** 3
        chans.append(dark * (0.35 + 0.65 * uni(n, 2, 0.6)) * 0.7 + bright * swell ** 2 * 0.9)
    return np.stack(chans, axis=1)


def current(seconds=16):
    n = SR * seconds
    x = np.zeros((n, 2))
    for fc in (180, 310, 520, 800, 1250):
        b = shaped_noise(n, lambda f: band(f, fc * 0.85, fc * 1.18, 4))
        amp = uni(n, int(rng.integers(1, 5)), rng.random()) ** 2
        x += pan(b * amp * (300 / fc) ** 0.4, rng.uniform(-0.8, 0.8))
    return x


def pad(freqs, ir, seconds=32):
    n = SR * seconds
    t = np.arange(n) / SR
    x = np.zeros((n, 2))
    for f0 in freqs:
        for detune in (-0.0015, 0.0, 0.0015):
            voice = np.zeros(n)
            for h, ha in ((1, 1.0), (2, 0.3), (3, 0.1)):
                f = round(f0 * (1 + detune) * h * seconds) / seconds  # whole cycles per loop
                voice += ha * np.sin(2 * np.pi * (f * t + rng.random()))
            amp = 0.6 + 0.4 * lfo(n, int(rng.integers(1, 4)), rng.random())
            x += pan(voice * amp * (110 / f0) ** 0.35, rng.uniform(-0.7, 0.7))
    return reverb(x, ir, wet=0.6, dry=0.5, circular=True)


# ---------- one-shots ----------

def bell(f0, ir):
    n = int(SR * 3)
    t = np.arange(n) / SR
    x = np.zeros(n)
    for ratio, a, tau in ((1, 1.0, 0.9), (2.0, 0.35, 0.5), (3.01, 0.12, 0.25), (4.2, 0.06, 0.15)):
        x += a * np.sin(2 * np.pi * f0 * ratio * t) * np.exp(-t / tau)
    x *= np.minimum(t / 0.008, 1)
    return fade_tail(reverb(x, ir, wet=0.45, dry=0.7)[:int(SR * 5)], 1.5)


def pluck(f0, ir):
    """A plucked string, like a harp. the high harmonics die away first."""
    n = int(SR * 3)
    t = np.arange(n) / SR
    x = np.zeros(n)
    tau0 = 1.5 * (440 / f0) ** 0.4
    for h in range(1, 13):
        if f0 * h > 9000:
            break
        a = abs(np.sin(h * np.pi * 0.22)) / h ** 1.2  # plucked a bit off of the middle of the string
        x += a * np.sin(2 * np.pi * f0 * h * t) * np.exp(-t * (1 + 0.35 * (h - 1) ** 1.3) / tau0)
    x *= np.minimum(t / 0.002, 1)
    return fade_tail(reverb(x, ir, wet=0.35, dry=0.8)[:int(SR * 5)], 1.5)


def swell(f0, ir):
    """A soft tone with no attack. it fades in, holds, and fades away."""
    n = int(SR * 5)
    t = np.arange(n) / SR
    x = np.zeros((n, 2))
    for detune in (-0.002, 0.0, 0.002):
        voice = np.zeros(n)
        vibrato = 1 + 0.002 * np.sin(2 * np.pi * (4.5 * t + rng.random()))
        phase = 2 * np.pi * np.cumsum(f0 * (1 + detune) * vibrato) / SR + 2 * np.pi * rng.random()
        for h, a in ((1, 1.0), (2, 0.25), (3, 0.08)):
            voice += a * np.sin(h * phase)
        x += pan(voice, detune * 300)
    env = np.where(t < 1.1, np.sin(np.pi * t / 2.2) ** 2, np.exp(-(t - 1.1) / 1.3))
    return fade_tail(reverb(x * env[:, None], ir, wet=0.6, dry=0.5)[:int(SR * 6.5)], 2.5)


def ping(ir):
    n = int(SR * 2)
    t = np.arange(n) / SR
    f = 1320 * (1 - 0.01 * t)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.25) * np.minimum(t / 0.004, 1)
    out = np.zeros((int(SR * 4.5), 2))
    for k in range(6):
        start = int(SR * 0.45 * k)
        out[start:start + n] += pan(x * 0.45 ** k, 0.5 * (-1) ** k * (k > 0))
    return fade_tail(reverb(out, ir, wet=0.5, dry=0.6)[:int(SR * 7)], 2)


def whale(times, hz, ir):
    seconds = times[-1]
    n = int(SR * seconds)
    t = np.arange(n) / SR
    u = t / seconds
    f = np.interp(t, times, hz)
    k = int(SR * 0.3)
    f = np.convolve(np.pad(f, k, mode="edge"), np.hanning(k) / np.sum(np.hanning(k)), mode="same")[k:-k]
    f *= 1 + 0.02 * u * np.sin(2 * np.pi * 5 * t)
    phase = 2 * np.pi * np.cumsum(f) / SR
    x = np.zeros(n)
    for h, a in ((1, 1.0), (2, 0.5), (3, 0.3), (4, 0.15), (5, 0.08)):
        x += a * np.sin(h * phase) * (0.7 + 0.3 * np.sin(2 * np.pi * (0.4 * h * u + 0.1 * h)))
    x *= np.sin(np.pi * u) ** 1.5
    return fade_tail(reverb(x, ir, wet=0.7, dry=0.4)[:int(SR * (seconds + 5))], 3)


def bubbles(count, seconds, ir):
    out = np.zeros((int(SR * (seconds + 0.3)), 2))
    for _ in range(count):
        dur = rng.uniform(0.04, 0.09)
        n = int(SR * dur)
        t = np.arange(n) / SR
        f0 = rng.uniform(500, 1800)
        f = f0 * 1.6 ** (t / dur)
        b = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / (dur * 0.4)) * np.minimum(t / 0.002, 1)
        start = int(SR * rng.uniform(0, seconds))
        out[start:start + n] += pan(b * rng.uniform(0.4, 1), rng.uniform(-0.8, 0.8))
    return fade_tail(reverb(out, ir, wet=0.35, dry=0.8), 0.5)


# ---------- render ----------

ir_long = make_ir(6.0, hi=3500)
ir_mid = make_ir(3.5)
ir_short = make_ir(1.2, hi=6000)

loops = {
    "loop_rumble": save("loop_rumble", rumble(), loop=True),
    "loop_wash": save("loop_wash", wash(), loop=True),
    "loop_current": save("loop_current", current(), loop=True),
    # D minor add9, then Bb major 7. both share D and A, so they crossfade cleanly.
    "loop_pad_dm": save("loop_pad_dm", pad((73.42, 110.0, 146.83, 174.61, 220.0, 329.63), ir_long), loop=True),
    "loop_pad_bb": save("loop_pad_bb", pad((58.27, 87.31, 116.54, 146.83, 220.0, 349.23), ir_long), loop=True),
}

shots = {
    "shot_ping": save("shot_ping", ping(ir_long)),
    "shot_whale_low": save("shot_whale_low", whale((0, 1.2, 2.4, 3.6), (180, 320, 240, 140), ir_long)),
    "shot_whale_high": save("shot_whale_high", whale((0, 0.7, 1.5, 2.6), (400, 700, 500, 260), ir_long)),
    "shot_bubbles_sparse": save("shot_bubbles_sparse", bubbles(7, 1.6, ir_short)),
    "shot_bubbles_dense": save("shot_bubbles_dense", bubbles(22, 1.2, ir_short)),
}

# D minor pentatonic over two octaves. a melody is just a list of indexes into this scale.
SCALE = (("D4", 293.66), ("F4", 349.23), ("G4", 392.00), ("A4", 440.00), ("C5", 523.25),
         ("D5", 587.33), ("F5", 698.46), ("G5", 783.99), ("A5", 880.00), ("C6", 1046.50))
# the notes that fill the pentatonic scale in to all of F major, for tunes that need them
EXTRA = (("E4", 329.63), ("Bb4", 466.16), ("E5", 659.25), ("Bb5", 932.33))
raw_notes = [bell(f, ir_mid) for _, f in SCALE + EXTRA]
raw_notes = [x / np.sqrt(np.mean(x ** 2)) for x in raw_notes]  # the reverb colors each pitch, so level them by rms
note_peak = max(np.max(np.abs(x)) for x in raw_notes)
notes = [save("note_%s" % (SCALE + EXTRA)[i][0], x, ref_peak=note_peak) for i, x in enumerate(raw_notes)]

# the melody, in the abstract: (beat, scale index, loudness)
MELODY = ((0, 3, 1.0), (1, 5, 0.8), (2, 6, 0.9), (4, 5, 0.7), (5, 3, 0.8), (6, 2, 0.7),
          (8, 3, 0.9), (9, 5, 0.8), (10, 7, 1.0), (11, 6, 0.7), (13, 5, 0.8), (14, 3, 0.7), (16, 0, 1.0))
BEAT = 1.4


def place(mix, clip, at, gain=1.0):
    start = int(SR * at)
    clip = clip[:len(mix) - start]
    mix[start:start + len(clip)] += clip * gain


def curve(n, points):
    t = np.arange(n) / SR
    return np.interp(t, [p[0] for p in points], [p[1] for p in points])[:, None]


def tiled(x, n):
    return np.tile(x, (n // len(x) + 1, 1))[:n]


def play_melody(mix, at, gain):
    for beat, idx, vel in MELODY:
        place(mix, notes[idx], at + beat * BEAT, gain * vel)


n = SR * 30
mel = tiled(loops["loop_pad_dm"], n) * 0.5 * curve(n, ((0, 0), (3, 1), (26, 1), (30, 0)))
play_melody(mel, 2, 0.5)
save("demo_melody", mel, peak_db=-1)

n = SR * 70
mix = np.zeros((n, 2))
mix += tiled(loops["loop_rumble"], n) * 0.7 * curve(n, ((0, 0), (6, 1), (64, 1), (70, 0)))
mix += tiled(loops["loop_wash"], n) * 0.45 * curve(n, ((4, 0), (14, 1), (44, 0.5), (64, 0.5), (70, 0)))
mix += tiled(loops["loop_current"], n) * 0.35 * curve(n, ((18, 0), (28, 1), (46, 1), (56, 0)))
mix += tiled(loops["loop_pad_dm"], n) * 0.45 * curve(n, ((8, 0), (18, 1), (32, 1), (38, 0), (50, 0), (56, 1), (64, 1), (70, 0)))
mix += tiled(loops["loop_pad_bb"], n) * 0.45 * curve(n, ((32, 0), (38, 1), (50, 1), (56, 0)))
place(mix, shots["shot_ping"], 12, 0.25)
place(mix, shots["shot_bubbles_sparse"], 16, 0.2)
place(mix, shots["shot_whale_low"], 19, 0.3)
place(mix, shots["shot_bubbles_dense"], 34, 0.2)
place(mix, shots["shot_whale_high"], 44, 0.22)
place(mix, shots["shot_ping"], 52, 0.2)
place(mix, shots["shot_bubbles_sparse"], 60, 0.2)
play_melody(mix, 24, 0.28)
save("demo_mix", np.tanh(mix * 1.2), peak_db=-1)

print("\n".join(report))

# the other instruments that a melody can be played on. they are rendered last, so that
# adding one does not change the random numbers that the sounds above are made from.
for instrument, make, ir in (("pluck", pluck, ir_mid), ("swell", swell, ir_long)):
    raw = [make(f, ir) for _, f in SCALE + EXTRA]
    raw = [x / np.sqrt(np.mean(x ** 2)) for x in raw]
    peak = max(np.max(np.abs(x)) for x in raw)
    for (name, _), x in zip(SCALE + EXTRA, raw):
        save("%s_%s" % (instrument, name), x, ref_peak=peak)

print("\n".join(report[-28:]))
