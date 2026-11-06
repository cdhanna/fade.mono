"""
Puts the trailer together: the shots that build/render-trailer.ps1 recorded, cut and joined in the
order that build/trailer_cut.json gives, with the music and the sounds of the game mixed under them.

    py -3 build/trailer_cut.py                      make the trailer
    py -3 build/trailer_cut.py --plan               only print where everything lands, and make nothing
    py -3 build/trailer_cut.py --preview            make it quickly, at half the size
    py -3 build/trailer_cut.py --cut other.json     use another cut
    py -3 build/trailer_cut.py --music build/music/a.json build/music/b.json
                                                    the same trailer with other music, one video for each

THE SHOTS. every shot is a video, and a .json of the same name next to it that the game wrote while
it recorded (see RecordSystem in Fade.MonoGame.Game): how many frames it has, every sound that the
game played and on which frame, and the marks that the shot made with `record mark`, such as "swap"
or "pop". the sounds are not in the videos. they are mixed back in here, from the game's own files.

THE MUSIC is a melody of the game's, played on the game's own instruments: the notes are read out of
setup_melodies in Fade.MonoGame/fish_routines_music.fbasic, so it is the same tune as in the game.
every melody there asks for a chord, and that chord's pad plays under it, as it does in the game.

LINING THE SHOTS UP WITH THE MUSIC. a shot can say which of its marks should land on a note:
    "sync": {"mark": "swap", "note": "nearest"}
moves where the shot is cut in, so that its first "swap" lands on the note that is nearest to where
it would have landed. "note" can also be a number: the note of the tune, counting from 0, and
"occurrence" picks a later mark of the same name. the shot is cut in later or earlier to make it
land. its end stays where it is, so the shot gets longer or shorter ("keep_length": true moves its end
with its start instead).

A SHOT in the cut is:
    {"clip": "trailer_shark", "in": 0, "out": null, "sync": {...}, "sounds": true}
"in" and "out" are frames of the video. "out" can be null for the end, or less than 0 to count back
from the end. either can be a mark of the shot instead: {"mark": "wave", "before": 5}.
"sounds": false leaves the game's sounds out of that shot.

SOUNDS OF ITS OWN. the cut can add sounds by hand, at a time in the trailer, or at a frame of a shot:
    "sounds": [{"at": 1.5, "asset": "Fish/Audio/bubble-wave", "volume": 0.5},
               {"shot": 3, "frame": 40, "asset": "Fish/Audio/diver-splash"}]
"skip_sounds" lists the starts of the asset names of game sounds to leave out. the game's own music
is left out by default, because the trailer has music of its own.

Only the standard library is used, and ffmpeg and ffprobe, which have to be on the PATH.
"""

import argparse
import json
import math
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
ASSETS = os.path.join(REPO, "Fade.MonoGame", "Assets")
MUSIC_SOURCE = os.path.join(REPO, "Fade.MonoGame", "fish_routines_music.fbasic")

# the game's notes, and the names of their sound files
NOTE_FILES = {
    "D4": "d4", "E4": "e4", "F4": "f4", "G4": "g4", "A4": "a4", "Bb4": "bb4", "C5": "c5",
    "D5": "d5", "E5": "e5", "F5": "f5", "G5": "g5", "A5": "a5", "Bb5": "bb5", "C6": "c6",
}
INSTRUMENTS = {"INSTRUMENT_BELL": "note", "INSTRUMENT_PLUCK": "pluck", "INSTRUMENT_SWELL": "swell",
               "INSTRUMENT_CHOIR": "choir", "INSTRUMENT_WAIL": "wail"}
CHORD_PADS = {"MUSIC_CHORD_DM": "loop-pad-dm", "MUSIC_CHORD_BB": "loop-pad-bb"}


def fail(message):
    print("trailer_cut: " + message, file=sys.stderr)
    sys.exit(1)


def asset_file(asset):
    """the sound file of an asset that the game loads, like Fish/Audio/bubble-pop-2"""
    if os.path.isabs(asset):
        return asset if os.path.exists(asset) else None
    path = os.path.join(ASSETS, asset.replace("/", os.sep))
    for ending in (".mp3", ".wav", ".ogg"):
        if os.path.exists(path + ending):
            return path + ending
    return None


# THE MELODIES

def read_melodies():
    """every melody of setup_melodies: its comment, how long a beat is, its instrument, chord and beats"""
    with open(MUSIC_SOURCE, encoding="utf-8") as f:
        lines = f.read().splitlines()
    melodies = []
    comment = ""
    inside = False
    for line in lines:
        text = line.strip()
        if text.startswith("setup_melodies:"):
            inside = True
            continue
        if not inside:
            continue
        if text == "return":
            break
        if text.startswith("`"):
            comment = (comment + " " + text.lstrip("` ")).strip()
            continue
        new = re.match(r"newMelody\((\d+),\s*(\w+),\s*(\w+)\)", text)
        if new:
            melodies.append({"comment": comment.lower(), "beat_ms": int(new.group(1)),
                             "instrument": new.group(2), "chord": new.group(3), "beats": []})
            comment = ""
            continue
        bar = re.match(r"melodyBar9?\((.*)\)", text)
        if bar and melodies:
            melodies[-1]["beats"].extend(token.strip() for token in bar.group(1).split(","))
            continue
        if text:
            comment = ""
    return melodies


def build_music(music):
    """the notes and the pads of the music, each with the time it starts at, in seconds"""
    name = music.get("melody", "clair de lune").lower()
    parts = [m for m in read_melodies() if name in m["comment"]]
    if not parts:
        fail(f'there is no melody called "{name}" in {MUSIC_SOURCE}')

    start = float(music.get("start", 0))
    repeat = int(music.get("repeat", 1))
    notes = []
    beats = []
    pads = []
    at = start
    for _ in range(repeat):
        for part in parts:
            beat = float(music.get("beat_ms", part["beat_ms"])) / 1000.0
            instrument = INSTRUMENTS.get(part["instrument"], "note")
            if music.get("instrument"):
                instrument = music["instrument"]
            length = beat * len(part["beats"])
            pad = CHORD_PADS.get(part["chord"])
            if pad:
                pads.append({"asset": "Fish/Audio/Music/" + pad, "at": at, "length": length})
            for n, token in enumerate(part["beats"]):
                beats.append(at + n * beat)
                if token == "REST":
                    continue
                if token not in NOTE_FILES:
                    fail(f"the melody has a note that is not known: {token}")
                notes.append({"asset": f"Fish/Audio/Music/{instrument}-{NOTE_FILES[token]}",
                              "at": at + n * beat, "name": token})
            at += length
    return notes, beats, pads, at



