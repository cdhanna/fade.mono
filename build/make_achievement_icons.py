"""Makes the Steam achievement icons out of the game's own art.

    python build/make_achievement_icons.py

Writes steam/achievement-icons/<API_NAME>.png and <API_NAME>_locked.png, 256x256, for every
achievement in steam/achievements.md. Each is drawn at 128 px with the sprite scaled by a whole
number, so the pixels stay square, and then doubled. The frame says what kind it is: gold for an
ending, bronze/silver/gold with pips for a tier, teal for the radio, violet for a card, green for
quests. The locked icon is the same picture, grey and dark.

The art is read from Fade.MonoGame/Assets/Fish/Textures, which is not in git (see push-assets.ps1).
NEVER use the old 16 px fish (Goldfish.png, Bass.png, Clownfish.png and the rest at the top of Textures),
their skeletons, or the smoke sheets: their licenses do not allow them in a logo. The fish here are the new
ones in Art32; the rest is the game's own generated art, the diver (make_diver.py), or drawn below.
"""
import math
import os
import random
from PIL import Image, ImageDraw, ImageEnhance

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEX = os.path.join(ROOT, "Fade.MonoGame", "Assets", "Fish", "Textures")
OUT = os.path.join(ROOT, "steam", "achievement-icons")

SIZE = 128   # drawn at this, then doubled
ROOM = 96    # the most room a sprite gets

FRAMES = {
    "ending": (232, 192, 96),
    "bronze": (190, 120, 64),
    "silver": (200, 210, 224),
    "gold": (244, 204, 84),
    "radio": (96, 208, 192),
    "card": (180, 140, 255),
    "quest": (122, 214, 122),
}

# white silhouettes are tinted; art that has its own colors is left alone (tint None)
ICONS = [
    # name, frame, art, tint, pips
    ("END_HOME", "ending", "ending_home.png", (200, 228, 255), 0),
    ("END_REEF", "ending", "ending_reef.png", (255, 140, 170), 0),
    ("END_ANCIENT", "ending", "ending_ancient.png", (150, 230, 140), 0),
    ("END_DIVER", "ending", "diver", None, 0),
    ("END_NEPTUNE", "ending", "badge_trident.png", (255, 215, 100), 0),
    ("END_AVA", "ending", "Art32/Angler.png", None, 0),
    ("POP_1K", "bronze", "pop", None, 1),
    ("POP_10K", "silver", "pop", None, 2),
    ("POP_100K", "gold", "pop", None, 3),
    ("GAMES_10", "bronze", "victory", None, 1),
    ("GAMES_50", "silver", "victory", None, 2),
    ("GAMES_100", "gold", "victory", None, 3),
    ("ALL_QUESTS", "quest", "quests", None, 0),
    ("CARD_ROTATE", "card", "Cards/rotate.png", (160, 220, 255), 0),
    ("CARD_OATH", "card", "Cards/oceanic_oath.png", (255, 220, 150), 0),
    # the radio: the dots and dashes of the word, as the hint. a word that shares one shows the first.
    ("MORSE_SOS", "radio", "morse:SOS", None, 0),
    ("MORSE_CQ", "radio", "morse:CQ", None, 0),
    ("MORSE_SING", "radio", "morse:SING", None, 0),
    ("MORSE_CTHULHU", "radio", "morse:CTHULHU", None, 0),
    ("MORSE_NEMO", "radio", "morse:NEMO", None, 0),
    ("MORSE_FREDDI", "radio", "morse:FREDDI", None, 0),
    ("MORSE_U571", "radio", "morse:U571", None, 0),
    ("MORSE_WILLY", "radio", "morse:WILLY", None, 0),
    ("MORSE_PEARL", "radio", "morse:PEARL", None, 0),
    ("MORSE_DOPEFISH", "radio", "morse:DOPEFISH", None, 0),
    ("MORSE_JAWS", "radio", "morse:JAWS", None, 0),
    ("MORSE_ARIEL", "radio", "morse:ARIEL", None, 0),
    ("MORSE_MYLEG", "radio", "morse:MYLEG", None, 0),
    ("MORSE_ANGLER", "radio", "morse:HELP", None, 0),
    ("MORSE_FRIEND", "radio", "morse:FRIEND", None, 0),
]


