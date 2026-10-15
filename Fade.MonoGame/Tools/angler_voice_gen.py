"""Makes the voice of the old angler fish: a little bubble for every vowel. see THE VOICE in
fish_headers_angler. Needs ffmpeg on the path.

    py Tools/angler_voice_gen.py               from the Fade.MonoGame folder: samples of every style
    py Tools/angler_voice_gen.py pop           and puts the "pop" style into the game, too

There are a few STYLES of voice to try. every run renders all of them into ../angler-voice-samples,
a folder for each, to judge by ear:

    vowels.mp3      the six bubbles, one after the other: ah, eh, ee, oh, oo, uh
    line-N.mp3      the angler saying a few of its lines, over the ambience, by the same rules that
                    the game says them by (see SPEAK below, and anglerSpeakLetter in fish_routines_angler)

and when a style is named, its six sounds are written into Assets/Fish/Audio, as
angler-ah.mp3, angler-eh.mp3, angler-ee.mp3, angler-oh.mp3, angler-oo.mp3 and angler-uh.mp3,
which is what the game plays. the styles:

    voice       a buzz at the pitch of a low voice, rising like a bubble, with the overtones near
                the formants of the vowel made louder. the most like talking.
    pop         a bubble popping: one tone that jumps up in pitch as it dies away, very short. the
                vowel only changes how high it is, ee the highest and oo the lowest.
    bloop       two of those at once, at the first two formants of the vowel, so that it is a
                bubble, and a little like the vowel.
    recorded    the bubble pops that the game has already (bubble-pop-2 and bubble-pop-4), each one
                cut down to the pop itself, pitched for the vowel, and with the formants of the
                vowel made louder. real bubbles, and so the least like a synthesizer.
"""
import math
import os
import random
import struct
import subprocess
import sys
import wave

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.join(HERE, "..", "Assets", "Fish", "Audio")
AMBIENCE = os.path.join(AUDIO, "underwater-ambiencewav.mp3")
SAMPLES = os.path.join(HERE, "..", "..", "angler-voice-samples")

RATE = 24000          # the same as the ambience
NAMES = ["ah", "eh", "ee", "oh", "oo", "uh"]

# the formants of each vowel, in hertz, with how loud and how wide each one is. a little higher than
# a man's, because it is a fish, and a cartoon.
VOWELS = {
    "ah": ((800, 1.0, 90), (1200, 0.6, 110), (2600, 0.25, 160)),
    "eh": ((580, 1.0, 80), (1950, 0.55, 110), (2650, 0.25, 160)),
    "ee": ((300, 1.0, 60), (2400, 0.5, 120), (3100, 0.3, 160)),
    "oh": ((600, 1.0, 80), (900, 0.7, 90), (2500, 0.15, 160)),
    "oo": ((330, 1.0, 60), (900, 0.5, 90), (2350, 0.12, 160)),
    "uh": ((680, 1.0, 90), (1250, 0.55, 110), (2500, 0.2, 160)),
}

# how high each vowel's bubble is, for the styles where the pitch is all there is to tell them
# apart: from oo, the lowest, to ee, the highest. in octaves above the bubble's own pitch.
VOWEL_PITCH = {"oo": 0.0, "oh": 0.15, "uh": 0.3, "ah": 0.45, "eh": 0.65, "ee": 0.85}

# the volume that the game plays them at. the same as ANGLER_VOICE_VOLUME# in fish_headers_angler.
VOICE_VOLUME = 0.55


# THE PIECES

def lowpass(samples, cutoff):
    alpha = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    out = []
    last = 0.0
    for v in samples:
        last += alpha * (v - last)
        out.append(last)
    return out