# THE SCORE: music that is written against the pictures, in place of a melody that runs on its own clock.
# a variant of the music (build/music/*.json, see --music) can have a "score", a list of cues. every cue
# starts at an ANCHOR, a moment of the trailer:
#     2.5                                         seconds into the trailer
#     {"shot": "trailer_quest_grid", "mark": "punch", "n": 2, "offset": -0.05}
#                                                 the 2nd "punch" mark of that shot (n can be -1 for the last)
#     {"shot": "trailer_finale", "edge": "start"}  where the shot starts. "settled" is after the transition into
#                                                 it is over, and "end" is where it ends
#     {"end": 0}                                  the end of the trailer
# and the cues are:
#     {"type": "notes", "at": A, "notes": ["D4", "A4"], "instrument": "note", "volume": 0.4, "strum": 0.03}
#         a chord, or one note
#     {"type": "each", "on": {"shot": ..., "mark": "word"}, "notes": ["D5", "F5", ...], ...}
#         a note on every one of those marks, the next note of the list each time ("on" can be a list of anchors)
#     {"type": "phrase", "melody": "gymnopedie", "parts": [0, 1], "at": A, "beat_ms": 900, "pins": [[4, A], ...]}
#         a melody of the game's. "pins" ties its notes (counted from 0, rests left out) to anchors: the
#         music is stretched and squeezed between them so that each of those notes lands on its moment.
#         "until" stretches the whole of it to end on an anchor instead. or "land": [n, A] places it,
#         unstretched, so that note n lands on A ("end_at": A also sets one even tempo to end there).
#         "stop_at": A leaves out the notes from A on, when the cut has that moment, and "rest": [[A, A]]
#         leaves out the ones between two moments.
# a cue about a shot that the cut does not have is left out, so one score can go with several cuts.
#     {"type": "pulse", "from": A, "to": A, "beat_ms": 400, "pattern": ["A4", "D5", "F5"], "volume_to": 0.5}
#         a pattern over and over. "beat_ms": "fit" picks the beat between "fit_ms" [lo, hi] that puts
#         the anchors of "fit_to" closest to beats. "lock_to" (anchors) bends the beat a little between
#         them so that every one lands on a beat. "every": 3 plays the pattern on every third beat only.
#         "id" names its beats, and a phrase with "snap": that id has its notes moved onto them.
#     {"type": "pad", "chord": "dm", "from": A, "to": A, "volume": 0.3}
#     {"type": "morse", "at": A, "text": "CQ", "unit_ms": 95, "letter_gap": 4, "asset": "Fish/Audio/morse-bip-radio"}
#         the game's morse tone, keyed out letter by letter, the way the shooting star spells it
#     {"type": "sound", "at": A, "asset": "Fish/Audio/Music/shot-whale-low", "volume": 0.5, "pitch": 0, "until": A}
#         or "file": "build/trailer-sounds/whoosh-up.wav" for a sound that the game does not have (see
#         build/trailer_sounds.py). a sound is music, and goes with "music_fade", unless it has "keep": true.
# a variant can also have "music_fade": {"at": A, "seconds": 3}: the music, its pads and the sound of the
# sea all fade out there, and the sounds of the game and the ones that "keep" go on. and the cut's
# "music": {"music_gain": 0.6} turns all of that down (or up) against the sounds of the game.
# every cue can have "instrument" (note, pluck, swell, choir, wail), "volume", "octave" (-1 is an octave
# down, played slower) and "pan".

class AnchorMissing(Exception):
    """a cue of the score is about a shot, or a mark, that the cut does not have. the cue is left out."""


def shot_named(shots, name):
    for shot in shots:
        if shot["name"] == name:
            return shot
    raise AnchorMissing(f"there is no shot called {name} in the cut")


def anchor_time(shots, total, fps, anchor):
    if isinstance(anchor, (int, float)):
        return float(anchor)
    offset = float(anchor.get("offset", 0))
    if "t" in anchor:
        return float(anchor["t"]) + offset
    if "end" in anchor:
        return total + float(anchor["end"]) + offset
    shot = shot_named(shots, anchor["shot"])
    if "mark" in anchor:
        frames = [m["frame"] for m in shot["sidecar"].get("marks", []) if m["name"] == anchor["mark"]]
        n = int(anchor.get("n", 1))
        if not frames or abs(n) > len(frames) or n == 0:
            raise AnchorMissing(f'{shot["name"]} has {len(frames)} "{anchor["mark"]}" marks, and the score wants number {n}')
        frame = frames[n - 1] if n > 0 else frames[n]
        return shot["at"] + (frame - shot["in"] + shot["pad"]) / fps + offset
    edge = anchor.get("edge", "start")
    if edge == "settled":
        return shot["at"] + shot["overlap"] / fps + offset
    if edge == "end":
        return shot["at"] + shot["length"] + offset
    return shot["at"] + offset


def anchor_list(shots, total, fps, on):
    """every moment that an "on" means: a list of anchors, or every mark of a name in a shot"""
    if isinstance(on, list):
        times = []
        for a in on:
            times += anchor_list(shots, total, fps, a) if isinstance(a, dict) and "mark" in a and "n" not in a else [anchor_time(shots, total, fps, a)]
        return times
    if isinstance(on, dict) and "mark" in on and "n" not in on:
        shot = shot_named(shots, on["shot"])
        count = len([m for m in shot["sidecar"].get("marks", []) if m["name"] == on["mark"]])
        times = [anchor_time(shots, total, fps, {**on, "n": n}) for n in range(1, count + 1)]
        first, last = int(on.get("from", 1)), int(on.get("to", count))
        # only the ones that are in the part of the shot that is used
        lo, hi = shot["at"], shot["at"] + shot["length"]
        return [t for t in times[first - 1:last] if lo <= t <= hi]
    return [anchor_time(shots, total, fps, on)]


def score_note(cue, token, at, volume=None):
    instrument = cue.get("instrument", "note")
    if token not in NOTE_FILES:
        fail(f"the score has a note that is not known: {token}")
    return {"asset": f"Fish/Audio/Music/{instrument}-{NOTE_FILES[token]}", "at": at, "name": token,
            "volume": float(cue.get("volume", 0.35) if volume is None else volume),
            "pitch": float(cue.get("octave", 0)), "pan": float(cue.get("pan", 0))}


def fit_beat(start, times, lo, hi):
    """the beat, from lo to hi milliseconds, that puts the moments closest to beats counted from start"""
    best, best_miss = lo, None
    for ms in range(int(lo), int(hi) + 1):
        beat = ms / 1000.0
        miss = 0
        for t in times:
            if t < start:
                continue
            phase = ((t - start) / beat) % 1
            miss += min(phase, 1 - phase) ** 2
        if best_miss is None or miss < best_miss:
            best, best_miss = ms, miss
    return best


