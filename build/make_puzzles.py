"""The pieces that the boards of the long visit are built with. build/make_puzzle_boards.py puts them together.

A board is built BACKWARDS from an empty sea: it starts with the last move, and every step puts one more group of fish
in, and then undoes the swaps that would pop them. The steps are not all alike, or every board has the same answer. A
board has a THEME that says which kinds of step it leans on:

    gather  big groups. a kind with 4 or 5 fish has to go all at once, so popping 3 of them strands the rest
    chain   a group that only forms when the group in the way of it pops and its fish fall
    carry   a fish that is several swaps from its group, which has to be walked there
    tower   a narrow board, where the columns are tall and the order they come down in matters
    mix     some of everything
"""

import pathlib

W = 9
H = 7
MIN_GROUP = 3
OUT = pathlib.Path(__file__).resolve().parent.parent / "Fade.MonoGame" / "fish_puzzles.fbasic"

# what each theme leans on: big shapes, chains, how far a fish may have to be carried, how wide the board may get
THEMES = {
    "mix": dict(big=.35, chain=.3, carry=2, wide=9),
    "gather": dict(big=.9, chain=.1, carry=1, wide=9),
    "chain": dict(big=.2, chain=.75, carry=1, wide=9),
    "carry": dict(big=.3, chain=.15, carry=4, wide=9),
    "tower": dict(big=.3, chain=.4, carry=2, wide=4),
}
CYCLE = ["gather", "chain", "carry", "tower", "mix"]

def groups(g):
    """Every group that is big enough to pop, as lists of cells."""
    seen = set()
    found = []
    for i, v in enumerate(g):
        if v == 0 or i in seen:
            continue
        stack = [i]
        seen.add(i)
        cells = []
        while stack:
            c = stack.pop()
            cells.append(c)
            x, y = c % W, c // W
            for n in ((c - 1) if x > 0 else -1, (c + 1) if x < W - 1 else -1,
                      (c - W) if y > 0 else -1, (c + W) if y < H - 1 else -1):
                if n >= 0 and n not in seen and g[n] == v:
                    seen.add(n)
                    stack.append(n)
        if len(cells) >= MIN_GROUP:
            found.append(cells)
    return found


def fall(g):
    for x in range(W):
        col = [g[x + y * W] for y in range(H) if g[x + y * W]]
        col = [0] * (H - len(col)) + col
        for y in range(H):
            g[x + y * W] = col[y]


def settle(g):
    """Pops and falls until nothing pops. This is one look of handle_combos after another."""
    while True:
        found = groups(g)
        if not found:
            return
        for cells in found:
            for c in cells:
                g[c] = 0
        fall(g)


def can_swap(g, a, b):
    if g[a] == 0 or g[b] == 0 or g[a] == g[b]:
        return False
    ax, ay, bx, by = a % W, a // W, b % W, b // W
    return abs(ax - bx) + abs(ay - by) == 1


def play(g, a, b):
    n = list(g)
    n[a], n[b] = n[b], n[a]
    settle(n)
    return n


def neighbours(c):
    x, y = c % W, c // W
    if x < W - 1:
        yield c + 1
    if y < H - 1:
        yield c + W


def all_swaps(g):
    for a in range(W * H):
        for b in neighbours(a):
            if can_swap(g, a, b):
                yield a, b


def heights(g):
    return [sum(1 for y in range(H) if g[x + y * W]) for x in range(W)]


def put_in(g, col, row_from_floor, kind):
    """A fish goes into a column at this height, and whatever is above it goes up by one."""
    fish = [g[col + y * W] for y in range(H - 1, -1, -1) if g[col + y * W]]
    if len(fish) >= H or row_from_floor > len(fish):
        return False
    fish.insert(row_from_floor, kind)
    for i, y in enumerate(range(H - 1, -1, -1)):
        g[col + y * W] = fish[i] if i < len(fish) else 0
    return True


def last_board(rng, kinds):
    """The last move needs something to swap with, so it pops two groups at once. There are a few ways."""
    a, b = rng.sample(kinds, 2)
    g = [0] * (W * H)
    floor = H - 1
    how = rng.randrange(3)
    if how == 0:
        # two rows, and the swap goes down the end of them
        left = rng.randint(1, W - 4)
        for i, k in enumerate((a, a, b)):
            g[left + i + floor * W] = k
        for i, k in enumerate((b, b, a)):
            g[left + i + (floor - 1) * W] = k
        return g, (left + 2 + (floor - 1) * W, left + 2 + floor * W)
    if how == 1:
        # one row, and the swap is across the middle of it
        left = rng.randint(1, W - 7)
        for i, k in enumerate((a, a, b, a, b, b)):
            g[left + i + floor * W] = k
        return g, (left + 2 + floor * W, left + 3 + floor * W)
    # two columns, and the swap goes across the bottom of them
    left = rng.randint(1, W - 3)
    for i, k in enumerate((a, a, b)):
        g[left + (floor - 2 + i) * W] = k
    for i, k in enumerate((b, b, a)):
        g[left + 1 + (floor - 2 + i) * W] = k
    return g, (left + floor * W, left + 1 + floor * W)


