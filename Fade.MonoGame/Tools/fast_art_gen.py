"""Draws the pictures of the three cards that only the fast gives out, and the badges of the modifiers.

    py Tools/fast_art_gen.py

The card pictures are 48 by 48, white on nothing, with hard edges, like the rest of
Assets/Fish/Textures/Cards. The badges are 12 by 12, the same way. See THE FAST in fish_headers_fast.
There is no PIL here, so the PNGs are written by hand.
"""
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
CARDS = os.path.join(HERE, "..", "Assets", "Fish", "Textures", "Cards")
BADGES = os.path.join(HERE, "..", "Assets", "Fish", "Textures")


class Canvas:
    def __init__(self, size):
        self.size = size
        self.on = [[0] * size for _ in range(size)]

    def dot(self, x, y, value=1):
        x = int(round(x))
        y = int(round(y))
        if 0 <= x < self.size and 0 <= y < self.size:
            self.on[y][x] = value

    def rect(self, x0, y0, x1, y1, value=1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, value)

    def frame(self, x0, y0, x1, y1, thick):
        self.rect(x0, y0, x1, y0 + thick - 1)
        self.rect(x0, y1 - thick + 1, x1, y1)
        self.rect(x0, y0, x0 + thick - 1, y1)
        self.rect(x1 - thick + 1, y0, x1, y1)

    def line(self, x0, y0, x1, y1, thick=1):
        steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for n in range(steps + 1):
            t = n / steps
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            for dy in range(thick):
                for dx in range(thick):
                    self.dot(x + dx, y + dy)

    def save(self, path):
        raw = bytearray()
        for row in self.on:
            raw.append(0)
            for value in row:
                raw += b"\xff\xff\xff\xff" if value else b"\x00\x00\x00\x00"

        def chunk(kind, data):
            body = kind + data
            return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

        png = b"\x89PNG\r\n\x1a\n"
        png += chunk(b"IHDR", struct.pack(">IIBBBBB", self.size, self.size, 8, 6, 0, 0, 0))
        png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
        png += chunk(b"IEND", b"")
        with open(path, "wb") as out:
            out.write(png)
        print("wrote", os.path.normpath(path))


def rotate_card():
    """a box of fish, with an arrow going around two of its corners"""
    c = Canvas(48)
    for gy in range(3):
        for gx in range(3):
            if gx == 1 and gy == 1:
                continue
            c.rect(11 + gx * 10, 11 + gy * 10, 16 + gx * 10, 16 + gy * 10)

    # the arrow over the top, heading right and then down
    c.rect(8, 4, 38, 6)
    c.rect(39, 4, 41, 30)
    for n in range(5):
        c.rect(40 - n, 31 + (4 - n), 40 + n, 31 + (4 - n))

    # and the one under the bottom, heading left and then up
    c.rect(9, 41, 39, 43)
    c.rect(6, 17, 8, 43)
    for n in range(5):
        c.rect(7 - n, 12 + n, 7 + n, 12 + n)
    return c


def see_all_card():
    """every card there is, laid out in rows"""
    c = Canvas(48)
    for gy in range(3):
        for gx in range(4):
            x = 5 + gx * 10
            y = 5 + gy * 13
            c.frame(x, y, x + 7, y + 10, 1)
            c.rect(x + 2, y + 2, x + 5, y + 4)
    return c


def oath_card():
    """a hook with nothing on it"""
    c = Canvas(48)
    c.frame(21, 3, 27, 9, 2)          # the eye of the hook
    c.rect(23, 9, 25, 32)             # the shank
    c.line(24, 32, 22, 38, 3)         # the bend
    c.line(22, 38, 16, 40, 3)
    c.line(16, 40, 11, 36, 3)
    c.line(11, 36, 10, 28, 3)         # and up to the point
    c.line(10, 28, 15, 31, 2)         # the barb
    return c


def badge_oath():
    c = Canvas(12)
    c.rect(5, 0, 6, 7)
    c.line(5, 7, 3, 10, 2)
    c.line(3, 10, 1, 8, 2)
    c.dot(1, 6)
    c.dot(1, 7)
    c.dot(4, 0)
    c.dot(7, 0)
    return c


def badge_coral():
    c = Canvas(12)
    c.rect(5, 4, 6, 11)
    c.rect(1, 2, 2, 7)
    c.rect(1, 7, 5, 8)
    c.rect(9, 1, 10, 5)
    c.rect(6, 5, 10, 6)
    c.rect(5, 0, 6, 3)
    return c


def badge_night():
    c = Canvas(12)
    for y in range(12):
        for x in range(12):
            far = (x - 5.5) ** 2 + (y - 5.5) ** 2
            bite = (x - 8.0) ** 2 + (y - 4.0) ** 2
            if far <= 30 and bite > 20:
                c.dot(x, y)
    return c


def badge_beat():
    c = Canvas(12)
    c.rect(4, 1, 5, 9)
    c.rect(4, 1, 10, 2)
    c.rect(9, 1, 10, 7)
    c.rect(1, 8, 4, 10)
    c.rect(6, 6, 9, 8)
    return c


if __name__ == "__main__":
    rotate_card().save(os.path.join(CARDS, "rotate.png"))
    see_all_card().save(os.path.join(CARDS, "see_all.png"))
    oath_card().save(os.path.join(CARDS, "oceanic_oath.png"))
    badge_oath().save(os.path.join(BADGES, "BadgeOath.png"))
    badge_coral().save(os.path.join(BADGES, "BadgeEverCoral.png"))
    badge_night().save(os.path.join(BADGES, "BadgeNight.png"))
    badge_beat().save(os.path.join(BADGES, "BadgeBeat.png"))