def build_score(score, shots, total, fps):
    notes, pads, sounds = [], [], []
    melodies = read_melodies()
    grids = {}
    # the pulses first, so that a phrase can be snapped to the beats of one ("snap": its "id")
    score = sorted(score, key=lambda cue: 0 if cue["type"] == "pulse" else 1)
    for cue in score:
        # a cue that is about a shot the cut does not have is left out, so that one score fits many cuts
        before = len(notes), len(pads), len(sounds)
        try:
            kind = cue["type"]
            when = lambda a: anchor_time(shots, total, fps, a)
            if kind == "notes":
                at = when(cue["at"])
                for n, token in enumerate(cue["notes"]):
                    notes.append(score_note(cue, token, at + n * float(cue.get("strum", 0))))
            elif kind == "each":
                times = anchor_list(shots, total, fps, cue["on"])
                tokens = cue["notes"]
                for n, at in enumerate(times):
                    if n >= len(tokens) and not cue.get("cycle", True):
                        break
                    token = tokens[n % len(tokens)]
                    chord = token if isinstance(token, list) else [token]
                    for k, single in enumerate(chord):
                        if single != "REST":
                            notes.append(score_note(cue, single, at + float(cue.get("offset", 0)) + k * float(cue.get("strum", 0))))
            elif kind == "phrase":
                name = cue["melody"].lower()
                parts = [m for m in melodies if name in m["comment"]]
                if not parts:
                    fail(f'there is no melody called "{name}" in {MUSIC_SOURCE}')
                chosen = [parts[i] for i in cue.get("parts", range(len(parts)))] * int(cue.get("repeat", 1))
                # the melody on its own clock: every note, and where it would be
                raw, clock = [], 0.0
                for part in chosen:
                    beat = float(cue.get("beat_ms", part["beat_ms"])) / 1000.0
                    for token in part["beats"]:
                        if token != "REST":
                            raw.append((clock, token))
                        clock += beat
                skip = int(cue.get("skip_notes", 0))
                raw = raw[skip:skip + int(cue.get("max_notes", len(raw)))]
                if not raw:
                    continue
                base = raw[0][0]
                raw = [(t - base, token) for t, token in raw]
                # "land": [n, A] puts the phrase where its note n lands on A, at an even tempo: nothing is
                # stretched or squeezed. with "end_at": A, the tempo is the one even tempo that also has the
                # last note on that.
                if "land" in cue:
                    index, anchor = cue["land"]
                    land_t, land_at = raw[index][0], when(anchor)
                    print(f'  {cue["melody"]}: note {index} ({raw[index][1]}) lands at {land_at:.2f} s')
                    scale = 1.0
                    if "end_at" in cue and raw[-1][0] > land_t:
                        scale = (when(cue["end_at"]) - land_at) / (raw[-1][0] - land_t)
                    start = land_at - land_t * scale
                    cue = {**cue, "pins": [], "until": None}
                    cue.pop("until")
                    raw = [(t * scale, token) for t, token in raw]
                else:
                    start = when(cue["at"])
                # the pins: (where the note is on the melody's own clock, where it lands in the trailer)
                pins = [(0.0, start)]
                for index, anchor in cue.get("pins", []):
                    pins.append((raw[index][0], when(anchor)))
                if "until" in cue:
                    pins.append((raw[-1][0], when(cue["until"])))
                pins.sort()

                def warp(t):
                    if len(pins) == 1:
                        return start + t
                    for (a0, b0), (a1, b1) in zip(pins, pins[1:]):
                        if t <= a1:
                            return b0 + (t - a0) * (b1 - b0) / max(a1 - a0, 1e-6)
                    a0, b0 = pins[-2]
                    a1, b1 = pins[-1]
                    rate = (b1 - b0) / max(a1 - a0, 1e-6) if cue.get("keep_stretch") else 1.0
                    return b1 + (t - a1) * rate
                snap = grids.get(cue.get("snap"), [])
                pinned = {index for index, _ in cue.get("pins", [])}

                def place(n, t):
                    at = warp(t)
                    if snap and n not in pinned and n != 0:
                        nearest = min(snap, key=lambda b: abs(b - at))
                        at = nearest
                    return at
                # "rest": [[A, A], ...] leaves the notes out between those moments, to make room for something else
                rests = []
                for rest_from, rest_to in cue.get("rest", []):
                    try:
                        rests.append((when(rest_from), when(rest_to)))
                    except AnchorMissing:
                        pass
                stop = None
                if "stop_at" in cue:
                    try:
                        stop = when(cue["stop_at"])
                    except AnchorMissing:
                        stop = None
                for n, (t, token) in enumerate(raw):
                    if stop is not None and place(n, t) >= stop:
                        break
                    if any(a <= place(n, t) < b for a, b in rests):
                        continue
                    volume = float(cue.get("volume", 0.35))
                    if n in cue.get("accent", []):
                        volume *= float(cue.get("accent_gain", 1.4))
                    at = place(n, t)
                    notes.append(score_note(cue, token, at, volume))
                    if cue.get("double"):
                        notes.append(score_note({**cue, "instrument": cue["double"], "octave": float(cue.get("double_octave", cue.get("octave", 0)))},
                                                token, at, volume * float(cue.get("double_volume", 0.5))))
            elif kind == "pulse":
                start, end = when(cue["from"]), when(cue["to"])
                beat_ms = cue.get("beat_ms", 400)
                if beat_ms == "fit":
                    lo, hi = cue.get("fit_ms", [350, 650])
                    beat_ms = fit_beat(start, anchor_list(shots, total, fps, cue["fit_to"]), lo, hi)
                    print(f"  the pulse fits best at a beat of {beat_ms} ms")
                beat = float(beat_ms) / 1000.0
                # the beats. "lock_to" makes every one of those moments land on a beat: between two of them,
                # there are as many beats as fit best, each a little longer or shorter than "beat_ms".
                times = []
                if cue.get("lock_to"):
                    locks = sorted(t for t in anchor_list(shots, total, fps, cue["lock_to"]) if start < t < end)
                    edges = [start] + locks + [end]
                    for a, b in zip(edges, edges[1:]):
                        count = max(1, round((b - a) / beat))
                        times += [a + k * (b - a) / count for k in range(count)]
                else:
                    at = start
                    while at < end - 0.001:
                        times.append(at)
                        at = start + len(times) * beat
                if cue.get("id"):
                    grids[cue["id"]] = times
                pattern = cue["pattern"]
                v0 = float(cue.get("volume", 0.3))
                v1 = float(cue.get("volume_to", v0))
                every = int(cue.get("every", 1))
                for n, at in enumerate(times):
                    if n % every:
                        continue
                    token = pattern[(n // every) % len(pattern)]
                    level = v0 + (v1 - v0) * (at - start) / max(end - start, 1e-6)
                    chord = token if isinstance(token, list) else [token]
                    for single in chord:
                        if single != "REST":
                            notes.append(score_note(cue, single, at, level))
            elif kind == "pad":
                start, end = when(cue["from"]), when(cue["to"])
                pads.append({"asset": "Fish/Audio/Music/loop-pad-" + cue.get("chord", "dm"), "at": start,
                             "length": max(0.5, end - start), "volume": cue.get("volume")})
            elif kind == "sound":
                asset = os.path.join(REPO, cue["file"].replace("/", os.sep)) if "file" in cue else cue["asset"]
                sound = {"asset": asset, "at": when(cue["at"]), "volume": float(cue.get("volume", 0.5)),
                         "pitch": float(cue.get("pitch", 0)), "pan": float(cue.get("pan", 0)), "music": not cue.get("keep")}
                if "until" in cue:
                    sound["until"] = when(cue["until"]) - sound["at"]
                sounds.append(sound)
            elif kind == "morse":
                # the game's morse: a tone that is keyed on and off. a dot is a unit long, a dash three, a unit
                # of nothing after each, and "letter_gap" more after a letter (the shooting star has 4)
                codes = {"A": ".-", "B": "-...", "C": "-.-.", "D": "-..", "E": ".", "F": "..-.", "G": "--.", "H": "....",
                         "I": "..", "J": ".---", "K": "-.-", "L": ".-..", "M": "--", "N": "-.", "O": "---", "P": ".--.",
                         "Q": "--.-", "R": ".-.", "S": "...", "T": "-", "U": "..-", "V": "...-", "W": ".--", "X": "-..-",
                         "Y": "-.--", "Z": "--.."}
                unit = float(cue.get("unit_ms", 95)) / 1000.0
                gap = int(cue.get("letter_gap", 4))
                t = when(cue["at"])
                for letter in cue["text"].upper():
                    for sign in codes.get(letter, ""):
                        length = unit * (3 if sign == "-" else 1)
                        sounds.append({"asset": cue.get("asset", "Fish/Audio/morse-bip-radio"), "at": t, "until": length,
                                       "volume": float(cue.get("volume", 0.1)), "pitch": float(cue.get("pitch", 0)),
                                       "pan": float(cue.get("pan", 0)), "music": not cue.get("keep")})
                        t += length + unit
                    t += gap * unit
            else:
                fail(f"the score has a cue of a kind that is not known: {kind}")
        except AnchorMissing as missing:
            del notes[before[0]:], pads[before[1]:], sounds[before[2]:]
            print(f"  a {cue['type']} of the score is left out: {missing}")
    notes = [n for n in notes if 0 <= n["at"] < total]
    return notes, pads, sounds


# THE SHOTS

def probe_frames(video):
    out = subprocess.run(["ffprobe", "-v", "error", "-select_streams", "v", "-count_packets",
                          "-show_entries", "stream=nb_read_packets", "-of", "csv=p=0", video],
                         capture_output=True, text=True)
    try:
        return int(out.stdout.strip())
    except ValueError:
        fail(f"could not read {video}")


def make_grid(clips_dir, entry, fps):
    """lay several shots out side by side, into a video of its own, and use that as the shot.

    the first shot is shown on its own first, full frame, for "stages"[0] frames: at its own size, so
    that its words are sharp. then the punches: quick zooms out, a little past and back, to the next
    square of shots from the top left corner: two by two (three more), three by three (five more), four
    by four (seven more), held for the rest of "stages" each. every shot of the grid starts from the frame
    that the first one carries on from. every shot in the grid is cropped to "crop"
    (x, y, w, h of the 1920x1080 video: the board, and little else). the grid is laid out at 3840x2160,
    so that at two by two every shot is at its own size. the sounds of the first shot go with it."""
    names = entry["grid"]
    cols = int(entry.get("cols", 4))
    rows = (len(names) + cols - 1) // cols
    name = entry.get("clip", "trailer_grid")
    out = os.path.join(clips_dir, name + ".mp4")
    videos = [os.path.join(clips_dir, n + ".mp4") for n in names]
    for video in videos:
        if not os.path.exists(video):
            fail(f"the grid needs {video}")
    holds = entry.get("stages", [200, 70, 70, 140])
    punch = int(entry.get("punch", 14))
    skip = int(entry.get("skip", 0))
    first = holds[0]
    rest = sum(holds[1:]) + punch * (len(holds) - 1)
    frames = first + rest
    # it is made again when a shot of it, or this script, is newer than it, or when the cut asks for it
    # to be made another way than it was
    newest = max(os.path.getmtime(v) for v in videos + [os.path.abspath(__file__)])
    made_from = json.dumps({k: v for k, v in entry.items() if k not in ("transition", "sync", "in", "out", "remake")}, sort_keys=True)
    sidecar_path = os.path.join(clips_dir, name + ".json")
    same = False
    if os.path.exists(sidecar_path):
        with open(sidecar_path, encoding="utf-8") as f:
            same = json.load(f).get("made_from") == made_from
    if os.path.exists(out) and os.path.getmtime(out) > newest and same and not entry.get("remake"):
        return name
    cx, cy, cw, ch = entry.get("crop", [432, 268, 1180, 664])
    canvas_w, canvas_h = 3840, 2160
    w, h = canvas_w // cols, canvas_h // rows
    inputs, graph = [], []
    for k, video in enumerate(videos):
        inputs += ["-i", video]
    # the first shot on its own, full frame
    graph.append("[0:v]split=2[first_solo][first_grid]")
    graph.append(f"[first_solo]trim=start_frame={skip}:end_frame={skip + first},setpts=PTS-STARTPTS,setsar=1[solo]")
    # the grid: every shot from the same frame, the one that the first shot carries on from
    for k in range(len(videos)):
        start = skip + first
        source = "[first_grid]" if k == 0 else f"[{k}:v]"
        graph.append(f"{source}trim=start_frame={start}:end_frame={start + rest},setpts=PTS-STARTPTS,"
                     f"tpad=stop_mode=clone:stop_duration=10,trim=end_frame={rest},crop={cw}:{ch}:{cx}:{cy},"
                     f"scale={w - 8}:{h - 8}:flags=lanczos,pad={w}:{h}:4:4:color=black[g{k}]")
    layout = "|".join(f"{(k % cols) * w}_{(k // cols) * h}" for k in range(len(videos)))
    graph.append("".join(f"[g{k}]" for k in range(len(videos))) + f"xstack=inputs={len(videos)}:layout={layout}:fill=black[grid]")
    # the punches: from the first shot filling the screen, out to two by two, three by three, four by four
    zooms = [cols / (n + 1) for n in range(len(holds))]
    parts = []
    t = 0
    for n in range(1, len(holds)):
        parts.append(("punch", t, t + punch, zooms[n - 1], zooms[n]))
        t += punch
        parts.append(("hold", t, t + holds[n], zooms[n], zooms[n]))
        t += holds[n]
    expr = f"{zooms[-1]}"
    for kind, start, end, z0, z1 in reversed(parts):
        if kind == "hold":
            piece = f"{z0}"
        else:
            p = f"((on-{start})/{end - start})"
            over = float(entry.get("overshoot", 0.45))
            back = f"(1+{over + 1}*pow({p}-1,3)+{over}*pow({p}-1,2))"
            piece = f"{z0}+({z1}-{z0})*{back}"
        expr = f"if(lt(on,{end}),{piece},{expr})"
    graph.append(f"[grid]zoompan=z='max(1,{expr})':x=0:y=0:d=1:s=1920x1080:fps={fps},setsar=1[punched]")
    graph.append("[solo][punched]concat=n=2:v=1:a=0[out]")
    print(f"making the grid {out} from {len(videos)} shots ...")
    result = subprocess.run(["ffmpeg", "-y", "-v", "error"] + inputs + ["-filter_complex", ";".join(graph), "-map", "[out]",
                             "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-pix_fmt", "yuv420p", "-r", str(fps), out])
    if result.returncode != 0:
        fail("ffmpeg failed to make the grid")
    # the sounds of the first shot, which is on screen the whole time. and from "all_sounds_from" (a frame
    # of the shots), the sounds of all of them, so that every box is heard as it bubbles away
    sounds = []
    every_from = entry.get("all_sounds_from")
    for k, shot_name in enumerate(names):
        path = os.path.join(clips_dir, shot_name + ".json")
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as f:
            for sound in json.load(f).get("sounds", []):
                if k > 0 and (every_from is None or sound["frame"] < every_from):
                    continue
                frame = sound["frame"] - skip
                if 0 <= frame < frames:
                    sounds.append({**sound, "frame": frame, "volume": sound.get("volume", 1) * (1 if k == 0 else 0.5)})
    sounds.sort(key=lambda sound: sound["frame"])
    # marks for the music: "punch" where each punch starts, and the "close" of every shot's marks, in order
    marks = []
    t = first
    for n in range(1, len(holds)):
        marks.append({"name": "punch", "frame": t})
        t += punch + holds[n]
    for shot_name in names:
        path = os.path.join(clips_dir, shot_name + ".json")
        if os.path.exists(path):
            with open(path, encoding="utf-8") as f:
                closes = [m["frame"] for m in json.load(f).get("marks", []) if m["name"] == "close"]
                if closes and 0 <= closes[0] - skip < frames:
                    marks.append({"name": "close", "frame": closes[0] - skip})
    marks.sort(key=lambda mark: mark["frame"])
    with open(os.path.join(clips_dir, name + ".json"), "w", encoding="utf-8") as f:
        json.dump({"video": name + ".mp4", "fps": fps, "frames": frames, "marks": marks, "sounds": sounds,
                   "made_from": made_from}, f)
    return name


def load_shot(clips_dir, entry):
    if "grid" in entry:
        entry = dict(entry)
        entry["clip"] = make_grid(clips_dir, entry, 60)
    name = entry["clip"]
    video = os.path.join(clips_dir, name + ".mp4")
    if not os.path.exists(video):
        fail(f"there is no video for {name}: {video}")
    sidecar_path = os.path.join(clips_dir, name + ".json")
    sidecar = {"fps": 60, "frames": 0, "marks": [], "sounds": []}
    if os.path.exists(sidecar_path):
        with open(sidecar_path, encoding="utf-8") as f:
            sidecar = json.load(f)
    else:
        print(f"  {name}: no .json next to it, so it has no sounds and no marks. record it again to get them.")
    frames = probe_frames(video)
    return {"entry": entry, "name": name, "video": video, "sidecar": sidecar, "frames": frames}


def find_mark(shot, mark, occurrence):
    found = [m["frame"] for m in shot["sidecar"].get("marks", []) if m["name"] == mark]
    if len(found) < occurrence:
        fail(f'{shot["name"]} has {len(found)} "{mark}" marks, and the cut wants number {occurrence}')
    return found[occurrence - 1]


def lay_out(shots, notes, beats, fps, transitions):
    """where every shot is cut in and out, and where it lands in the trailer"""
    at = 0.0
    for k, shot in enumerate(shots):
        entry = shot["entry"]
        # a transition into this shot lays it over the end of the shot before it
        shot["overlap"] = 0
        if k > 0 and entry.get("transition"):
            shot["overlap"] = transitions[entry["transition"]]["frames"]
            at -= shot["overlap"] / fps
        cut_in = entry.get("in", 0)
        # "in" can be a mark of the shot: {"mark": "wave", "before": 5} cuts in 5 frames before it
        if isinstance(cut_in, dict):
            cut_in = find_mark(shot, cut_in["mark"], int(cut_in.get("occurrence", 1))) - int(cut_in.get("before", 0))
        cut_in = max(0, int(cut_in))
        cut_out = entry.get("out")
        # and so can "out": {"mark": "wave", "before": 5} cuts out 5 frames before it
        if isinstance(cut_out, dict):
            cut_out = find_mark(shot, cut_out["mark"], int(cut_out.get("occurrence", 1))) - int(cut_out.get("before", 0))
        cut_out = shot["frames"] if cut_out is None else int(cut_out)
        if cut_out < 0:
            cut_out = shot["frames"] + cut_out
        cut_out = min(cut_out, shot["frames"])

        sync = entry.get("sync")
        shot["sync_note"] = None
        shot["sync_time"] = None
        shot["pad"] = 0
        if sync and notes:
            mark = find_mark(shot, sync["mark"], int(sync.get("occurrence", 1)))
            if mark < cut_in:
                fail(f'{shot["name"]}: the "{sync["mark"]}" mark is before where the shot is cut in')
            lands = at + (mark - cut_in) / fps
            # what it can land on: the notes of the tune, or every beat of it, rests and all
            times = [n["at"] for n in notes] if sync.get("to", "note") == "note" else beats
            target = sync.get("note", "nearest")
            # cut in at the mark itself, and it lands at the start of the shot. cut in at the first
            # frame, and it lands as late as it can. later than that, the first frame of the shot is
            # held for a moment, up to "hold" seconds.
            # and a shot that a transition goes into has its start under the transition, so its mark
            # has to land after the transition is over, where it can be seen
            earliest = at + (shot["overlap"] + 6) / fps
            latest = at + mark / fps
            # by default a sync never cuts into the start of a shot (the cursor walks in at the start of
            # most of them, and a cut there makes it jump). it only holds the first frame a moment.
            if sync.get("hold_only", True):
                earliest = max(earliest, lands)
            most_hold = float(sync.get("hold", 1.0))
            if target == "nearest":
                choices = [n for n in range(len(times)) if earliest <= times[n] <= latest + most_hold]
                # in a rest of the tune, it lands on a beat of it instead
                if not choices and times is not beats:
                    times = beats
                    choices = [n for n in range(len(times)) if earliest <= times[n] <= latest + most_hold]
                if not choices:
                    print(f'  {shot["name"]}: nothing in the music can be reached from its "{sync["mark"]}", so it is not lined up')
                else:
                    target = min(choices, key=lambda n: abs(times[n] - lands) + (0.5 if times[n] > latest else 0))
            if isinstance(target, int):
                if target >= len(times):
                    fail(f'{shot["name"]}: the music has {len(times)} of those, and the cut wants number {target}')
                when = times[target]
                new_in = mark - round((when - at) * fps)
                pad = 0
                if new_in < 0:
                    pad = -new_in
                    new_in = 0
                if new_in > mark - shot["overlap"] or pad > most_hold * fps:
                    print(f'  {shot["name"]}: {target} cannot be reached from its "{sync["mark"]}", so it is not lined up')
                else:
                    # the shot keeps its length, unless the cut says otherwise: its end moves with its start
                    if entry.get("keep_length", False):
                        cut_out = min(shot["frames"], cut_out + (new_in - cut_in) - pad)
                    cut_in = new_in
                    shot["pad"] = pad
                    shot["sync_note"] = target
                    shot["sync_time"] = when
        if shot["overlap"] and cut_out - cut_in < shot["overlap"]:
            fail(f'{shot["name"]} is shorter than the transition into it')
        if cut_out <= cut_in:
            fail(f'{shot["name"]} is cut in at {cut_in} and out at {cut_out}, so nothing of it is left')
        shot["in"] = cut_in
        shot["out"] = cut_out
        shot["at"] = at
        shot["length"] = (cut_out - cut_in + shot["pad"]) / fps
        at += shot["length"]
    return at


def game_sounds(shots, cut, fps):
    """the sounds that the game played in the parts of the shots that are used"""
    skip_prefixes = cut.get("skip_sounds", ["Fish/Audio/Music/"])
    volume = float(cut.get("sfx_volume", 0.6))
    scales = cut.get("sound_volumes", {})
    sounds = []
    for shot in shots:
        if not shot["entry"].get("sounds", True):
            continue
        played = shot["sidecar"].get("sounds", [])
        for n, sound in enumerate(played):
            frame = sound["frame"]
            if frame < shot["in"] or frame >= shot["out"]:
                continue
            if sound.get("loop"):
                continue
            if any(sound["asset"].startswith(p) for p in skip_prefixes):
                continue
            # in the game, a sound slot that plays again stops what it was playing. the bubbles take turns
            # with a few slots, so only a few of them are ever heard at once. the same here.
            until = None
            if "sfx" in sound:
                later = [s["frame"] for s in played[n + 1:] if s.get("sfx") == sound["sfx"]]
                if later:
                    until = (later[0] - frame) / fps
            gain = volume
            for prefix, scale in scales.items():
                if sound["asset"].startswith(prefix):
                    gain *= float(scale)
            sounds.append({"asset": sound["asset"], "at": shot["at"] + (frame - shot["in"] + shot["pad"]) / fps,
                           "volume": sound.get("volume", 1) * gain, "pitch": sound.get("pitch", 0),
                           "pan": sound.get("pan", 0), "until": until})
    return sounds

def extra_sounds(cut, shots_by_index, fps):
    """the sounds that the cut adds by hand: at a time in the trailer, or at a frame of a shot"""
    sounds = []
    for extra in cut.get("sounds", []):
        if "shot" in extra:
            shot = shots_by_index[int(extra["shot"])]
            at = shot["at"] + (float(extra.get("frame", 0)) - shot["in"] + shot["pad"]) / fps
        else:
            at = float(extra["at"])
        sounds.append({"asset": extra["asset"], "at": at, "volume": float(extra.get("volume", 1)),
                       "pitch": float(extra.get("pitch", 0)), "pan": float(extra.get("pan", 0))})
    return sounds


# FFMPEG

def load_transitions(cut, clips_dir):
    """the transitions that the cut can use, each a matte video and a bubbles video that the game
    recorded, and how many of their frames lay over each other"""
    found = {}
    for name, kind in cut.get("transitions", {}).items():
        # a fade is one shot dissolving into the next, over so many frames, and needs no videos
        if kind.get("type") == "fade":
            found[name] = {"type": "fade", "frames": int(kind.get("frames", 30)), "skip": 0, "speed": 1, "sidecar": {}}
            continue
        matte = os.path.join(clips_dir, kind["matte"] + ".mp4")
        fx = os.path.join(clips_dir, kind["fx"] + ".mp4")
        for video in (matte, fx):
            if not os.path.exists(video):
                fail(f'the "{name}" transition needs {video}. record it with render-trailer.ps1 -Shot transition')
        skip = int(kind.get("skip", 1))
        # "speed" plays the wave faster than the game does, so that it lays over less of each shot
        speed = float(kind.get("speed", 1))
        frames = int((min(probe_frames(matte), probe_frames(fx)) - skip) / speed)
        if "frames" in kind:
            frames = min(frames, int(kind["frames"]))
        sidecar = {}
        fx_sidecar = os.path.join(clips_dir, kind["fx"] + ".json")
        if os.path.exists(fx_sidecar):
            with open(fx_sidecar, encoding="utf-8") as f:
                sidecar = json.load(f)
        found[name] = {"type": "wipe", "matte": matte, "fx": fx, "skip": skip, "frames": frames, "speed": speed, "sidecar": sidecar,
                       "sound": kind.get("sound"), "sound_volume": kind.get("sound_volume", 0.6)}
    return found


def transition_sounds(shots, transitions, cut, fps):
    """the sounds that the game played with each transition's wave, such as the rush of the bubbles"""
    skip_prefixes = cut.get("skip_sounds", ["Fish/Audio/Music/"])
    volume = float(cut.get("sfx_volume", 0.6))
    sounds = []
    for shot in shots:
        if not shot["overlap"]:
            continue
        kind = transitions[shot["entry"]["transition"]]
        # a transition can say its sound outright: the game fades its own wave sound in from nothing,
        # so what it recorded is at no volume at all
        if kind.get("sound"):
            sounds.append({"asset": kind["sound"], "at": shot["at"], "volume": float(kind.get("sound_volume", 0.6)),
                           "pitch": 0, "pan": 0, "until": shot["overlap"] / fps + 0.4})
            continue
        for sound in kind["sidecar"].get("sounds", []):
            frame = int((sound["frame"] - kind["skip"]) / kind["speed"])
            if frame < 0 or frame >= kind["frames"] or sound.get("loop"):
                continue
            if any(sound["asset"].startswith(p) for p in skip_prefixes):
                continue
            sounds.append({"asset": sound["asset"], "at": shot["at"] + frame / fps,
                           "volume": sound.get("volume", 1) * volume, "pitch": sound.get("pitch", 0),
                           "pan": sound.get("pan", 0)})
    return sounds


def render(shots, total, notes, pads, sounds, cut, out_path, preview, transitions):
    fps = int(cut.get("fps", 60))
    music = cut.get("music", {})
    inputs = []
    graph = []

    # the pictures: every shot cut down. a shot that a transition goes into or out of is split into
    # its middle, and its ends that the transition lays over each other.
    segments = []
    for k, shot in enumerate(shots):
        inputs += ["-i", shot["video"]]
        hold = f",tpad=start_mode=clone:start_duration={shot['pad'] / fps:.4f}" if shot["pad"] else ""
        length = shot["out"] - shot["in"] + shot["pad"]
        head = shot["overlap"]
        tail = shots[k + 1]["overlap"] if k + 1 < len(shots) else 0
        graph.append(f"[{k}:v]trim=start_frame={shot['in']}:end_frame={shot['out']},setpts=PTS-STARTPTS{hold},"
                     f"format=yuv420p,split=3[c{k}a][c{k}b][c{k}c]")
        graph.append(f"[c{k}a]trim=start_frame={head}:end_frame={length - tail},setpts=PTS-STARTPTS[mid{k}]")
        graph.append(f"[c{k}b]trim=start_frame=0:end_frame={max(head, 1)},setpts=PTS-STARTPTS[head{k}]")
        graph.append(f"[c{k}c]trim=start_frame={length - max(tail, 1)}:end_frame={length},setpts=PTS-STARTPTS[tail{k}]")
        if head:
            segments.append(f"[tr{k}]")
        else:
            graph.append(f"[head{k}]nullsink")
        segments.append(f"[mid{k}]")
        if not tail:
            graph.append(f"[tail{k}]nullsink")

    # the transitions: the matte says where each shot shows, green for the one going and red for the one
    # coming, and the bubbles go over the top of both
    input_index = len(shots)
    for k, shot in enumerate(shots):
        if not shot["overlap"]:
            continue
        kind = transitions[shot["entry"]["transition"]]
        frames = shot["overlap"]
        if kind["type"] == "fade":
            graph.append(f"[tail{k - 1}][head{k}]blend=all_expr='A*(1-min(N/{frames},1))+B*min(N/{frames},1)',format=yuv420p[tr{k}]")
            continue
        inputs += ["-i", kind["matte"], "-i", kind["fx"]]
        skip = kind["skip"]
        red = "if(gt(r(X,Y),g(X,Y)),255,0)"
        speed = kind["speed"]
        source_end = skip + int(math.ceil(frames * speed)) + 1
        timing = f"trim=start_frame={skip}:end_frame={source_end},setpts=(PTS-STARTPTS)/{speed},fps={fps},trim=end_frame={frames}"
        graph.append(f"[{input_index}:v]{timing},format=gbrp,geq=r='{red}':g='{red}':b='{red}'[mask{k}]")
        graph.append(f"[{input_index + 1}:v]{timing},format=gbrp[fx{k}]")
        graph.append(f"[tail{k - 1}]format=gbrp[ta{k}]")
        graph.append(f"[head{k}]format=gbrp[he{k}]")
        graph.append(f"[ta{k}][he{k}][mask{k}]maskedmerge[mm{k}]")
        graph.append(f"[mm{k}][fx{k}]blend=all_mode=screen,format=yuv420p[tr{k}]")
        input_index += 2

    scale = ",scale=960:540" if preview else ""
    if cut.get("vertical"):
        # 9 by 16, for phones: the middle of the picture, where the board is, as wide as the frame, over a
        # blurred and darkened copy of the whole of it that fills the rest
        graph.append("".join(segments) + f"concat=n={len(segments)}:v=1:a=0,split=2[vbg][vfg]")
        graph.append("[vbg]scale=-2:1920,crop=1080:1920,boxblur=24:2,eq=brightness=-0.12[vbgb]")
        graph.append("[vfg]crop=1100:1080:410:0,scale=1080:-2[vfgs]")
        graph.append(f"[vbgb][vfgs]overlay=0:(H-h)/2{',scale=540:960' if preview else ''}[vout]")
    else:
        graph.append("".join(segments) + f"concat=n={len(segments)}:v=1:a=0{scale}[vout]")

    # the sounds: every file once, split for as many times as it plays
    plays = []
    for note in notes:
        plays.append({"volume": float(music.get("volume", 0.8)), "pitch": 0, "pan": 0, "music": True, **note})
    plays += sounds
    files = {}
    for play in plays:
        path = asset_file(play["asset"])
        if path is None:
            print(f"  no sound file for {play['asset']}, so it is left out")
            continue
        files.setdefault(path, []).append(play)

    mix = []
    music_mix = []
    for path, uses in files.items():
        inputs += ["-i", path]
        labels = [f"s{input_index}_{n}" for n in range(len(uses))]
        if len(uses) > 1:
            graph.append(f"[{input_index}:a]asplit={len(uses)}" + "".join(f"[{l}in]" for l in labels))
        else:
            graph.append(f"[{input_index}:a]anull[{labels[0]}in]")
        for label, use in zip(labels, uses):
            delay = max(0, int(round(use["at"] * 1000)))
            chain = f"[{label}in]aresample=48000,aformat=channel_layouts=stereo"
            if use.get("until"):
                fade = 0.02 if use["until"] < 1 else 0.4
                chain += f",atrim=0:{max(0.02, use['until']):.4f},afade=t=out:st={max(0.0, use['until'] - fade):.4f}:d={fade}"
            if abs(use["pitch"]) > 0.001:
                rate = int(48000 * math.pow(2, use["pitch"]))
                chain += f",asetrate={rate},aresample=48000"
            pan = max(-1.0, min(1.0, use["pan"]))
            left = min(1.0, 1 - pan)
            right = min(1.0, 1 + pan)
            chain += f",volume={use['volume']:.4f},pan=stereo|c0={left:.3f}*c0|c1={right:.3f}*c1"
            chain += f",adelay={delay}:all=1[{label}]"
            graph.append(chain)
            (music_mix if use.get("music") else mix).append(label)
        input_index += 1

    # the pads under the chords, looped for as long as their part of the tune lasts
    pad_volume = float(music.get("pad_volume", 0.35))
    for n, pad in enumerate(pads if music.get("pads", True) else []):
        path = asset_file(pad["asset"])
        if path is None:
            continue
        inputs += ["-stream_loop", "-1", "-i", path]
        label = f"pad{n}"
        fade = min(1.5, pad["length"] / 4)
        graph.append(f"[{input_index}:a]aresample=48000,aformat=channel_layouts=stereo,"
                     f"atrim=0:{pad['length'] + 2:.3f},volume={float(pad.get('volume') or pad_volume):.3f},"
                     f"afade=t=in:d={fade:.3f},afade=t=out:st={pad['length'] + 0.5:.3f}:d=1.5,"
                     f"adelay={int(pad['at'] * 1000)}:all=1[{label}]")
        music_mix.append(label)
        input_index += 1

    # the sound of the deep, under all of it: the layers that the game itself plays, looped, each at the
    # level the game gives it (setup_music in fish_routines_music), times "ambience_volume"
    layers = music.get("layers")
    if layers is None:
        layers = [{"asset": music.get("ambience", "Fish/Audio/Music/loop-ambience"), "volume": 1}]
    for n, layer in enumerate(layers):
        path = asset_file(layer["asset"])
        if not path:
            print(f"  no sound file for {layer['asset']}, so it is left out")
            continue
        inputs += ["-stream_loop", "-1", "-i", path]
        level = float(layer.get("volume", 1)) * float(music.get("ambience_volume", 0.25))
        graph.append(f"[{input_index}:a]aresample=48000,aformat=channel_layouts=stereo,atrim=0:{total:.3f},"
                     f"afade=t=in:d=1.0,volume={level:.3f}[amb{n}]")
        music_mix.append(f"amb{n}")
        input_index += 1

    # the sound fades out at the end: from where the last shot goes black, if it says so with a
    # "black" mark, so that the pictures go first and the music after them. else over the last 1.5 s.
    fade_at = max(0.0, total - 1.5)
    last = shots[-1]
    blacks = [m["frame"] for m in last["sidecar"].get("marks", []) if m["name"] == "black"]
    if blacks and last["in"] <= blacks[0] < last["out"]:
        fade_at = last["at"] + (blacks[0] - last["in"] + last["pad"]) / fps

    # the music, and the sound of the sea under it, can go before the rest: "music_fade" in the music, a
    # time in the trailer and how long it takes
    if music_mix:
        fade = music.get("music_fade")
        chain = "".join(f"[{l}]" for l in music_mix) + f"amix=inputs={len(music_mix)}:normalize=0:dropout_transition=0"
        if fade:
            chain += f",afade=t=out:st={float(fade['at']):.3f}:d={float(fade.get('seconds', 3)):.3f}"
        if music.get("music_gain"):
            chain += f",volume={float(music['music_gain']):.3f}"
        graph.append(chain + "[music_all]")
        mix.append("music_all")
    if mix:
        graph.append("".join(f"[{l}]" for l in mix) + f"amix=inputs={len(mix)}:normalize=0:dropout_transition=0,"
                     f"atrim=0:{total:.3f},afade=t=out:st={fade_at:.3f}:d={max(0.1, total - fade_at):.3f},"
                     f"alimiter=limit=0.95[aout]")

    with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False, encoding="utf-8") as f:
        f.write(";\n".join(graph))
        graph_path = f.name
    command = ["ffmpeg", "-y", "-v", "error"] + inputs + ["-filter_complex_script", graph_path, "-map", "[vout]"]
    if mix:
        command += ["-map", "[aout]", "-c:a", "aac", "-b:a", "256k"]
    command += ["-c:v", "libx264", "-preset", "veryfast" if preview else "slow", "-crf", "20" if preview else "15",
                "-pix_fmt", "yuv420p", "-r", str(fps), "-movflags", "+faststart", out_path]
    print(f"rendering {out_path} ({total:.2f} s, {len(mix)} sounds) ...")
    result = subprocess.run(command)
    os.unlink(graph_path)
    if result.returncode != 0:
        fail("ffmpeg failed")
    # the pictures must be as long as the plan, or they drift ahead of the music (2026-10-23: a cut came
    # out 209 frames short, and "you." showed 3.5 s before its note)
    made = probe_frames(out_path)
    planned = int(round(total * fps))
    if abs(made - planned) > 2:
        fail(f"{out_path} has {made} frames, and the plan has {planned}: the pictures and the music are out of step")
    print("done.")


def main():
    parser = argparse.ArgumentParser(description="put the trailer's shots together, with music and sounds")
    parser.add_argument("--cut", default=os.path.join(HERE, "trailer_cut.json"))
    parser.add_argument("--plan", action="store_true", help="only print where everything lands")
    parser.add_argument("--preview", action="store_true", help="quicker, and half the size")
    parser.add_argument("--out", help="where the trailer goes, in place of the cut's output")
    parser.add_argument("--music", nargs="+", help="variants of the music (build/music/*.json): the same pictures, "
                        "with each one's music in place of the cut's, into trailer-clips/music/trailer_<name>.mp4")
    args = parser.parse_args()

    with open(args.cut, encoding="utf-8") as f:
        cut = json.load(f)
    base = os.path.dirname(os.path.abspath(args.cut))
    clips_dir = os.path.normpath(os.path.join(base, cut.get("clips", "../trailer-clips")))
    out_path = args.out or os.path.normpath(os.path.join(base, cut.get("output", "../trailer-clips/trailer_cut.mp4")))
    fps = int(cut.get("fps", 60))

    notes, beats, pads, music_end = build_music(cut.get("music", {}))
    shots = [load_shot(clips_dir, entry) for entry in cut["shots"]]
    transitions = load_transitions(cut, clips_dir)
    total = lay_out(shots, notes, beats, fps, transitions)
    sounds = game_sounds(shots, cut, fps) + extra_sounds(cut, shots, fps) + transition_sounds(shots, transitions, cut, fps)

    print(f"{len(shots)} shots, {total:.2f} s. the music has {len(notes)} notes and ends at {music_end:.2f} s.")
    for n, shot in enumerate(shots):
        sync = ""
        if shot["sync_note"] is not None:
            sync = f'  "{shot["entry"]["sync"]["mark"]}" lands at {shot["sync_time"]:.2f} s'
            if shot["pad"]:
                sync += f', after its first frame is held {shot["pad"] / fps:.2f} s'
        into = f'  ({shot["entry"]["transition"]} in, {shot["overlap"] / fps:.2f} s)' if shot["overlap"] else ""
        print(f"  {n:2}  {shot['at']:6.2f} s  {shot['length']:5.2f} s  {shot['name']:32} frames {shot['in']}-{shot['out']}{sync}{into}")
    if music_end < total:
        print(f"  the music ends {total - music_end:.1f} s before the pictures do. \"repeat\" or \"beat_ms\" in the cut can stretch it.")

    if args.plan and not args.music:
        return
    if not args.music and not cut.get("music_variant"):
        render(shots, total, notes, pads, sounds, cut, out_path, args.preview, transitions)
        return
    # the cut can name a variant as its own music: "music_variant", a path from the cut's folder. then the
    # trailer is made with that music, to the cut's own output.
    own = not args.music
    if own:
        args.music = [os.path.join(base, cut["music_variant"])]
    # the variants: the pictures stay as the cut lays them out, so that only the music is different
    for variant_path in args.music:
        with open(variant_path, encoding="utf-8") as f:
            variant = json.load(f)
        name = os.path.splitext(os.path.basename(variant_path))[0]
        music = {**cut.get("music", {}), **variant.get("music", {})}
        if "score" in variant:
            v_notes, v_pads, v_sounds = build_score(variant["score"], shots, total, fps)
            music["pads"] = True
            if variant.get("music_fade"):
                fade = variant["music_fade"]
                try:
                    music["music_fade"] = {"at": anchor_time(shots, total, fps, fade["at"]), "seconds": fade.get("seconds", 3)}
                except AnchorMissing as missing:
                    print(f"  the music does not fade early: {missing}")
        else:
            v_notes, _, v_pads, _ = build_music(music)
            v_sounds = []
        print(f"{name}: {len(v_notes)} notes, {len(v_pads)} pads, {len(v_sounds)} sounds of its own")
        if args.plan:
            for note in sorted(v_notes, key=lambda n: n["at"]):
                print(f"    {note['at']:6.2f}  {note['asset'].split('/')[-1]:10} {note['volume']:.2f}")
            continue
        v_cut = {**cut, "music": music}
        v_out = out_path if own else os.path.join(os.path.dirname(out_path), "music", f"trailer_{name}.mp4")
        os.makedirs(os.path.dirname(v_out), exist_ok=True)
        render(shots, total, v_notes, v_pads, sounds + v_sounds, v_cut, v_out, args.preview, transitions)


if __name__ == "__main__":
    main()
