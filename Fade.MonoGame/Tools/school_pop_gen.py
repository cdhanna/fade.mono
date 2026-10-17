"""
Makes the pop of a fish out in the background, out of the pops that the game already has, so that it
sounds like the same pop heard a long way off through the water. Needs ffmpeg on the path.

    py Tools/school_pop_gen.py                  from the Fade.MonoGame folder

It writes these into Assets/Fish/Audio:

    school-pop-far-1.mp3    bubble-pop-2, far off: the top taken off of it, so that it is muffled, a
    school-pop-far-2.mp3    bubble-pop-4    little smear of echo after it, and quieter.

and, into ../school-pop-samples, how a flush sounds in the game: a run of them, one after the other,
the way that the fish of a flush pop, each a little higher or lower, over the ambience, at about the
volumes that the game plays them at. nobody can judge a sound from its file alone.

See THE TIDE FLUSHES THE SCHOOLS in fish_headers_school, and schoolFlushPop in fish_routines_school.
"""
import os
import random
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
SAMPLES = os.path.join(HERE, "..", "..", "school-pop-samples")
TEMP = os.path.join(SAMPLES, "_temp")
AMBIENCE = os.path.join(AUDIO, "underwater-ambiencewav.mp3")

# the pops of the board, and the far versions of them
POPS = [("bubble-pop-2.mp3", "school-pop-far-1.mp3"), ("bubble-pop-4.mp3", "school-pop-far-2.mp3")]

# far off, through the water: the highs go, and some of the lows, a short smear of echo, and quieter.
FAR = "highpass=f=180,lowpass=f=900,lowpass=f=900,aecho=0.8:0.5:40|75:0.35|0.2,volume=0.7,apad=pad_dur=0.15"

# how a flush sounds: this many pops, this far apart, each played this much higher or lower, at the
# volume that the game plays them at (SCHOOL_FLUSH_VOLUME), over the ambience at its own volume
FLUSH_POPS = 9
FLUSH_GAP_MS = 140
FLUSH_PITCH = 0.06
GAME_POP_VOLUME = 0.35
GAME_AMBIENCE_VOLUME = 0.6


def run(args):
    subprocess.run(["ffmpeg", "-loglevel", "error", "-y"] + args, check=True)


def make_far():
    for source, target in POPS:
        run(["-i", os.path.join(AUDIO, source), "-af", FAR, "-ac", "1", "-ar", "44100",
             "-c:a", "libmp3lame", "-q:a", "2", os.path.join(AUDIO, target)])
        print("wrote", target)


def make_samples():
    os.makedirs(TEMP, exist_ok=True)
    random.seed(3)
    inputs = ["-i", AMBIENCE]
    filters = ["[0:a]atrim=0:4,volume=%.2f[amb]" % GAME_AMBIENCE_VOLUME]
    labels = ["[amb]"]
    for n in range(FLUSH_POPS):
        _, far = POPS[n % 2]
        inputs += ["-i", os.path.join(AUDIO, far)]
        rate = 2 ** (random.uniform(-FLUSH_PITCH, FLUSH_PITCH))
        delay = 300 + n * FLUSH_GAP_MS
        filters.append("[%d:a]asetrate=44100*%.4f,aresample=44100,volume=%.2f,adelay=%d|%d[p%d]"
                       % (n + 1, rate, GAME_POP_VOLUME, delay, delay, n))
        labels.append("[p%d]" % n)
    filters.append("%samix=inputs=%d:normalize=0:duration=first[out]" % ("".join(labels), len(labels)))
    out = os.path.join(SAMPLES, "flush-far-pops.mp3")
    run(inputs + ["-filter_complex", ";".join(filters), "-map", "[out]", "-c:a", "libmp3lame", "-q:a", "2", out])
    print("wrote", out)

    # and the same run with the pops of the board, to hear the difference
    inputs = ["-i", AMBIENCE]
    filters = ["[0:a]atrim=0:4,volume=%.2f[amb]" % GAME_AMBIENCE_VOLUME]
    labels = ["[amb]"]
    random.seed(3)
    for n in range(FLUSH_POPS):
        near, _ = POPS[n % 2]
        inputs += ["-i", os.path.join(AUDIO, near)]
        rate = 2 ** (random.uniform(-FLUSH_PITCH, FLUSH_PITCH))
        delay = 300 + n * FLUSH_GAP_MS
        filters.append("[%d:a]asetrate=44100*%.4f,aresample=44100,volume=%.2f,adelay=%d|%d[p%d]"
                       % (n + 1, rate, GAME_POP_VOLUME, delay, delay, n))
        labels.append("[p%d]" % n)
    filters.append("%samix=inputs=%d:normalize=0:duration=first[out]" % ("".join(labels), len(labels)))
    out = os.path.join(SAMPLES, "flush-board-pops-for-comparison.mp3")
    run(inputs + ["-filter_complex", ";".join(filters), "-map", "[out]", "-c:a", "libmp3lame", "-q:a", "2", out])
    print("wrote", out)


if __name__ == "__main__":
    make_far()
    make_samples()
