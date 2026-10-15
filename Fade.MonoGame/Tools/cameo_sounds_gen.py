"""
Makes the two sounds of the cameos: the things that swim through when one of the secret words is
tapped out on the radio. see CAMEOS in fish_headers_cameo. Needs ffmpeg on the path, and Windows,
for the voice.

    py Tools/cameo_sounds_gen.py              from the Fade.MonoGame folder

It writes these into Assets/Fish/Audio:

    cameo-jaws.mp3         two low notes, a half step apart, over and over, faster and faster. for JAWS.
    cameo-my-leg.mp3       somebody shouts "my leg!". for MYLEG. it is cut out of the recording that
                           is next to this file, cameo-my-leg-recording.m4a, and cleaned up.
    cameo-my-leg-squeak.mp3  the voice that Windows reads text out with, turned up in pitch,
    cameo-my-leg-gruff.mp3   and turned down. either can go there in its place.

and, into ../cameo-samples, how each one sounds in the game, over the ambience.

Nothing here is taken from a film or a show. the notes are made out of sine waves, the recording
is Chris, shouting into a microphone, and the other two voices are the one that Windows reads text
out with. when the recording is not there, the "squeak" take is what the game gets.
"""
import os
import shutil
import subprocess
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
AMBIENCE = os.path.join(AUDIO, "underwater-ambiencewav.mp3")
SAMPLES = os.path.join(HERE, "..", "..", "cameo-samples")

RATE = 24000         # the same as the ambience
JAWS_VOLUME = 0.55   # how loud the game plays the notes. the same as CAMEO_JAWS_VOLUME# in fish_headers_cameo.
LEG_VOLUME = 0.7     # and the voice. the same as CAMEO_LEG_VOLUME#.
JAWS_LENGTH = 9.0    # how long the notes go on for, in seconds. the fin takes this long to cross: see setup_cameo.

LOW = 82.41          # the two notes: E, and the F above it
HIGH = 87.31

# the takes of the voice that Windows reads out: its name, how much higher it is, and how much faster on top of that
VOICES = [
    ("-squeak", 1.38, 0.92),
    ("-gruff", 0.82, 1.05),
]

RECORDING = os.path.join(HERE, "cameo-my-leg-recording.m4a")
SHOUT_FROM = 0.76    # where the shout is in the recording, in seconds, with a little room on both sides
SHOUT_TO = 1.66


def ffmpeg(*args):
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def jaws_notes():
    """when every note starts, how long it is, and how high. they come in pairs, and both the wait
    between the pairs and the notes themselves get shorter."""
    notes = []
    at = 0.0
    gap = 2.0
    length = 0.5
    while at + length * 2.2 < JAWS_LENGTH:
        notes.append((at, length, LOW))
        notes.append((at + length * 1.1, length, HIGH))
        at += max(length * 2.2, gap)
        gap *= 0.7
        length = max(0.16, length * 0.88)
    return notes


def make_jaws():
    out = os.path.join(AUDIO, "cameo-jaws.mp3")
    notes = jaws_notes()
    inputs = []
    parts = []
    for n, (at, length, pitch) in enumerate(notes):
        # a low string: the note, and the first few notes above it that ring along
        wave = "+".join(f"{loud}*sin({2 * k}*PI*{pitch}*t)" for k, loud in ((1, 0.5), (2, 0.3), (3, 0.2), (4, 0.12), (5, 0.07)))
        inputs += ["-f", "lavfi", "-i", f"aevalsrc='{wave}':sample_rate={RATE}:duration={length:.3f}"]
        parts.append(f"[{n}:a]afade=t=in:d=0.03,afade=t=out:st={length * 0.55:.3f}:d={length * 0.45:.3f},"
                     f"adelay={int(at * 1000)}[n{n}]")
    row = "".join(f"[n{n}]" for n in range(len(notes)))
    # they get louder as they get faster
    parts.append(f"{row}amix=inputs={len(notes)}:normalize=0,lowpass=f=1400,"
                 f"volume='0.4+0.5*t/{JAWS_LENGTH}':eval=frame[out]")
    ffmpeg(*inputs, "-filter_complex", ";".join(parts), "-map", "[out]", "-t", str(JAWS_LENGTH),
           "-ar", str(RATE), "-ac", "1", "-c:a", "libmp3lame", "-b:a", "96k", out)
    return out


