"""
Draws the picture that the radio log has in the codec: a little radio, white on nothing, 48 by 48,
with no smoothing, like the art of the cards. see CODEC_ENTRY_RADIO, and MORSE in fish_headers.

    py Tools/radio_icon_gen.py                 from the Fade.MonoGame folder

It writes Assets/Fish/Textures/Cards/radio.png. it needs nothing but Python.
"""
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Fish", "Textures", "Cards", "radio.png")
SIZE = 48

lit = [[0] * SIZE for _ in range(SIZE)]


def box(x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            lit[y][x] = 1


def hole(x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            lit[y][x] = 0


def frame(x0, y0, x1, y1, thick):
    box(x0, y0, x1, y1)
    hole(x0 + thick, y0 + thick, x1 - thick, y1 - thick)


# the body, with its feet
frame(6, 20, 41, 41, 2)
box(9, 42, 12, 43)
box(35, 42, 38, 43)

# the speaker: a grid of holes, which are dots here
for row in range(4):
    for column in range(4):
        box(11 + column * 4, 25 + row * 4, 12 + column * 4, 26 + row * 4)

# the dial, and the two knobs under it
frame(29, 24, 37, 30, 1)
box(33, 26, 33, 28)
box(29, 34, 31, 36)
box(35, 34, 37, 36)

# the antenna goes up and to the right, with a knob on the end
for n in range(15):
    box(30 + n * 10 // 15, 19 - n, 31 + n * 10 // 15, 19 - n)
box(39, 3, 42, 5)

# and what comes out of it: a dot and a dash, above the radio
box(8, 9, 10, 11)
box(14, 9, 22, 11)


def chunk(kind, data):
    body = kind + data
    return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)


rows = b""
for y in range(SIZE):
    rows += b"\x00" + b"".join(b"\xff\xff\xff\xff" if lit[y][x] else b"\xff\xff\xff\x00" for x in range(SIZE))
png = (b"\x89PNG\r\n\x1a\n"
       + chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
       + chunk(b"IDAT", zlib.compress(rows, 9))
       + chunk(b"IEND", b""))
with open(OUT, "wb") as f:
    f.write(png)
print(os.path.normpath(OUT))
