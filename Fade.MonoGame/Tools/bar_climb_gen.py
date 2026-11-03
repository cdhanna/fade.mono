"""
Audition takes for a sound under the bar of the round as it fills: a soft pad built as a Shepard tone,
the sound that seems to climb forever, because each part of it fades in at the bottom, rises an
octave, and fades out at the top as the next one takes its place.

    py Tools/bar_climb_gen.py                 from the Fade.MonoGame folder

It writes nothing into the game. It writes the takes, and a sample of each one in the game, into
../bar-climb-samples. each sample is a stretch of a round over the game's D minor chord and the
water: the fish of a few combos pop, and the sound plays as their points land in the bar, which is
SCORE_FLY_LAND_MS (760 ms) after they pop.

    a-glide          one endless climb, an octave every 16 s, that swells up while points land and
                     dies away when they stop. it climbs the whole time, heard or not.
    b-glide-picks-up the same climb, but paused while it is quiet, so that each combo picks the climb
                     up where the last one left it, and a round is one long climb. (pause sfx and
                     resume sfx can do that in the game.)
    c-steps          the climb in steps, in the key: each landing moves it up one note of D minor, as a
                     soft Shepard chord, no faster than a step every 300 ms. it never leaves the key.
    d-steps-glide    the steps, but each one slides into the next, so that it is a climb in the key.

Needs numpy, and ffmpeg on the path.
"""
import os
import subprocess
import tempfile

import wave

import numpy as np

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
MUSIC = os.path.join(HERE, "..", "Assets", "Fish", "Audio", "Music")
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
OUT = os.path.join(HERE, "..", "..", "bar-climb-samples")
rng = np.random.default_rng(11)

D2 = 73.42
CENTER = np.log2(293.66)  # the parts are loudest around D4, and fade out above and below
SPREAD = 1.15             # how many octaves wide that hump is
OCTAVES = 7
LAND_MS = 0.76            # SCORE_FLY_LAND_MS

# the stretch of a round: when each combo starts, how many fish it pops, and how far apart they pop
COMBOS = [(0.6, 5, 0.11), (3.0, 9, 0.09), (4.3, 4, 0.12), (8.2, 14, 0.08), (11.6, 3, 0.14)]
LENGTH = 17.0


def fftconvolve(a, b):
    n = len(a) + len(b) - 1
    size = 1 << (n - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(a, size) * np.fft.rfft(b, size), size)[:n]


def write_wav(path, x):
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype("<i2").tobytes())


def stereo(x):
    return x if x.ndim == 2 else np.stack([x, x], axis=1)


def make_ir(seconds, lo=200, hi=4500):
    n = int(SR * seconds)
    t = np.arange(n) / SR
    f = np.fft.rfftfreq(n, 1 / SR)
    shape = 1 / np.sqrt(1 + (lo / np.maximum(f, 1e-6)) ** 4) / np.sqrt(1 + (f / hi) ** 4)
    chans = []
    for _ in range(2):
        spec = (rng.standard_normal(len(f)) + 1j * rng.standard_normal(len(f))) * shape
        x = np.fft.irfft(spec, n) * np.exp(-6.9 * t / seconds)
        chans.append(x / np.sqrt(np.sum(x ** 2)))
    return np.stack(chans, axis=1)


IR = make_ir(3.5)


def reverb(x, wet=0.55, dry=0.6, circular=False):
    x = stereo(x)
    if circular:
        n = len(x)
        ir = np.zeros((n, 2))
        ir[:len(IR)] = IR[:n]
        w = np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * np.fft.rfft(ir[:, c]), n) for c in range(2)], axis=1)
        return dry * x + wet * w
    w = np.stack([fftconvolve(x[:, c], IR[:, c]) for c in range(2)], axis=1)
    out = wet * w
    out[:len(x)] += dry * x
    return out


def weight(log2f):
    return np.exp(-0.5 * ((log2f - CENTER) / SPREAD) ** 2)


def voice(freq):
    """a soft pad voice that follows the frequency curve: a sine, a little of its octave, and two
    slightly detuned copies, so that it shimmers. it is quieter where the hump says so."""
    out = np.zeros((len(freq), 2))
    for detune, side in ((-0.002, -0.5), (0.0, 0.0), (0.002, 0.5)):
        ph = 2 * np.pi * np.cumsum(freq * (1 + detune)) / SR + 2 * np.pi * rng.random()
        v = np.sin(ph) + 0.18 * np.sin(2 * ph + 1.3)
        a = (side + 1) * np.pi / 4
        out += np.stack([v * np.cos(a), v * np.sin(a)], axis=1)
    return out * weight(np.log2(freq))[:, None]


def glide_loop(octave_seconds=16.0):
    """the endless climb, as a loop of one octave. one part travels up all the octaves, and the loop
    is that part laid over itself an octave apart, so the end of the loop runs into its start."""
    n = int(SR * octave_seconds)
    t = np.arange(n * OCTAVES) / SR
    freq = D2 / 2 * 2 ** (t / octave_seconds)
    part = voice(freq)
    loop = np.zeros((n, 2))
    for k in range(OCTAVES):
        loop += part[k * n:(k + 1) * n]
    loop = reverb(loop, circular=True)
    return loop / np.max(np.abs(loop))


# the notes of D minor, in semitones above D
SCALE = [0, 2, 3, 5, 7, 8, 10]


def step_tone(semis, seconds=2.6):
    """one step of the climb in the key: the same note in every octave, swelling in softly and dying
    away. with a third of a chord above it, so that it is a pad and not a beep."""
    n = int(SR * seconds)
    t = np.arange(n) / SR
    out = np.zeros((n, 2))
    for k in range(OCTAVES):
        f = D2 / 2 * 2 ** (k + semis / 12)
        out += voice(np.full(n, f))
    env = np.minimum(t / 0.09, 1) * np.exp(-t / 0.9)
    out *= env[:, None]
    return out


