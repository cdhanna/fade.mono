"""
Makes the notes of THE CLIMB (see fish_headers_charge.fbasic): every time points land in the bar of the
round, a note plays a step higher up D minor, as a Shepard tone, so that the climb never gets to the
top. the notes are sounds of the game, tuned to D: a bubble popping while the bar fills, the bloop
voice for the first spill past the end, and the sonar ping after that. bar_cue_samples_gen.py makes
them, the same as its round-bubble, round-bloop and round-sonar samples.

    py Tools/bar_cue_sounds_gen.py            from the Fade.MonoGame folder

It writes cue-<instrument>-<n>.mp3 into Assets/Fish/Audio, for n from 1 to 7, the notes D E F G A Bb C.
each one is the note in four octaves at once, the top one quieter the higher the note is, and the
bottom one louder, so that 7 goes on into 1 as a step up, and the climb can go round and round.

Needs numpy, and ffmpeg on the path. New sounds have to be pushed: see build/push-assets.ps1.
"""
import os

import numpy as np

from bar_climb_gen import SR, AUDIO, reverb
from bar_charge_sounds_gen import write_mp3
from bar_cue_samples_gen import shepard_note, SHEP_SCALE

LONG = 1.6   # how long each note is, with its ring
FADE = 0.4   # and the end of it fades out over this long


def main():
    for inst in ("bubble", "bloop", "sonar"):
        for k in range(len(SHEP_SCALE)):
            x = shepard_note(inst, k, SHEP_SCALE)
            x = reverb(x, wet=0.25, dry=0.9)
            n = int(SR * LONG)
            if len(x) < n:
                x = np.concatenate([x, np.zeros((n - len(x), 2))])
            x = x[:n].copy()
            x[-int(SR * FADE):] *= np.linspace(1, 0, int(SR * FADE))[:, None] ** 2
            write_mp3("cue-%s-%d" % (inst, k + 1), x)


if __name__ == "__main__":
    main()
