"""
The second batch of audition takes for the sound under the bar of the round: the climb of
b-glide-picks-up (see bar_climb_gen.py), made into a force of the ocean that charges up over the
whole round, and lets go when the bar fills.

    py Tools/bar_charge_gen.py                from the Fade.MonoGame folder

It writes nothing into the game. each sample, in ../bar-climb-samples, is a whole round over the
game's D minor chord and the water: combos land their points in the bar until it is full. what
makes them charge up:

  - the climb only moves while points land, as in b, and it goes faster and higher the fuller the
    bar is. (set sfx pitch does that in the game. a faster loop is a higher one, and climbs faster.)
  - it is louder and brighter the fuller the bar is, and it does not die away all the way between
    combos once the bar is past half: it holds, quietly, like something waiting.
  - a deep swell of the sea under it, which only grows over the round.
  - when the bar fills, it rushes up and breaks: a deep boom, and the wave of bubbles from the menu.

    e-charge         the tones of b, with all of the above
    f-surge          the climb made out of the water instead of tones: the recording of being under
                     the water, heard through a filter that climbs like the Shepard tone does
    g-force          e and f together: the tones riding on the climbing water
    h-force-held     g, but it never goes quiet between combos, once it has started. it only rests
                     at a hum that is stronger the fuller the bar is

    take-surge-loop  the climbing water, as the loop that the game would play

Needs numpy, and ffmpeg on the path.
"""
import os

import numpy as np

from bar_climb_gen import (SR, MUSIC, AUDIO, OUT, LAND_MS, rng, glide_loop, reverb, activity,
                           load_mp3, fit, save, stereo)

# the round: when each combo starts, how many fish it pops, and how far apart. the last one fills the bar.
COMBOS = [(0.6, 4, 0.12), (3.2, 7, 0.1), (4.5, 3, 0.12), (7.6, 10, 0.09), (9.0, 5, 0.1),
          (12.4, 12, 0.08), (13.8, 6, 0.09), (16.2, 16, 0.075)]
TAIL = 6.0          # how long the sample goes on after the bar fills
OCTAVE_S = 16.0     # how long the loop takes to climb an octave, played at its own pitch
FAST = 0.45         # how many octaves higher it plays when the bar is full
HOLD_FROM = 0.5     # how full the bar is when it starts to hold between combos


def landings():
    times = []
    for start, count, gap in COMBOS:
        for i in range(count):
            # later fish of a combo are worth more, as in the game
            times.append((start + i * gap + LAND_MS, 1 + i * 0.25))
    return sorted(times)


def fill_curve(lands, n):
    """how full the bar is, from 0 to 1. it steps up as each one lands, and slides up over 90 ms,
    as the bar in the game does."""
    total = sum(p for _, p in lands)
    f = np.zeros(n)
    got = 0.0
    for tm, p in lands:
        got += p
        f[int(tm * SR):] = got / total
    k = int(SR * 0.09)
    return np.convolve(np.pad(f, (k, 0), mode="edge"), np.ones(k) / k, mode="valid")[:n]


def surge_loop():
    """the water, climbing: the recording of being under the water, through a set of bands an octave
    apart that rise an octave over the loop, loudest in the middle, as the parts of a Shepard tone are.
    it is cut one blend longer than the loop, and the end is laid over the start, so it has no seam."""
    water = load_mp3(os.path.join(AUDIO, "underwater-ambiencewav.mp3"))
    blend = 1.5
    n = int(SR * OCTAVE_S)
    m = int(SR * blend)
    start = int(SR * 30)
    x = water[start:start + n + m]
    # louder: the recording is a quiet one
    x = x / np.sqrt(np.mean(x ** 2)) * 0.1

    frame, hop = 4096, 1024
    win = np.hanning(frame)
    freqs = np.fft.rfftfreq(frame, 1 / SR)
    logf = np.log2(np.maximum(freqs, 1))
    out = np.zeros_like(x)
    norm = np.zeros(len(x))
    low = np.log2(55.0)
    for i in range(0, len(x) - frame, hop):
        pos = ((i + frame / 2) / n) % 1  # how far up the octave it is
        gain = np.zeros(len(freqs))
        for k in range(7):
            c = low + k + pos
            hump = np.exp(-0.5 * ((c - np.log2(330)) / 1.1) ** 2)
            gain += hump * np.exp(-0.5 * ((logf - c) / 0.07) ** 2)
        for ch in range(2):
            spec = np.fft.rfft(x[i:i + frame, ch] * win) * gain
            out[i:i + frame, ch] += np.fft.irfft(spec, frame) * win
        norm[i:i + frame] += win ** 2
    out /= np.maximum(norm, 1e-3)[:, None]
    loop = out[:n].copy()
    ramp = np.linspace(0, 1, m)[:, None]
    loop[:m] = loop[:m] * ramp + out[n:n + m] * (1 - ramp)
    loop = reverb(loop, wet=0.5, dry=0.7, circular=True)
    return loop / np.max(np.abs(loop))


