"""
Draws the picture that the usual ending has in the codec: a little house, white on nothing, 16 by
16, with no smoothing, like the heart and the eye of the other endings. see CODEC_ENTRY_GOODBYE,
and THE FISH GO HOME in fish_headers.

    py Tools/ending_home_gen.py                from the Fade.MonoGame folder

It writes Assets/Fish/Textures/ending_home.png. it needs nothing but Python.
"""
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Fish", "Textures", "ending_home.png")
SIZE = 16

lit = [[0] * SIZE for _ in range(SIZE)]


def box(x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            lit[y][x] = 1


def hole(x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            lit[y][x] = 0


# the roof, which is two pixels wider on every row down, and the chimney on the right of it
for row in range(6):
    box(7 - row, 2 + row, 8 + row, 2 + row)
box(11, 2, 12, 5)

# the walls, with a door in the middle and nobody home
box(3, 8, 12, 13)
hole(7, 10, 8, 13)

rows = b""
for y in range(SIZE):
    rows += b"\x00"
    for x in range(SIZE):
        rows += b"\xff\xff\xff\xff" if lit[y][x] else b"\xff\xff\xff\x00"


def chunk(kind, data):
    body = kind + data
    return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)


png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(rows, 9))
png += chunk(b"IEND", b"")

with open(OUT, "wb") as f:
    f.write(png)
print("wrote", os.path.normpath(OUT))
