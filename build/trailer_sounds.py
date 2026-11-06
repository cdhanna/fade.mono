"""
Makes the sounds that the trailer has and the game does not, into build/trailer-sounds:

    whoosh-up.wav     air rushing past, rising, for the view going up out of the sea and into the sky
    space-drone.wav   the sound of space: a low, slow, beating hum, with a faint shimmer high over it
    cq-distant.wav    CQ in morse, far away, as if on a radio: the same dots and dashes, at the same
                      speed, as the shooting star of the end card (MORSE_STAR_UNIT_MS is 95 there), so
                      that it can be laid under the star. the first dash starts on the first sample.

    py -3 build/trailer_sounds.py

Only the standard library. A score of build/music uses them with "file": "build/trailer-sounds/...".
"""
import math
import os
import random
import struct
import wave

RATE = 48000
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "trailer-sounds")


def write(name, left, right=None):
    right = right or left
    peak = max(max(abs(v) for v in left), max(abs(v) for v in right), 1e-9)
    gain = 0.89 / peak
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name), "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(RATE)
        frames = bytearray()
        for l, r in zip(left, right):
            frames += struct.pack("<hh", int(l * gain * 32767), int(r * gain * 32767))
        f.writeframes(bytes(frames))
    print(name, f"{len(left) / RATE:.2f} s")


def bandpass(samples, centre_at, q):
    """a band pass whose centre can move: centre_at(i) is the centre at sample i, in Hz"""
    out = []
    x1 = x2 = y1 = y2 = 0.0
    for i, x in enumerate(samples):
        w = 2 * math.pi * centre_at(i) / RATE
        alpha = math.sin(w) / (2 * q)
        b0, b2 = alpha, -alpha
        a0, a1, a2 = 1 + alpha, -2 * math.cos(w), 1 - alpha
        y = (b0 * x + b2 * x2 - a1 * y1 - a2 * y2) / a0
        x2, x1, y2, y1 = x1, x, y1, y
        out.append(y)
    return out


def lowpass(samples, hz):
    k = 1 - math.exp(-2 * math.pi * hz / RATE)
    out, y = [], 0.0
    for x in samples:
        y += k * (x - y)
        out.append(y)
    return out


def whoosh():
    random.seed(3)
    length = int(RATE * 3.2)
    noise = [random.uniform(-1, 1) for _ in range(length)]
    # the air rises: the band climbs from low to high, and swells up and away
    sweep = lambda i: 220 * (12 ** (i / length) ** 1.4)
    body = bandpass(noise, sweep, 1.6)
    hiss = bandpass(noise, lambda i: sweep(i) * 2.4, 3.0)
    left, right = [], []
    for i in range(length):
        t = i / length
        swell = math.sin(math.pi * min(1.0, t * 1.15)) ** 1.6 if t < 0.87 else 0.0
        v = (body[i] + 0.35 * hiss[i]) * swell
        pan = -0.5 + t  # it goes past, from the left to the right
        left.append(v * (1 - pan) / 1.5)
        right.append(v * (1 + pan) / 1.5)
    write("whoosh-up.wav", left, right)


def drone():
    random.seed(5)
    length = int(RATE * 16)
    tones = [(73.42, 0.5), (73.62, 0.4), (110.0, 0.35), (110.3, 0.3), (146.83, 0.18), (220.4, 0.08)]
    shimmer = [(1174.7, 0.012), (1760.0, 0.009), (2349.3, 0.006)]
    noise = lowpass([random.uniform(-1, 1) for _ in range(length)], 300)
    left, right = [], []
    for i in range(length):
        t = i / RATE
        v = sum(a * math.sin(2 * math.pi * f * t) for f, a in tones)
        v *= 0.75 + 0.25 * math.sin(2 * math.pi * 0.07 * t)
        s = sum(a * math.sin(2 * math.pi * f * t + k) * (0.5 + 0.5 * math.sin(2 * math.pi * (0.11 + k * 0.05) * t + k))
                for k, (f, a) in enumerate(shimmer))
        fade = min(1.0, t / 3.0) * min(1.0, (16 - t) / 3.0)
        n = noise[i] * 0.6
        left.append((v + s + n) * fade)
        right.append((v + s * -1 + n) * fade)
    write("space-drone.wav", left, right)


def cq():
    unit = 0.095
    # the steps of the star's trail: a dot is one step, a dash three, a step of nothing after each,
    # and four more after a letter (MORSE_STAR_LETTER_GAP)
    marks = []
    for letter in ("-.-.", "--.-"):
        for sign in letter:
            marks.append(3 if sign == "-" else 1)
            marks.append(-1)
        marks.append(-4)
    length = int(RATE * (sum(abs(m) for m in marks) * unit + 1.2))
    dry = [0.0] * length
    at = 0
    for m in marks:
        steps = int(abs(m) * unit * RATE)
        if m > 0:
            edge = int(RATE * 0.006)
            for i in range(steps):
                t = (at + i) / RATE
                env = min(1.0, i / edge, (steps - i) / edge)
                wobble = 1 + 0.002 * math.sin(2 * math.pi * 5 * t)
                dry[at + i] = env * math.sin(2 * math.pi * 660 * wobble * t)
        at += steps
    # far away, on a radio: only the middle of it, a little hiss, and two echoes
    random.seed(9)
    band = bandpass(dry, lambda i: 900, 1.2)
    hiss = lowpass([random.uniform(-1, 1) * 0.04 for _ in range(length)], 3000)
    out = []
    for i in range(length):
        v = band[i] + hiss[i]
        for delay, gain in ((0.21, 0.35), (0.42, 0.15)):
            j = i - int(delay * RATE)
            if j >= 0:
                v += band[j] * gain
        out.append(v)
    write("cq-distant.wav", out)


if __name__ == "__main__":
    whoosh()
    drone()
    cq()
