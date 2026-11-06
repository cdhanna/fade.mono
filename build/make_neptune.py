"""Draws the pictures of Neptune (see THE GOD OF THE SEA in Fade.MonoGame/fish_headers_neptune.fbasic):

    Neptune/Pillar0..2.png    three columns of the temple, each grown over in a way of its own
    Neptune/Temple.png        what goes on top of the columns: the beam and the pediment
    Neptune/Steps.png         what the columns stand on
The temple has been under the sea a long time: the marble is stained, chipped at its edges and
patched with green, seaweed hangs from it and climbs it, and coral grows on it. weather() does that,
from a seed of its own for every picture, so the pictures come out the same every time.
    badge_trident.png         the badge of the ending on the tables, white, the way the others are

Everything is drawn here, in the style of the new art: no outline of its own, a dark rim in the darkest shade, light from the upper left.

    py -I build/make_neptune.py [preview folder]
"""
import os, struct, sys, zlib

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.join(HERE, '..', 'Fade.MonoGame', 'Assets', 'Fish', 'Textures')


# ---- png in and out ------------------------------------------------------------------------------

def read_png(path):
    d = open(path, 'rb').read()
    i = 8
    idat = b''
    while i < len(d):
        l = struct.unpack('>I', d[i:i + 4])[0]
        t = d[i + 4:i + 8]
        c = d[i + 8:i + 8 + l]
        if t == b'IHDR':
            w, h, bd, ct = struct.unpack('>IIBB', c[:10])
        if t == b'IDAT':
            idat += c
        i += 12 + l
    raw = zlib.decompress(idat)
    bpp = 4 if ct == 6 else 3
    stride = w * bpp
    rows = []
    prev = bytearray(stride)
    p = 0
    for y in range(h):
        f = raw[p]
        p += 1
        line = bytearray(raw[p:p + stride])
        p += stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + b) & 255
            elif f == 3:
                line[x] = (line[x] + (a + b) // 2) & 255
            elif f == 4:
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                pr = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 255
        rows.append([tuple(line[x * bpp:(x + 1) * bpp]) + ((255,) if bpp == 3 else ()) for x in range(w)])
        prev = line
    return rows


def write_png(path, rows, scale=1):
    h = len(rows) * scale
    w = len(rows[0]) * scale
    raw = b''
    for r in rows:
        line = b''.join(bytes(px) for px in r for _ in range(scale))
        raw += (b'\x00' + line) * scale
    def chunk(t, d):
        c = struct.pack('>I', len(d)) + t + d
        return c + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, 'wb').write(png)


# ---- a canvas ------------------------------------------------------------------------------------

