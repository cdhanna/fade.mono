"""
Makes the sound that swells up and dies away under a swap, out of the underwater ambience, so
that it belongs to the water that is already playing. Needs ffmpeg on the path.

    py Tools/swap_swell_gen.py                 from the Fade.MonoGame folder

It writes three takes into Assets/Fish/Audio, each a loop of LOOP seconds:

    swap-swell-soft.mp3      the middle of the ambience only: a soft rush of water
    swap-swell-bright.mp3    more of the top of it: closer, and a little fizzy
    swap-swell-quick.mp3     the soft one played faster, so it is higher and busier

and, for each one, a sample of how it sounds in the game into ../swap-sound-samples: the
ambience, with the take swelling up and away over it the way a swap does, a few times.

The game plays the loop all the time with its volume at 0, and a swap brings the volume up and
back down over about a third of a second. see SFX_SWAP in fish_routines, and doSwap in fish_funcs.
"""
import os
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
SOURCE = os.path.join(AUDIO, "underwater-ambiencewav.mp3")
SAMPLES = os.path.join(HERE, "..", "..", "swap-sound-samples")

START = 67      # the liveliest stretch of the ambience starts here, in seconds
LOOP = 8        # how long the loop is
BLEND = 1       # the end is blended into the start over this long, so that the loop has no seam
LOUDNESS = -18  # about 10 dB over the ambience itself, which sits near -29

# each take: its name, and the filters that shape the stretch of ambience
TAKES = [
    ("soft", "highpass=f=250,lowpass=f=1800"),
    ("bright", "highpass=f=600,lowpass=f=3500"),
    ("quick", "asetrate=24000*1.5,aresample=24000,highpass=f=350,lowpass=f=2600"),
]

# when the swaps of the sample come, in seconds, and where in the loop each one starts
SWAPS = [(2.0, 0.4), (4.0, 2.1), (5.0, 3.3), (7.0, 5.0), (7.5, 6.2), (8.0, 1.2), (10.0, 4.4)]
SWELL = 0.15    # how long the volume takes to come up, and then to go back down


def ffmpeg(*args):
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def make_loop(name, shape):
    out = os.path.join(AUDIO, f"swap-swell-{name}.mp3")
    # the stretch is cut one blend longer than the loop. its last part, fading out, is laid over
    # its first part, fading in, and the rest follows, so the end of the loop runs into the start.
    graph = (
        f"[0:a]{shape},asplit=3[h][m][t];"
        f"[h]atrim=0:{BLEND},afade=t=in:d={BLEND}[head];"
        f"[t]atrim={LOOP}:{LOOP + BLEND},asetpts=PTS-STARTPTS,afade=t=out:d={BLEND}[tail];"
        f"[head][tail]amix=inputs=2:normalize=0[join];"
        f"[m]atrim={BLEND}:{LOOP},asetpts=PTS-STARTPTS[rest];"
        f"[join][rest]concat=n=2:v=0:a=1,loudnorm=I={LOUDNESS}:TP=-3:LRA=7,aresample=24000[out]"
    )
    # a faster take uses up more of the recording for the same length of loop
    ffmpeg("-ss", str(START), "-t", str((LOOP + BLEND) * 2), "-i", SOURCE,
           "-filter_complex", graph, "-map", "[out]", "-t", str(LOOP),
           "-c:a", "libmp3lame", "-b:a", "128k", out)
    return out


def make_sample(name, loop):
    os.makedirs(SAMPLES, exist_ok=True)
    out = os.path.join(SAMPLES, f"swap-swell-{name}-in-game.mp3")
    parts = [f"[1:a]asplit={len(SWAPS)}" + "".join(f"[s{n}]" for n in range(len(SWAPS)))]
    for n, (when, where) in enumerate(SWAPS):
        ms = int(when * 1000)
        parts.append(
            f"[s{n}]atrim={where}:{where + SWELL * 2},asetpts=PTS-STARTPTS,"
            f"afade=t=in:d={SWELL},afade=t=out:st={SWELL}:d={SWELL},adelay={ms}|{ms}[d{n}]"
        )
    mix = "[0:a]" + "".join(f"[d{n}]" for n in range(len(SWAPS)))
    parts.append(f"{mix}amix=inputs={len(SWAPS) + 1}:normalize=0:duration=first[out]")
    ffmpeg("-ss", "20", "-t", "12", "-i", SOURCE, "-i", loop,
           "-filter_complex", ";".join(parts), "-map", "[out]",
           "-c:a", "libmp3lame", "-b:a", "160k", out)
    return out


if __name__ == "__main__":
    for take, shape in TAKES:
        made = make_loop(take, shape)
        print("loop  ", os.path.normpath(made))
        print("sample", os.path.normpath(make_sample(take, made)))
