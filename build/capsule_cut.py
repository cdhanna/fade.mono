"""Cuts the Steam page's art out of the frames fish_tests_capsule.fbasic rendered (build/render-capsules.ps1).

    py -3 build/capsule_cut.py <out folder> <layouts.json>

Reads <out>/raw/run.log, the [capsule] lines the tests printed:
    [capsule] take <frame>                          raw/<frame>.png was saved
    [capsule] region <name> <x> <y> <w> <h>         a box in the world's own pixels (570 x 320)
and writes <out>/<asset> for every asset in the layouts file (build/capsules.json).

AN ASSET says:
    size        [w, h], what Steam asks for
    frame       which frame of the game it is cut from (raw/<frame>.png); none for a logo-only asset
    view        the part of the world it shows: "w", its width in world pixels (the height follows from
                the size), and "dx", "dy", where its middle is from the middle of the fish (region fish),
                in world pixels. A view past the frame's edge is moved back inside it.
    logo        (optional) the title over it: "x", "y" its middle as parts of the asset (0..1), "w" its
                width as a part of the asset's width, and "stack" for a word to a line
    logo_only   the logo alone over clear, as big as fits with "margin" (a part of each side) left round it
An asset is saved by its name's ending: .png (clear kept), .jpg (no clear), and .ico with the smaller
sizes Windows asks for inside it too.

THE LOGO is keyed out of raw/logo_black.png and raw/logo_white.png (raw/logo_stack_*.png for a stacked
one): the same words over black and over white. What white lets through is 1 - alpha, and the colour
over black is the colour times alpha. The sea's specks drift between the two takes, so only the title's
own box is kept, and bits of it too small to be part of a letter are dropped.
"""

import json
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

WORLD_W, WORLD_H = 570, 320
ICO_SIZES = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]


def read_log(log):
    takes, regions = set(), {}
    for line in log.read_text(encoding="utf-8-sig").splitlines():
        parts = line.split()
        if len(parts) >= 3 and parts[0] == "[capsule]" and parts[1] == "take":
            takes.add(parts[2])
        elif len(parts) == 7 and parts[0] == "[capsule]" and parts[1] == "region":
            regions[parts[2]] = tuple(float(p) for p in parts[3:])
    return takes, regions


# ---- the logo --------------------------------------------------------------------------------------

def keyed_logo(raw, stack, base=None, where=False):
    """the title keyed out of its takes over black and white; with where, also its top left in the frame"""
    base = base or ("logo_stack" if stack else "logo")
    black = Image.open(raw / f"{base}_black.png").convert("RGB")
    white = Image.open(raw / f"{base}_white.png").convert("RGB")
    # the title's box: its bright letters over black, and room round them for the shadow
    bright = black.convert("L").point(lambda v: 255 if v > 150 else 0)
    box = bright.getbbox()
    if box is None:
        raise SystemExit(f"raw/{base}_black.png has no title in it")
    s = black.width / WORLD_W
    pad = round(8 * s)
    box = (max(0, box[0] - pad), max(0, box[1] - pad), min(black.width, box[2] + pad), min(black.height, box[3] + pad))
    black, white = black.crop(box), white.crop(box)
    through = ImageChops.subtract(white, black)
    # the most that any channel lets through is how see-through the pixel is
    r, g, b = through.split()
    alpha = ImageChops.invert(ImageChops.lighter(ImageChops.lighter(r, g), b))
    alpha = alpha.point(lambda a: 0 if a < 24 else a)
    # a speck is a bit of the sea, far smaller than a letter: everything that touches is found, a world
    # pixel at a time, and the bits under SPECK world pixels (a bubble, a speck; a letter is hundreds) are dropped
    alpha = ImageChops.multiply(alpha, unspecked(alpha, s))
    # the colour, out from under its alpha
    a = alpha.load()
    bl = black.load()
    out = Image.new("RGBA", black.size, (0, 0, 0, 0))
    o = out.load()
    for y in range(black.height):
        for x in range(black.width):
            av = a[x, y]
            if av:
                c = bl[x, y]
                o[x, y] = (min(255, c[0] * 255 // av), min(255, c[1] * 255 // av), min(255, c[2] * 255 // av), av)
    inside = out.getchannel("A").getbbox()
    if where:
        return out.crop(inside), (box[0] + inside[0], box[1] + inside[1])
    return out.crop(inside)


SPECK = 60


def unspecked(alpha, s):
    """A mask of alpha with the small bits taken away, worked out at the world's own size."""
    gw, gh = max(1, round(alpha.width / s)), max(1, round(alpha.height / s))
    grid = alpha.resize((gw, gh), Image.BOX).point(lambda a: 255 if a > 8 else 0)
    px = grid.load()
    seen = set()
    drop = Image.new("L", (gw, gh), 255)
    dp = drop.load()
    for sy in range(gh):
        for sx in range(gw):
            if px[sx, sy] == 0 or (sx, sy) in seen:
                continue
            bit, todo = [], [(sx, sy)]
            seen.add((sx, sy))
            while todo:
                x, y = todo.pop()
                bit.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1), (x + 1, y + 1), (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1)):
                    if 0 <= nx < gw and 0 <= ny < gh and px[nx, ny] and (nx, ny) not in seen:
                        seen.add((nx, ny))
                        todo.append((nx, ny))
            if len(bit) < SPECK:
                for x, y in bit:
                    dp[x, y] = 0
    return drop.resize(alpha.size, Image.NEAREST).filter(ImageFilter.MinFilter(3))


