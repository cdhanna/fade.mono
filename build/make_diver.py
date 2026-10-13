"""
builds the pictures of the diver for the fish game, from the pixel art diver of dkproductions
(dkproductions.itch.io/pixel-art-diver).

    py -I make_diver.py <PixelArt_Diver_SpriteSheet_v1.png> <the game's Textures folder> <a preview png>

what it writes:
    Diver.png       16 by 16, and empty. the diver's cell on the board has this picture, and the
                    diver itself is drawn on top of the cell, from the sheet.
    DiverSheet.png  3 rows of 12 frames, 32 by 32 each.
                    row 0  swimming, with the tank (row 4 of the pack)
                    row 1  the same, holding a camera out in front. this row is new art.
                    row 2  upright, with the tank (row 3 of the pack). the diver goes up like this.
                    every frame has a dark outline, one pixel wide, like the fish of the game have.
    DiverFocus.png  16 by 16. the corners of a viewfinder.
    DiverPhoto.png  12 by 14. a little photo paper, with a dark window for the fish.
    codec_diver.png 16 by 16. the diver from the waist up, which is its picture in the codec.
"""
import os, struct, sys, zlib

FRAME = 32
SKIN = (255, 195, 159, 255)
HAND = (222, 170, 139, 255)
SUIT_DARK = (39, 49, 103, 255)
CAM_LIGHT = (184, 184, 184, 255)
CAM_DARK = (146, 146, 146, 255)
CAM_LENS = (22, 26, 44, 255)
CLEAR = (0, 0, 0, 0)
OUTLINE = (14, 12, 22, 255)


def read_png(path):
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n"
    pos = 8
    idat = b""
    width = height = 0
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
                pred = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pred) & 255
        rows.append([tuple(line[x * 4:x * 4 + 4]) for x in range(width)])
        prev = line
    return width, height, rows


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


def blank(width, height):
    return [[CLEAR] * width for _ in range(height)]


def frame_of(sheet, row, col):
    return [[sheet[row * FRAME + y][col * FRAME + x] for x in range(FRAME)] for y in range(FRAME)]


def with_camera(frame):
    """the swimming diver, holding a camera out in front of its face with one arm"""
    out = [list(line) for line in frame]

    # the face is the skin that is furthest to the right
    face_x = face_y = -1
    for y in range(FRAME):
        for x in range(FRAME):
            if out[y][x] == SKIN and x > face_x:
                face_x, face_y = x, y
    assert face_x >= 0, "no face in this frame"

    # the arm reaches forward under the chin, from the chest to the camera
    arm_y = face_y + 2
    for x in range(face_x - 5, face_x + 1):
        if out[arm_y][x][3] == 0:
            out[arm_y][x] = SUIT_DARK
    out[arm_y][face_x + 1] = HAND

    # the camera: three wide and three tall, with a lens, and a viewfinder on top
    cam_x = face_x + 2
    cam_y = face_y
    assert cam_x + 2 < FRAME
    for dy in range(3):
        for dx in range(3):
            out[cam_y + dy][cam_x + dx] = CAM_LIGHT if dy < 2 else CAM_DARK
    out[cam_y + 1][cam_x + 1] = CAM_LENS
    out[cam_y - 1][cam_x] = CAM_DARK
    return out


def outlined(frame):
    """the frame, with a dark pixel on every empty pixel that touches the diver"""
    out = [list(line) for line in frame]
    for y in range(FRAME):
        for x in range(FRAME):
            if frame[y][x][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < FRAME and 0 <= ny < FRAME and frame[ny][nx][3]:
                    out[y][x] = OUTLINE
                    break
    return out


def paste(target, frame, row, col):
    for y in range(FRAME):
        for x in range(FRAME):
            target[row * FRAME + y][col * FRAME + x] = frame[y][x]


def main():
    source, folder, preview = sys.argv[1], sys.argv[2], sys.argv[3]
    width, height, sheet = read_png(source)
    assert (width, height) == (384, 192), (width, height)

    out = blank(12 * FRAME, 3 * FRAME)
    for col in range(12):
        swim = frame_of(sheet, 4, col)
        paste(out, outlined(swim), 0, col)
        paste(out, outlined(with_camera(swim)), 1, col)
        paste(out, outlined(frame_of(sheet, 3, col)), 2, col)
    write_png(os.path.join(folder, "DiverSheet.png"), out)

    write_png(os.path.join(folder, "Diver.png"), blank(16, 16))

    # the picture of the diver in the codec: the top of the upright diver, with its outline
    upright = outlined(frame_of(sheet, 3, 0))
    write_png(os.path.join(folder, "codec_diver.png"), [line[8:24] for line in upright[5:21]])

    # the corners of a viewfinder
    white = (255, 255, 255, 255)
    focus = blank(16, 16)
    for n in range(5):
        for (x, y) in ((n, 0), (0, n), (15 - n, 0), (15, n), (n, 15), (0, 15 - n), (15 - n, 15), (15, 15 - n)):
            focus[y][x] = white
    write_png(os.path.join(folder, "DiverFocus.png"), focus)

    # a photo paper: white, with a window, and more white under the window than over it
    paper = (244, 244, 236, 255)
    edge = (196, 200, 204, 255)
    window = (36, 66, 108, 255)
    photo = [[paper] * 12 for _ in range(14)]
    for x in range(12):
        photo[13][x] = edge
    for y in range(14):
        photo[y][11] = edge
    for y in range(1, 10):
        for x in range(1, 11):
            photo[y][x] = window
    write_png(os.path.join(folder, "DiverPhoto.png"), photo)

    # a big copy of the sheet on the color of the water, to look at
    scale = 4
    water = (28, 58, 94, 255)
    big = []
    for line in out:
        wide = []
        for p in line:
            wide += [p if p[3] else water] * scale
        big += [wide] * scale
    write_png(preview, big)


main()