def ride(loop, rate, moving):
    """play a loop the way the game would: paused while it is quiet, and faster (and higher) as
    the rate goes up. the place in the loop moves on by the rate on every sample that it plays."""
    step = np.where(moving, rate, 0.0)
    head = np.cumsum(step) % len(loop)
    i = head.astype(int)
    j = (i + 1) % len(loop)
    frac = (head - i)[:, None]
    return loop[i] * (1 - frac) + loop[j] * frac


def brighten(x, amount):
    """a soft top that opens up as amount goes from 0 to 1: the sound with its top taken off, and the
    top mixed back in by the amount"""
    fr = np.fft.rfftfreq(len(x), 1 / SR)
    keep = 1 / np.sqrt(1 + (fr / 700) ** 2)
    y = np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * keep, len(x)) for c in range(2)], axis=1)
    return y + (x - y) * amount[:, None]


def deep_swell(n, fill):
    """a slow heave of the sea under it all: low D and A, beating against each other, and dark noise
    that rolls. it grows with the bar."""
    t = np.arange(n) / SR
    tone = 0.0
    for f, a in ((36.71, 1.0), (36.9, 0.8), (55.0, 0.45), (73.42, 0.2)):
        tone = tone + a * np.sin(2 * np.pi * f * t + rng.random() * 6.28)
    noise = rng.standard_normal(n)
    spec = np.fft.rfft(noise)
    fr = np.fft.rfftfreq(n, 1 / SR)
    spec *= 1 / (1 + (fr / 140) ** 4)
    noise = np.fft.irfft(spec, n)
    noise /= np.max(np.abs(noise))
    roll = 0.6 + 0.4 * np.sin(2 * np.pi * t / 5.3)
    x = (tone / 2.4 + 0.8 * noise) * roll * fill ** 1.5
    return stereo(x)


def release(n, at):
    """the bar is full: a deep boom that falls, and the wave of bubbles"""
    out = np.zeros((n, 2))
    s = int(at * SR)
    m = int(SR * 4)
    t = np.arange(m) / SR
    f = 30 + 40 * np.exp(-t / 0.25)
    boom = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 1.2) * np.minimum(t / 0.01, 1)
    out[s:s + m] += stereo(boom)[:n - s] * 0.9
    wave = load_mp3(os.path.join(AUDIO, "bubble-wave.mp3")) * 0.6
    j = min(n, s + len(wave))
    out[s:j] += wave[:j - s]
    return reverb(out, wet=0.35, dry=0.8)[:n]


def main():
    lands = landings()
    full_at = lands[-1][0] + 0.1
    length = full_at + TAIL
    n = int(SR * length)

    pad = fit(load_mp3(os.path.join(MUSIC, "loop-pad-dm.mp3")), n) * 0.45
    amb = fit(load_mp3(os.path.join(MUSIC, "loop-ambience.mp3")), n)
    under = 0.5 * (pad + amb)
    pop = load_mp3(os.path.join(AUDIO, "bubble-pop-2.mp3")) * 0.35
    for tm, _ in lands:
        i = int((tm - LAND_MS) * SR)
        j = min(n, i + len(pop))
        under[i:j] += pop[:j - i] * rng.uniform(0.7, 1.0)

    fill = fill_curve(lands, n)
    t = np.arange(n) / SR
    before = t < full_at
    level = activity([tm for tm, _ in lands], length, attack=0.3, release=1.8, hold=0.5)
    # past half full, it holds between combos, more the fuller the bar
    held = np.clip((fill - HOLD_FROM) / (1 - HOLD_FROM), 0, 1) * 0.35
    level = np.maximum(level, held)
    # the rush: over the last 0.7 s before it breaks, it comes all the way up, and then it lets go
    rush = np.clip(1 - (full_at - t) / 0.7, 0, 1) * before
    level = np.maximum(level, rush)
    after = np.clip(1 - (t - full_at) / 1.4, 0, 1)
    level = np.where(before, level, after * level[int(full_at * SR) - 1])
    loud = level * (0.55 + 0.45 * fill)

    rate = 2 ** (FAST * fill + 0.5 * rush)
    moving = level > 0.01
    swell = deep_swell(n, fill) * np.where(before, 1, after)[:, None]
    boom = release(n, full_at)

    tones = ride(glide_loop(OCTAVE_S), rate, moving)
    tones = brighten(tones, 0.25 + 0.75 * fill) * loud[:, None]

    surge = surge_loop()
    save("take-surge-loop", surge)
    water = ride(surge, rate, moving) * loud[:, None]

    def mix(name, parts):
        save(name, under + sum(parts) + 0.12 * swell + 0.5 * boom)

    mix("e-charge", [0.24 * tones])
    mix("f-surge", [0.3 * water])
    mix("g-force", [0.17 * tones, 0.24 * water])

    # h: once it has started, it never goes all the way quiet
    level_h = np.where(before & (t > lands[0][0]), np.maximum(level, 0.2 + 0.3 * fill), level)
    loud_h = level_h * (0.55 + 0.45 * fill)
    moving_h = level_h > 0.01
    tones_h = brighten(ride(glide_loop(OCTAVE_S), rate, moving_h), 0.25 + 0.75 * fill) * loud_h[:, None]
    water_h = ride(surge, rate, moving_h) * loud_h[:, None]
    mix("h-force-held", [0.17 * tones_h, 0.24 * water_h])

    print("wrote", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