# ---- the cuts --------------------------------------------------------------------------------------

def save(img, path):
    ext = path.suffix.lower()
    if ext in (".jpg", ".jpeg"):
        img.convert("RGB").save(path, quality=95)
    elif ext == ".ico":
        img.convert("RGBA").save(path, sizes=[sz for sz in ICO_SIZES if sz[0] <= img.width])
    elif path.stem == "library_logo":
        img.save(path)
    else:
        # only the logo is see-through. the game's frames can carry a little alpha from the fade of the render
        # target, and Steam shows the page's color through it
        img.convert("RGB").save(path)


def scaled(img, size):
    """Down with Lanczos; up (never wanted, but a view can ask for it) with nearest, to keep the pixels."""
    if size[0] <= img.width:
        return img.resize(size, Image.LANCZOS)
    return img.resize(size, Image.NEAREST)


def cut(name, spec, raw, regions, logos, out):
    w, h = spec["size"]
    if spec.get("logo_only"):
        lg = logos(spec.get("stack", False))
        room = 1 - 2 * spec.get("margin", 0.04)
        k = min(w * room / lg.width, h * room / lg.height)
        layer = lg.resize((round(lg.width * k), round(lg.height * k)), Image.LANCZOS)
        pic = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        pic.alpha_composite(layer, ((w - layer.width) // 2, (h - layer.height) // 2))
        save(pic, out / name)
        return pic.size, "logo"

    frame = Image.open(raw / f"{spec['frame']}.png").convert("RGBA")
    s = frame.width / WORLD_W
    fx, fy, fw, fh = regions["fish"]
    view = spec["view"]
    vw = view["w"]
    vh = vw * h / w
    cx = fx + fw / 2 + view.get("dx", 0)
    cy = fy + fh / 2 + view.get("dy", 0)
    # kept inside the frame
    vw, vh = min(vw, WORLD_W), min(vh, WORLD_H)
    x0 = min(max(cx - vw / 2, 0), WORLD_W - vw)
    y0 = min(max(cy - vh / 2, 0), WORLD_H - vh)
    box = tuple(round(v * s) for v in (x0, y0, x0 + vw, y0 + vh))
    pic = scaled(frame.crop(box), (w, h))
    if "logo" in spec:
        lo = spec["logo"]
        lg = logos(lo.get("stack", False))
        lw = round(w * lo["w"])
        layer = lg.resize((lw, round(lg.height * lw / lg.width)), Image.LANCZOS)
        pic.alpha_composite(layer, (round(w * lo["x"] - layer.width / 2), round(h * lo["y"] - layer.height / 2)))
    save(pic if (out / name).suffix.lower() == ".png" else pic.convert("RGB"), out / name)
    return pic.size, spec["frame"]


def main():
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    out = Path(sys.argv[1])
    raw = out / "raw"
    layouts = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))
    takes, regions = read_log(raw / "run.log")
    if "fish" not in regions:
        raise SystemExit(f"no [capsule] region fish line in {raw / 'run.log'}: render capsule_keyart")
    cache = {}

    def logos(stack):
        if stack not in cache:
            cache[stack] = keyed_logo(raw, stack)
        return cache[stack]

    for name, spec in layouts["assets"].items():
        if spec.get("frame") and spec["frame"] not in takes:
            print(f"{name:<24} skipped: raw/{spec['frame']}.png was never taken")
            continue
        size, src = cut(name, spec, raw, regions, logos, out)
        print(f"{name:<24} {size[0]} x {size[1]}  from {src}")


if __name__ == "__main__":
    main()
