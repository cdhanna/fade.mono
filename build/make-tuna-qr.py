"""
Makes the QR codes that the fish of the fish game line up into when TUNA is typed. See TUNA in fish_headers.fbasic.

    py build/make-tuna-qr.py https://coral.org https://coralrestoration.org

It takes one link or several, up to eight. The game picks one of them at random every time. It writes
Fade.MonoGame/fish_tuna_code.fbasic, which is part of the game: one number for every row of every
code, with a bit for every module, set for a dark one. The game makes every module out of a fish.

A code is always version 2 (25 by 25 modules), in byte mode, at the lowest level of error correction,
which holds 32 characters. A longer link has to go through a short one that redirects to it.
Nothing but Python is needed.
"""
import os
import sys

VERSION = 2
SIZE = 17 + 4 * VERSION
DATA_CODEWORDS = 34  # version 2, level L: one block of 34 data codewords and 10 for error correction
EC_CODEWORDS = 10
LEVEL_BITS = 1  # level L, as the format information spells it


def gf_multiply(a, b):
    result = 0
    while b:
        if b & 1:
            result ^= a
        a <<= 1
        if a & 0x100:
            a ^= 0x11D
        b >>= 1
    return result


def error_correction(data, count):
    # the generator polynomial, highest power first, without its leading 1
    generator = [0] * (count - 1) + [1]
    root = 1
    for _ in range(count):
        for i in range(count):
            generator[i] = gf_multiply(generator[i], root)
            if i + 1 < count:
                generator[i] ^= generator[i + 1]
        root = gf_multiply(root, 2)

    remainder = [0] * count
    for byte in data:
        factor = byte ^ remainder.pop(0)
        remainder.append(0)
        for i in range(count):
            remainder[i] ^= gf_multiply(generator[i], factor)
    return remainder


def codewords(text):
    data = text.encode("utf-8")
    capacity = DATA_CODEWORDS - 2  # the mode and the count take 12 bits
    if len(data) > capacity:
        raise SystemExit("%r is %d bytes, and this code only holds %d" % (text, len(data), capacity))

    bits = [0, 1, 0, 0]  # byte mode
    bits += [(len(data) >> i) & 1 for i in range(7, -1, -1)]
    for byte in data:
        bits += [(byte >> i) & 1 for i in range(7, -1, -1)]
    bits += [0] * min(4, DATA_CODEWORDS * 8 - len(bits))
    bits += [0] * (-len(bits) % 8)

    words = [int("".join(map(str, bits[i:i + 8])), 2) for i in range(0, len(bits), 8)]
    pad = 0xEC
    while len(words) < DATA_CODEWORDS:
        words.append(pad)
        pad ^= 0xEC ^ 0x11
    return words + error_correction(words, EC_CODEWORDS)


def blank():
    modules = [[0] * SIZE for _ in range(SIZE)]
    reserved = [[False] * SIZE for _ in range(SIZE)]

    def put(x, y, dark):
        if 0 <= x < SIZE and 0 <= y < SIZE:
            modules[y][x] = 1 if dark else 0
            reserved[y][x] = True

    # the timing lines
    for i in range(SIZE):
        put(6, i, i % 2 == 0)
        put(i, 6, i % 2 == 0)

    # the three finders, with the light line around each
    for cx, cy in ((3, 3), (SIZE - 4, 3), (3, SIZE - 4)):
        for dy in range(-4, 5):
            for dx in range(-4, 5):
                ring = max(abs(dx), abs(dy))
                put(cx + dx, cy + dy, ring != 2 and ring != 4)

    # the one alignment square that version 2 has
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            put(SIZE - 7 + dx, SIZE - 7 + dy, max(abs(dx), abs(dy)) != 1)

    # room for the format information, which is written once the mask is known
    for i in range(9):
        put(8, i, modules[i][8])
        put(i, 8, modules[8][i])
    for i in range(8):
        put(SIZE - 1 - i, 8, 0)
        put(8, SIZE - 1 - i, 0)
    put(8, SIZE - 8, 1)
    return modules, reserved


def write_format(modules, mask):
    data = (LEVEL_BITS << 3) | mask
    remainder = data
    for _ in range(10):
        remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537)
    bits = ((data << 10) | remainder) ^ 0x5412

    def bit(i):
        return (bits >> i) & 1

    for i in range(6):
        modules[i][8] = bit(i)
    modules[7][8] = bit(6)
    modules[8][8] = bit(7)
    modules[8][7] = bit(8)
    for i in range(9, 15):
        modules[8][14 - i] = bit(i)
    for i in range(8):
        modules[8][SIZE - 1 - i] = bit(i)
    for i in range(8, 15):
        modules[SIZE - 15 + i][8] = bit(i)
    modules[SIZE - 8][8] = 1