def peaking(samples, freq, gain_db, q):
    """Make the sound around a frequency louder: a peaking filter, from the audio EQ cookbook."""
    a = 10 ** (gain_db / 40)
    w = 2 * math.pi * freq / RATE
    alpha = math.sin(w) / (2 * q)
    b0, b1, b2 = 1 + alpha * a, -2 * math.cos(w), 1 - alpha * a
    a0, a1, a2 = 1 + alpha / a, -2 * math.cos(w), 1 - alpha / a
    out = []
    x1 = x2 = y1 = y2 = 0.0
    for x in samples:
        y = (b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0
        out.append(y)
        x2, x1 = x1, x
        y2, y1 = y1, y
    return out


def normalize(samples, peak=0.85):
    top = max(abs(v) for v in samples) or 1
    return [v / top * peak for v in samples]


def repitch(samples, octaves):
    """Play it faster or slower, so it is higher or lower, and shorter or longer."""
    rate = 2 ** octaves
    out = []
    pos = 0.0
    while pos < len(samples) - 1:
        a = int(pos)
        frac = pos - a
        out.append(samples[a] * (1 - frac) + samples[a + 1] * frac)
        pos += rate
    return out


def chirp(freq_from, freq_to, length, decay, rise=0.25):
    """One bubble: a tone that slides from one pitch to another as it dies away. `rise` is how much of
    the length it takes to get most of the way up, and `decay` how quickly it dies, in seconds."""
    count = int(RATE * length)
    out = []
    phase = 0.0
    for n in range(count):
        t = n / RATE
        u = min(1.0, t / (length * rise))
        freq = freq_from + (freq_to - freq_from) * (1 - (1 - u) ** 2)
        phase += 2 * math.pi * freq / RATE
        attack = min(1.0, n / (RATE * 0.002))
        out.append(math.sin(phase) * attack * math.exp(-t / decay))
    return out


def formant_gain(freq, formants):
    gain = 0.04
    for centre, loud, width in formants:
        gain += loud / (1 + ((freq - centre) / width) ** 2)
    return gain


# THE STYLES. each takes the name of a vowel, and gives back its sound.

def style_voice(name):
    formants = VOWELS[name]
    length = 0.12
    pitch_from, pitch_to = 132.0, 178.0
    count = int(RATE * length)
    out = [0.0] * count
    phases = {}
    for n in range(count):
        t = n / count
        pitch = pitch_from * (pitch_to / pitch_from) ** (1 - (1 - t) ** 2)
        sample = 0.0
        k = 1
        while pitch * k < 4800:
            phases[k] = phases.get(k, 0.0) + 2 * math.pi * pitch * k / RATE
            sample += formant_gain(pitch * k, formants) / k ** 0.6 * math.sin(phases[k])
            k += 1
        attack = min(1.0, n / (RATE * 0.008))
        out[n] = sample * attack * (1 - t) ** 1.6
    return normalize(lowpass(out, 2600))


def style_pop(name):
    base = 420 * 2 ** VOWEL_PITCH[name]
    return normalize(lowpass(chirp(base, base * 1.9, 0.07, 0.018), 4000))


def style_bloop(name):
    f1, f2 = VOWELS[name][0][0], VOWELS[name][1][0]
    low = chirp(f1 * 0.7, f1 * 1.25, 0.09, 0.03)
    high = chirp(f2 * 0.7, f2 * 1.25, 0.09, 0.018)
    return normalize(lowpass([a + 0.45 * b for a, b in zip(low, high)], 3800))


_RECORDED = None


def recorded_pops():
    """The two bubble pops that the game has, cut down to the pop. each has one pop in it, about a
    tenth of a second in, and it is gone in about fifty milliseconds."""
    global _RECORDED
    if _RECORDED is None:
        _RECORDED = []
        for clip, start, length in (("bubble-pop-2", 0.095, 0.07), ("bubble-pop-4", 0.125, 0.08)):
            raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", os.path.join(AUDIO, clip + ".mp3"),
                                  "-ac", "1", "-ar", str(RATE), "-f", "s16le", "-"], capture_output=True, check=True).stdout
            values = struct.unpack("<%dh" % (len(raw) // 2), raw)
            cut = [v / 32768 for v in values[int(start * RATE):int((start + length) * RATE)]]
            # fade the very end, so that it does not click
            fade = int(RATE * 0.01)
            for i in range(fade):
                cut[-1 - i] *= i / fade
            _RECORDED.append(cut)
    return _RECORDED


def style_recorded(name):
    pops = recorded_pops()
    # the two pops take turns between the vowels, so that they are not all the same bubble
    pop = pops[NAMES.index(name) % 2]
    pop = repitch(pop, VOWEL_PITCH[name] - 0.3)
    for centre, loud, width in VOWELS[name][:2]:
        pop = peaking(pop, centre, 9 * loud, centre / (width * 2))
    return normalize(pop)


MUSIC = os.path.join(AUDIO, "Music")
_GRAINS = {}


def grain(clip, length, skip=0):
    """A short piece of a recording in Assets/Fish/Audio/Music: the loudest stretch of it that is
    `length` seconds long, or the next loudest one after `skip` others, with its ends faded so that
    it does not click. the recordings of the horror coral hold a note for a while, and a piece of
    that is a moment of a voice."""
    key = (clip, length, skip)
    if key not in _GRAINS:
        raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", os.path.join(MUSIC, clip + ".mp3"),
                              "-ac", "1", "-ar", str(RATE), "-f", "s16le", "-"], capture_output=True, check=True).stdout
        values = [v / 32768 for v in struct.unpack("<%dh" % (len(raw) // 2), raw)]
        size = int(RATE * length)
        step = size // 2
        windows = []
        for start in range(0, len(values) - size, step):
            piece = values[start:start + size]
            windows.append((sum(v * v for v in piece), start))
        windows.sort(reverse=True)
        # the loud stretches that overlap the ones before are passed over
        picked = []
        for loud, start in windows:
            if all(abs(start - other) >= size for other in picked):
                picked.append(start)
            if len(picked) > skip:
                break
        start = picked[min(skip, len(picked) - 1)]
        piece = values[start:start + size]
        fade = int(RATE * 0.015)
        for i in range(fade):
            piece[i] *= i / fade
            piece[-1 - i] *= i / fade
        _GRAINS[key] = piece
    return list(_GRAINS[key])


def horror(name, clips, length, pitch_shift, boost=9):
    """A vowel out of the horror recordings: a grain of one of them, pitched for the vowel, with the
    formants of the vowel made louder. the vowels take turns between the clips, and between the
    loudest pieces of each, so that they are not all the same moment."""
    index = NAMES.index(name)
    clip = clips[index % len(clips)]
    piece = grain(clip, length, index // len(clips))
    piece = repitch(piece, VOWEL_PITCH[name] * 0.5 + pitch_shift)
    for centre, loud, width in VOWELS[name][:2]:
        piece = peaking(piece, centre, boost * loud, centre / (width * 2))
    # a quick swell and a fall, like a syllable
    count = len(piece)
    for n in range(count):
        t = n / count
        piece[n] *= min(1.0, t / 0.15) * (1 - t) ** 0.8
    return normalize(piece)


def style_wail(name):
    return horror(name, ["wail-d4", "wail-g4", "wail-a4"], 0.13, 0.0)


def style_scream(name):
    return horror(name, ["shot-scream-4", "shot-scream-5", "shot-scream-6"], 0.12, -0.2)


def style_dirge(name):
    return horror(name, ["shot-dirge-1", "shot-dirge-2", "shot-dirge-3"], 0.16, -0.3)


STYLES = {
    "voice": style_voice,
    "pop": style_pop,
    "bloop": style_bloop,
    "recorded": style_recorded,
    # the horror coral's recordings, for a voice from the dark. see THE MUSIC OF THE CORAL in fish_headers.
    "wail": style_wail,
    "scream": style_scream,
    "dirge": style_dirge,
}


# SPEAK. the rules that the game says a line by. they are the same as anglerSpeakLetter, in
# fish_routines_angler, and the numbers are the ones in fish_headers_angler.
LETTER_MS = 38            # ANGLER_LETTER_MS
PAUSE_MS = {",": 150, ".": 260, "?": 260, "!": 260, ":": 150}   # ANGLER_COMMA_MS, ANGLER_STOP_MS
QUESTION_PITCH = 0.35     # ANGLER_ASK_PITCH#: how much the last word before a ? goes up, in octaves
QUESTION_STEP = 0.12      # ANGLER_ASK_STEP#: and how much more for every bubble of it after the first
SHOUT_VOLUME = 1.6        # ANGLER_SHOUT#: how much louder a word is before a !
TRAIL_FADE = 0.55         # ANGLER_TRAIL#: every bubble of the last word before ... is this much quieter than the one before
JITTER = 0.06             # ANGLER_JITTER#: how far a bubble's pitch wanders, either way, in octaves


def vowel_of(text, i):
    """The sound for the vowel at i, or None if it is not the start of a run of vowels."""
    c = text[i].lower()
    if c not in "aeiouy":
        return None
    if i > 0 and text[i - 1].lower() in "aeiou":
        return None
    if c == "y":
        # a y at the start of a word is not a vowel
        if i == 0 or not text[i - 1].isalpha():
            return None
        return "ee"
    nxt = text[i + 1].lower() if i + 1 < len(text) else ""
    if c == "o" and nxt in "ou":
        return "oo"
    if c == "e" and nxt in "ea":
        return "ee"
    return {"a": "ah", "e": "eh", "i": "ee", "o": "oh", "u": "uh"}[c]


def word_end(text, i):
    """What the word that i is in ends with: '?', '!', '...', or ''."""
    j = i
    while j < len(text) and text[j] != " ":
        j += 1
    tail = text[i:j]
    if tail.endswith("..."):
        return "..."
    if tail.endswith("?"):
        return "?"
    if tail.endswith("!"):
        return "!"
    return ""


def speak(line):
    """The line, said the way the game says it: a list of (when, sound, pitch, volume)."""
    events = []
    ms = 0
    word_bubble = 0
    for i, c in enumerate(line):
        if c == " ":
            word_bubble = 0
        sound = vowel_of(line, i)
        if sound:
            pitch = random.uniform(-JITTER, JITTER)
            volume = 1.0
            end = word_end(line, i)
            if end == "?":
                pitch += QUESTION_PITCH + QUESTION_STEP * word_bubble
            if end == "!":
                volume = SHOUT_VOLUME
            if end == "...":
                volume = TRAIL_FADE ** (word_bubble + 1)
            events.append((ms, sound, pitch, min(volume * VOICE_VOLUME, 1.0)))
            word_bubble += 1
        ms += PAUSE_MS.get(c, LETTER_MS)
    return events, ms


def render(events, length_ms, sounds):
    total = int(RATE * (length_ms / 1000 + 0.5))
    out = [0.0] * total
    for when, sound, pitch, volume in events:
        start = int(RATE * when / 1000)
        for n, value in enumerate(repitch(sounds[sound], pitch)):
            if start + n < total:
                out[start + n] += value * volume
    return out


# WRITING THEM OUT

def write_wav(path, samples):
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v)) * 32767)) for v in samples))