CLEAR = (0, 0, 0, 0)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [[CLEAR] * w for _ in range(h)]

    def put(self, x, y, c):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c if len(c) == 4 else c + (255,)

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return CLEAR

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                dx = (x + .5 - cx) / rx
                dy = (y + .5 - cy) / ry
                if dx * dx + dy * dy <= 1:
                    self.put(x, y, c)

    def line(self, x0, y0, x1, y1, c, wide=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n + 1):
            t = i / max(n, 1)
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            for dx in range(wide):
                for dy in range(wide):
                    self.put(round(x) + dx, round(y) + dy, c)

    def stamp(self, rows, ox, oy, flip=False, tint=None):
        h = len(rows)
        w = len(rows[0])
        for y in range(h):
            for x in range(w):
                c = rows[y][w - 1 - x if flip else x]
                if c[3] > 0:
                    if tint:
                        c = tuple(int(c[k] * tint[k]) for k in range(3)) + (255,)
                    self.put(ox + x, oy + y, c)

    def rim(self, color, only=None):
        """every filled pixel at the edge of the shape gets the rim color"""
        edge = []
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c[3] == 0:
                    continue
                if only and c[:3] not in only:
                    continue
                if any(self.get(x + dx, y + dy)[3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    edge.append((x, y))
        for x, y in edge:
            self.put(x, y, color)

    def rows(self):
        return self.px

    def half(self):
        """half the size, keeping a pixel wherever most of the four had one"""
        out = Canvas(self.w // 2, self.h // 2)
        for y in range(out.h):
            for x in range(out.w):
                four = [self.get(x * 2 + dx, y * 2 + dy) for dy in (0, 1) for dx in (0, 1)]
                solid = [c for c in four if c[3] > 0]
                if len(solid) >= 2:
                    # the darkest of them, so that the shape keeps its rim
                    out.put(x, y, min(solid, key=lambda c: c[0] + c[1] + c[2]))
        return out


# ---- colors --------------------------------------------------------------------------------------

GOLD = (236, 190, 72)
GOLD_LIGHT = (255, 228, 140)
GOLD_SHADE = (178, 122, 40)
GOLD_RIM = (92, 54, 20)

SHELL = (246, 214, 196)
SHELL_LIGHT = (255, 238, 226)
SHELL_SHADE = (214, 160, 150)
SHELL_RIM = (110, 62, 70)

SKIN = (226, 168, 128)
SKIN_LIGHT = (246, 198, 160)
SKIN_SHADE = (186, 120, 92)
SKIN_RIM = (96, 52, 44)

BEARD = (226, 238, 248)
BEARD_SHADE = (164, 186, 214)

CLOTH = (52, 128, 214)
CLOTH_LIGHT = (96, 176, 246)
CLOTH_SHADE = (32, 82, 160)

EYE = (20, 24, 40)

MARBLE = (238, 234, 224)
MARBLE_LIGHT = (255, 253, 246)
MARBLE_SHADE = (196, 190, 178)
MARBLE_DARK = (150, 144, 136)
MARBLE_RIM = (84, 82, 96)

STAIN = (172, 178, 168)
ALGAE = (96, 150, 104)
ALGAE_DARK = (58, 104, 76)
WEED = (64, 152, 84)
WEED_LIGHT = (120, 196, 110)
WEED_DARK = (34, 96, 58)
CORAL = (240, 120, 128)
CORAL_LIGHT = (255, 176, 168)
CORAL_DARK = (176, 64, 92)


# ---- the temple ----------------------------------------------------------------------------------

import random


def solid(c, x, y):
    return c.get(x, y)[3] > 0


def weather(c, seed, weeds_down=0, weeds_up=0, corals=0, coral_rows=None):
    """the sea has had the picture a long time: moss along its tops, stains that wander, streaks where
    the water has run down it, chips out of its edges, seaweed hanging off of it and climbing it, and
    coral growing on it"""
    rng = random.Random(seed)
    marble = [MARBLE, MARBLE_LIGHT, MARBLE_SHADE, MARBLE_DARK, MARBLE_RIM]
    def stone(x, y):
        return c.get(x, y)[3] > 0 and c.get(x, y)[:3] in marble

    # moss along every top: a pixel with water over it is often green, and sometimes the one under it too
    tops = [(x, y) for y in range(c.h) for x in range(c.w) if stone(x, y) and not solid(c, x, y - 1)]
    for x, y in tops:
        if rng.random() < .55:
            c.put(x, y, WEED_DARK if rng.random() < .4 else ALGAE)
            if rng.random() < .35 and stone(x, y + 1):
                c.put(x, y + 1, ALGAE_DARK)

    # stains that wander over the stone
    for _ in range(max(2, c.w * c.h // 500)):
        x, y = rng.randrange(c.w), rng.randrange(c.h)
        col = rng.choice((STAIN, STAIN, ALGAE_DARK, ALGAE))
        for _ in range(rng.randrange(6, 22)):
            if stone(x, y):
                c.put(x, y, col if c.get(x, y)[:3] != MARBLE_RIM else MARBLE_RIM)
            x += rng.choice((-1, 0, 1))
            y += rng.choice((-1, 0, 1, 1))

    # streaks where water has run down from the tops
    for _ in range(max(2, c.w // 7)):
        x, y = rng.choice(tops)
        for k in range(rng.randrange(3, 11)):
            if stone(x, y + k) and c.get(x, y + k)[:3] != MARBLE_RIM:
                c.put(x, y + k, STAIN if k % 3 else MARBLE_SHADE)

    # chips out of the edges
    for _ in range(c.w * c.h // 200):
        x, y = rng.randrange(c.w), rng.randrange(c.h)
        if solid(c, x, y) and any(not solid(c, x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
            c.put(x, y, CLEAR)

    # seaweed hanging down from the underside of it
    for _ in range(weeds_down):
        x = rng.randrange(4, c.w - 4)
        rows = [yy for yy in range(c.h) if solid(c, x, yy)]
        if not rows:
            continue
        y = max(rows)
        long = rng.randrange(5, 13)
        for k in range(long):
            wx = x + (1 if (k // 3) % 2 else 0)
            if y + 1 + k < c.h:
                c.put(wx, y + 1 + k, WEED_DARK if k % 4 == 3 else WEED)
                if k % 3 == 1:
                    c.put(wx + 1, y + 1 + k, WEED_LIGHT)

    # seaweed climbing up it from the bottom
    for _ in range(weeds_up):
        x = rng.randrange(2, c.w - 2)
        rows = [yy for yy in range(c.h) if solid(c, x, yy)]
        if not rows:
            continue
        y = max(rows)
        tall = rng.randrange(10, max(11, c.h // 2))
        for k in range(tall):
            wx = x + (1 if (k // 4) % 2 else 0)
            if solid(c, wx, y - k):
                c.put(wx, y - k, WEED_DARK if k % 5 == 4 else WEED)
            if k % 4 == 2:
                if solid(c, wx + 1, y - k):
                    c.put(wx + 1, y - k, WEED_LIGHT)
                if solid(c, wx - 1, y - k + 1):
                    c.put(wx - 1, y - k + 1, WEED)

    # coral: little fans, growing up off of the tops
    for _ in range(corals):
        x = rng.randrange(4, c.w - 4)
        rows = [yy for yy in range(c.h) if solid(c, x, yy)]
        if not rows:
            continue
        y = min(rows) if coral_rows is None else rng.choice(coral_rows)
        if not solid(c, x, y):
            continue
        pink = rng.random() < .5
        main = CORAL if pink else (236, 160, 90)
        light = CORAL_LIGHT if pink else (255, 206, 140)
        dark = CORAL_DARK if pink else (170, 100, 50)
        size = rng.choice((3, 4, 5))
        for k in range(1, size):
            c.put(x, y - k, main)
        for side in (-1, 1):
            bx, by = x, y - 1
            for k in range(size - 1):
                bx += side
                by -= 1
                c.put(bx, by, main if k < size - 2 else light)
                if k == 1:
                    c.put(bx + side, by + 1, light)
        c.put(x, y - size, light)
        c.put(x, y, dark)


def pillar(seed):
    """a column: a capital with scrolls, a fluted shaft, and a base"""
    w, h = 18, 104
    c = Canvas(w, h)
    # the capital, with a scroll at either end
    c.rect(0, 0, w - 1, 2, MARBLE)
    c.rect(1, 3, w - 2, 6, MARBLE)
    c.ellipse(2.5, 6, 2.5, 2.5, MARBLE_SHADE)
    c.ellipse(w - 2.5, 6, 2.5, 2.5, MARBLE_SHADE)
    c.put(2, 6, MARBLE_DARK)
    c.put(w - 3, 6, MARBLE_DARK)
    c.rect(3, 8, w - 4, 9, MARBLE)
    # the shaft, fluted: a light line, a dark one, over and over, and shade down the right
    for y in range(10, h - 7):
        for x in range(3, w - 3):
            k = (x - 3) % 3
            col = MARBLE if k == 0 else (MARBLE_LIGHT if k == 1 else MARBLE_SHADE)
            if x >= w - 6:
                col = MARBLE_SHADE if k != 2 else MARBLE_DARK
            if x <= 4:
                col = MARBLE_LIGHT
            c.put(x, y, col)
    # the base
    c.rect(2, h - 7, w - 3, h - 5, MARBLE)
    c.rect(0, h - 4, w - 1, h - 1, MARBLE_SHADE)
    c.rect(0, h - 4, w - 1, h - 4, MARBLE)
    c.rim(MARBLE_RIM)
    weather(c, seed, weeds_up=3, corals=1)
    return c


def temple():
    """the beam that lies across the tops of the columns, and the pediment over it, with a trident in it"""
    w, h = 232, 64
    c = Canvas(w, h)
    # the pediment: a low triangle
    peak = 24
    for y in range(0, peak + 1):
        half = (y / peak) * (w / 2 - 2)
        for x in range(int(w / 2 - half), int(w / 2 + half) + 1):
            c.put(x, y, MARBLE)
    # the inside of it is set back, a little darker, with the trident in gold
    for y in range(6, peak - 1):
        half = ((y - 6) / (peak - 6)) * (w / 2 - 24)
        for x in range(int(w / 2 - half), int(w / 2 + half) + 1):
            c.put(x, y, MARBLE_SHADE)
    tx = w // 2
    c.rect(tx - 1, 10, tx, 21, GOLD)
    c.rect(tx - 4, 13, tx + 3, 13, GOLD)
    c.rect(tx - 4, 10, tx - 4, 13, GOLD)
    c.rect(tx + 3, 10, tx + 3, 13, GOLD)
    c.put(tx - 1, 8, GOLD)
    c.put(tx, 8, GOLD)
    c.put(tx - 1, 9, GOLD)
    c.put(tx, 9, GOLD)
    # the cornice, the frieze with its little blocks, and the beam
    c.rect(0, peak + 1, w - 1, peak + 4, MARBLE_LIGHT)
    c.rect(4, peak + 5, w - 5, peak + 14, MARBLE)
    for x in range(8, w - 8, 12):
        c.rect(x, peak + 6, x + 4, peak + 13, MARBLE_SHADE)
        c.put(x + 1, peak + 7, MARBLE_DARK)
        c.put(x + 3, peak + 7, MARBLE_DARK)
    c.rect(6, peak + 15, w - 7, peak + 22, MARBLE)
    c.rect(6, peak + 20, w - 7, peak + 22, MARBLE_SHADE)
    c.rect(4, peak + 23, w - 5, peak + 25, MARBLE_LIGHT)
    c.rim(MARBLE_RIM)
    weather(c, 11, weeds_down=15, corals=5)
    return c


def steps():
    w, h = 240, 15
    c = Canvas(w, h)
    for k in range(3):
        y0 = k * 5
        inset = (2 - k) * 6
        c.rect(inset, y0, w - 1 - inset, y0 + 4, MARBLE)
        c.rect(inset, y0, w - 1 - inset, y0, MARBLE_LIGHT)
        c.rect(inset, y0 + 4, w - 1 - inset, y0 + 4, MARBLE_SHADE)
    c.rim(MARBLE_RIM)
    weather(c, 23, corals=5, coral_rows=[5, 10])
    return c


def badge():
    """the badge of the ending: a trident, white on clear, 16 pixels, the way that the other badges are"""
    c = Canvas(16, 16)
    W = (255, 255, 255)
    c.rect(7, 4, 8, 15, W)
    c.rect(3, 6, 12, 7, W)
    c.rect(3, 1, 4, 7, W)
    c.rect(11, 1, 12, 7, W)
    c.rect(7, 0, 8, 4, W)
    c.put(2, 2, W)
    c.put(13, 2, W)
    return c


if __name__ == '__main__':
    preview = sys.argv[1] if len(sys.argv) > 1 else None
    out = os.path.join(TEX, 'Neptune')
    pieces = {
        'Pillar0.png': pillar(31),
        'Pillar1.png': pillar(47),
        'Pillar2.png': pillar(59),
        'Temple.png': temple(),
        'Steps.png': steps(),
    }
    for name, canvas in pieces.items():
        write_png(os.path.join(out, name), canvas.rows())
        if preview:
            write_png(os.path.join(preview, 'nep_' + name), canvas.rows(), 4)
    write_png(os.path.join(TEX, 'badge_trident.png'), badge().rows())
    if preview:
        write_png(os.path.join(preview, 'nep_badge.png'), badge().rows(), 8)
    print('wrote', len(pieces) + 1, 'pictures')
