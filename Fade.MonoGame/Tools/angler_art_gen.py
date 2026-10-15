"""Draws the pictures of the old angler fish, who answers questions. see THE ANGLER in
fish_headers_angler.

    py Tools/angler_art_gen.py                 from the Fade.MonoGame folder
    py Tools/angler_art_gen.py <folder>        and also writes big copies there, to look at

It writes these:

    Angler.png              the fish, facing right, with the stalk that its light hangs from.
                            the light is not on it. it is a sprite of its own, so that it can glow
                            and swing. its socket is at ANGLER_TIP below.
    AnglerBulb.png          the light: a little bulb, the kind with a screw at the top
    AnglerGlow.png          the soft light that goes around it. white, so that it can be tinted.
    Cards/bright_idea.png   the picture of the card. 48 by 48, white on nothing, like the other cards.
    AnglerIcon.png          a bulb that can stand in a line of text, like {bulb}. 16 by 16, white,
                            so that the text tints it, like the pictures of the kinds of card.

this needs nothing but python.
"""
import math
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
TEXTURES = os.path.join(HERE, "..", "Assets", "Fish", "Textures")

CLEAR = (0, 0, 0, 0)
OUTLINE = (12, 10, 20, 255)

BODY = (74, 62, 92, 255)       # a dusky purple
BODY_DARK = (52, 42, 68, 255)  # the shade of it, along the back and under the fins
BELLY = (112, 96, 120, 255)    # a paler belly
FIN = (60, 50, 80, 255)
FIN_RIB = (92, 80, 112, 255)
MOUTH = (30, 14, 26, 255)      # the inside of the mouth
TOOTH = (236, 232, 214, 255)
EYE = (230, 236, 190, 255)
PUPIL = (12, 10, 20, 255)
STALK = (120, 104, 130, 255)
SPOT = (96, 82, 116, 255)      # the freckles on the back

# where the light hangs from, in the pixels of Angler.png. the game puts the middle of the top of
# the bulb here. it is ANGLER_TIP_X and ANGLER_TIP_Y in fish_headers_angler.
ANGLER_TIP = (47, 9)

W, H = 52, 34


def grid(w, h):
    return [[CLEAR] * w for _ in range(h)]


def put(pic, x, y, color):
    if 0 <= y < len(pic) and 0 <= x < len(pic[0]):
        pic[y][x] = color


def outline(pic, dark=OUTLINE):
    """Every empty pixel that is next to a drawn one, up, down, left or right, goes dark."""
    h = len(pic)
    w = len(pic[0])
    out = [row[:] for row in pic]
    for y in range(h):
        for x in range(w):
            if pic[y][x][3]:
                continue
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if 0 <= nx < w and 0 <= ny < h and pic[ny][nx][3]:
                    out[y][x] = dark
                    break
    return out


def angler():
    pic = grid(W, H)

    # the body is a big round lump, heavier at the front, with a huge underbite
    cx, cy = 26.0, 19.0
    for y in range(H):
        for x in range(W):
            dx = (x - cx) / (17.5 if x > cx else 13.0)
            dy = (y - cy) / 11.0
            if dx * dx + dy * dy <= 1:
                color = BODY
                if y < cy - 6:
                    color = BODY_DARK
                if y > cy + 3:
                    color = BELLY
                pic[y][x] = color

    # the tail: a fan behind the body, narrow where it joins on and wide at the end, with a notch
    # in the end, and ribs that spread out from the joint
    for x in range(2, 15):
        spread = 3 + (14 - x) * 0.62
        notch = 2.2 if x < 5 else 0
        for y in range(H):
            off = abs(y - 19)
            if off <= spread and not (off < notch):
                put(pic, x, y, FIN)
    for slope in (-0.62, -0.3, 0.3, 0.62):
        for x in range(3, 14):
            y = int(round(19 + (14 - x) * slope))
            if pic[y][x] == FIN:
                put(pic, x, y, FIN_RIB)

    # a ragged little fin on the back
    for n, (x, top) in enumerate(((20, 8), (21, 7), (22, 7), (23, 8), (24, 7), (25, 8))):
        for y in range(top, 10):
            put(pic, x, y, FIN)

    # the fin at its side, low down
    for y in range(22, 28):
        for x in range(22, 22 + (y - 21)):
            put(pic, x, y, FIN)
    for x in range(23, 28):
        put(pic, x, 26, FIN_RIB)

    # the mouth: a wide dark gape, with the lower jaw jutting out past the upper one
    for y in range(17, 25):
        left = 34 + (y - 17) // 2
        for x in range(left, 45):
            put(pic, x, y, MOUTH)
    # the lower jaw reaches further forward
    for x in range(36, 46):
        put(pic, x, 25, BELLY)
        put(pic, x, 26, BELLY)
    for x in range(38, 44):
        put(pic, x, 27, BELLY)

    # the teeth: long and crooked, down from the top, and up from the jaw
    for x, top, length in ((36, 17, 3), (38, 17, 2), (40, 17, 4), (42, 17, 2), (44, 17, 3)):
        for y in range(top, top + length):
            put(pic, x, y, TOOTH)
    for x, bottom, length in ((37, 24, 2), (39, 24, 3), (41, 24, 2), (43, 24, 3), (45, 24, 2)):
        for y in range(bottom - length + 1, bottom + 1):
            put(pic, x, y, TOOTH)

    # a small pale eye, high up and near the front, which is not looking at anything much
    for x, y in ((36, 11), (37, 11), (36, 12), (37, 12)):
        put(pic, x, y, EYE)
    put(pic, 37, 12, PUPIL)

    # freckles on the back
    for x, y in ((17, 12), (21, 11), (28, 10), (31, 12), (15, 16), (25, 13)):
        put(pic, x, y, SPOT)

    # the stalk: up from the top of the head, and over in an arch, out in front of the mouth
    tx, ty = ANGLER_TIP
    points = []
    steps = 60
    for n in range(steps + 1):
        t = n / steps
        # a curve from the head (31, 8) up to (38, 1) and down to the tip
        x = (1 - t) ** 2 * 31 + 2 * (1 - t) * t * 40 + t * t * tx
        y = (1 - t) ** 2 * 9 + 2 * (1 - t) * t * -6 + t * t * (ty - 1)
        points.append((int(round(x)), int(round(y))))
    for x, y in points:
        put(pic, x, y, STALK)

    return outline(pic)


