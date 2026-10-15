"""
Makes the sounds of the tide and of the night, out of recordings that the game already has, so
that they belong to the water that is already playing. Needs ffmpeg on the path.

    py Tools/tide_sounds_gen.py                 from the Fade.MonoGame folder

It writes these into Assets/Fish/Audio:

    tide-ebb-1.mp3      the water has gone all the way out: a wave drawing back down the shore.
      to tide-ebb-6     six takes, each the back half of a different wave: three big ones from one
                        recording, and three gentle ones from another.
    tide-flow-1.mp3     and it is all the way back in: a wave coming up the shore, and breaking.
      to tide-flow-6    six takes, each the front half of one of the same waves.
    night-fall.mp3      night falls: a low bell, an octave under the lowest one that the music has
    night-pulse.mp3     a group is made at night: a muffled thup, and nothing more
    night-breath.mp3    a loop. the water of the night, swelling and sinking slowly, like breathing
    crab-click.mp3      a group of crabs pops at night, with the "midnight crab" card: dry clicks
    dawn-swell.mp3      day breaks: a rush of surf that builds for as long as the light takes, and three bright bells
    jelly-hum.mp3       a loop. the hum of a jellyfish that is glowing: the chord of the music, an octave up

The recordings of the sea that these are cut from are not part of the game, so they are not kept
with its sounds. they are in ../tide-sound-samples/sources, and this needs them there.

and, into ../tide-sound-samples, how each of them sounds in the game: over the ambience and the
chord, at about the volumes that the game plays them at. nobody can judge a sound from its file
alone, and the samples are what there is to listen to.

The night has no ambience of its own. the game turns the layers of the music that it already has
up and down, and plays the ambience a little lower. the samples do the same thing with filters,
which is close to it and not the same. the little bells of the night are notes of the music.

See THE SOUNDS OF THE TIDE in fish_headers_tide, and fish_routines_tide.
"""
import os
import re
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
MUSIC = os.path.join(AUDIO, "Music")
SAMPLES = os.path.join(HERE, "..", "..", "tide-sound-samples")
TEMP = os.path.join(SAMPLES, "_temp")

RIVER = os.path.join(AUDIO, "river-and-stones.mp3")

SOURCES = os.path.join(HERE, "..", "..", "tide-sound-samples", "sources")

# a recording of waves on a shore, about a minute of it, with a big wave every ten seconds or so.
# "Ocean Waves", by rmultimediaeu, from Pixabay.
WAVES = os.path.join(SOURCES, "ocean-waves.mp3")

# gentle waves lapping at a shore, with seagulls a long way off. the takes from it have the top
# taken off of them, which is where the seagulls are, and which suits a sound heard from under water.
# "Sea, gently lapping waves, far away seagulls", by jonathanslattermusic, from Pixabay.
LAPPING = os.path.join(SOURCES, "sea-lapping.mp3")

# loud surf on a beach, that never lets up. the rush of daybreak is a moment of it.
# "Baltic Sea waves, beach sound", by soundreality, from Pixabay.
SURF = os.path.join(SOURCES, "baltic-surf.mp3")

# the waves that the takes are cut from: the recording, how it is shaped, and then for each wave when
# it starts to build, when it breaks, and when it has drawn all the way back, in seconds of the
# recording. they were found by measuring how loud each recording is every half of a second: these
# are the ones that stand the furthest out of it. the takes are numbered in this order.
WAVE_TAKES = [
    (WAVES, "highpass=f=80", [(23.0, 26.0, 29.0), (30.5, 33.8, 37.0), (56.5, 59.6, 63.0)]),
    (LAPPING, "highpass=f=80,lowpass=f=2200", [(51.5, 53.6, 57.0), (63.0, 65.2, 68.0), (18.5, 21.6, 23.8)]),
]
WAVE_OVER = 0.8   # a take of a wave coming in runs on this long after it breaks, and one of it drawing back starts this long before
ROCKS = os.path.join(AUDIO, "rocks-falling.mp3")
POP = os.path.join(AUDIO, "bubble-pop-2.mp3")
AMBIENCE = os.path.join(MUSIC, "loop-ambience.mp3")
PAD = os.path.join(MUSIC, "loop-pad-dm.mp3")

RATE = 44100

