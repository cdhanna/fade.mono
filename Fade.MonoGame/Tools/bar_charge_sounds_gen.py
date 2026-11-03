"""
Makes the sound of THE CHARGE (see fish_headers_charge.fbasic) filling the bar of the round. the
climb that charged up under the bar (g-force, from bar_charge_gen.py) was tried in the game, as
bar-charge.mp3 with bar-charge-bright.mp3 and bar-charge-deep.mp3, and taken out again on 2026-10-29.

    py Tools/bar_charge_sounds_gen.py         from the Fade.MonoGame folder

It writes this into Assets/Fish/Audio:

    bar-charge-boom.mp3      a deep boom and the wave of bubbles, for when the charge fills the bar

Needs numpy, and ffmpeg on the path. New sounds have to be pushed: see build/push-assets.ps1.
"""
import os
import subprocess
import tempfile

import numpy as np

from bar_climb_gen import SR, AUDIO, rng, reverb, load_mp3, stereo, write_wav

LOOP = 16.0


def write_mp3(name, x, peak=0.89, ref=None):
    """ref is the loudest that it is measured against, when it has to stay as loud as another sound"""
    x = stereo(x)
    x = x / (ref or np.max(np.abs(x))) * peak
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, "x.wav")
        write_wav(wav, x)
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", wav,
                        "-b:a", "160k", os.path.join(AUDIO, name + ".mp3")], check=True)
    print("%-22s %5.1fs" % (name, len(x) / SR))


def thump(seconds, top, bottom, fall, ring, noise_amount):
    m = int(SR * seconds)
    t = np.arange(m) / SR
    f = bottom + (top - bottom) * np.exp(-t / fall)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / ring) * np.minimum(t / 0.008, 1)
    fr = np.fft.rfftfreq(m, 1 / SR)
    spec = (rng.standard_normal(len(fr)) + 1j * rng.standard_normal(len(fr))) / (1 + (fr / 400) ** 2)
    hiss = np.fft.irfft(spec, m)
    hiss = hiss / np.max(np.abs(hiss)) * np.exp(-t / (ring * 0.6)) * np.minimum(t / 0.02, 1)
    return stereo(x + noise_amount * hiss)


def main():
    boom = thump(4.0, 70, 30, 0.25, 1.2, 0.2) * 0.9
    wave = load_mp3(os.path.join(AUDIO, "bubble-wave.mp3")) * 0.6
    out = np.zeros((max(len(boom), len(wave)), 2))
    out[:len(boom)] += boom
    out[:len(wave)] += wave
    out = reverb(out, wet=0.35, dry=0.8)[:len(out) + int(SR * 1.5)]
    out[-int(SR * 1.5):] *= np.linspace(1, 0, int(SR * 1.5))[:, None]
    write_mp3("bar-charge-boom", out)


if __name__ == "__main__":
    main()
