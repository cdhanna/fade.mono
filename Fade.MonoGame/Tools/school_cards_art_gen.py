"""Draws the pictures of the three cards of the schools that came after "fall in line":

    flipper       two fish facing apart, with an arrow that turns around between them
    high_stakes   a big "x2"
    tolerance     three fish: one stays, and two swim off, with lines behind them

    py Tools/school_cards_art_gen.py

48 by 48, white on nothing, with hard edges, like the rest of Assets/Fish/Textures/Cards. See THREE MORE
CARDS in fish_headers_school. The drawing is shared with Tools/fall_in_line_art_gen.py.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fall_in_line_art_gen import SIZE, blank, dot, rect, fish, write_png  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
CARDS = os.path.join(HERE, "..", "Assets", "Fish", "Textures", "Cards")


def mirrored(on):
    return [list(reversed(row)) for row in on]


def fish_left(on, x, y):
    """a little fish facing left, 14 wide, its box starting at x, y"""
    piece = blank()
    fish(piece, SIZE - 14 - x, y)
    piece = mirrored(piece)
    for yy in range(SIZE):
        for xx in range(SIZE):
            if piece[yy][xx]:
                on[yy][xx] = 1
    # the eye is a hole, which the copy above cannot make
    on[y + 2][x + 4] = 0


def flipper():
    on = blank()
    fish(on, 6, 8)        # the top one faces right
    fish_left(on, 6, 31)  # and the bottom one faces left
    # a turning arrow on the right, from the top fish down around to the bottom one
    for y in range(12, 36):
        rect(on, 36, y, 37, y)
    rect(on, 24, 11, 37, 12)
    rect(on, 27, 35, 37, 36)
    # its head, pointing left at the bottom fish
    for k in range(4):
        rect(on, 23 + k, 35 - k, 23 + k, 36 + k)
    return on


GLYPH_X = [
    "##....##",
    "##....##",
    ".##..##.",
    "..####..",
    "..####..",
    ".##..##.",
    "##....##",
    "##....##",
]
GLYPH_2 = [
    ".######.",
    "##....##",
    "......##",
    "....###.",
    "..###...",
    ".##.....",
    "##......",
    "########",
]


def glyph(on, rows, x, y, scale):
    for gy, row in enumerate(rows):
        for gx, c in enumerate(row):
            if c == "#":
                rect(on, x + gx * scale, y + gy * scale, x + gx * scale + scale - 1, y + gy * scale + scale - 1)


def high_stakes():
    on = blank()
    glyph(on, GLYPH_X, 4, 20, 2)    # a small x
    glyph(on, GLYPH_2, 22, 8, 3)    # and a big 2
    return on


def tolerance():
    on = blank()
    fish(on, 4, 20)        # the one that stays, near the middle
    fish(on, 30, 6)        # and the two that swim off, to the right
    fish(on, 30, 34)
    for y in (8, 11, 36, 39):
        rect(on, 20, y, 23, y)
        rect(on, 25, y, 27, y)
    return on


def main():
    for name, draw in (("flipper", flipper), ("high_stakes", high_stakes), ("tolerance", tolerance)):
        on = draw()
        path = os.path.join(CARDS, name + ".png")
        write_png(on, path)
        print("wrote", path)
        for row in on:
            print("".join("#" if v else "." for v in row))


if __name__ == "__main__":
    main()
