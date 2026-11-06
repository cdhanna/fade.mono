# Cuts the draft key art (the capsule_draft_* tests in Fade.MonoGame/fish_tests_capsule.fbasic) to Steam's
# sizes, and composites it: a bloom on the bright parts (the title, the lit cursor), a vignette that closes
# in from the edges, and the bottom, the sides and the top pushed darker, so the eye lands on the title and the T.
#
#   py -3 build/capsule_drafts.py capsules/drafts
#   py -3 build/capsule_drafts.py capsules/drafts --steam capsules
#   py -3 build/capsule_drafts.py capsules/drafts --words capsules    only the logo's words, for the website
#
# reads <folder>/raw/capdraft_*.png and writes <folder>/<name>.png. The boxes are in the world's own pixels
# (570 x 320), whatever scale the frames were rendered at.
#
# --steam writes every picture of the Steam page and library (steam/store-checklist.md) into that folder
# instead, from the WIDE draft and the TALL one: see STEAM, further down. The library hero and the page
# background are cut from the takes with no title (raw/capdraft_*_clear.png), and the logo is keyed out of
# raw/draftlogo_black.png and raw/draftlogo_white.png (the capsule_draft_logo test), the same way
# capsule_cut.py keys the old one.
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter

WORLD_W = 570

# name: (frame, size, box x0, y0, x1, y1)

CUTS = {
    "wide_main":          ("capdraft_wide",            (1232, 706), (14, 4, 556, 315)),
    "wide_header":        ("capdraft_wide",            (920, 430),  (14, 30, 556, 283)),
    "center_main":        ("capdraft_center",          (1232, 706), (14, 4, 556, 315)),
    "center_header":      ("capdraft_center",          (920, 430),  (14, 4, 556, 257)),
    "icon_512":           ("capdraft_center",          (512, 512),  (195, 122, 375, 302)),
    "tall_library":       ("capdraft_tall",            (600, 900),  (178, 0, 392, 320)),
    "tall_vertical":      ("capdraft_tall",            (748, 896),  (151, 0, 419, 320)),
}


def bloom(im, threshold=170, radius=0.018, strength=0.55):
    """the bright parts, blurred and added back over the picture"""
    r = max(2, int(im.width * radius))
    lum = im.convert("L").point(lambda v: 0 if v < threshold else int((v - threshold) * 255 / (255 - threshold)))
    bright = Image.composite(im, Image.new("RGB", im.size), lum)
    glow = bright.filter(ImageFilter.GaussianBlur(r))
    glow = glow.point(lambda v: int(v * strength))
    return ImageChops.add(im, glow)


