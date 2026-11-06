"""
Cuts the shorts: the shots that build/render-trailer.ps1 recorded, laid out for a phone held upright,
1080 x 1920, with the game's own sounds and music under them. Nothing here runs the game. it only
works from the videos that are already in trailer-clips.

    py -3 build/shorts_cut.py                   cut every short in build/shorts.json
    py -3 build/shorts_cut.py --only relax deep only the shorts (or shots) with these in the name
    py -3 build/shorts_cut.py --plan            print where everything lands, and make nothing
    py -3 build/shorts_cut.py --preview         quickly, at half the size
    py -3 build/shorts_cut.py --clips           print the tests that record their shots (render-shorts.ps1 -Record)
    py -3 build/shorts_cut.py --out <folder>    put the videos there (default: shorts at the top of the repo)

A SHORT in shorts.json is the trailer's cut (build/trailer_cut.py) made small: "shots", "music" and
the sound settings work the way they do there, and the file's "defaults" fill in what a short leaves
out. every shot can have a "transition", and "sync", as in the trailer. its picture, from the top:

    the hook       "hook", a line or a few ("\\n" between them), up the whole time, over the top
    the picture    the part of the game's 1920 x 1080 "crop" ([x, y, w, h]) shows, as wide as the
                   short, its top and bottom edges feathered into the backdrop. a shot can have its
                   own "crop". it does not have to be the same shape, the middle of it stays put
    the backdrop   the same recording, blurred, with the deep blue of the sea over it. it is darkest
                   at the top and the bottom, where the words go
    the captions   under the picture. each has a "text", "dur" seconds, and starts at "at" seconds
                   into the short, or at a "mark" ("occurrence" for a later one) or "frame" of its
                   "shot" (a number, counted from 0), "offset" seconds from there
    the end        "end_card": a part of a shot played there and back, slowly, so that it can be
                   held, filling the whole frame, and "cta" over it. it is laid on as the last shot

THE SOUND is mixed by trailer_cut.render, so it is the trailer's mix, and is then brought to -14 LUFS,
where the platforms play it. every short stays under 25 MiB, so that a website can serve it: the
quality is lowered a step at a time until it fits.

The platforms' buttons and the short's title cover the bottom fifth and the right edge, so nothing that
has to be read goes there. the words are in the game's font (ZeroCool.ttf).

Only the standard library is used (and trailer_cut), and ffmpeg and ffprobe, which have to be on the
PATH.
"""

import argparse
import copy
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

import trailer_cut as tc

HERE = tc.HERE
W, H = 1080, 1920
SIZE_LIMIT = 25 * 1024 * 1024


def run_ffmpeg(args, cwd=None):
    """every ffmpeg here is run from the work folder, so that its filters never have to spell out a
    Windows path (the colon in C: is a separator to them)"""
    result = subprocess.run(["ffmpeg", "-y", "-v", "error"] + args, cwd=cwd)
    if result.returncode != 0:
        tc.fail("ffmpeg failed: " + " ".join(args[:12]))


def even(value):
    return int(round(value / 2)) * 2


def load(path):
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    defaults = data.get("defaults", {})
    shorts = []
    for spec in data["shorts"]:
        short = copy.deepcopy(defaults)
        for key, value in spec.items():
            if key == "music" and isinstance(value, dict):
                short.setdefault("music", {}).update(copy.deepcopy(value))
            else:
                short[key] = copy.deepcopy(value)
        shorts.append(short)
    return data, shorts


# THE STILLS AND THE PLATES

def make_mask(work, w, h, feather):
    """white, going to black over feather pixels at the top and the bottom, for the picture's edges"""
    name = f"mask_{w}x{h}.png"
    if not os.path.exists(os.path.join(work, name)):
        t = f"min(1,min(Y,{h - 1}-Y)/{feather})"
        run_ffmpeg(["-f", "lavfi", "-i", f"color=white:s={w}x{h}", "-frames:v", "1", "-vf",
                    f"format=gray,geq=lum='255*{t}*{t}*(3-2*{t})'", name], work)
    return name


def make_wash(work, w, h, colour, middle, edge):
    """the deep blue that goes over the blurred backdrop: thin in the middle, thick at the top and bottom"""
    name = "wash.png"
    if not os.path.exists(os.path.join(work, name)):
        d = f"abs(Y-{h / 2})/{h / 2}"
        run_ffmpeg(["-f", "lavfi", "-i", f"color=c={colour}:s={w}x{h}", "-frames:v", "1", "-vf",
                    f"format=rgba,geq=r='r(X,Y)':g='g(X,Y)':b='b(X,Y)':a='255*({middle}+({edge}-{middle})*pow({d},1.5))'",
                    name], work)
    return name


