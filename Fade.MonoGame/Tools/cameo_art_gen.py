"""Draws the pictures of the cameos: the things that swim through when one of the secret words is
tapped out on the radio. see CAMEOS in fish_headers_cameo.

    py Tools/cameo_art_gen.py                 from the Fade.MonoGame folder

It writes these into Assets/Fish/Textures/Cameos. every one of them faces right, because that is
the way that they all go.

    Freddi.png      a yellow fish with pink fins, for FREDDI
    Sub.png         a submarine, for U571
    Ship.png        a black ship with black sails, for PEARL
    Dopefish.png    a green fish with two big teeth, for DOPEFISH
    Fin.png         the fin of a shark, for JAWS
    Fork.png        a fork, for ARIEL
    Mermaid.png     and the mermaid that comes after it

the rest of the cameos use pictures that the game has already, and MYLEG has none: it is only heard.

each picture is written out below as rows of letters, one letter for a pixel, and PALETTE says
which color a letter is. a dot is nothing. the dark outline is not written out: it is put around
whatever was drawn. this needs nothing but python.
"""
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Fish", "Textures", "Cameos")

CLEAR = (0, 0, 0, 0)
OUTLINE = (18, 16, 28, 255)

PALETTE = {
    "K": OUTLINE,
    "W": (250, 250, 250, 255),   # white
    "Y": (255, 214, 64, 255),    # yellow
    "y": (232, 170, 40, 255),    # the shade of it
    "P": (232, 96, 168, 255),    # pink
    "p": (186, 62, 132, 255),
    "R": (214, 48, 40, 255),     # red
    "r": (160, 30, 34, 255),
    "S": (246, 200, 160, 255),   # skin
    "s": (214, 158, 122, 255),
    "G": (66, 176, 96, 255),     # green
    "g": (40, 132, 78, 255),
    "L": (150, 222, 110, 255),   # a light green
    "V": (140, 92, 196, 255),    # violet
    "B": (128, 196, 236, 255),   # a light blue
    "b": (84, 150, 204, 255),
    "A": (150, 158, 172, 255),   # gray
    "a": (104, 112, 130, 255),
    "D": (66, 72, 90, 255),      # a dark gray
    "H": (52, 46, 58, 255),      # the black of the ship
    "h": (84, 78, 94, 255),      # and where the light catches it
    "O": (255, 236, 150, 255),   # the light of a window
    "T": (236, 232, 204, 255),   # bone
}

FREDDI = [
    "................",
    "........PP......",
    ".......PPPp.....",
    ".PP...PPPPp.....",
    ".PPp.YYYYYYY....",
    ".PPPYYYYYYWWY...",
    "..PPYYYYYYWKWY..",
    "..PPYYYYYYWKWY..",
    "..PPYYYYYYYYYY..",
    ".PPPYYyyyyyYRR..",
    ".PPp.yyyyyyy....",
    ".PP...ypPPy.....",
    ".......pPp......",
    "................",
    "................",
    "................",
]

FIN = [
    "................",
    "................",
    ".....A..........",
    ".....AA.........",
    ".....AAA........",
    ".....AAAA.......",
    "......AAAA......",
    "......AaAAA.....",
    "......AaaAAA....",
    "......AaaaAAA...",
    ".....AAaaaAAAA..",
    ".....AAaaaaAAA..",
    "....AAAaaaaAAAA.",
    "....AAAAaaaAAAA.",
    "...AAAAAAaaAAAA.",
    "...AAAAAAAAAAAA.",
]

FORK = [
    "................",
    ".....W.W.W......",
    ".....W.A.A......",
    ".....W.A.A......",
    ".....W.A.A......",
    ".....WAAAA......",
    ".....WAAAa......",
    "......AAa.......",
    ".......A........",
    ".......A........",
    ".......A........",
    ".......A........",
    "......WAa.......",
    "......WAa.......",
    ".......a........",
    "................",
]

MERMAID = [
    "................................",
    "....................RRRR........",
    "..................RRRRRRRR......",
    "................RRRRRRSSSS......",
    ".GG............RRRrRRSSSSSS.....",
    ".GGG...........RRrRRRSSSKSS.....",
    ".GGGG...GGGGGG.RrRRRRSSSSSS.....",
    "..GGGGGGGGLGGGGVVSSRRRSSSS......",
    "...GGGGGLGGGLGGVVVSSSSSSs.......",
    "..GGGGGGGGGGGGGVVSSSSSSSSSSSS...",
    ".GGGG...ggggggg..sss............",
    ".GGG............................",
    ".GG.............................",
    "................................",
    "................................",
    "................................",
]


def grid(w, h):
    return [[CLEAR] * w for _ in range(h)]


def from_rows(rows):
    return [[PALETTE[c] if c != "." else CLEAR for c in row] for row in rows]


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


def box(pic, x0, y0, x1, y1, color):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= y < len(pic) and 0 <= x < len(pic[0]):
                pic[y][x] = color