def vignette(im, inner=0.55, darkest=0.38):
    """darker toward the corners, an ellipse that fits the picture"""
    w, h = im.size
    small = (max(8, w // 8), max(8, h // 8))
    mask = Image.new("L", small, 0)
    d = ImageDraw.Draw(mask)
    steps = 40
    for i in range(steps):
        t = i / (steps - 1)
        scale = 1.45 - (1.45 - inner) * t
        v = int(255 * (darkest + (1 - darkest) * t))
        ew, eh = small[0] * scale, small[1] * scale
        d.ellipse([(small[0] - ew) / 2, (small[1] - eh) / 2, (small[0] + ew) / 2, (small[1] + eh) / 2], fill=v)
    mask = mask.filter(ImageFilter.GaussianBlur(small[0] / 14)).resize((w, h), Image.BICUBIC)
    dark = Image.new("RGB", (w, h), (2, 6, 18))
    return Image.composite(im, dark, mask)


def gradient(im, bottom=0.62, sides=0.42, top=0.18):
    """darker along the bottom, and down both sides, and a little at the top: a multiply, so the colors keep"""
    w, h = im.size
    sw, sh = 64, 64
    mask = Image.new("L", (sw, sh))
    px = mask.load()
    for y in range(sh):
        fy = y / (sh - 1)
        # the bottom 45% falls off toward the edge, and the top 15% a little
        b = max(0.0, (fy - 0.55) / 0.45) ** 1.6 * bottom
        t = max(0.0, (0.15 - fy) / 0.15) ** 2 * top
        for x in range(sw):
            fx = x / (sw - 1)
            edge = max(0.0, (abs(fx - 0.5) * 2 - 0.45) / 0.55) ** 1.8 * sides
            dark = 1 - (1 - b) * (1 - t) * (1 - edge)
            px[x, y] = int(255 * (1 - dark))
    mask = mask.resize((w, h), Image.BICUBIC)
    return Image.composite(im, Image.new("RGB", (w, h), (1, 4, 14)), mask)


def main(folder):
    for name, (frame, size, box) in CUTS.items():
        try:
            im = Image.open(f"{folder}/raw/{frame}.png").convert("RGB")
        except FileNotFoundError:
            print(f"skip {name}: no {frame}.png")
            continue
        k = im.width / WORLD_W
        crop = im.crop(tuple(round(v * k) for v in box))
        out = crop.resize(size, Image.LANCZOS)
        out = bloom(out)
        out = gradient(out)
        out = vignette(out)
        out.save(f"{folder}/{name}.png")
        print(name, size)


# ---- STEAM ------------------------------------------------------------------------------------------
# name: (frame, box in world pixels, how it is finished). "scene" has the bloom, the darker edges and the
# vignette of the drafts; "dim" is that, darker again and softer, for behind the page; "icon" has only the bloom.
STEAM = {
    "main_capsule.png":     ("capdraft_wide",         (14, 4, 556, 315),  "scene"),
    "header_capsule.png":   ("capdraft_wide",         (14, 30, 556, 283), "scene"),
    "library_header.png":   ("capdraft_wide",         (14, 30, 556, 283), "scene"),
    "event_cover.png":      ("capdraft_wide",         (14, 7, 556, 312),  "scene"),
    "event_header.png":     ("capdraft_wide",         (0, 34, 570, 218),  "scene"),
    "vertical_capsule.png": ("capdraft_tall",         (151, 0, 419, 320), "scene"),
    "library_capsule.png":  ("capdraft_tall",         (178, 0, 392, 320), "scene"),
    "library_hero.png":     ("capdraft_center_clear", (10, 94, 560, 271), "scene"),
    "page_background.png":  ("capdraft_wide_clear",   (0, 0, 570, 320),   "dim"),
}
SIZES = {
    "main_capsule.png": (1232, 706), "header_capsule.png": (920, 430), "library_header.png": (920, 430),
    "event_cover.png": (800, 450), "event_header.png": (1920, 622), "vertical_capsule.png": (748, 896),
    "library_capsule.png": (600, 900), "library_hero.png": (3840, 1240), "page_background.png": (1438, 810),
    "shortcut_icon.png": (512, 512), "shortcut_icon.ico": (256, 256), "community_icon.jpg": (184, 184),
    "small_capsule.png": (462, 174), "library_logo.png": (1280, 720),
}
# THE ICONS are the crab of the board (its picture with the thin outline, which reads at an icon's size),
# made bigger by whole pixels: on clear for the shortcut, and on the deep blue of the sea for the community
# icon, which is a jpg. the game's own icon (Fade.MonoGame/Icon.png) is the same crab, as it is.
CRAB = "Fade.MonoGame/Assets/Fish/Textures/Art32/Crab_b.png"
SEA_BLUE = (14, 40, 74)


def crab_icon(size, back=None):
    crab = Image.open(CRAB).convert("RGBA")
    crab = crab.crop(crab.getchannel("A").getbbox())
    k = max(1, int(size * 0.9 // max(crab.width, crab.height)))
    big = crab.resize((crab.width * k, crab.height * k), Image.NEAREST)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0) if back is None else back + (255,))
    out.alpha_composite(big, ((size - big.width) // 2, (size - big.height) // 2))
    return out


# the small capsule: the logo, as big as it goes, over the board with no title, darkened
SMALL_BACK = ("capdraft_wide_clear", (180, 93, 480, 206))


def finish(im, how):
    if how == "icon":
        return bloom(im)
    im = vignette(gradient(bloom(im)))
    if how == "dim":
        im = im.filter(ImageFilter.GaussianBlur(max(1, im.width // 400)))
        im = Image.eval(im, lambda v: int(v * 0.6))
    return im


def cut_box(folder, frame, box, size):
    im = Image.open(f"{folder}/raw/{frame}.png").convert("RGB")
    k = im.width / WORLD_W
    want = size[0] / size[1]
    x0, y0, x1, y1 = box
    if abs((x1 - x0) / (y1 - y0) - want) > 0.02:
        raise SystemExit(f"the box {box} is not the shape of {size}")
    return im.crop(tuple(round(v * k) for v in box)).resize(size, Image.LANCZOS)


def save(im, path):
    if path.endswith(".jpg"):
        im.convert("RGB").save(path, quality=95)
    elif path.endswith(".ico"):
        im.convert("RGBA").save(path, sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
    elif path.endswith("library_logo.png") or path.endswith("shortcut_icon.png"):
        im.save(path)
    else:
        im.convert("RGB").save(path)


def draft_logo(folder):
    """the logo of the drafts, keyed, the way capsule_cut.py keys the old one"""
    from pathlib import Path
    import capsule_cut
    return capsule_cut.keyed_logo(Path(folder) / "raw", False, "draftlogo")


WORDS_WIDE = 1100  # how wide the three words are together, in the website's pictures of them


def draft_words(folder, out):
    """the words of the logo, each keyed out by itself with no splash (the capsule_draft_words test), for the
    website to float: <out>/logo_words/word_0.png (the first "Fish"), word_1.png and word_2.png ("Pop"), all at
    one scale, and words.json with where each one's box is in the whole, which is WORDS_WIDE across"""
    from pathlib import Path
    import json, os
    import capsule_cut
    raw = Path(folder) / "raw"
    keyed = [capsule_cut.keyed_logo(raw, False, f"draftword{i}", where=True) for i in range(3)]
    x0 = min(at[0] for _, at in keyed)
    y0 = min(at[1] for _, at in keyed)
    x1 = max(at[0] + im.width for im, at in keyed)
    y1 = max(at[1] + im.height for im, at in keyed)
    k = WORDS_WIDE / (x1 - x0)
    os.makedirs(f"{out}/logo_words", exist_ok=True)
    words = []
    for i, (im, at) in enumerate(keyed):
        w, h = round(im.width * k), round(im.height * k)
        im.resize((w, h), Image.LANCZOS).save(f"{out}/logo_words/word_{i}.png")
        words.append({"file": f"word_{i}.png", "x": round((at[0] - x0) * k), "y": round((at[1] - y0) * k), "w": w, "h": h})
    whole = {"w": WORDS_WIDE, "h": round((y1 - y0) * k), "words": words}
    with open(f"{out}/logo_words/words.json", "w") as f:
        json.dump(whole, f, indent=2)
    print("logo_words", whole["w"], whole["h"])


def fit(logo, size, margin):
    w, h = size
    k = min(w * (1 - 2 * margin) / logo.width, h * (1 - 2 * margin) / logo.height)
    return logo.resize((round(logo.width * k), round(logo.height * k)), Image.LANCZOS)


def steam(folder, out):
    import os
    os.makedirs(out, exist_ok=True)
    for name, (frame, box, how) in STEAM.items():
        im = finish(cut_box(folder, frame, box, SIZES[name]), how)
        save(im, f"{out}/{name}")
        print(name, SIZES[name])
    for name, back in (("shortcut_icon.png", None), ("shortcut_icon.ico", None), ("community_icon.jpg", SEA_BLUE)):
        save(crab_icon(SIZES[name][0], back), f"{out}/{name}")
        print(name, SIZES[name])
    logo = draft_logo(folder)
    size = SIZES["library_logo.png"]
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    big = fit(logo, size, 0.04)
    canvas.alpha_composite(big, ((size[0] - big.width) // 2, (size[1] - big.height) // 2))
    save(canvas, f"{out}/library_logo.png")
    print("library_logo.png", size)
    draft_words(folder, out)
    size = SIZES["small_capsule.png"]
    back = cut_box(folder, SMALL_BACK[0], SMALL_BACK[1], size)
    back = finish(back, "dim").convert("RGBA")
    small = fit(logo, size, 0.03)
    back.alpha_composite(small, ((size[0] - small.width) // 2, (size[1] - small.height) // 2))
    save(back, f"{out}/small_capsule.png")
    print("small_capsule.png", size)


if __name__ == "__main__":
    args = sys.argv[1:]
    if "--words" in args:
        i = args.index("--words")
        draft_words(args[0] if i > 0 else "capsules/drafts", args[i + 1])
    elif "--steam" in args:
        i = args.index("--steam")
        steam(args[0] if i > 0 else "capsules/drafts", args[i + 1])
    else:
        main(args[0] if args else "capsules/drafts")
