"""The Steam page's screenshots and its description's moving pictures, cut out of the trailer's shots.

    py -3 build/steam_media.py                      every screenshot and clip in build/steam_media.json
    py -3 build/steam_media.py --only coral,night   only the ones with these in their names
    py -3 build/steam_media.py --no-clips           only the screenshots (--no-shots: only the clips)
    py -3 build/steam_media.py --sheet trailer_night_held [--every 30]
                                                    a contact sheet of a shot, a frame every 30 with its
                                                    number on it, to pick frames from

The shots are the trailer's (build/render-trailer.ps1 records them into trailer-clips/), so the pictures
keep up with the art: record the trailer again, then run this again. The trailer records near-lossless
(crf 14), so a frame of it is as good as a screenshot.

build/steam_media.json says:
    screenshots   [{"name", "clip", "frame"}]  -> <out>/screenshot_<n>_<name>.png, 1920 x 1080
    clips         [{"name", "clip", "in", "out"}] (frames of the shot; "out" < 0 counts from its end)
                  -> <out>/clips/<name>.mp4 (H.264), .webm (VP9) and .gif, silent, looping, with a short
                  fade from and to black so the seam is soft
    out           the folder (default capsules)
    clip_width, gif_width, gif_fps

Steamworks (Store Page Extra Asset Management, partner.steamgames.com/doc/store/page/assets) takes PNG,
JPG, GIF, WEBP, MP4 and WEBM for the description, plays animations only in About This Game and special
announcements, caps them at 12 s, and asks for 1170 px wide. Upload the .mp4s; the .gifs are for places
that cannot play a video (a press kit, a forum). ffmpeg has to be on the PATH.
"""

import argparse
import json
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
CLIPS = REPO / "trailer-clips"
FPS = 60
FADE = 0.3


def ffmpeg(*args):
    r = subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *map(str, args)])
    if r.returncode:
        raise SystemExit(f"ffmpeg failed: {' '.join(map(str, args))}")


def frames_of(clip):
    side = CLIPS / f"{clip}.json"
    if side.exists():
        n = json.loads(side.read_text(encoding="utf-8")).get("frames")
        if n:
            return int(n)
    out = subprocess.run(["ffprobe", "-v", "error", "-count_packets", "-select_streams", "v:0",
                          "-show_entries", "stream=nb_read_packets", "-of", "csv=p=0", str(CLIPS / f"{clip}.mp4")],
                         capture_output=True, text=True).stdout.strip()
    return int(out)


def screenshot(n, s, out):
    path = out / f"screenshot_{n}_{s['name']}.png"
    vf = f"select=eq(n\\,{s['frame']}),scale=1920:1080:flags=lanczos"
    ffmpeg("-i", CLIPS / f"{s['clip']}.mp4", "-vf", vf, "-frames:v", "1", path)
    print(f"{path.name:<36} {s['clip']} frame {s['frame']}")


def clip(c, cfg, out):
    total = frames_of(c["clip"])
    a = c.get("in", 0)
    b = c.get("out", total)
    if b < 0:
        b = total + b
    length = (b - a) / FPS
    if length > 12:
        print(f"  {c['name']}: {length:.1f} s, more than the 12 s a description clip should be")
    src = CLIPS / f"{c['clip']}.mp4"
    cut = f"trim=start_frame={a}:end_frame={b},setpts=PTS-STARTPTS"
    fades = f"fade=t=in:st=0:d={FADE},fade=t=out:st={max(0, length - FADE):.3f}:d={FADE}"
    w = cfg.get("clip_width", 1170)
    folder = out / "clips"
    folder.mkdir(parents=True, exist_ok=True)
    mp4, webm, gif = (folder / f"{c['name']}.{e}" for e in ("mp4", "webm", "gif"))
    vf = f"{cut},scale={w}:-2:flags=lanczos,{fades},format=yuv420p"
    ffmpeg("-i", src, "-an", "-vf", vf, "-c:v", "libx264", "-preset", "slow", "-crf", "20", "-movflags", "+faststart", mp4)
    ffmpeg("-i", src, "-an", "-vf", vf, "-c:v", "libvpx-vp9", "-crf", "34", "-b:v", "0", "-row-mt", "1", "-deadline", "good", "-cpu-used", "2", webm)
    # a GIF of its own palette, made from the clip itself; the sea's gradients want a gentle dither
    gw, gfps = cfg.get("gif_width", 616), cfg.get("gif_fps", 30)
    gvf = (f"{cut},fps={gfps},scale={gw}:-2:flags=lanczos,{fades},split[a][b];"
           f"[a]palettegen=max_colors=192:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle")
    ffmpeg("-i", src, "-an", "-filter_complex", gvf, gif)
    mb = lambda p: p.stat().st_size / 1e6
    print(f"{c['name']:<24} {length:4.1f} s   mp4 {mb(mp4):4.1f} MB   webm {mb(webm):4.1f} MB   gif {mb(gif):4.1f} MB")


def sheet(clip_name, every, out):
    from PIL import Image, ImageDraw
    total = frames_of(clip_name)
    tmp = out / "sheet_frames"
    tmp.mkdir(parents=True, exist_ok=True)
    for f in tmp.glob("*.png"):
        f.unlink()
    ffmpeg("-i", CLIPS / f"{clip_name}.mp4", "-vf", f"select=not(mod(n\\,{every})),scale=384:216", "-vsync", "vfr", tmp / "f_%04d.png")
    files = sorted(tmp.glob("*.png"))
    cols = 5
    rows = (len(files) + cols - 1) // cols
    img = Image.new("RGB", (cols * 384, rows * 216), (0, 0, 0))
    d = ImageDraw.Draw(img)
    for i, f in enumerate(files):
        x, y = (i % cols) * 384, (i // cols) * 216
        img.paste(Image.open(f), (x, y))
        d.rectangle([x, y, x + 60, y + 18], fill=(0, 0, 0))
        d.text((x + 4, y + 3), str(i * every), fill=(255, 255, 0))
    path = out / f"sheet_{clip_name}.png"
    img.save(path)
    for f in files:
        f.unlink()
    tmp.rmdir()
    print(f"{path}  ({total} frames)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--no-clips", action="store_true")
    ap.add_argument("--no-shots", action="store_true")
    ap.add_argument("--sheet")
    ap.add_argument("--every", type=int, default=30)
    ap.add_argument("--config", default=str(REPO / "build" / "steam_media.json"))
    args = ap.parse_args()
    cfg = json.loads(Path(args.config).read_text(encoding="utf-8"))
    out = REPO / cfg.get("out", "capsules")
    out.mkdir(parents=True, exist_ok=True)
    if args.sheet:
        sheet(args.sheet, args.every, out)
        return
    only = [o for o in args.only.split(",") if o]
    keep = lambda name: not only or any(o in name for o in only)
    for n, s in enumerate(cfg.get("screenshots", []), 1):
        if keep(s["name"]) and not args.no_shots:
            screenshot(n, s, out)
    for c in cfg.get("clips", []):
        if keep(c["name"]) and not args.no_clips:
            clip(c, cfg, out)


if __name__ == "__main__":
    main()