SHAPES = [
    [(0, 0), (1, 0), (2, 0)],              # a row
    [(0, 0), (0, 1), (0, 2)],              # a column
    [(0, 0), (1, 0), (1, 1)],              # the Ls
    [(0, 0), (1, 0), (0, 1)],
    [(0, 1), (0, 0), (1, 0)],
]
BIG_SHAPES = [
    [(0, 0), (1, 0), (2, 0), (3, 0)],      # four
    [(0, 0), (0, 1), (0, 2), (0, 3)],
    [(0, 0), (1, 0), (2, 0), (1, 1)],
    [(0, 0), (0, 1), (1, 1), (1, 0)],
    [(0, 0), (1, 0), (2, 0), (2, 1)],
    [(0, 0), (1, 0), (2, 0), (3, 0), (4, 0)],  # five
    [(0, 0), (1, 0), (2, 0), (0, 1), (2, 1)],
    [(0, 0), (0, 1), (0, 2), (1, 2), (2, 2)],
    [(0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1)],  # six
]


def place(n, shape, left, base, kind):
    """Puts a group in, pushing up what is over it. The cells that it ended up in, or None."""
    placed = []
    for col_off, up in sorted(shape, key=lambda p: p[1]):
        col = left + col_off
        if col >= W or base + up > heights(n)[col]:
            return None
        if not put_in(n, col, base + up, kind):
            return None
        placed.append(col + (H - 1 - base - up) * W)
    return placed


def span(hs, theme):
    used = [x for x in range(W) if hs[x]]
    return used, THEMES[theme]["wide"]


def pick_left(rng, used, wide, sw):
    low = max(0, min(used) - sw)
    high = min(W - sw, max(used) + 1)
    if wide < W:
        low = max(low, max(used) - wide + 1)
        high = min(high, min(used) + wide - sw)
    if high < low:
        return None
    return rng.randint(low, high)


def neighbours_of(c):
    x, y = c % W, c // W
    if x > 0:
        yield c - 1
    if x < W - 1:
        yield c + 1
    if y > 0:
        yield c - W
    if y < H - 1:
        yield c + W


def add_step(rng, g, kinds, theme):
    """One more group, and the swaps that pop it. None if this try did not work out."""
    th = THEMES[theme]
    n = list(g)
    used, wide = span(heights(n), theme)
    shape = rng.choice(BIG_SHAPES if rng.random() < th["big"] else SHAPES)
    kind = rng.choice(kinds)
    sw = max(p[0] for p in shape) + 1

    # a chain: a group that is whole, and then another one that is put in the way of it
    if rng.random() < th["chain"]:
        under = rng.choice(SHAPES)
        uw = max(p[0] for p in under) + 1
        left = pick_left(rng, used, wide, uw)
        if left is None:
            return None
        ukind = rng.choice([k for k in kinds if k != kind])
        wholes = place(n, under, left, rng.randint(0, 2), ukind)
        if wholes is None:
            return None
        found = groups(n)
        if len(found) != 1 or set(found[0]) != set(wholes):
            return None
        used, wide = span(heights(n), theme)
        left = rng.randint(max(0, left - sw + 1), min(W - sw, left + uw - 1))
        placed = place(n, shape, left, rng.randint(0, 2), kind)
    else:
        left = pick_left(rng, used, wide, sw)
        if left is None:
            return None
        placed = place(n, shape, left, rng.randint(0, 2), kind)
    if placed is None or len(used) > wide + 1:
        return None

    # the group has to be the only thing on the board that pops
    found = groups(n)
    if len(found) != 1 or set(found[0]) != set(placed):
        return None

    # undo the swap: one fish of the group trades places with a fish next to it
    tries = [(c, o) for c in placed for o in neighbours_of(c) if n[o] and o not in placed]
    rng.shuffle(tries)
    for c, o in tries:
        before = list(n)
        before[c], before[o] = before[o], before[c]
        if groups(before):
            continue
        if play(before, c, o) != g:
            continue
        swaps = [(o, c)]

        # and it may have to be walked there, from further away. every step of the walk pops nothing.
        at = o
        for _ in range(rng.randint(0, th["carry"])):
            options = [q for q in neighbours_of(at) if before[q] and before[q] != before[at] and q not in placed]
            rng.shuffle(options)
            walked = False
            for q in options:
                back = list(before)
                back[at], back[q] = back[q], back[at]
                if groups(back):
                    continue
                before = back
                swaps.insert(0, (q, at))
                at = q
                walked = True
                break
            if not walked:
                break
        return before, swaps
    return None


def joined(g):
    """No board starts out cut in two. that is for the player to do."""
    hs = heights(g)
    used = [x for x in range(W) if hs[x]]
    return used[-1] - used[0] + 1 == len(used)
