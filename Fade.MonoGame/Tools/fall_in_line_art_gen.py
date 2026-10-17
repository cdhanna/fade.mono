"""Draws the picture of the "fall in line" card: three little fish, one under the other, all facing the
same way, with an arrow coming down beside them, for fish that fall together and school.

    py Tools/fall_in_line_art_gen.py

48 by 48, white on nothing, with hard edges, like the rest of Assets/Fish/Textures/Cards. See THE CARD in
fish_headers_school. There is no PIL here, so the PNG is written by hand.
"""
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Fish", "Textures", "Cards", "fall_in_line.png")
SIZE = 48


def blank():
    return [[0] * SIZE for _ in range(SIZE)]


def dot(on, x, y):
    if 0 <= x < SIZE and 0 <= y < SIZE:
        on[y][x] = 1


def rect(on, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            dot(on, x, y)


def fish(on, x, y):
    """a little fish facing right, 14 wide and 8 tall, its body starting at x, y"""
    # the body: a rounded block
    rect(on, x + 4, y + 1, x + 11, y + 6)
    rect(on, x + 3, y + 2, x + 12, y + 5)
    rect(on, x + 5, y, x + 10, y + 7)
    # the tail, at the back: a fan that gets wider away from the body, on a narrow stem
    rect(on, x, y, x, y + 7)
    rect(on, x + 1, y + 1, x + 1, y + 6)
    rect(on, x + 2, y + 2, x + 2, y + 5)
    rect(on, x + 3, y + 3, x + 3, y + 4)
    # the eye, near the front, as a hole
    on[y + 2][x + 9] = 0


def arrow(on, x, y0, y1):
    """an arrow pointing down, its shaft 2 wide at x"""
    rect(on, x, y0, x + 1, y1 - 3)
    for k in range(4):
        rect(on, x - 3 + k, y1 - 3 + k, x + 4 - k, y1 - 3 + k)


def write_png(on, path):
    raw = b""
    for row in on:
        raw += b"\x00" + b"".join(b"\xff\xff\xff\xff" if v else b"\x00\x00\x00\x00" for v in row)

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xffffffff)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def main():
    on = blank()
    # three fish in a column, all facing right, a little apart
    fish(on, 17, 6)
    fish(on, 17, 19)
    fish(on, 17, 32)
    # and the arrow coming down beside them, on the left
    arrow(on, 7, 6, 40)
    write_png(on, OUT)
    print("wrote", OUT)
    for row in on:
        print("".join("#" if v else "." for v in row))


if __name__ == "__main__":
    main()
