"""
Makes the sounds of the Morse code: the tone of the radio bips, that sounds while a push is held,
and the two signs, for when the radio comes on and when it goes off. Needs ffmpeg on the path.

    py Tools/morse_bip_gen.py                 from the Fade.MonoGame folder

It writes three takes of the tone into Assets/Fish/Audio, each a loop of LOOP seconds:

    morse-bip-radio.mp3    a tone with a little buzz and hiss in it, like a small radio
    morse-bip-clean.mp3    a plain tone, and a little higher
    morse-bip-low.mp3      a lower, rounder tone, more like a sonar

and the two signs:

    morse-on.mp3           two notes going up: the radio is on, and listening
    morse-off.mp3          three notes going down: nobody tapped for too long, and it is off

and, into ../morse-samples, how it sounds in the game, over the ambience. for each take of the
tone: the long hold that turns the radio on, the sign, and TUNA tapped out. and one more, of the
radio coming on, a few taps, and then going off after the wait.

The game plays the loop all the time with its volume at 0, and turns it up while something is
held down. see MORSE in fish_headers, and morseListen in fish_routines_morse.
"""
import os
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
AMBIENCE = os.path.join(AUDIO, "underwater-ambiencewav.mp3")
SAMPLES = os.path.join(HERE, "..", "..", "morse-samples")

RATE = 24000    # the same as the ambience
LOOP = 2        # how long the loop is, in seconds. every tone fits a whole number of times, so there is no seam.
VOLUME = 0.3    # how loud the game plays it. the same as MORSE_VOLUME# in fish_headers.
GAIN = 2.5      # the tones come out quiet, so they are turned up by this much
UNIT = 0.12     # how long a dot of the sample is, in seconds

# each take: its name, what it is made of, and the filters that mix them
TAKES = [
    ("radio",
     [f"sine=frequency=640:sample_rate={RATE}:duration={LOOP}",
      f"sine=frequency=1920:sample_rate={RATE}:duration={LOOP}",
      f"anoisesrc=color=pink:sample_rate={RATE}:amplitude=0.04:duration={LOOP}"],
     "[1:a]volume=0.22[buzz];[2:a]highpass=f=500,lowpass=f=3000[hiss];"
     "[0:a][buzz][hiss]amix=inputs=3:normalize=0,highpass=f=300,lowpass=f=3200[mix]"),
    ("clean",
     [f"sine=frequency=700:sample_rate={RATE}:duration={LOOP}"],
     "[0:a]anull[mix]"),
    ("low",
     [f"sine=frequency=440:sample_rate={RATE}:duration={LOOP}",
      f"sine=frequency=880:sample_rate={RATE}:duration={LOOP}"],
     "[1:a]volume=0.15[top];[0:a][top]amix=inputs=2:normalize=0,lowpass=f=1800[mix]"),
]

# the notes of the two signs: how high each one is, and how long, in seconds
SIGNS = [
    ("on", [(520, 0.09), (780, 0.16)]),
    ("off", [(780, 0.09), (520, 0.09), (390, 0.18)]),
]
SIGN_VOLUME = 0.35  # how loud the game plays them. the same as MORSE_SIGN_VOLUME# in fish_headers.
WAKE = 2.0          # how long the hold that turns the radio on is, in seconds. the same as MORSE_WAKE_MS.
SLEEP = 3.0         # and how long the radio waits before it goes off. the same as MORSE_SLEEP_MS.

# the word of the sample
WORD = ["-", "..-", "-.", ".-"]


def ffmpeg(*args):
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def make_loop(name, sources, graph):
    out = os.path.join(AUDIO, f"morse-bip-{name}.mp3")
    inputs = []
    for source in sources:
        inputs += ["-f", "lavfi", "-i", source]
    ffmpeg(*inputs, "-filter_complex", f"{graph};[mix]volume={GAIN}[out]", "-map", "[out]", "-t", str(LOOP),
           "-ar", str(RATE), "-ac", "1", "-c:a", "libmp3lame", "-b:a", "96k", out)
    return out


def make_sign(name, notes):
    out = os.path.join(AUDIO, f"morse-{name}.mp3")
    inputs = []
    parts = []
    for n, (pitch, length) in enumerate(notes):
        inputs += ["-f", "lavfi", "-i", f"sine=frequency={pitch}:sample_rate={RATE}:duration={length}"]
        parts.append(f"[{n}:a]afade=t=in:d=0.008,afade=t=out:st={length - 0.025:.3f}:d=0.025[n{n}]")
    row = "".join(f"[n{n}]" for n in range(len(notes)))
    parts.append(f"{row}concat=n={len(notes)}:v=0:a=1,volume={GAIN},apad=pad_dur=0.05[out]")
    ffmpeg(*inputs, "-filter_complex", ";".join(parts), "-map", "[out]",
           "-ar", str(RATE), "-ac", "1", "-c:a", "libmp3lame", "-b:a", "96k", out)
    return out


def pushes(word, at):
    """when every push of a word starts and ends, in seconds. a dash is three dots long, there is
    one dot of quiet between the pushes of a letter, and three between letters."""
    found = []
    for letter in word:
        for mark in letter:
            length = UNIT * (3 if mark == "-" else 1)
            found.append((at, at + length))
            at += length + UNIT
        at += UNIT * 2
    return found, at


def make_sample(out_name, loop, word, goes_off):
    """the ambience, with the radio coming on after a long hold, and a word tapped out. when
    `goes_off` is set, nobody taps any more after that, and the radio goes off."""
    os.makedirs(SAMPLES, exist_ok=True)
    out = os.path.join(SAMPLES, out_name)
    woke = 1.0 + WAKE
    held, end = pushes(word, woke + 0.9)
    keyed = "+".join(f"between(t,{start:.3f},{stop:.3f})" for start, stop in held)
    inputs = ["-ss", "20", "-t", f"{end + (SLEEP + 1.5 if goes_off else 1.5):.2f}", "-i", AMBIENCE,
              "-stream_loop", "-1", "-i", loop, "-i", os.path.join(AUDIO, "morse-on.mp3")]
    graph = (
        f"[1:a]volume='{VOLUME}*({keyed})':eval=frame[bips];"
        f"[2:a]volume={SIGN_VOLUME},adelay={int(woke * 1000)}[on];"
    )
    mixed = "[0:a][bips][on]"
    if goes_off:
        inputs += ["-i", os.path.join(AUDIO, "morse-off.mp3")]
        last = held[-1][1]
        graph += f"[3:a]volume={SIGN_VOLUME},adelay={int((last + SLEEP) * 1000)}[off];"
        mixed += "[off]"
    graph += f"{mixed}amix=inputs={4 if goes_off else 3}:normalize=0:duration=first[out]"
    ffmpeg(*inputs, "-filter_complex", graph, "-map", "[out]", "-c:a", "libmp3lame", "-b:a", "160k", out)
    return out


if __name__ == "__main__":
    for sign, notes in SIGNS:
        print("sign  ", os.path.normpath(make_sign(sign, notes)))
    for take, parts, mix in TAKES:
        made = make_loop(take, parts, mix)
        print("loop  ", os.path.normpath(made))
        print("sample", os.path.normpath(make_sample(f"morse-tuna-{take}.mp3", made, WORD, False)))
        if take == "radio":
            print("sample", os.path.normpath(make_sample("morse-goes-off.mp3", made, [".-", "."], True)))
