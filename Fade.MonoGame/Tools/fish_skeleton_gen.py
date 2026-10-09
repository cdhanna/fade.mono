"""Draws the skeleton of every kind of fish, from the picture of the fish. see SKELETONS in fish_headers.

    python fish_skeleton_gen.py <the Fish/Textures folder>

a skeleton keeps the dark outline of its fish, and its eyes, so that it can be told which fish it was.
what was inside the outline goes dark, and the bones are drawn over that: a skull where the head was,
a spine, ribs, and the rays of the fins. the kinds that are not shaped like a fish have rules of their own.

this needs nothing but python. the pictures are read and written by the two functions at the top.
"""
import os
import sys
import struct
import zlib


def read_png(path):
    data = open(path, "rb").read()
    pos = 8
    idat = b""
    while pos < len(data):
        length, kind = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            w, h, depth, color = struct.unpack(">IIBB", body[:10])
            assert depth == 8 and color == 6, (path, depth, color)
        if kind == b"IDAT":
            idat += body
        pos += 12 + length
    raw = zlib.decompress(idat)
    stride = w * 4
    rows = []
    prev = bytearray(stride)
    at = 0
    for _ in range(h):
        f = raw[at]
        line = bytearray(raw[at + 1:at + 1 + stride])
        at += 1 + stride
        for i in range(stride):
            a = line[i - 4] if i >= 4 else 0
            b = prev[i]
            c = prev[i - 4] if i >= 4 else 0
            if f == 1:
                line[i] = (line[i] + a) & 255
            elif f == 2:
                line[i] = (line[i] + b) & 255
            elif f == 3:
                line[i] = (line[i] + (a + b) // 2) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
        rows.append([tuple(line[x * 4:x * 4 + 4]) for x in range(w)])
        prev = line
    return rows


def write_png(path, rows):
    h = len(rows)
    w = len(rows[0])
    raw = b"".join(b"\x00" + b"".join(bytes(p) for p in row) for row in rows)

    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body) & 0xffffffff)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


BONE = (236, 232, 204, 255)
SHADE = (178, 172, 142, 255)
DARK = (30, 28, 48, 235)
SOCKET = (8, 6, 14, 255)


def is_dark(p):
    return p[3] >= 128 and (p[0] + p[1] + p[2]) / 3 < 50


def is_inside(p):
    return p[3] >= 128 and not is_dark(p)


def fish(x, y, skull_x, spine_y, tail_x):
    """A fish seen from the side, facing right: skull, spine, ribs, and the rays of the tail."""
    if x >= skull_x:
        return BONE
    if x <= tail_x:
        return SHADE if (y - spine_y) % 2 == 0 else DARK
    if y == spine_y:
        return BONE
    return SHADE if (x - skull_x) % 2 == 0 else DARK


def crab(x, y):
    """The shell is a skull with a row of teeth, and the claws and legs are bones with joints."""
    if 9 <= y <= 11 and 1 <= x <= 14:
        if y == 11 and 3 <= x <= 12 and x % 2 == 1:
            return SOCKET
        return BONE
    if y in (5, 8):
        return DARK
    return SHADE if y >= 12 else BONE


def worm(x, y):
    """A string of little bones, with a skull at the end."""
    if y <= 5:
        return BONE
    return DARK if (x + y) % 3 == 0 else SHADE


def jelly(x, y):
    """The bell is a skull, with teeth along the bottom of it, and the arms are strings of bones."""
    if y <= 7:
        if y == 7 and x % 2 == 0:
            return SOCKET
        if y == 5 and 7 <= x <= 8:
            return SOCKET
        return BONE
    return DARK if y % 2 == 0 else SHADE


def ray(x, y):
    """Seen from above: a spine down the middle, ribs fanning out into the wings, and a tail of little bones."""
    if y <= 5:
        return BONE
    if y >= 12:
        return DARK if x % 2 == 0 else SHADE
    if x == 8:
        return BONE
    return SHADE if y % 2 == 0 else DARK


BOTTLE_SKULL = {8: ".BBBB.", 9: "BBBBBB", 10: "BsBBsB", 11: "BBssBB", 12: ".BBBB.", 13: ".BsBs."}


def bottle(x, y, old):
    """The glass goes murky, and there is a little skull in it."""
    row = BOTTLE_SKULL.get(y)
    if row and 5 <= x <= 10:
        mark = row[x - 5]
        if mark == "B":
            return BONE
        if mark == "s":
            return SOCKET
    if y <= 2:
        return old  # the cork
    return (old[0] * 2 // 5, old[1] * 2 // 5 + 20, old[2] * 2 // 5, old[3])


KINDS = (
    ("Pufferfish", lambda x, y, old: fish(x, y, 10, 8, 2)),
    ("Catfish", lambda x, y, old: fish(x, y, 9, 7, 3)),
    ("Goldfish", lambda x, y, old: fish(x, y, 10, 8, 3)),
    ("Angelfish", lambda x, y, old: fish(x, y, 9, 9, 3)),
    ("Crab - Dungeness", lambda x, y, old: crab(x, y)),
    ("Bass", lambda x, y, old: fish(x, y, 10, 8, 3)),
    ("Worm", lambda x, y, old: worm(x, y)),
    ("Jellyfish", lambda x, y, old: jelly(x, y)),
    ("Stingray", lambda x, y, old: ray(x, y)),
    ("Bottle", bottle),
)

folder = sys.argv[1]
for name, bones in KINDS:
    rows = read_png(os.path.join(folder, name + ".png"))
    out = []
    for y, row in enumerate(rows):
        out.append([bones(x, y, p) if is_inside(p) else p for x, p in enumerate(row)])
    write_png(os.path.join(folder, "Skeleton " + name + ".png"), out)
    print("Skeleton " + name + ".png")