def bulb():
    """A bulb, 7 across and 10 down: a screw at the top, and the glass hanging under it."""
    rows = [
        "..sSs..",
        "..SsS..",
        "..sSs..",
        ".GGGGG.",
        "GGWWGGG",
        "GWWGGGG",
        "GWGGGGG",
        "GGGGGGg",
        ".GGGGg.",
        "..ggg..",
    ]
    colors = {
        "S": (180, 176, 168, 255),
        "s": (120, 116, 112, 255),
        "G": (255, 232, 140, 255),
        "g": (232, 188, 90, 255),
        "W": (255, 255, 236, 255),
    }
    pic = [[colors[c] if c != "." else CLEAR for c in row] for row in rows]
    return outline(pic)


def glow(size=64):
    """A soft round light, white, brightest in the middle. it is the one smooth thing here."""
    pic = grid(size, size)
    half = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - half, y - half) / half
            if d < 1:
                a = (1 - d) ** 2
                pic[y][x] = (255, 255, 255, int(round(255 * a)))
    return pic


def card():
    """A bulb on the end of a stalk that comes down from the top left, like the light of an angler fish."""
    size = 48
    on = [[0] * size for _ in range(size)]

    def dot(x, y):
        x = int(round(x))
        y = int(round(y))
        if 0 <= x < size and 0 <= y < size:
            on[y][x] = 1

    # the stalk, two pixels thick: out from the top left, and over in an arch, down to the bulb
    for n in range(200):
        t = n / 199
        x = (1 - t) ** 2 * 4 + 2 * (1 - t) * t * 30 + t * t * 30
        y = (1 - t) ** 2 * 18 + 2 * (1 - t) * t * -4 + t * t * 13
        dot(x, y)
        dot(x, y + 1)

    # the screw at the top of the bulb
    for y in range(14, 20):
        for x in range(27, 34):
            if y % 2 == 0 or x in (27, 33):
                dot(x, y)

    # the glass: a ring, with a glint inside
    cx, cy, r = 30, 29, 9
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - cx, y - cy)
            if r - 2 <= d <= r and y >= 20:
                dot(x, y)
    for x, y in ((26, 26), (26, 27), (27, 25), (28, 24)):
        dot(x, y)

    # rays of light around it
    for angle in (-40, 0, 40, 90, 140, 180, 220):
        a = math.radians(angle)
        for s in range(13, 17):
            dot(cx + math.cos(a) * s, cy + math.sin(a) * s)

    return [[(255, 255, 255, 255) if v else CLEAR for v in row] for row in on]


def icon():
    """A bulb that sits in a line of text, 16 by 16, white."""
    rows = [
        "................",
        "......####......",
        ".....######.....",
        "....##..####....",
        "....#..#####....",
        "....#.######....",
        "....########....",
        ".....######.....",
        "......####......",
        "......#..#......",
        "......####......",
        "......#..#......",
        ".......##.......",
        "................",
        "................",
        "................",
    ]
    return [[(255, 255, 255, 255) if c == "#" else CLEAR for c in row] for row in rows]


def write_png(path, rows):
    h = len(rows)
    w = len(rows[0])
    raw = b"".join(b"\x00" + b"".join(bytes(p) for p in row) for row in rows)

    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body) & 0xffffffff)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def big(pic, scale, back=(24, 40, 70, 255)):
    """A copy that is easy to look at: every pixel a block, over the color of the sea."""
    out = []
    for row in pic:
        line = []
        for p in row:
            a = p[3] / 255
            c = tuple(int(round(p[i] * a + back[i] * (1 - a))) for i in range(3)) + (255,)
            line += [c] * scale
        out += [line[:] for _ in range(scale)]
    return out


PICTURES = (
    (os.path.join(TEXTURES, "Angler.png"), angler()),
    (os.path.join(TEXTURES, "AnglerBulb.png"), bulb()),
    (os.path.join(TEXTURES, "AnglerGlow.png"), glow()),
    (os.path.join(TEXTURES, "Cards", "bright_idea.png"), card()),
    (os.path.join(TEXTURES, "AnglerIcon.png"), icon()),
)

for path, pic in PICTURES:
    assert all(len(row) == len(pic[0]) for row in pic), path
    write_png(path, pic)
    print(os.path.basename(path), len(pic[0]), "x", len(pic))
    if len(sys.argv) > 1:
        write_png(os.path.join(sys.argv[1], "big_" + os.path.basename(path)), big(pic, 8))
