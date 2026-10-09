"""Cuts the recorded voices of the coral out of two recordings. see THE MUSIC OF THE CORAL in fish_headers.

    python coral_clips_gen.py <the recording of the singer> <the recording of the monster> <folder to write the mp3 files to> [the recording of the screams]

with the fourth one, only what is made from it is made, and the rest is left alone.

the recordings are not kept with the game. they came from pixabay:
    pietix-acapella-waltz-female-527215.mp3                       a woman singing with herself, in b flat
    freesound_community-high-quality-monster-screech-65012.mp3    two long screeches
    jusatti890-scream-horror-sfx-490909.mp3                       three screams, one after another

what comes out:
    praise-1 to praise-9    phrases of the singing, cut where she breathes or where a new line starts.
                            the song is in b flat, which is the brighter of the two chords of the game, so
                            they are not changed in pitch. the quick, bouncing middle of the song is left out.
    shot-scream, -2, -3     the screeches, with the top taken off, as if through a lot of water
    shot-scream-4, -5, -6   the three screams, the same way
    shot-dirge-1, -2, -3    the three screams again, for the ending with the ancient one: played at a little
                            over half speed, which makes them that much lower and longer, in a very big room
    wail-d4 to wail-c6      the one stretch of the second screech that holds a pitch, slowed down or sped
                            up to every note, an octave below where it is written, and muffled the same way

this needs nothing but python and ffmpeg.
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
SINGER, MONSTER, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
HUMAN = sys.argv[4] if len(sys.argv) > 4 else ""
TMP = tempfile.mkdtemp(prefix="coral_clips_")
rng = random.Random(41)

# where every phrase of the singing starts and ends, in seconds, and how long it takes to fade away
PRAISE = ((0.28, 3.66, 0.30), (3.66, 6.36, 0.35), (6.36, 8.80, 0.30), (8.80, 10.70, 0.30), (10.70, 14.00, 0.35),
          (14.00, 18.20, 0.40), (18.15, 20.75, 0.30), (29.25, 33.80, 0.60), (33.75, 38.00, 0.40))

# the same for the screeches
SCREAMS = (("shot-scream", 0.37, 4.60, 1.4), ("shot-scream-2", 8.16, 11.75, 0.5), ("shot-scream-3", 11.75, 14.67, 0.4))

# and for the three screams of the other recording. each one starts where it gets loud all at once.
HUMAN_SCREAMS = ((0.46, 4.08, 0.5), (4.08, 9.30, 0.8), (9.30, 16.50, 2.2))

# how fast the screams of the ending are played. this much of the speed, and this much of the pitch.
DIRGE_SPEED = 0.6

# the stretch of the second screech that holds a pitch, and the pitch that it holds
MOAN = (9.17, 10.27)
MOAN_HZ = 610.8

NOTES = (("d4", 293.66), ("e4", 329.63), ("f4", 349.23), ("g4", 392.00), ("a4", 440.00), ("bb4", 466.16),
         ("c5", 523.25), ("d5", 587.33), ("e5", 659.25), ("f5", 698.46), ("g5", 783.99), ("a5", 880.00),
         ("bb5", 932.33), ("c6", 1046.50))


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
            low += (v - low) * (1 - dark)
            slow += (low - slow) * 0.02
            x.append((low - slow) * math.exp(-6.9 * i / n))
        energy = math.sqrt(sum(v * v for v in x))
        chans.append([v / energy for v in x])
    frames = array.array("h")
    for l, r in zip(*chans):
        frames.append(int(max(-1, min(1, l * 8)) * 32767))
        frames.append(int(max(-1, min(1, r * 8)) * 32767))
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(frames.tobytes())


def level_of(path):
    """How loud a file is on the whole, and at its loudest, in dB."""
    text = subprocess.run(["ffmpeg", "-hide_banner", "-i", path, "-af", "volumedetect", "-f", "null", "-"],
                          capture_output=True, text=True).stderr
    mean = float(text.split("mean_volume:")[1].split("dB")[0])
    peak = float(text.split("max_volume:")[1].split("dB")[0])
    return mean, peak


def cut(name, src, start, end, fade, before, wet, dry, ir, tail, mean_db):
    """Cut a piece out of a recording, put it in a room, and write it at a level.
    `before` is whatever is done to the piece first, as ffmpeg filters, each one ending with a comma."""
    length = end - start
    raw = os.path.join(TMP, name + ".wav")
    # afir's "dry" is how much of the sound goes into the echo, so the untouched sound is mixed back in
    # by hand, and its "wet" undoes the 8 that make_ir multiplied by.
    graph = ("[0]atrim=%g:%g,asetpts=PTS-STARTPTS,aformat=channel_layouts=stereo,aresample=%d,%s"
             "afade=t=in:d=0.03,afade=t=out:st=%g:d=%g,apad=pad_dur=%g,asplit[d][s];"
             "[s][1]afir=dry=1:wet=0.125:gtype=-1:irnorm=-1[w];"
             "[d][w]amix=inputs=2:weights=%g %g:normalize=0,volume=0.5,afade=t=out:st=%g:d=%g"
             % (start, end, SR, before, length - fade, fade, tail, dry, wet, length, tail))
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", src, "-i", ir, "-filter_complex", graph, raw], check=True)

    mean, peak = level_of(raw)
    gain = mean_db - mean
    if peak + gain > -1.5:
        gain = -1.5 - peak
    mp3 = os.path.join(OUT, name + ".mp3")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", raw, "-af", "volume=%gdB" % gain,
                    "-codec:a", "libmp3lame", "-q:a", "4", mp3], check=True)
    print("%-14s %5.1fs  %+5.1f dB" % (name, length + tail, gain))


ir_hall = os.path.join(TMP, "ir-hall.wav")
ir_dark = os.path.join(TMP, "ir-dark.wav")
ir_vast = os.path.join(TMP, "ir-vast.wav")
make_ir(ir_hall, 2.8, 0.55)
make_ir(ir_dark, 4.5, 0.85)
make_ir(ir_vast, 7.0, 0.9)

if HUMAN:
    for i, (start, end, fade) in enumerate(HUMAN_SCREAMS):
        # far off, through the water, like the screeches
        cut("shot-scream-%d" % (i + 4), HUMAN, start, end, fade, "lowpass=f=900,lowpass=f=900,", 1.0, 0.3, ir_dark, 3.5, -21.0)

        # and slow, in a room with no end to it. the piece is cut out first, because everything that
        # is done to it after it has been slowed down is that much longer.
        piece = os.path.join(TMP, "dirge-src-%d.wav" % i)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", str(start), "-t", str(end - start), "-i", HUMAN,
                        "-ar", str(SR), "-ac", "2", piece], check=True)
        slow = "asetrate=%d,aresample=%d,lowpass=f=1100,lowpass=f=1100," % (round(SR * DIRGE_SPEED), SR)
        cut("shot-dirge-%d" % (i + 1), piece, 0, (end - start) / DIRGE_SPEED, fade / DIRGE_SPEED, slow, 1.3, 0.25, ir_vast, 6.0, -21.0)
    sys.exit(0)

# the singing has a room of its own in it already, so it only gets a little more, to cover the cuts
for i, (start, end, fade) in enumerate(PRAISE):
    cut("praise-%d" % (i + 1), SINGER, start, end, fade, "", 0.3, 0.9, ir_hall, 2.2, -19.0)

# the screeches come from a long way off, through the water
for name, start, end, fade in SCREAMS:
    cut(name, MONSTER, start, end, fade, "lowpass=f=800,lowpass=f=800,", 1.0, 0.3, ir_dark, 3.5, -21.0)

# the moan. playing a recording slower makes it lower, and longer, by the same amount.
for name, hz in NOTES:
    ratio = (hz / 2) / MOAN_HZ
    before = "asetrate=%d,aresample=%d,lowpass=f=1500,lowpass=f=1500," % (round(SR * ratio), SR)
    # the cut is made before it is slowed down, so everything after that is that much longer
    length = (MOAN[1] - MOAN[0]) / ratio
    raw_src = os.path.join(TMP, "moan-src.wav")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", str(MOAN[0]), "-t", str(MOAN[1] - MOAN[0]), "-i", MONSTER,
                    "-ar", str(SR), "-ac", "2", raw_src], check=True)
    cut("wail-" + name, raw_src, 0, length, min(0.5, length * 0.35), before, 0.8, 0.5, ir_dark, 3.0, -20.5)
