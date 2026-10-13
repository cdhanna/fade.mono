"""
builds the pictures that the ending of the diver adds to the menus of the fish game.

    py -I make_diver_menu_art.py <the game's Textures folder> <a preview png>

what it writes:
    badge_mask.png                 16 by 16, white. a diving mask: the badge of a game that ended with
                                   the album full, in the table of high scores. it is tinted there.
    ending_album.png               16 by 16. a photo of a fish: the picture that the ending has in the codec.
    Menu/dive_boat_hunched.png     the evening boat, with the diver on the back of it hunched over
    Menu/dive_boat_day_hunched.png the same for the boat of the day
the two boats are made from Menu/dive_boat.png and Menu/dive_boat_day.png, which are not changed.
only the diver from the lap up is drawn again. its colors are taken from the picture that it is in.
"""
import os, struct, sys, zlib

CLEAR = (0, 0, 0, 0)


def read_png(path):
    data = open(path, "rb").read()
    pos = 8
    idat = b""
    while pos < len(data):
        length, kind = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height, depth, ctype, _, _, interlace = struct.unpack(">IIBBBBB", body)
            assert depth == 8 and ctype == 6 and interlace == 0
        elif kind == b"IDAT":
            idat += body
        pos += 12 + length
    raw = zlib.decompress(idat)
    stride = width * 4
    rows = []
    prev = bytearray(stride)
    at = 0
    for _ in range(height):
        ftype = raw[at]
        line = bytearray(raw[at + 1:at + 1 + stride])
        at += 1 + stride
        for i in range(stride):
            a = line[i - 4] if i >= 4 else 0
            b = prev[i]
            c = prev[i - 4] if i >= 4 else 0
            if ftype == 1:
                line[i] = (line[i] + a) & 255
            elif ftype == 2:
                line[i] = (line[i] + b) & 255
            elif ftype == 3:
                line[i] = (line[i] + ((a + b) >> 1)) & 255
            elif ftype == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if (pa <= pb and pa <= pc) else (b if pb <= pc else c))) & 255
        rows.append([tuple(line[x * 4:x * 4 + 4]) for x in range(width)])
        prev = line
    return rows


def write_png(path, rows):
    height = len(rows)
    width = len(rows[0])

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    raw = b"".join(b"\x00" + b"".join(bytes(p) for p in row) for row in rows)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    open(path, "wb").write(png)


MASK = [
    "................",
    "................",
    "................",
    ".AAAAAAAAAAAAAA.",
    "AA............AA",
    "A..AA..........A",
    "A.AA...........A",
    "A.A............A",
    "A..............A",
    "AA.....AA.....AA",
    ".AA...AAAA...AA.",
    "..AAAAA..AAAAA..",
    "................",
    "................",
    "................",
    "................",
]

# a photo paper with a fish on it. P is the paper, E its edge, W the window, O the fish and K its eye.
ALBUM = [
    "................",
    "..PPPPPPPPPPPE..",
    "..PWWWWWWWWWPE..",
    "..PWWWWWWWWWPE..",
    "..PWWWWWWWWWPE..",
    "..PWOWOOOOWWPE..",
    "..PWOOOOOKOWPE..",
    "..PWOWOOOOWWPE..",
    "..PWWWWWWWWWPE..",
    "..PWWWWWWWWWPE..",
    "..PPPPPPPPPPPE..",
    "..PPPPPPPPPPPE..",
    "..PPPPPPPPPPPE..",
    "..EEEEEEEEEEEE..",
    "................",
    "................",
]
ALBUM_COLORS = {
    ".": CLEAR,
    "P": (244, 244, 236, 255),
    "E": (196, 200, 204, 255),
    "W": (36, 66, 108, 255),
    "O": (255, 174, 48, 255),
    "K": (30, 24, 20, 255),
}

# the diver from the lap up, hunched over. the left edge of this is 134 pixels across the picture of
# the boat, and the top of it is 33 pixels down. A is the suit, B the glass of the mask, C the strap
# and the hose, and D the tank. the rows under it, which are the lap and the legs, are left as they are.
HUNCHED_X = 134
HUNCHED_Y = 33
HUNCHED = [
    "......................",  # 33
    "......................",
    "......................",
    "......................",
    "......................",
    "......................",
    "......................",
    "......................",  # 40
    "...........AAAAA......",
    "..........AAAAAAA.....",
    ".........AAAAAAAA.....",
    "....DDDDAAAAAAAAA.....",  # 44
    "....DDDDAAAAAAAAABBB..",
    "...DDDDAAAAAAA.AAABBB.",
    "...DDDDAAAAAAA..AAAA..",
    "...DDDDAAAAAAA........",  # 48
    "..DDDDCCAAAAAAAA......",
    "..DDDDCCAAAAAAAAA.....",
    "..DDDDAAAAAAAAAAAA....",
    "..DDDDAAAAAAAAAAAAA...",  # 52
    "..DDDDAAAAAAA..AAAAA..",
]


def hunched(boat):
    out = [list(line) for line in boat]
    # the colors of the diver, from where they are in the picture
    colors = {
        "A": boat[36][140],
        "B": boat[38][145],
        "C": boat[40][135],
        "D": boat[44][135],
    }
    for name, color in colors.items():
        assert color[3] == 255, (name, color)
    for dy, line in enumerate(HUNCHED):
        assert len(line) == 22, (dy, line)
        for dx, ch in enumerate(line):
            out[HUNCHED_Y + dy][HUNCHED_X + dx] = CLEAR if ch == "." else colors[ch]
    return out


def main():
    folder, preview = sys.argv[1], sys.argv[2]

    white = (255, 255, 255, 255)
    write_png(os.path.join(folder, "badge_mask.png"), [[white if ch == "A" else CLEAR for ch in line] for line in MASK])

    for line in ALBUM:
        assert len(line) == 16, line
    write_png(os.path.join(folder, "ending_album.png"), [[ALBUM_COLORS[ch] for ch in line] for line in ALBUM])

    shown = []
    for name in ("dive_boat", "dive_boat_day"):
        boat = read_png(os.path.join(folder, "Menu", name + ".png"))
        new = hunched(boat)
        write_png(os.path.join(folder, "Menu", name + "_hunched.png"), new)
        shown.append(new)

    # a big copy of the back of both boats, and of the badge, to look at
    scale = 6
    sky = (120, 150, 200, 255)
    big = []
    for boat in shown:
        for y in range(28, 72):
            wide = []
            for x in range(112, 168):
                p = boat[y][x]
                wide += [p if p[3] else sky] * scale
            big += [wide] * scale
    for line in MASK:
        wide = []
        for ch in line:
            wide += [white if ch == "A" else (40, 60, 100, 255)] * scale
        wide += [sky] * (len(big[0]) - len(wide))
        big += [wide] * scale
    write_png(preview, big)


main()
