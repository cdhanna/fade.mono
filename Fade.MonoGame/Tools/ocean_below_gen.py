"""Renders an audition batch of things that are heard from below the surface. uses the helpers of scuba_gen.py."""
import os
import sys

here = os.path.dirname(os.path.abspath(__file__))
source = open(os.path.join(here, "scuba_gen.py"), encoding="utf-8").read()
exec(source.split("# ---------- loops ----------")[0])  # only the helpers, not the rendering

SCUBA = sys.argv[3]  # where the scuba stems are, for the demo mix
rng = np.random.default_rng(33)


def old(name):
    sr, x = wavfile.read(os.path.join(OLD, name + ".wav"))
    return x / 32768.0


def curve(n, points):
    t = np.arange(n) / SR
    return np.interp(t, [p[0] for p in points], [p[1] for p in points])[:, None]


def tiled(x, n):
    return np.tile(x, (n // len(x) + 1, 1))[:n]


def splash(size):
    """Something hitting the water overhead: a thump, a rush, and a plume of bubbles that follows it down."""
    seconds = 1.2 + 1.6 * size
    n = int(SR * (seconds + 0.4))
    t = np.arange(n) / SR
    out = np.zeros((n, 2))

    f = (220 - 90 * size) * np.exp(-t / 0.12) + (90 - 45 * size)
    thump = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / (0.12 + 0.15 * size)) * np.minimum(t / 0.003, 1)
    rush = shaped_noise(n, lambda fr: band(fr, 150, 1800)) * np.exp(-t / (0.1 + 0.2 * size)) * np.minimum(t / 0.004, 1)
    out += stereo(thump * (0.6 + 0.6 * size) + rush * 0.7)

    for _ in range(int(30 + 120 * size)):
        at = 0.04 + seconds * rng.beta(1.2, 3.0)
        clip = bubble(rng.uniform(260 - 120 * size, 1700 - 700 * size), rng.uniform(0.03, 0.1)) * rng.uniform(0.15, 0.8) * (1 - 0.7 * at / seconds)
        place(out, pan(clip, rng.uniform(-0.6, 0.6)), at)
    return fade_tail(reverb(muffle(out, 2200), make_ir(1.6), wet=0.35, dry=0.9), 0.6)


def rain_above(seconds=16):
    """Rain on the surface, heard from underneath. every drop traps a tiny bubble that rings."""
    n = SR * seconds
    out = np.zeros((n, 2))
    for _ in range(seconds * 220):
        clip = bubble(rng.uniform(2500, 7000), rng.uniform(0.006, 0.02), glide=1.1) * rng.uniform(0.1, 1.0) ** 2
        place(out, pan(clip, rng.uniform(-1, 1)), rng.uniform(0, seconds), wrap=True)
    out += stereo(shaped_noise(n, lambda f: band(f, 1500, 6000))) * 0.03
    return reverb(muffle(out, 5000), make_ir(1.0, lo=800, hi=6000), wet=0.3, dry=0.9, circular=True)


def surf_above():
    """The waves that are breaking at the surface, heard from down below: only the low part of them gets through."""
    x = old("loop_wash")
    return muffle(x, 420, slope=3)


def dolphin():
    """A run of clicks that speeds up into a buzz, and then two whistles."""
    out = np.zeros((int(SR * 3.2), 2))
    click_n = int(SR * 0.002)
    ct = np.arange(click_n) / SR
    at = 0.05
    for k in range(34):
        click = rng.standard_normal(click_n) * np.exp(-ct / 0.0004)
        place(out, pan(click * 0.5, -0.3), at)
        at += 0.07 * 0.9 ** k + 0.006

    def whistle(start, seconds, points):
        m = int(SR * seconds)
        wt = np.arange(m) / SR
        f = np.interp(wt / seconds, [p[0] for p in points], [p[1] for p in points])
        k = int(SR * 0.08)
        f = np.convolve(np.pad(f, k, mode="edge"), np.hanning(k) / np.sum(np.hanning(k)), mode="same")[k:-k]
        f *= 1 + 0.01 * np.sin(2 * np.pi * 9 * wt)
        phase = 2 * np.pi * np.cumsum(f) / SR
        w = (np.sin(phase) + 0.2 * np.sin(2 * phase)) * np.sin(np.pi * wt / seconds) ** 0.7
        place(out, pan(w * 0.6, 0.3), start)

    whistle(1.2, 0.8, ((0, 3800), (0.5, 7200), (1, 5200)))
    whistle(2.15, 0.5, ((0, 5200), (0.6, 7800), (1, 6800)))

    f = np.fft.rfftfreq(len(out), 1 / SR)
    hp = 1 / np.sqrt(1 + (2200 / np.maximum(f, 1e-6)) ** 4)
    out = np.stack([np.fft.irfft(np.fft.rfft(out[:, c]) * hp, len(out)) for c in range(2)], axis=1)
    return fade_tail(reverb(muffle(out, 6500), make_ir(2.8, lo=1500, hi=7000), wet=0.6, dry=0.6)[:int(SR * 5.5)], 2)


def thunder_above(seconds=8):
    """Thunder, a long way off and through the water. only a slow, low roll of it is left."""
    n = int(SR * seconds)
    t = np.arange(n) / SR
    env = np.zeros(n)
    for _ in range(5):
        start = rng.uniform(0, 2.8)
        tau = rng.uniform(0.6, 1.8)
        bump = np.where(t >= start, np.exp(-(t - start) / tau) * np.minimum((t - start) / 0.08, 1), 0)
        env += bump * rng.uniform(0.4, 1.0)
    chans = [shaped_noise(n, lambda f: band(f, 25, 180) / np.maximum(f, 20)) * env for _ in range(2)]
    mono = shaped_noise(n, lambda f: band(f, 25, 180) / np.maximum(f, 20)) * env
    x = np.stack([0.6 * mono + 0.4 * c for c in chans], axis=1)
    return fade_tail(muffle(x, 220), 2.5)


shots = {
    "shot_splash_small": save("shot_splash_small", splash(0.15)),
    "shot_splash_big": save("shot_splash_big", splash(1.0)),
    "shot_dolphin": save("shot_dolphin", dolphin()),
    "shot_thunder_above": save("shot_thunder_above", thunder_above()),
}
loops = {
    "loop_rain_above": save("loop_rain_above", rain_above(), loop=True),
    "loop_surf_above": save("loop_surf_above", surf_above(), loop=True),
}


def scuba(name):
    sr, x = wavfile.read(os.path.join(SCUBA, name + ".wav"))
    return x / 32768.0


n = SR * 50
mix = np.zeros((n, 2))
mix += tiled(scuba("loop_deep"), n) * 0.7 * curve(n, ((0, 0), (4, 1), (45, 1), (50, 0)))
mix += tiled(loops["loop_surf_above"], n) * 0.5 * curve(n, ((0, 0), (6, 1), (45, 1), (50, 0)))
mix += tiled(loops["loop_rain_above"], n) * 0.2 * curve(n, ((14, 0), (22, 1), (40, 1), (48, 0)))
mix += tiled(old("loop_pad_dm"), n) * 0.35 * curve(n, ((4, 0), (12, 1), (45, 1), (50, 0)))
place(mix, shots["shot_splash_small"] * 0.35, 6)
place(mix, shots["shot_thunder_above"] * 0.5, 16)
place(mix, shots["shot_dolphin"] * 0.2, 26)
place(mix, shots["shot_splash_big"] * 0.4, 34)
place(mix, shots["shot_splash_small"] * 0.3, 41)
save("demo_from_below", np.tanh(mix * 1.2), peak_db=-1)

print("\n".join(report))