def load(rel):
    return Image.open(os.path.join(TEX, rel)).convert("RGBA")


def crop(im):
    box = im.getbbox()
    return im.crop(box) if box else im


def tint(im, rgb):
    r, g, b = rgb
    px = im.load()
    out = im.copy()
    po = out.load()
    for y in range(im.size[1]):
        for x in range(im.size[0]):
            pr, pg, pb, pa = px[x, y]
            k = (pr + pg + pb) / 765.0
            po[x, y] = (int(r * k), int(g * k), int(b * k), pa)
    return out


def scaled(im, room=ROOM):
    sc = max(1, min(room // im.size[0], room // im.size[1]))
    return im.resize((im.size[0] * sc, im.size[1] * sc), Image.NEAREST), sc


def shadowed(canvas, im, x, y, step):
    """pastes im with a dark copy of it one of its pixels down and to the right"""
    shadow = Image.new("RGBA", im.size, (4, 12, 28, 0))
    shadow.putalpha(im.getchannel("A").point(lambda a: a * 0.55))
    canvas.alpha_composite(shadow, (x + step, y + step))
    canvas.alpha_composite(im, (x, y))


def centered(canvas, im, step, dy=0):
    x = (SIZE - im.size[0]) // 2
    y = (SIZE - im.size[1]) // 2 + dy
    shadowed(canvas, im, x, y, step)


def background(name, bubbles=True):
    """the sea: bands from light at the top to deep at the bottom, and a few bubbles"""
    im = Image.new("RGBA", (SIZE, SIZE))
    d = ImageDraw.Draw(im)
    top, bottom = (44, 98, 148), (10, 26, 56)
    band = 8
    for y in range(0, SIZE, band):
        t = y / (SIZE - band)
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        d.rectangle([0, y, SIZE, y + band], fill=c + (255,))
    rng = random.Random(name)
    for _ in range(5 if bubbles else 0):
        r = rng.choice([2, 2, 3, 4])
        x = rng.randrange(10, SIZE - 10)
        y = rng.randrange(10, SIZE - 10)
        d.ellipse([x - r, y - r, x + r, y + r], outline=(150, 200, 235, 120))
    return im


def frame(im, rgb):
    d = ImageDraw.Draw(im)
    dark = tuple(int(c * 0.45) for c in rgb)
    light = tuple(min(255, int(c * 1.2) + 20) for c in rgb)
    d.rectangle([0, 0, SIZE - 1, SIZE - 1], outline=dark + (255,), width=1)
    d.rectangle([1, 1, SIZE - 2, SIZE - 2], outline=rgb + (255,), width=4)
    d.line([2, 2, SIZE - 3, 2], fill=light + (255,))  # light along the top
    d.rectangle([5, 5, SIZE - 6, SIZE - 6], outline=dark + (255,), width=1)


def pips(im, count, rgb):
    """small diamonds along the bottom, one for each tier"""
    d = ImageDraw.Draw(im)
    gap = 12
    x0 = SIZE // 2 - (count - 1) * gap // 2
    y = SIZE - 17
    dark = tuple(int(c * 0.4) for c in rgb)
    for i in range(count):
        x = x0 + i * gap
        d.polygon([(x, y - 5), (x + 5, y), (x, y + 5), (x - 5, y)], fill=rgb + (255,), outline=dark + (255,))


def pixels(rows, palette):
    """a picture written out as rows of letters, one letter for a pixel. a dot is nothing."""
    im = Image.new("RGBA", (len(rows[0]), len(rows)))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                im.putpixel((x, y), palette[ch])
    return im


def art_pop(canvas):
    # a burst of the pop, behind the goldfish
    d = ImageDraw.Draw(canvas)
    mid = (SIZE // 2, SIZE // 2 - 6)
    for n in range(8):
        a = n * 3.14159 / 4
        for r in range(26, 50, 2):
            w = 3 if r < 40 else 2
            x = int(mid[0] + r * math.cos(a))
            y = int(mid[1] + r * math.sin(a))
            d.rectangle([x - w // 2, y - w // 2, x + w // 2, y + w // 2], fill=(255, 236, 170, 210))
    fish, sc = scaled(crop(load("Art32/Goldfish_b.png")), 72)
    centered(canvas, fish, sc // 2 or 1, -6)


QUEST_BOX = [
    "WWWWWWWWWWWW....",
    "W..........W....",
    "W..........W..GG",
    "W..........W.GGG",
    "W..........WGGG.",
    "W..........GGG..",
    "W.GG......GGG...",
    "W.GGG....GGGW...",
    "W..GGG..GGG.W...",
    "W...GGGGGG..W...",
    "W....GGGG...W...",
    "W.....GG....W...",
    "WWWWWWWWWWWWW...",
]


def art_quests(canvas):
    # the box of a quest, with the check mark of one that is done
    box = pixels(QUEST_BOX, {"W": (230, 240, 255, 255), "G": (130, 240, 130, 255)})
    box, sc = scaled(box, 88)
    centered(canvas, box, sc // 2)


def art_victory(canvas, rgb, lift):
    # a V, for victory, in the metal of the tier
    rows = [
        "VV........VV",
        "VV........VV",
        "VVV......VVV",
        ".VV......VV.",
        ".VVV....VVV.",
        "..VV....VV..",
        "..VVV..VVV..",
        "...VV..VV...",
        "...VVVVVV...",
        "....VVVV....",
        "....VVVV....",
        ".....VV.....",
    ]
    light = tuple(min(255, int(c * 1.15) + 25) for c in rgb)
    v = pixels(rows, {"V": light + (255,)})
    v, sc = scaled(v, 76)
    centered(canvas, v, sc // 2, lift)


def art_qr(canvas):
    # SOS makes a QR code: one like it, with its three corner squares, and the rest made up
    n = 21
    rng = random.Random("SOS")
    grid = [[rng.random() < 0.45 for _ in range(n)] for _ in range(n)]
    for ox, oy in ((0, 0), (n - 7, 0), (0, n - 7)):
        for y in range(-1, 8):
            for x in range(-1, 8):
                gx, gy = ox + x, oy + y
                if 0 <= gx < n and 0 <= gy < n:
                    ring = max(abs(x - 3), abs(y - 3))
                    grid[gy][gx] = ring in (0, 1, 3)
    qr = Image.new("RGBA", (n + 2, n + 2), (230, 245, 255, 255))
    for y in range(n):
        for x in range(n):
            if grid[y][x]:
                qr.putpixel((x + 1, y + 1), (14, 22, 44, 255))
    qr, sc = scaled(qr, 92)
    centered(canvas, qr, 2)


def art_clown(canvas):
    # NEMO: the new goldfish, orange, with the white bands of a clownfish
    fish = load("Art32/Goldfish_b.png")
    px = fish.load()
    w, h = fish.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0 or r + g + b < 120:
                continue  # the outline stays
            k = (r + g + b) / 600.0
            px[x, y] = (min(255, int(255 * k + 30)), int(120 * k), int(20 * k), a)
    box = fish.getbbox()
    for band in (0.3, 0.55, 0.8):
        bx = int(box[0] + (box[2] - box[0]) * band)
        for y in range(h):
            for x in (bx - 1, bx, bx + 1):
                r, g, b, a = px[x, y]
                if a and r + g + b >= 120:
                    px[x, y] = (250, 250, 250, a)
            for x in (bx - 2, bx + 2):
                r, g, b, a = px[x, y]
                if a and r + g + b >= 120:
                    px[x, y] = (20, 16, 24, a)
    fish, sc = scaled(crop(fish), ROOM)
    centered(canvas, fish, sc // 2)


def art_diver(canvas):
    # the diver, upright, the way it goes up at the end: row 2 of the sheet, frames of 32
    sheet = load("DiverSheet.png")
    diver, sc = scaled(crop(sheet.crop((0, 64, 32, 96))), 104)
    centered(canvas, diver, sc // 2)


MORSE = {
    "A": ".-", "B": "-...", "C": "-.-.", "D": "-..", "E": ".", "F": "..-.", "G": "--.", "H": "....",
    "I": "..", "J": ".---", "K": "-.-", "L": ".-..", "M": "--", "N": "-.", "O": "---", "P": ".--.",
    "Q": "--.-", "R": ".-.", "S": "...", "T": "-", "U": "..-", "V": "...-", "W": ".--", "X": "-..-",
    "Y": "-.--", "Z": "--..", "1": ".----", "2": "..---", "3": "...--", "4": "....-", "5": ".....",
    "6": "-....", "7": "--...", "8": "---..", "9": "----.", "0": "-----",
}
MORSE_LIGHT = (255, 222, 140)


def art_morse(canvas, word):
    """the word on the radio, a letter to a row, centered, with no letters: the player works it out"""
    rows = [MORSE[ch] for ch in word]
    # a dot is u square, a dash is 3u long, with u between them, and a little less than u between the rows.
    # u is the biggest that fits in the frame.
    units = max(sum(3 if m == "-" else 1 for m in r) + len(r) - 1 for r in rows)
    for u in range(7, 1, -1):
        gap = u - 1
        height = len(rows) * u + (len(rows) - 1) * gap
        if units * u <= 100 and height <= 100:
            break
    widths = [(sum(3 if m == "-" else 1 for m in r) + len(r) - 1) * u for r in rows]
    y = (SIZE - height) // 2
    d = ImageDraw.Draw(canvas)
    dark = (6, 14, 30, 160)
    for r, w in zip(rows, widths):
        x = (SIZE - w) // 2
        for m in r:
            long = 3 * u if m == "-" else u
            d.rectangle([x + 1, y + 1, x + long, y + u], fill=dark)                  # its shadow
            d.rectangle([x, y, x + long - 1, y + u - 1], fill=MORSE_LIGHT + (255,))
            x += long + u
        y += u + gap


# sprites that are too dark for the sea get a light behind them: the glow of the angler's bulb
GLOW = set()


def glow(canvas):
    light = load("AnglerGlow.png").resize((SIZE, SIZE), Image.BILINEAR)
    light.putalpha(light.getchannel("A").point(lambda a: min(255, a * 1.6)))
    canvas.alpha_composite(light)


def make(name, kind, art, rgb, count):
    im = background(name, not art.startswith("morse:"))
    if name in GLOW:
        glow(im)
    room = ROOM - 14 if count else ROOM
    lift = -8 if count else 0
    if art == "pop":
        art_pop(im)
    elif art == "quests":
        art_quests(im)
    elif art == "victory":
        art_victory(im, FRAMES[kind], lift)
    elif art == "qr":
        art_qr(im)
    elif art == "clown":
        art_clown(im)
    elif art == "diver":
        art_diver(im)
    elif art.startswith("morse:"):
        art_morse(im, art[len("morse:"):])
    else:
        sprite = crop(load(art))
        if rgb:
            sprite = tint(sprite, rgb)
        sprite, sc = scaled(sprite, room)
        centered(im, sprite, sc, lift)
    frame(im, FRAMES[kind])
    if count:
        pips(im, count, FRAMES[kind])
    big = im.resize((SIZE * 2, SIZE * 2), Image.NEAREST).convert("RGB")
    big.save(os.path.join(OUT, name + ".png"))

    locked = ImageEnhance.Color(big).enhance(0)
    locked = ImageEnhance.Brightness(locked).enhance(0.5)
    locked.save(os.path.join(OUT, name + "_locked.png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    for icon in ICONS:
        make(*icon)
    print(f"{len(ICONS)} icons in {OUT}")


if __name__ == "__main__":
    main()