def make_end_plate(work, clips, card, w, h, fps):
    """the end card's picture: a few moments of a shot, there and back and slowed down, because the shots
    have very little of the sunset with no words on it. the water hardly moves, so it never shows"""
    name = "end_plate"
    video = os.path.join(work, name + ".mp4")
    if os.path.exists(video):
        return name
    x, y, cw, ch = card["crop"]
    a, b = card["in"], card["out"]
    frames = int(card.get("seconds", 4) * fps)
    slow = float(card.get("slow", 2))
    graph = (f"[0:v]trim=start_frame={a}:end_frame={b},setpts=PTS-STARTPTS,crop={cw}:{ch}:{x}:{y},"
             f"scale={w}:{h}:flags=lanczos,format=yuv420p,split[f][r];[r]reverse[b];[f][b]concat=n=2:v=1:a=0,"
             f"setpts={slow}*PTS,fps={fps},loop=-1:size=32767,trim=end_frame={frames},setpts=PTS-STARTPTS,setsar=1[out]")
    run_ffmpeg(["-i", os.path.join(clips, card["clip"] + ".mp4"), "-filter_complex", graph, "-map", "[out]",
                "-frames:v", str(frames), "-c:v", "libx264", "-preset", "ultrafast", "-crf", "12", "-r", str(fps),
                name + ".mp4"], work)
    with open(os.path.join(work, name + ".json"), "w", encoding="utf-8") as f:
        json.dump({"fps": fps, "frames": frames, "marks": [], "sounds": []}, f)
    return name