def speak(words, path):
    """Windows reads the words out, into a wav file"""
    script = ("Add-Type -AssemblyName System.Speech; "
              "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; "
              "$s.Rate = 2; "
              f"$s.SetOutputToWaveFile('{path}'); "
              f"$s.Speak('{words}'); "
              "$s.Dispose()")
    subprocess.run(["powershell", "-NoProfile", "-Command", script], check=True)


def make_voice(spoken, name, pitch, pace):
    out = os.path.join(AUDIO, f"cameo-my-leg{name}.mp3")
    # the pitch is moved by playing it at another speed, and the speed is then put back, and changed a little
    tempo = pace / pitch
    graph = (f"[0:a]silenceremove=start_periods=1:start_threshold=-45dB,aresample={RATE},"
             f"asetrate={int(RATE * pitch)},aresample={RATE},atempo={tempo:.3f},"
             f"highpass=f=180,lowpass=f=4200,volume=2.2,alimiter=limit=0.95,apad=pad_dur=0.1[out]")
    ffmpeg("-i", spoken, "-filter_complex", graph, "-map", "[out]",
           "-ar", str(RATE), "-ac", "1", "-c:a", "libmp3lame", "-b:a", "96k", out)
    return out


def make_shout():
    """the shout, cut out of the recording: the hum under the voice and the hiss of the room are
    taken off, it is evened out a little, and it is turned up as far as it goes"""
    out = os.path.join(AUDIO, "cameo-my-leg.mp3")
    length = SHOUT_TO - SHOUT_FROM
    graph = (f"[0:a]atrim=start={SHOUT_FROM}:end={SHOUT_TO},asetpts=PTS-STARTPTS,"
             f"pan=mono|c0=0.5*c0+0.5*c1,highpass=f=90,afftdn=nr=14:nf=-48,"
             f"acompressor=threshold=-18dB:ratio=3:attack=4:release=90:makeup=2,"
             f"afade=t=in:d=0.02,afade=t=out:st={length - 0.08:.3f}:d=0.08,"
             f"volume=6dB,alimiter=limit=0.9,apad=pad_dur=0.1[out]")
    ffmpeg("-i", RECORDING, "-filter_complex", graph, "-map", "[out]",
           "-ar", str(RATE), "-ac", "1", "-c:a", "libmp3lame", "-b:a", "128k", out)
    return out


def make_sample(out_name, clip, volume, length):
    """the ambience, with the sound coming in after a second"""
    os.makedirs(SAMPLES, exist_ok=True)
    out = os.path.join(SAMPLES, out_name)
    ffmpeg("-ss", "20", "-t", f"{length:.2f}", "-i", AMBIENCE, "-i", clip,
           "-filter_complex", f"[1:a]volume={volume},adelay=1000[one];[0:a][one]amix=inputs=2:normalize=0:duration=first[out]",
           "-map", "[out]", "-c:a", "libmp3lame", "-b:a", "160k", out)
    return out


if __name__ == "__main__":
    made = make_jaws()
    print("notes ", os.path.normpath(made))
    print("sample", os.path.normpath(make_sample("jaws.mp3", made, JAWS_VOLUME, JAWS_LENGTH + 2)))

    with tempfile.TemporaryDirectory() as folder:
        spoken = os.path.join(folder, "spoken.wav")
        speak("My leg!", spoken)
        for name, pitch, pace in VOICES:
            made = make_voice(spoken, name, pitch, pace)
            print("voice ", os.path.normpath(made))
            print("sample", os.path.normpath(make_sample(f"my-leg{name}.mp3", made, LEG_VOLUME, 4)))
            if name == "-squeak" and not os.path.exists(RECORDING):
                shutil.copyfile(made, os.path.join(AUDIO, "cameo-my-leg.mp3"))

    if os.path.exists(RECORDING):
        made = make_shout()
        print("shout ", os.path.normpath(made))
        print("sample", os.path.normpath(make_sample("my-leg.mp3", made, LEG_VOLUME, 4)))