def to_mp3(samples, path, over_ambience=False):
    wav = path + ".wav"
    write_wav(wav, samples)
    command = ["ffmpeg", "-y", "-loglevel", "error", "-i", wav]
    if over_ambience:
        # over the ambience, at about the level that the game plays it at
        command += ["-i", AMBIENCE, "-filter_complex",
                    "[1:a]volume=0.5[amb];[0:a][amb]amix=inputs=2:duration=first:normalize=0"]
    command += ["-ar", str(RATE), "-ac", "1", "-c:a", "libmp3lame", "-q:a", "3", path]
    subprocess.run(command, check=True)
    os.remove(wav)


LINES = [
    "Ask, little fish. I have been down here long.",
    "Every swap that pops something fills a step of its bar.",
    "What does the moon want?",
    "Then I will wait in the dark!",
    "Blink slowly. I cannot blink, so I would not know...",
]


def main():
    chosen = sys.argv[1] if len(sys.argv) > 1 else None
    if chosen is not None and chosen not in STYLES:
        sys.exit("no style called " + chosen + ". there is " + ", ".join(STYLES))

    for style, make in STYLES.items():
        random.seed(7)
        folder = os.path.join(SAMPLES, style)
        os.makedirs(folder, exist_ok=True)
        sounds = {name: make(name) for name in NAMES}

        # the six of them in a row, with a little gap between
        row = []
        for name in NAMES:
            row += [v * VOICE_VOLUME for v in sounds[name]] + [0.0] * int(RATE * 0.25)
        to_mp3(row, os.path.join(folder, "vowels.mp3"))

        for n, line in enumerate(LINES):
            events, length = speak(line)
            to_mp3(render(events, length, sounds), os.path.join(folder, "line-%d.mp3" % (n + 1)), True)
        print(style, "->", folder)

        if style == chosen:
            for name in NAMES:
                to_mp3(sounds[name], os.path.join(AUDIO, "angler-" + name + ".mp3"))
            print("  and into the game")


main()