# how loud the game plays each of them, from 0 to 1. these are the same numbers as the ones in
# fish_headers_tide, and the two have to be kept the same by hand.
VOLUME = {
    "tide-ebb": 0.30,
    "tide-flow": 0.30,
    "night-fall": 0.60,
    "night-pulse": 0.55,
    "night-breath": 0.35,
    "crab-click": 0.40,
    "dawn-swell": 0.60,
    "jelly-hum": 0.18,
}
BED = 0.5       # how loud the ambience is under a sample
BED_PAD = 0.3   # and the chord
PLINK = 0.14    # and one of the little bells of the night


def note(name):
    return os.path.join(MUSIC, f"note-{name}.mp3")


def ffmpeg(*args):
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def peak_of(path):
    out = subprocess.run(["ffmpeg", "-hide_banner", "-i", path, "-af", "volumedetect", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    return float(re.search(r"max_volume: ([-0-9.]+) dB", out).group(1))


def render(name, inputs, graph, peak):
    """mix the inputs with the graph, whose last label is [out], and bring the loudest moment of it to `peak` dB"""
    rough = os.path.join(TEMP, name + ".wav")
    args = []
    for one in inputs:
        args += ["-i", one]
    ffmpeg(*args, "-filter_complex", graph, "-map", "[out]", "-ar", str(RATE), "-ac", "2", rough)
    gain = peak - peak_of(rough)
    final = os.path.join(AUDIO, name + ".mp3")
    ffmpeg("-i", rough, "-af", f"volume={gain:.2f}dB", "-ar", str(RATE), "-ac", "2", "-codec:a", "libmp3lame", "-q:a", "3", final)
    print(f"  {name}.mp3")
    return final


def loop_graph(shape, start, length, blend, after):
    """a stretch of a recording, shaped, whose end is blended into its start so that it loops with no seam"""
    return (
        f"[0:a]{shape},atrim={start}:{start + length + blend},asetpts=PTS-STARTPTS,asplit=3[h][m][t];"
        f"[h]atrim=0:{blend},afade=t=in:d={blend}[head];"
        f"[t]atrim={length}:{length + blend},asetpts=PTS-STARTPTS,afade=t=out:d={blend}[tail];"
        f"[head][tail]amix=inputs=2:normalize=0[join];"
        f"[m]atrim={blend}:{length},asetpts=PTS-STARTPTS[rest];"
        f"[join][rest]concat=n=2:v=0:a=1,{after}[out]"
    )


def make_sounds():
    print("the sounds:")

    # the two sounds of a cycle are the two halves of a wave on a shore. the recordings are quiet
    # ones, so the takes are brought up a long way.
    take = 0
    for source, shape, waves in WAVE_TAKES:
        for builds, breaks, gone in waves:
            take += 1

            # the water comes back in: the wave builds, which fades in, and it is let go soon after it breaks
            length = breaks + WAVE_OVER - builds
            fade = min(1.2, length * 0.4)
            render(f"tide-flow-{take}", [source],
                   f"[0:a]atrim={builds}:{breaks + WAVE_OVER},asetpts=PTS-STARTPTS,{shape},"
                   f"afade=t=in:st=0:d={fade:.2f},afade=t=out:st={length - 0.7:.2f}:d=0.7[out]", -6)

            # and it goes out: from just before the wave breaks, drawing back for as long as it takes, fading away
            length = gone - (breaks - WAVE_OVER)
            fade = min(1.6, length * 0.5)
            render(f"tide-ebb-{take}", [source],
                   f"[0:a]atrim={breaks - WAVE_OVER}:{gone},asetpts=PTS-STARTPTS,{shape},"
                   f"afade=t=in:st=0:d=0.45,afade=t=out:st={length - fade:.2f}:d={fade:.2f}[out]", -6)

    # the low bell of nightfall: the lowest note of the music played at half speed, which is an
    # octave down, with the fifth over it very quietly, and a long tail
    render("night-fall", [note("d4"), note("a4")],
           f"[0:a]asetrate={RATE}*0.5,aresample={RATE},lowpass=f=900[low];"
           f"[1:a]asetrate={RATE}*0.5,aresample={RATE},lowpass=f=700,volume=0.3,adelay=350|350[fifth];"
           "[low][fifth]amix=inputs=2:normalize=0,aecho=0.8:0.7:120|260:0.35|0.25,"
           "atrim=0:6,afade=t=out:st=3.5:d=2.5[out]", -6)

    # the pulse: a pop of a bubble, slowed right down and with the top taken off of it, so that it
    # is a thup and not a pop. there used to be a high bell over it, which was annoying, and is gone.
    render("night-pulse", [POP],
           f"[0:a]silenceremove=start_periods=1:start_threshold=-35dB,asetrate=48000*0.45,aresample={RATE},lowpass=f=520,"
           "atrim=0:0.7,afade=t=out:st=0.45:d=0.25[out]", -6)

    # the breathing of the night: the low middle of the ambience, swelling and sinking once in
    # every loop. the loop is 8 seconds, and the swell takes 8 seconds, so it has no seam.
    render("night-breath", [AMBIENCE],
           loop_graph("highpass=f=120,lowpass=f=700", 40, 8, 1, "tremolo=f=0.125:d=0.85"), -9)

    # the clicks of the crabs: only the loud moments of the falling rocks, pushed together, with
    # the bottom taken off of them so that they are dry
    render("crab-click", [ROCKS],
           "[0:a]highpass=f=1800,silenceremove=stop_periods=-1:stop_duration=0.02:stop_threshold=-38dB,"
           "atempo=1.5,atrim=0:0.5,afade=t=out:st=0.36:d=0.14[out]", -6)

    # day breaks: a rush of surf that builds for as long as the light takes to come back, and three
    # bright bells going up, the first of them right as it peaks
    render("dawn-swell", [SURF, note("d5"), note("f5"), note("a5")],
           "[0:a]atrim=38.9:40.0,asetpts=PTS-STARTPTS,highpass=f=300,"
           "afade=t=in:st=0:d=0.8:curve=cub,afade=t=out:st=0.82:d=0.28,volume=0.7[rush];"
           "[1:a]atrim=0:3.5,volume=0.55,adelay=760|760[n1];"
           "[2:a]atrim=0:3.5,volume=0.50,adelay=960|960[n2];"
           "[3:a]atrim=0:3.6,volume=0.50,adelay=1160|1160[n3];"
           "[rush][n1][n2][n3]amix=inputs=4:normalize=0,atrim=0:4.6,afade=t=out:st=3.0:d=1.6[out]", -6)

    # the hum of a jellyfish: the chord that the music is in, an octave up and thin, wavering
    # twice in every loop
    render("jelly-hum", [PAD],
           loop_graph(f"asetrate={RATE}*2,aresample={RATE},highpass=f=500,lowpass=f=5000", 2, 8, 1, "tremolo=f=0.25:d=0.4"), -9)


def sound(name):
    return os.path.join(AUDIO, name + ".mp3")


def sample(name, seconds, parts):
    """
    one sample. every part is (path, when it starts in seconds, how loud, filters or "", loops or not).
    a part that loops plays from its start time to the end of the sample.
    """
    args = []
    graph = []
    labels = []
    for n, (path, start, loud, shape, loops) in enumerate(parts):
        if loops:
            args += ["-stream_loop", "-1"]
        args += ["-i", path]
        chain = f"[{n}:a]"
        chain += (shape + ",") if shape else ""
        chain += f"volume={loud},adelay={int(start * 1000)}|{int(start * 1000)},aresample={RATE}[p{n}]"
        graph.append(chain)
        labels.append(f"[p{n}]")
    graph.append("".join(labels) + f"amix=inputs={len(parts)}:normalize=0,atrim=0:{seconds},afade=t=out:st={seconds - 0.6}:d=0.6[out]")
    out = os.path.join(SAMPLES, name + ".mp3")
    ffmpeg(*args, "-filter_complex", ";".join(graph), "-map", "[out]", "-t", str(seconds), "-ar", str(RATE), "-ac", "2", "-codec:a", "libmp3lame", "-q:a", "3", out)
    print(f"  {name}.mp3")


# the water in the day, and the water at night: lower, and with the top taken off of it
DAY = (AMBIENCE, 0, BED, "", True)
CHORD = (PAD, 0, BED_PAD, "", True)
NIGHT_SHAPE = "asetrate=24000*0.87,aresample=24000,lowpass=f=1300"


def day_until(seconds):
    return (AMBIENCE, 0, BED, f"afade=t=out:st={seconds}:d=3", True)


def night_from(seconds):
    return [
        (AMBIENCE, seconds, BED * 0.8, NIGHT_SHAPE + ",afade=t=in:st=0:d=3", True),
        (sound("night-breath"), seconds, VOLUME["night-breath"], "afade=t=in:st=0:d=3", True),
    ]


def pitched(octaves):
    return f"asetrate={RATE}*{2 ** octaves:.4f},aresample={RATE}"


def make_samples():
    print("the samples:")

    # the game slides the pitch of a wave while it plays: down for one that is going out, and up for
    # one that is coming in. the samples do not, because ffmpeg has no filter that does.

    # the gentle takes, on their own, to check that no seagull got through
    sample("1b-the-tide-turns-gently", 34, [DAY, CHORD,
        (sound("tide-ebb-4"), 2.5, VOLUME["tide-ebb"], "", False),
        (sound("tide-flow-4"), 8.0, VOLUME["tide-flow"], "", False),
        (sound("tide-ebb-5"), 13.0, VOLUME["tide-ebb"], pitched(0.04), False),
        (sound("tide-flow-5"), 18.5, VOLUME["tide-flow"], pitched(-0.03), False),
        (sound("tide-ebb-6"), 23.5, VOLUME["tide-ebb"], pitched(-0.04), False),
        (sound("tide-flow-6"), 28.0, VOLUME["tide-flow"], pitched(0.03), False)])

    # three cycles, one with each of the big takes. the game picks a take at random, and plays it a
    # little higher or lower every time, the way that the last two of these are.
    sample("1-the-tide-turns", 40, [DAY, CHORD,
        (sound("tide-ebb-1"), 2.5, VOLUME["tide-ebb"], "", False),
        (sound("tide-flow-1"), 8.5, VOLUME["tide-flow"], "", False),
        (sound("tide-ebb-2"), 15.0, VOLUME["tide-ebb"], pitched(0.04), False),
        (sound("tide-flow-2"), 21.0, VOLUME["tide-flow"], pitched(-0.03), False),
        (sound("tide-ebb-3"), 27.5, VOLUME["tide-ebb"], pitched(-0.04), False),
        (sound("tide-flow-3"), 33.5, VOLUME["tide-flow"], pitched(0.03), False)])

    sample("2-night-falls", 24, [day_until(4), CHORD] + night_from(4) + [
        (sound("night-fall"), 4.0, VOLUME["night-fall"], "", False),
        (note("a5"), 11.0, PLINK, "", False),
        (note("d5"), 15.5, PLINK, "", False),
        (note("c6"), 18.5, PLINK, "", False),
        (note("f5"), 21.0, PLINK, "", False)])

    sample("3-a-group-is-made-at-night", 12, [CHORD] + night_from(0) + [
        (sound("night-pulse"), 2.0, VOLUME["night-pulse"], "", False),
        (sound("night-pulse"), 4.5, VOLUME["night-pulse"], "", False),
        (sound("night-pulse"), 6.5, VOLUME["night-pulse"], "", False),
        (sound("night-pulse"), 8.0, VOLUME["night-pulse"], "", False)])

    sample("4-midnight-crab", 9, [CHORD] + night_from(0) + [
        (POP, 2.0, 0.25, "", False), (sound("crab-click"), 2.0, VOLUME["crab-click"], "", False),
        (POP, 3.4, 0.25, "", False), (sound("crab-click"), 3.4, VOLUME["crab-click"], pitched(0.05), False),
        (POP, 4.3, 0.25, "", False), (sound("crab-click"), 4.3, VOLUME["crab-click"], pitched(-0.05), False)])

    # the light comes back with the rush, and then everything that the night held pops at once
    pops = [(POP, 5.4 + n * 0.07, 0.22, pitched(-0.2 + (n % 5) * 0.08), False) for n in range(9)]
    sample("5-day-breaks", 13, [
        (AMBIENCE, 0, BED * 0.8, NIGHT_SHAPE + ",afade=t=out:st=4:d=0.8", True),
        (sound("night-breath"), 0, VOLUME["night-breath"], "afade=t=out:st=4:d=0.8", True),
        (AMBIENCE, 4, BED, "afade=t=in:st=0:d=0.8", True),
        CHORD,
        (sound("dawn-swell"), 4.0, VOLUME["dawn-swell"], "", False)] + pops)

    sample("6-a-jellyfish-glows", 14, [CHORD] + night_from(0) + [
        (sound("jelly-hum"), 3.0, VOLUME["jelly-hum"], "afade=t=in:st=0:d=2,afade=t=out:st=7:d=2", True)])


if __name__ == "__main__":
    os.makedirs(TEMP, exist_ok=True)
    make_sounds()
    make_samples()
    for left in os.listdir(TEMP):
        os.remove(os.path.join(TEMP, left))
    os.rmdir(TEMP)

    # the takes of the cycle from before there were recordings of waves
    for old in ("tide-ebb.mp3", "tide-flow.mp3"):
        if os.path.exists(os.path.join(AUDIO, old)):
            os.remove(os.path.join(AUDIO, old))
    print("the samples are in", os.path.normpath(SAMPLES))