MASKS = (
    lambda x, y: (x + y) % 2 == 0,
    lambda x, y: y % 2 == 0,
    lambda x, y: x % 3 == 0,
    lambda x, y: (x + y) % 3 == 0,
    lambda x, y: (x // 3 + y // 2) % 2 == 0,
    lambda x, y: x * y % 2 + x * y % 3 == 0,
    lambda x, y: (x * y % 2 + x * y % 3) % 2 == 0,
    lambda x, y: ((x + y) % 2 + x * y % 3) % 2 == 0,
)


def penalty(modules):
    score = 0
    lines = [row for row in modules] + [[modules[y][x] for y in range(SIZE)] for x in range(SIZE)]
    for line in lines:
        # runs of five or more of the same color
        run = 1
        for i in range(1, SIZE):
            if line[i] == line[i - 1]:
                run += 1
            else:
                if run >= 5:
                    score += run - 2
                run = 1
        if run >= 5:
            score += run - 2

        # anything that looks like a finder
        text = "".join(map(str, line))
        for pattern in ("10111010000", "00001011101"):
            start = text.find(pattern)
            while start >= 0:
                score += 40
                start = text.find(pattern, start + 1)

    # blocks of two by two
    for y in range(SIZE - 1):
        for x in range(SIZE - 1):
            if modules[y][x] == modules[y][x + 1] == modules[y + 1][x] == modules[y + 1][x + 1]:
                score += 3

    # too much of one color
    dark = sum(map(sum, modules))
    score += 10 * (abs(dark * 20 - SIZE * SIZE * 10) // (SIZE * SIZE))
    return score


def make(text):
    words = codewords(text)
    bits = []
    for word in words:
        bits += [(word >> i) & 1 for i in range(7, -1, -1)]

    base, reserved = blank()

    # the data goes up and down the code in columns two wide, from the right, around everything else
    index = 0
    for right in range(SIZE - 1, 0, -2):
        if right <= 6:
            right -= 1
        for vertical in range(SIZE):
            for j in range(2):
                x = right - j
                upward = ((right + 1) & 2) == 0
                y = SIZE - 1 - vertical if upward else vertical
                if not reserved[y][x]:
                    if index < len(bits):
                        base[y][x] = bits[index]
                    index += 1

    best = None
    for mask, test in enumerate(MASKS):
        modules = [row[:] for row in base]
        for y in range(SIZE):
            for x in range(SIZE):
                if not reserved[y][x] and test(x, y):
                    modules[y][x] ^= 1
        write_format(modules, mask)
        score = penalty(modules)
        if best is None or score < best[0]:
            best = (score, modules)
    return best[1]


MOST = 8  # how many codes the game has room for. see TUNA_CODE in fish_headers.fbasic.


def write_fbasic(path, texts, codes):
    lines = [
        "` this file is written by build/make-tuna-qr.py. do not change it by hand: run the script again.",
        "` these are the QR codes that the fish line up into. see TUNA in fish_headers.",
        "",
        "` one number for every row of a code, from the top, and one code after another. the first module",
        "` of a row is the lowest bit, and a bit that is set is a dark module.",
        "function tunaLoadCode()",
        "    TUNA_CODE_COUNT = %d" % len(codes),
    ]
    for index, (text, modules) in enumerate(zip(texts, codes)):
        lines.append("")
        lines.append("    ` " + text)
        for y, row in enumerate(modules):
            lines.append("    TUNA_CODE(%d) = %d" % (index * SIZE + y, sum(module << x for x, module in enumerate(row))))
    lines.append("endfunction")
    with open(path, "w", newline="\n") as out:
        out.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    links = sys.argv[1:]
    if not links:
        raise SystemExit(__doc__)
    if len(links) > MOST:
        raise SystemExit("the game only has room for %d codes" % MOST)
    here = os.path.dirname(os.path.abspath(__file__))
    target = os.path.join(here, "..", "Fade.MonoGame", "fish_tuna_code.fbasic")
    write_fbasic(target, links, [make(link) for link in links])
    print("wrote %d code(s) to %s" % (len(links), os.path.normpath(target)))