def stage_shot(work, n, shot, default_crop, style, w, h, scale, fps):
    """one shot, laid out upright: its backdrop and its feathered picture. the words go on at the end, over
    all of it, so that they stay still through the transitions"""
    entry = shot["entry"]
    name = f"stage_{n}.mp4"
    if entry.get("plate"):
        return shot["video"]
    x, y, cw, ch = entry.get("crop", default_crop)
    picture_h = even(w * ch / cw)
    feather = int(style.get("feather", 56) * scale)
    picture_y = even(style.get("picture_mid", 840) * scale - picture_h / 2)
    mask = make_mask(work, w, picture_h, feather)
    wash = make_wash(work, w, h, style.get("navy", "0x0A1A33"), style.get("wash_middle", 0.35), style.get("wash_edge", 0.92))
    hold = f",tpad=start_mode=clone:start_duration={shot['pad'] / fps:.4f}" if shot["pad"] else ""
    length = shot["out"] - shot["in"] + shot["pad"]
    # the backdrop is the middle of the same crop, tall enough to fill the frame. blurred small and scaled
    # back up, because a blur that wide at the full size is slow, and it would look no different
    band = 1080 * 9 // 16
    band_x = max(0, min(1920 - band, x + cw // 2 - band // 2))
    graph = [f"[0:v]trim=start_frame={shot['in']}:end_frame={shot['out']},setpts=PTS-STARTPTS{hold},split=2[a][b]",
             f"[a]crop={band}:1080:{band_x}:0,scale={w // 5}:{h // 5}:flags=bilinear,gblur=sigma={style.get('blur', 7)},"
             f"scale={w}:{h}:flags=bicubic,format=yuv420p[blur]",
             "[blur][1:v]overlay=0:0:shortest=1[back]",
             f"[b]crop={cw}:{ch}:{x}:{y},scale={w}:{picture_h}:flags=lanczos,format=yuva420p[pic]",
             "[2:v]format=gray[mask]",
             "[pic][mask]alphamerge[soft]",
             # (the grid of the quests says its pixels are square and the other shots do not say, and the
             # filters that join two shots stop at that)
             f"[back][soft]overlay=0:{picture_y}:shortest=1,format=yuv420p,setsar=1[out]"]
    run_ffmpeg(["-i", shot["video"], "-loop", "1", "-i", wash, "-loop", "1", "-i", mask,
                "-filter_complex", ";".join(graph), "-map", "[out]", "-frames:v", str(length), "-c:v", "libx264",
                "-preset", "ultrafast", "-crf", "12", "-r", str(fps), name], work)
    return os.path.join(work, name)


def stage_transitions(work, transitions, w, h, fps):
    """the wave of bubbles is recorded wide. the upright one is the middle of it, scaled to the height"""
    upright = {}
    for key, kind in transitions.items():
        kind = dict(kind)
        if kind["type"] == "wipe":
            for part, flags in (("matte", "neighbor"), ("fx", "lanczos")):
                name = f"tr_{key}_{part}.mp4"
                run_ffmpeg(["-i", kind[part], "-vf", f"scale=-2:{h}:flags={flags},crop={w}:{h}", "-c:v", "libx264",
                            "-preset", "ultrafast", "-crf", "10", "-r", str(fps), name], work)
                kind[part] = os.path.join(work, name)
        upright[key] = kind
    return upright


# THE WORDS

def write_text(work, key, text):
    name = key + ".txt"
    with open(os.path.join(work, name), "w", encoding="utf-8") as f:
        f.write(text)
    return name


def drawtext(work, key, text, size, style, mid, alpha, scale):
    """one line of words in the middle of the short across, at mid (the middle of its line). pale blue
    with a navy edge and shadow, like the title of the game"""
    edge = max(2, size // 16)
    drop = max(3, size // 11)
    return (f"drawtext=fontfile=font.ttf:textfile={write_text(work, key, text)}:expansion=none:fontsize={size}:"
            f"fontcolor={style.get('colour', '0xD6E9FF')}:borderw={edge}:bordercolor={style.get('shadow', '0x0A1A33')}:"
            f"shadowcolor={style.get('shadow', '0x0A1A33')}:shadowx=0:shadowy={drop}:"
            f"x=(w-text_w)/2:y={mid:.1f}-line_h/2:alpha='{alpha}'")


def fade_alpha(t0, t1, fade):
    """1 from t0 to t1, faded in and out over fade either side of that"""
    return (f"if(lt(t,{t0:.3f}),0,if(lt(t,{t0 + fade:.3f}),(t-{t0:.3f})/{fade},if(lt(t,{t1 - fade:.3f}),1,"
            f"if(lt(t,{t1:.3f}),({t1:.3f}-t)/{fade},0))))")


def block(work, key, text, size, style, mid, alpha, scale, line=1.3):
    """a few lines, around mid"""
    lines = text.split("\n")
    gap = size * line
    top = mid - gap * (len(lines) - 1) / 2
    return [drawtext(work, f"{key}_{j}", words, size, style, top + gap * j, alpha, scale) for j, words in enumerate(lines)]


def caption_times(short, shots, fps, end_at):
    """every caption, with when it starts and ends. each one gives way to the next"""
    found = []
    for cap in short.get("captions", []):
        if "at" in cap:
            t0 = float(cap["at"])
        else:
            shot = shots[int(cap.get("shot", 0))]
            frame = cap["frame"] if "frame" in cap else tc.find_mark(shot, cap["mark"], int(cap.get("occurrence", 1)))
            t0 = shot["at"] + (frame - shot["in"] + shot["pad"]) / fps
        t0 += float(cap.get("offset", 0))
        found.append((t0, t0 + float(cap.get("dur", 2.2)), cap))
    found.sort(key=lambda c: c[0])
    timed = []
    for n, (t0, t1, cap) in enumerate(found):
        if n + 1 < len(found):
            t1 = min(t1, found[n + 1][0])
        timed.append((t0, min(t1, end_at), cap))
    return timed


def lay_words(work, data, short, captions, end_at, total, scale):
    style = data.get("style", {})
    s = lambda v: int(round(v * scale))
    words = []
    hook_size = s(short.get("hook_size", style.get("hook_size", 76)))
    hook_alpha = f"if(lt(t,0.25),t/0.25,if(gt(t,{end_at:.3f}),max(0,1-(t-{end_at:.3f})/0.35),1))"
    words += block(work, "hook", short.get("hook", ""), hook_size, style, s(short.get("hook_y", style.get("hook_y", 190))),
                   hook_alpha, scale, 1.22)
    for n, (t0, t1, cap) in enumerate(captions):
        size = s(cap.get("size", style.get("caption_size", 56)))
        words += block(work, f"cap{n}", cap["text"], size, style, s(style.get("caption_y", 1450)),
                       fade_alpha(t0, t1, 0.16), scale)
    # the call, over the sky of the end card, once the transition into it is over
    cta = short.get("cta", data.get("cta", {}))
    card = data["end_card"]
    at = end_at + float(card.get("cta_at", 0.5))
    alpha = f"if(lt(t,{at:.3f}),0,min(1,(t-{at:.3f})/0.5))"
    for key in ("first", "title", "date"):
        line = cta[key]
        words += block(work, "cta_" + key, line["text"], s(line["size"]), style, s(line["y"]), alpha, scale)
    return words


# THE SOUND

def measure_loudness(video, target):
    """the first of the two passes of loudnorm: how loud it is, so that the second can move all of it by the
    same amount. the one pass mode rides the level up and down, which pumps the quiet parts"""
    out = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", video, "-vn", "-af",
                          f"loudnorm=I={target}:TP=-1.5:LRA=20:print_format=json", "-f", "null", "-"],
                         capture_output=True, text=True, encoding="utf-8", errors="replace")
    found = re.findall(r"\{[^{}]*\}", out.stderr)
    if out.returncode != 0 or not found:
        tc.fail(f"could not measure the loudness of {video}")
    return json.loads(found[-1])


def loudnorm(measured, target):
    return (f"loudnorm=I={target}:TP=-1.5:LRA=20:linear=true:measured_I={measured['input_i']}:"
            f"measured_TP={measured['input_tp']}:measured_LRA={measured['input_lra']}:"
            f"measured_thresh={measured['input_thresh']}:offset={measured['target_offset']}")


# A SHORT

def cut_short(data, short, clips, out_dir, work_root, preview, plan):
    fps = int(short.get("fps", 60))
    style = data.get("style", {})
    scale = 0.5 if preview else 1.0
    w, h = even(W * scale), even(H * scale)
    target = float(data.get("loudness", -14))
    work = tempfile.mkdtemp(prefix="short_", dir=work_root)
    try:
        # the end card is laid on as the last shot of the cut, so that a transition goes into it
        card = data["end_card"]
        make_end_plate(work, clips, card, w, h, fps)
        entries = copy.deepcopy(short["shots"])
        cut = {**short, "transitions": data.get("transitions", {}), "sounds": short.get("sounds", [])}
        notes, beats, pads, _ = tc.build_music(short.get("music", {}))
        shots = [tc.load_shot(clips, entry) for entry in entries]
        shots.append(tc.load_shot(work, {"clip": "end_plate", "plate": True, "sounds": False, "transition": card.get("transition", "fade")}))
        transitions = tc.load_transitions(cut, clips)
        total = tc.lay_out(shots, notes, beats, fps, transitions)
        sounds = (tc.game_sounds(shots, cut, fps) + tc.extra_sounds(cut, shots, fps)
                  + tc.transition_sounds(shots, transitions, cut, fps))
        # (the music is long enough for any short, and what it has after the end costs time for nothing)
        notes = [note for note in notes if note["at"] < total]
        pads = [pad for pad in pads if pad["at"] < total]
        end_at = shots[-1]["at"] + shots[-1]["overlap"] / fps
        captions = caption_times(short, shots, fps, end_at)

        print(f'{short["name"]}: {len(shots)} shots, {total:.2f} s, {len(sounds)} sounds, {len(notes)} notes. the end card is at {end_at:.2f} s')
        for n, shot in enumerate(shots):
            sync = f', "{shot["entry"]["sync"]["mark"]}" lands at {shot["sync_time"]:.2f}' if shot["sync_note"] is not None else ""
            print(f"  {n:2}  {shot['at']:6.2f} s  {shot['length']:5.2f} s  {shot['name']:30} frames {shot['in']}-{shot['out']}{sync}")
        for t0, t1, cap in captions:
            print(f"  {t0:6.2f}  {cap['text']!r}, to {t1:.2f}")
        if not 15 <= total <= 30:
            print(f"  it is {total:.1f} s long, and a short should be 15 to 30 s")
        if plan:
            return

        shutil.copy(tc.os.path.join(tc.REPO, style.get("font", "Fade.MonoGame/Assets/Fish/Fonts/ZeroCool.ttf")), os.path.join(work, "font.ttf"))
        default_crop = short.get("crop", [540, 140, 880, 880])
        # trailer_cut.render takes it from here: it joins the shots, runs the transitions and mixes the sound,
        # none of which minds what shape the pictures are. it is given the upright shots in place of the
        # wide ones, cut in at their first frame, because stage_shot already cut them
        staged = []
        for n, shot in enumerate(shots):
            print(f"  laying out {shot['name']} ...")
            video = stage_shot(work, n, shot, default_crop, style, w, h, scale, fps)
            frames = shot["out"] - shot["in"] + shot["pad"]
            staged.append({**shot, "video": video, "in": 0, "out": frames, "pad": 0, "sidecar": {}})
        base = os.path.join(work, "base.mp4")
        tc.render(staged, total, notes, pads, sounds, {k: v for k, v in cut.items() if k != "vertical"}, base, False,
                  stage_transitions(work, transitions, w, h, fps))

        graph = ",".join(lay_words(work, data, short, captions, end_at, total, scale))
        with open(os.path.join(work, "words.filter"), "w", encoding="utf-8") as f:
            f.write(f"[0:v]{graph},format=yuv420p[final]")
        measured = measure_loudness(base, target)
        out = os.path.join(out_dir, short["name"] + ("_preview" if preview else "") + ".mp4")
        crf = int(style.get("crf", 20))
        while True:
            print(f"  encoding at crf {crf} ...")
            # (aresample: the mix can start a little after 0, and a player that goes by the first packet
            # would play it early. -t: loudnorm hands back a little more sound than it was given)
            run_ffmpeg(["-i", "base.mp4", "-filter_complex_script", "words.filter", "-map", "[final]", "-map", "0:a",
                        "-c:v", "libx264", "-preset", "veryfast" if preview else "medium", "-crf", str(crf),
                        "-pix_fmt", "yuv420p", "-r", str(fps), "-af", "aresample=async=1:first_pts=0," + loudnorm(measured, target),
                        "-ar", "48000", "-c:a", "aac", "-b:a", "192k", "-t", f"{total:.3f}", "-movflags", "+faststart", out], work)
            size = os.path.getsize(out)
            if size < SIZE_LIMIT or crf >= 36:
                break
            crf += 2
        if size >= SIZE_LIMIT:
            print(f"  {out} is {size / 1048576:.1f} MiB, over the limit")
        print(f"  wrote {out}, {size / 1048576:.1f} MiB, crf {crf}")
    finally:
        shutil.rmtree(work, ignore_errors=True)


def tests_for(videos):
    """the tests that record these videos, because that is what render-trailer.ps1 -Shot picks by. a video
    and its test do not always share a name: trailer_finale records trailer_fish_live_here, say"""
    recorded_by = {}
    for path in glob.glob(os.path.join(tc.REPO, "Fade.MonoGame", "fish_tests*.fbasic")):
        test = None
        with open(path, encoding="utf-8") as f:
            for line in f:
                found = re.match(r"\s*test\s+(trailer_\w+)", line)
                if found:
                    test = found.group(1)
                found = re.search(r'trailerRecord\("(\w+)"\)', line)
                if found and test:
                    recorded_by[found.group(1)] = test
    tests = set()
    for video in videos:
        if video in recorded_by:
            tests.add(recorded_by[video])
        else:
            # (the grid of the quests is one: build/trailer_cut.py makes it out of the quests' videos)
            print(f"no test records {video}, so it stays as it is", file=sys.stderr)
    return tests


def main():
    sys.stdout.reconfigure(line_buffering=True)
    parser = argparse.ArgumentParser(description="cut the shorts")
    parser.add_argument("--cut", default=os.path.join(HERE, "shorts.json"))
    parser.add_argument("--only", nargs="*", default=[])
    parser.add_argument("--plan", action="store_true", help="print where everything lands, and make nothing")
    parser.add_argument("--preview", action="store_true", help="quickly, at half the size")
    parser.add_argument("--clips", action="store_true", help="only print the tests that record the shots they use")
    parser.add_argument("--out", help="where the videos go, in place of the file's output")
    options = parser.parse_args()

    data, shorts = load(options.cut)
    base = os.path.dirname(os.path.abspath(options.cut))
    clips = os.path.normpath(os.path.join(base, data.get("clips", "../trailer-clips")))
    out_dir = options.out or os.path.normpath(os.path.join(base, data.get("output", "../shorts")))
    # (powershell -File hands -Short a,b over as one word)
    only = [word for option in options.only for word in option.split(",") if word]
    chosen = []
    for short in shorts:
        names = [short["name"]] + [shot["clip"] for shot in short["shots"]]
        if not only or any(o in n for o in only for n in names):
            chosen.append(short)
    if not chosen:
        tc.fail("no short has any of these in its name: " + " ".join(only))
    if options.clips:
        names = {data["end_card"]["clip"]}
        for short in chosen:
            names.update(shot["clip"] for shot in short["shots"])
            for shot in short["shots"]:
                kind = data.get("transitions", {}).get(shot.get("transition"), {})
                names.update(kind[part] for part in ("matte", "fx") if part in kind)
        print("\n".join(sorted(tests_for(names))))
        return
    os.makedirs(out_dir, exist_ok=True)
    work_root = tempfile.mkdtemp(prefix="shorts_cut_")
    try:
        for short in chosen:
            cut_short(data, short, clips, out_dir, work_root, options.preview, options.plan)
    finally:
        shutil.rmtree(work_root, ignore_errors=True)


if __name__ == "__main__":
    main()
