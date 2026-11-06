"""Draws the squid of the night (Fade.MonoGame/Assets/Fish/Textures/Art32/Squid.png), in the style of
the new 32 pixel fish: no outline of its own (FishEffect draws the thick one), a dark rim in the darkest
shade of the body, light from the upper left, big eyes, and a scatter of pale spots that light up at
night the way that the pale parts of every fish do. see THE SQUID in fish_headers_squid.

    py -I build/make_squid.py            writes the picture, and a preview 8 times as big next to it
"""
import os, struct, sys, zlib

W = H = 32

RIM = (92, 22, 74)
SHADE = (176, 52, 112)
BODY = (222, 86, 138)
LIGHT = (242, 146, 182)
SHINE = (255, 214, 228)
EYE_WHITE = (255, 255, 255)
EYE_DARK = (0, 0, 0)
SPOT = (168, 236, 255)


# how far the hood reaches either side of the middle line, which is between columns 15 and 16, row by row
MANTLE = {1: 1, 2: 2, 3: 3, 4: 3, 5: 4, 6: 4, 7: 4, 8: 4, 9: 4, 10: 5, 11: 5, 12: 5, 13: 5, 14: 5,
          15: 5, 16: 5, 17: 5}
# and the head under it, with the eyes
HEAD = {18: 7, 19: 8, 20: 8, 21: 8, 22: 7}
# how far out a fin reaches past the hood, row by row. they are near the top, and swept back.
FINS = {3: 1, 4: 3, 5: 5, 6: 6, 7: 6, 8: 4, 9: 2}
# the middle of each arm, row by row from row 23. they are 3 pixels wide, and curl out at the ends.
ARMS = [
    [9, 9, 8, 8, 7, 6, 5],
    [13, 13, 13, 12, 12, 12, 13, 14],
    [18, 18, 18, 19, 19, 19, 18, 17],
    [22, 22, 23, 23, 24, 25, 26],
]


def spans(table, extra=None):
    cells = set()
    for y, half in table.items():
        for x in range(16 - half - (extra or {}).get(y, 0), 16 + half + (extra or {}).get(y, 0)):
            cells.add((x, y))
    return cells


def inside_mantle(x, y):
    return (x, y) in spans(MANTLE)


def inside_fins(x, y):
    fins = set()
    for y2, reach in FINS.items():
        half = MANTLE[y2]
        for k in range(1, reach + 1):
            fins.add((16 - half - k, y2))
            fins.add((16 + half - 1 + k, y2))
    return (x, y) in fins


def inside_head(x, y):
    return (x, y) in spans(HEAD)


def arms():
    cells = set()
    for path in ARMS:
        for k, cx in enumerate(path):
            for dx in (-1, 0, 1):
                cells.add((cx + dx, 23 + k))
    return cells


def draw():
    filled = {}
    arm = arms()
    for y in range(H):
        for x in range(W):
            if inside_mantle(x, y) or inside_fins(x, y) or inside_head(x, y) or (x, y) in arm:
                filled[(x, y)] = BODY

    # shading: the light comes from the upper left, so the right of everything is in shade
    for (x, y) in list(filled):
        if x >= 18:
            filled[(x, y)] = SHADE
        if (x - 2, y) not in filled and (x - 1, y) in filled:
            filled[(x, y)] = LIGHT
        if (x, y) in arm and x >= 16:
            filled[(x, y)] = SHADE

    # a shine down the hood
    for (x, y) in ((15, 2), (14, 3), (14, 4), (13, 5), (13, 6), (13, 7), (13, 8), (12, 10), (12, 11), (12, 12)):
        filled[(x, y)] = SHINE

    # the pale spots, which glow at night
    for (x, y) in ((16, 4), (16, 8), (18, 11), (14, 14), (17, 16), (5, 7), (26, 7)):
        if (x, y) in filled:
            filled[(x, y)] = SPOT

    # the rim: every pixel at the edge is the darkest shade
    for (x, y) in list(filled):
        edge = any((x + dx, y + dy) not in filled for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        if edge:
            filled[(x, y)] = RIM

    # the eyes, big ones, on the head, looking down and in
    for (x, y) in ((10, 19), (11, 19), (10, 20), (11, 20), (20, 19), (21, 19), (20, 20), (21, 20)):
        filled[(x, y)] = EYE_WHITE
    for (x, y) in ((11, 20), (20, 20)):
        filled[(x, y)] = EYE_DARK

    rows = []
    for y in range(H):
        row = []
        for x in range(W):
            c = filled.get((x, y))
            row.append((0, 0, 0, 0) if c is None else c + (255,))
        rows.append(row)
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
    open(path, 'wb').write(png)


if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(here, '..', 'Fade.MonoGame', 'Assets', 'Fish', 'Textures', 'Art32', 'Squid.png')
    rows = draw()
    write_png(out, rows)
    preview = sys.argv[1] if len(sys.argv) > 1 else None
    if preview:
        write_png(preview, rows, 8)
    print('wrote', os.path.normpath(out))