def glide_steps(times, length):
    """the steps, sliding: the pitch moves a note up the scale at each landing, and glides there over
    a fifth of a second. the sound is up while it is moving, and dies away when it stops."""
    n = int(SR * length)
    target = np.zeros(n)
    semis = 0
    for tm in times:
        semis += 1
        target[int(tm * SR):] = semis
    # how far up the scale it is, as semitones, for each step: SCALE wraps into the next octave
    def degree_to_semis(d):
        d = int(d)
        return SCALE[d % 7] + 12 * (d // 7)
    want = np.array([degree_to_semis(d) for d in target], dtype=float)
    # glide over 0.2 s
    k = int(SR * 0.2)
    kern = np.ones(k) / k
    pos = np.convolve(np.pad(want, (k, 0), mode="edge"), kern, mode="valid")[:n]
    # the climb is endless: the pitch is folded into one octave, and the parts are an octave apart
    log2f = np.log2(D2 / 2) + (pos / 12) % 1
    out = np.zeros((n, 2))
    for oct_k in range(OCTAVES):
        out += voice(2 ** (log2f + oct_k))
    return out


def landings():
    times = []
    for start, count, gap in COMBOS:
        for i in range(count):
            times.append(start + i * gap + LAND_MS)
    return sorted(times)


def pops():
    return [t - LAND_MS for t in landings()]


def activity(times, length, attack=0.35, release=1.6, hold=0.45):
    """how far up the sound is: it comes up while points are landing, and dies away once they stop"""
    n = int(SR * length)
    on = np.zeros(n)
    for tm in times:
        on[int(tm * SR):int((tm + hold) * SR)] = 1
    lvl = np.zeros(n)
    a = 1 - np.exp(-1 / (SR * attack))
    r = 1 - np.exp(-1 / (SR * release))
    cur = 0.0
    # one pole, up quickly and down slowly. done in blocks, for speed.
    block = 64
    for i in range(0, n, block):
        tgt = on[i]
        c = a if tgt > cur else r
        seg = np.arange(1, min(block, n - i) + 1)
        lvl[i:i + len(seg)] = tgt + (cur - tgt) * (1 - c) ** seg
        cur = lvl[i + len(seg) - 1]
    return lvl


def load_mp3(path):
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, "x.wav")
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", path,
                        "-ac", "2", "-ar", str(SR), wav], check=True)
        with wave.open(wav, "rb") as w:
            x = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").reshape(-1, 2)
    return x.astype(np.float64) / 32768


def fit(x, n):
    """loop or cut a sound to n samples"""
    reps = int(np.ceil(n / len(x)))
    return np.tile(x, (reps, 1))[:n]


def bed(n):
    """the game under the sample: the D minor chord, the water, and the fish popping"""
    pad = fit(load_mp3(os.path.join(MUSIC, "loop-pad-dm.mp3")), n) * 0.45
    amb = fit(load_mp3(os.path.join(MUSIC, "loop-ambience.mp3")), n) * 1.0
    out = 0.5 * (pad + amb)  # the music group sits at about half
    pop = load_mp3(os.path.join(AUDIO, "bubble-pop-2.mp3")) * 0.35
    for tm in pops():
        i = int(tm * SR)
        j = min(n, i + len(pop))
        out[i:j] += pop[:j - i] * rng.uniform(0.7, 1.0)
    return out


def save(name, x):
    os.makedirs(OUT, exist_ok=True)
    x = stereo(x)
    peak = np.max(np.abs(x))
    if peak > 0.95:
        x = x / peak * 0.95
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, "x.wav")
        write_wav(wav, x)
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", wav,
                        "-b:a", "160k", os.path.join(OUT, name + ".mp3")], check=True)


def main():
    n = int(SR * LENGTH)
    under = bed(n)
    lands = landings()
    level = activity(lands, LENGTH)
    loud = 0.22  # how loud the climb is over the game, at its fullest

    # a: the climb runs the whole time, and is only heard while points land
    loop = glide_loop()
    save("take-glide-loop", loop)
    a = fit(loop, n) * level[:, None]
    save("a-glide", under + loud * a)

    # b: the same, but it only moves on while it can be heard
    moving = level > 0.01
    head = np.cumsum(moving) % len(loop)
    b = loop[head] * level[:, None]
    save("b-glide-picks-up", under + loud * b)

    # c: a note up the scale for each landing, but no faster than a step every 300 ms
    steps = []
    for tm in lands:
        if not steps or tm - steps[-1] >= 0.3:
            steps.append(tm)
    tones = [step_tone(SCALE[d % 7]) for d in range(7)]
    tone_peak = max(np.max(np.abs(tt)) for tt in tones)
    for i, tt in enumerate(tones):
        save("take-step-%d" % (i + 1), reverb(tt / tone_peak)[:int(SR * 4)])
    c = np.zeros((n + SR * 3, 2))
    for i, tm in enumerate(steps):
        tt = tones[(i + 1) % 7] / tone_peak
        s = int(tm * SR)
        c[s:s + len(tt)] += tt * 0.7
    c = reverb(c)[:n]
    save("c-steps", under + loud * 1.2 * c / np.max(np.abs(c)))

    # d: the steps, sliding into each other, up while it is moving
    d = glide_steps(steps, LENGTH)
    d = reverb(d * activity(steps, LENGTH, attack=0.15, release=1.3, hold=0.3)[:, None])[:n]
    save("d-steps-glide", under + loud * d / np.max(np.abs(d)))

    print("wrote", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