def sub():
    """A long hull, a tower with a periscope, a gun on the deck, and a propeller at the back."""
    w, h = 48, 20
    pic = grid(w, h)
    for y in range(h):
        for x in range(w):
            dx = (x - 24.5) / 20.5
            dy = (y - 12.5) / 4.6
            if dx * dx + dy * dy <= 1:
                color = PALETTE["A"]
                if y >= 14:
                    color = PALETTE["a"]
                if y == 10:
                    color = PALETTE["B"]
                pic[y][x] = color
    box(pic, 8, 8, 40, 8, PALETTE["a"])        # the deck
    box(pic, 21, 4, 28, 8, PALETTE["A"])       # the tower
    box(pic, 21, 7, 28, 8, PALETTE["a"])
    box(pic, 25, 1, 25, 3, PALETTE["D"])       # the periscope
    box(pic, 25, 1, 27, 1, PALETTE["D"])
    box(pic, 32, 6, 36, 6, PALETTE["D"])       # the gun
    box(pic, 32, 7, 33, 7, PALETTE["D"])
    box(pic, 1, 9, 2, 16, PALETTE["D"])        # the propeller
    box(pic, 3, 12, 4, 13, PALETTE["D"])
    for x in (14, 20, 26, 32):                 # the windows
        pic[12][x] = PALETTE["O"]
    return outline(pic)


def ship():
    """A black ship from the side: a hull that is high at the back, three masts, and black sails."""
    w, h = 40, 34
    pic = grid(w, h)
    for y in range(25, 32):                    # the hull, narrower lower down
        lean = y - 25
        box(pic, 5 + lean, y, 31 - lean // 2, y, PALETTE["H"])
    box(pic, 5, 25, 31, 25, PALETTE["h"])      # the rail
    box(pic, 3, 21, 9, 24, PALETTE["H"])       # the back, which is higher
    box(pic, 3, 21, 9, 21, PALETTE["h"])
    for n in range(6):                         # the pole at the front
        pic[24 - n // 2][31 + n] = PALETTE["h"]
    for x in (12, 17, 22, 27):                 # the windows of the guns
        pic[27][x] = PALETTE["O"]
    for x, top in ((11, 6), (19, 2), (27, 8)):  # the masts
        box(pic, x, top, x, 24, PALETTE["h"])
    sails = ((7, 8, 15, 13), (7, 15, 15, 21), (14, 4, 24, 10), (14, 12, 24, 20), (24, 10, 31, 15), (24, 17, 31, 22))
    for x0, y0, x1, y1 in sails:
        box(pic, x0, y0, x1, y1, PALETTE["D"])
        box(pic, x0, y0, x1, y0, PALETTE["a"])
        # the bottom of a sail is torn
        for x in range(x0, x1 + 1, 3):
            pic[y1][x] = CLEAR
    box(pic, 20, 1, 23, 2, PALETTE["H"])       # the flag, with a bone white spot on it
    pic[1][21] = PALETTE["T"]
    return outline(pic)


def dopefish():
    """A round green fish with eyes that bulge, each looking its own way, and two big teeth."""
    w, h = 24, 24
    pic = grid(w, h)
    for y in range(h):
        for x in range(w):
            dx = (x - 13.5) / 8.2
            dy = (y - 13.0) / 7.6
            if dx * dx + dy * dy <= 1:
                color = PALETTE["G"]
                if y >= 16:
                    color = PALETTE["L"]
                if (x + y * 2) % 7 == 0 and y < 15 and x < 12:
                    color = PALETTE["g"]
                pic[y][x] = color
    for n in range(5):                         # the tail
        box(pic, 1 + n, 9 + n, 1 + n, 17 - n, PALETTE["g"])
    box(pic, 9, 3, 13, 5, PALETTE["g"])        # the fin on its back
    box(pic, 10, 2, 12, 2, PALETTE["g"])
    box(pic, 10, 20, 13, 21, PALETTE["g"])     # and underneath
    for ex, px, py in ((12, 13, 8), (18, 18, 9)):  # the eyes
        box(pic, ex - 1, 6, ex + 2, 10, PALETTE["W"])
        box(pic, ex, 5, ex + 1, 11, PALETTE["W"])
        pic[py][px] = OUTLINE
    box(pic, 12, 14, 21, 14, OUTLINE)          # the mouth
    box(pic, 15, 15, 16, 18, PALETTE["W"])     # the teeth
    box(pic, 18, 15, 19, 18, PALETTE["W"])
    box(pic, 17, 15, 17, 18, OUTLINE)
    box(pic, 14, 15, 14, 18, OUTLINE)
    box(pic, 20, 15, 20, 18, OUTLINE)
    box(pic, 15, 19, 19, 19, OUTLINE)
    return outline(pic)


def write_png(path, rows):
    h = len(rows)
    w = len(rows[0])
    raw = b"".join(b"\x00" + b"".join(bytes(p) for p in row) for row in rows)

    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body) & 0xffffffff)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


PICTURES = (
    ("Freddi", outline(from_rows(FREDDI))),
    ("Sub", sub()),
    ("Ship", ship()),
    ("Dopefish", dopefish()),
    ("Fin", outline(from_rows(FIN))),
    ("Fork", outline(from_rows(FORK))),
    ("Mermaid", outline(from_rows(MERMAID))),
)

os.makedirs(OUT, exist_ok=True)
for name, pic in PICTURES:
    assert all(len(row) == len(pic[0]) for row in pic), name
    write_png(os.path.join(OUT, name + ".png"), pic)
    print(name + ".png", len(pic[0]), "x", len(pic))
