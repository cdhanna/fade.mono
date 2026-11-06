"""Makes the thirty boards of the long visit, and writes them to Fade.MonoGame/fish_puzzles.fbasic.

A board is fish that do not come back when they pop, and the goal is an empty sea. The rules are the rules of the game:
a swap has to pop something, or it is not made. Three fish do something else:

    belly up  pops when the cursor comes to it, with any belly up fish that touch it. the cursor has to go around one
              to keep it. it matches with its own kind like any other fish. going around costs nothing, so one is only
              worth having if WHEN it pops matters, which is what timing measures, or if it is part of a gate.
    bubbled   its swaps are free, so it is the only fish that moves without popping, and each swap still sets off
              whatever it puts together. two bubbles that touch both pop, whatever kinds they are.
    rainbow   three that touch all pop, whatever kinds they are. it matches with its own kind like any other fish.

A gate is belly up fish on a diagonal. They do not touch, so each pops alone, and the cursor cannot get between them, so
the fish behind them cannot be reached until one is popped, and that drops what is on top of it. It takes a wall of the
sea or a second diagonal to close: a corner is one gate, and a tent standing on the floor is two, with fish under it.

A bubble is no way out of a board. Every step that it takes pushes a fish one cell over, and that fish stays where it was
pushed, so a walk that is not thought through pops the wrong group, or leaves fish that can never come together.

Boards are built backwards from an empty sea, with the pieces of make_puzzles. Then solve() finds every position that the
player can get to. A bubble can go around in circles, so that is a graph and not a tree. Every board has been solved
all the way, which means that:

    wins     how often someone clears it who picks any legal move at random. it is how the boards are put in order.
    lost     how many of the positions cannot be cleared
    needs    what the board cannot be cleared without: a free swap of a bubble, three rainbow fish coming together, or
             a belly up fish popped by the cursor
    timing   of the moves that pop a belly up fish from a position that can win, how many of them lose it
    sealed   how many fish the cursor cannot get to at the start

The hints are written here too: every position that can still win, and the move from it that is nearest to the end.
A position that is not there is lost, which is what stops the snow. See HINTS in fish_headers_puzzle. So that the rules
here cannot drift away from the rules of the game, a few other moves from every step of the line are written down with
where they end up, and long_puzzles_agree_with_the_game makes them in the real game.

    python -I build/make_puzzle_boards.py            look for boards, and write them. this takes a few minutes.
    python -I build/make_puzzle_boards.py --check    print them and their ratings, and write nothing
    python -I build/make_puzzle_boards.py --resume   keep what the last search found, and only look for what it did not
    python -I build/make_puzzle_boards.py --again    solve the boards that are there again, and write them
"""

import collections
import math
import multiprocessing
import pathlib
import pickle
import random
import re
import sys
import tempfile
import time

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import make_puzzles as m

W, H = m.W, m.H
BELLY, BUBBLE, RAINBOW = 10, 20, 40  # a fish is its kind plus one of these. the same as puzzleHintCell.
OUT = m.OUT
KEPT = pathlib.Path(tempfile.gettempdir()) / "fish_puzzle_boards.pickle"  # what a search has found so far
CANDIDATES = 12  # how many boards are tried for every one that is kept
TRIES = 500  # and how many builds that may take
MOST_STATES = 12000  # a board with more positions than this takes too long to look at
MOST_HINTS = 800  # and one with more winning positions than this takes up too much of the room for them
TIMING_WANTED = .2  # a board with belly up fish has to lose this much of the time that one is popped
SECONDS = 300  # how long is spent on the boards for one place
GOES = 4  # and how many times the ones that found nothing are tried again
MOST_LINE = 24  # the same as PUZZLE_SWAP_ROOM
MOVE_FLAG = 1000  # in the list, b + this is the cursor moving from a to b, with no swap
HINT_MOD = 50000017  # the same as PUZZLE_HINT_MOD
HINT_ROOM = 8000  # the same as PUZZLE_HINT_ROOM
HINTS_EACH = 400  # how many go in one function
CHECKS_EACH_STEP = 6  # how many other moves are written down from each step of a line
CHECK_ROOM = 4000  # the same as the size of PUZZLE_CHECK_AT
SWEEPS = 60  # how many times the chance of a win is worked out again. a loop takes more than one go.

# what each board is made of: pops of groups, belly up fish, bubble pairs, carried bubbles, rainbow threes, a gate, and
# what it has to need. the fish are there to make the later boards harder, so a board does not have to need them. only
# the board that one comes in on does, or she would be explaining something that does not matter yet.
SETS = [
    # the swaps that pop
    (4, 0, 0, 0, 0, "", ""), (4, 0, 0, 0, 0, "", ""), (5, 0, 0, 0, 0, "", ""), (5, 0, 0, 0, 0, "", ""), (6, 0, 0, 0, 0, "", ""),
    (6, 0, 0, 0, 0, "", ""), (7, 0, 0, 0, 0, "", ""), (7, 0, 0, 0, 0, "", ""), (8, 0, 0, 0, 0, "", ""), (8, 0, 0, 0, 0, "", ""),
    # belly up comes in
    (3, 1, 0, 0, 0, "", "belly"), (5, 1, 0, 0, 0, "", ""), (7, 0, 0, 0, 0, "", ""), (5, 0, 0, 0, 0, "corner", ""), (5, 1, 0, 0, 0, "corner", ""),
    # bubbles come in
    (2, 0, 0, 1, 0, "", "bubble"), (3, 0, 0, 1, 0, "", ""), (6, 0, 0, 0, 0, "corner", ""), (3, 1, 0, 1, 0, "", ""), (2, 1, 1, 0, 0, "", ""),
    # rainbows come in
    (3, 0, 0, 0, 1, "", "rainbow"), (5, 0, 0, 0, 1, "", ""), (8, 0, 0, 0, 0, "", ""), (5, 0, 0, 0, 1, "corner", ""), (4, 0, 0, 1, 1, "", ""),
    # anything
    (6, 0, 0, 0, 0, "tent", ""), (2, 1, 0, 1, 1, "", ""), (4, 0, 0, 0, 1, "tent", ""), (1, 2, 1, 0, 1, "", ""), (5, 0, 0, 0, 0, "tent", ""),
]
# how many boards go together, and how often someone who picks any move should clear the first and the last of them.
# a new fish comes in on the first board of a lot, and that board stays first. the rest go from easiest to hardest.
LOTS = ((10, .45, .01), (5, .3, .01), (5, .2, .008), (5, .15, .006), (5, .06, .003))
NEW_FISH_LOTS = (1, 2, 3)


def kind(v):
    return v % 10


def is_belly(v):
    return v // 10 == 1


def is_bubble(v):
    return v // 10 == 2


def is_rainbow(v):
    return v // 10 == 4


def beside(c):
    x, y = c % W, c // W
    if x > 0:
        yield c - 1
    if x < W - 1:
        yield c + 1
    if y > 0:
        yield c - W
    if y < H - 1:
        yield c + W


def patches(g, wanted):
    """The fish that touch, of those that wanted says yes to. A list of lists of cells."""
    seen = set()
    found = []
    for i, v in enumerate(g):
        if i in seen or not v or not wanted(v):
            continue
        seen.add(i)
        stack = [i]
        cells = []
        while stack:
            c = stack.pop()
            cells.append(c)
            for n in beside(c):
                if n not in seen and g[n] and wanted(g[n]):
                    seen.add(n)
                    stack.append(n)
        found.append(cells)
    return found


def look(g, forced=()):
    """One look of handle_combos: what pops, and whether rainbow fish popped by being rainbow fish. A forced fish leaves
    its group: the group is found with it, and then has to be big enough without it."""
    gone = set(forced)
    for cell in range(W * H):
        if is_bubble(g[cell]) and any(is_bubble(g[n]) for n in beside(cell)):
            gone.add(cell)
    plain = set()
    for k in set(kind(v) for v in g if v):
        for cells in patches(g, lambda v: kind(v) == k):
            cells = [c for c in cells if c not in gone]
            if len(cells) >= m.MIN_GROUP:
                plain.update(cells)
    wild = False
    popped = gone | plain
    for cells in patches(g, is_rainbow):
        if len(cells) >= m.MIN_GROUP:
            if any(c not in popped for c in cells):
                wild = True
            gone.update(cells)
    return gone | plain, wild


def settle(g, forced=()):
    """Looks until nothing pops. Whether any look was rainbow fish coming together."""
    wild_seen = False
    while True:
        gone, wild = look(g, forced)
        forced = ()
        if not gone:
            return wild_seen
        wild_seen = wild_seen or wild
        for c in gone:
            g[c] = 0
        m.fall(g)


# the builder of make_puzzles looks at the board with this, so it sees these rules too
m.groups = lambda g: [sorted(look(g)[0])] if look(g)[0] else []


def touched(g, c):
    """The fish that pop when the cursor comes to this cell. See popBellyUpFish."""
    if not is_belly(g[c]):
        return ()
    return tuple([c] + [n for n in beside(c) if is_belly(g[n])])


def do_swap(g, a, b):
    n = list(g)
    n[a], n[b] = n[b], n[a]
    wild = settle(n, touched(n, b))
    return n, wild


def do_touch(g, c):
    n = list(g)
    wild = settle(n, touched(n, c))
    return n, wild


def fish_count(g):
    return sum(1 for v in g if v)


def reach(g, cursor):
    """Where the cursor can get to without popping a belly up fish. It can go over a gap."""
    seen = {cursor}
    stack = [cursor]
    while stack:
        c = stack.pop()
        for n in beside(c):
            if n not in seen and not is_belly(g[n]):
                seen.add(n)
                stack.append(n)
    return seen


def start_cursor(g):
    """Where puzzleLowestFish puts the cursor."""
    best, far_best = 0, 999
    for c, v in enumerate(g):
        if v:
            x, y = c % W, c // W
            far = (H - 1 - y) * 2 + abs(x - W // 2)
            if far < far_best:
                far_best, best = far, c
    return best


def moves(g, cursor):
    """Every move: (how, from, to, board after, cursor after, what it took). What it took is a set of 'bubble', when a
    swap popped nothing and was only allowed because of a bubble, 'rainbow', when rainbow fish came together, and 'touch',
    when the cursor popped a belly up fish."""
    here = reach(g, cursor)
    out = []
    done = set()
    count = fish_count(g)
    for c in sorted(here):
        for n in beside(c):
            if is_belly(g[n]) and n not in here and n not in done:
                done.add(n)
                after, wild = do_touch(g, n)
                out.append(("touch", c, n, after, n, {"touch", "rainbow"} if wild else {"touch"}))
            if g[c] and g[n] and g[c] != g[n] and (c, n) not in done:
                done.add((c, n))
                after, wild = do_swap(g, c, n)
                took = {"rainbow"} if wild else set()
                landed = list(g)
                landed[c], landed[n] = landed[n], landed[c]
                if touched(landed, n):
                    took.add("touch")
                if fish_count(after) == count:
                    if not is_bubble(g[c]) and not is_bubble(g[n]):
                        continue
                    took.add("bubble")
                out.append(("swap", c, n, after, n, took))
    return out


def key(b, c):
    return tuple(b), min(reach(b, c))


class TooBig(Exception):
    pass


def solve(g):
    """Finds every position that can be got to. The rating of the board, a shortest line that clears it, and its hints."""
    cursor = start_cursor(g)
    first = key(g, cursor)
    index = {first: 0}
    states = [(list(g), cursor)]
    edges = []
    at = 0
    while at < len(states):
        b, c = states[at]
        at += 1
        out = []
        if any(b):
            seen = {}
            for how, a, n, after, to, took in moves(b, c):
                k = key(after, to)
                if k not in index:
                    if len(states) >= MOST_STATES:
                        raise TooBig
                    index[k] = len(states)
                    states.append((after, to))
                # a swap can be made from either side, and that is one move
                if index[k] not in seen:
                    seen[index[k]] = (how, a, n, took)
            out = [(to, how, a, n, took) for to, (how, a, n, took) in seen.items()]
        edges.append(out)

    back = [[] for _ in states]
    for i, out in enumerate(edges):
        for to, _, _, _, _ in out:
            back[to].append(i)

    # how far each position is from an empty sea
    def distances(banned=frozenset()):
        far = {}
        queue = collections.deque()
        for i, (b, _) in enumerate(states):
            if not any(b):
                far[i] = 0
                queue.append(i)
        while queue:
            i = queue.popleft()
            for o in back[i]:
                if o not in far and any(e[0] == i and not (e[4] & banned) for e in edges[o]):
                    far[o] = far[i] + 1
                    queue.append(o)
        return far

    far = distances()
    if 0 not in far:
        return None

    # the chance of a win for someone who picks any move
    chance = [1.0 if not any(b) else 0.0 for b, _ in states]
    order = sorted(far, key=far.get)
    for _ in range(SWEEPS):
        moved = 0.0
        for i in order:
            if edges[i]:
                new = sum(chance[e[0]] for e in edges[i]) / len(edges[i])
                moved = max(moved, abs(new - chance[i]))
                chance[i] = new
        if moved < .0005:
            break

    hints = {}
    for i in far:
        if edges[i]:
            best = min((e for e in edges[i] if e[0] in far), key=lambda e: far[e[0]])
            hints[key(*states[i])] = (best[1], best[2], best[3])

    line = []
    i = 0
    while far[i]:
        best = min((e for e in edges[i] if e[0] in far), key=lambda e: far[e[0]])
        line.append((best[1], best[2], best[3]))
        i = best[0]

    needs = set()
    if 0 not in distances(frozenset({"bubble"})):
        needs.add("bubble")
    if 0 not in distances(frozenset({"rainbow"})):
        needs.add("rainbow")
    if 0 not in distances(frozenset({"touch"})):
        needs.add("belly")

    pops = [e[0] in far for i in far for e in edges[i] if "touch" in e[4]]
    return dict(wins=chance[0], states=len(states), lost=len(states) - len(far), line=line, hints=hints, needs=needs,
                free=sum(1 for e in edges[0] if e[0] in far), choices=len(edges[0]),
                timing=pops.count(False) / len(pops) if pops else 0)


def stable(g):
    return not look(g)[0]


def belly_step(rng, g, kinds):
    """One more fish goes in, belly up. Touching it gets back to this board."""
    for _ in range(30):
        col = rng.randrange(W)
        used = [x for x in range(W) if m.heights(g)[x]]
        if col < min(used) - 1 or col > max(used) + 1:
            continue
        n = list(g)
        height = m.heights(n)[col]
        if height >= H:
            continue
        row_from_floor = rng.randint(0, height)
        if not m.put_in(n, col, row_from_floor, rng.choice(kinds) + BELLY):
            continue
        cell = col + (H - 1 - row_from_floor) * W
        if any(is_belly(n[o]) for o in beside(cell)) or not stable(n):
            continue
        if do_touch(n, cell)[0] != g:
            continue
        return n
    return None


def gate_cells(rng, gate):
    """(column, row from the floor) of the belly up fish of a gate."""
    if gate == "corner":
        size = rng.randint(2, 3)
        cells = [(x, size - 1 - x) for x in range(size)]
        return cells if rng.random() < .5 else [(W - 1 - x, row) for x, row in cells]
    size = 3  # a smaller tent only has room for one fish
    peak = rng.randint(size - 1, W - size)
    return [(peak + d, size - 1 - abs(d)) for d in range(1 - size, size)]


def gate_step(rng, g, kinds, gate):
    """Belly up fish go in on a diagonal, and each comes out again by being touched. This is the last thing that goes in,
    because anything that went in after it could push the diagonal out of line."""
    n = list(g)
    for col, row in gate_cells(rng, gate):
        before = list(n)
        if not row <= m.heights(n)[col] < H or not m.put_in(n, col, row, rng.choice(kinds) + BELLY):
            return None
        cell = col + (H - 1 - row) * W
        if any(is_belly(n[o]) for o in beside(cell)) or not stable(n) or do_touch(n, cell)[0] != before:
            return None
    return n


def sealed(g):
    """How many fish the cursor cannot get to at the start."""
    here = reach(g, start_cursor(g))
    return sum(1 for c, v in enumerate(g) if v and c not in here and not is_belly(v))


def put_shape(rng, g, shape):
    """Room for a shape on the board, and what it pushed up. The board, and the cells of the shape."""
    n = list(g)
    used = [x for x in range(W) if m.heights(n)[x]]
    width = max(p[0] for p in shape) + 1
    left = m.pick_left(rng, used, W, width)
    if left is None:
        return None
    placed = m.place(n, shape, left, rng.randint(0, 3), 1)
    return (n, placed) if placed else None


def walk_away(rng, before, cell, stay, least, most):
    """The bubble in this cell goes back the way that it came, for a few steps. The board that it starts from, or None."""
    at = cell
    steps = rng.randint(least, most)
    for _ in range(steps):
        options = [q for q in beside(at) if before[q] and q not in stay and not is_rainbow(before[q]) and not is_bubble(before[q])]
        rng.shuffle(options)
        for q in options:
            walked = list(before)
            walked[at], walked[q] = walked[q], walked[at]
            if stable(walked):
                before = walked
                at = q
                break
        else:
            return None
    return before


def bubble_pair_step(rng, g, kinds):
    """Two bubbles that have come together, and then one of them goes back the way that it came."""
    made = put_shape(rng, g, rng.choice([[(0, 0), (1, 0)], [(0, 0), (0, 1)]]))
    if not made:
        return None
    n, placed = made
    for c in placed:
        n[c] = rng.choice(kinds) + BUBBLE
    if set(look(n)[0]) != set(placed):
        return None
    away = rng.choice(placed)
    return walk_away(rng, n, away, set(placed), 2, 5)


def bubble_carry_step(rng, g, kinds):
    """A group, and one of its fish is in a bubble, and goes back the way that it came."""
    made = put_shape(rng, g, rng.choice(m.SHAPES))
    if not made:
        return None
    n, placed = made
    k = rng.choice(kinds)
    for c in placed:
        n[c] = k
    if set(look(n)[0]) != set(placed):
        return None
    away = rng.choice(placed)
    n[away] = k + BUBBLE
    return walk_away(rng, n, away, set(placed), 2, 5)


def rainbow_step(rng, g, kinds):
    """Three rainbow fish that have come together, and then a swap pulls one of them away from the others."""
    made = put_shape(rng, g, rng.choice(m.SHAPES))
    if not made:
        return None
    n, placed = made
    for c in placed:
        n[c] = rng.choice(kinds) + RAINBOW
    if set(look(n)[0]) != set(placed):
        return None
    tries = [(c, o) for c in placed for o in beside(c) if n[o] and o not in placed and not is_belly(n[o]) and not is_bubble(n[o])]
    rng.shuffle(tries)
    for c, o in tries:
        before = list(n)
        before[c], before[o] = before[o], before[c]
        if stable(before) and do_swap(before, o, c)[0] == g:
            return before
    return None


def build(rng, pops, bellies, pairs, carries, rainbows, gate, theme):
    """A board. None if this try did not work out."""
    kinds = rng.sample(range(1, 8), min(6, 4 + pops // 5))
    g, _ = m.last_board(rng, kinds)
    todo = ["group"] * (pops - 1) + ["belly"] * bellies + ["pair"] * pairs + ["carry"] * carries + ["rainbow"] * rainbows
    rng.shuffle(todo)
    fails = 0
    for what in todo:
        while True:
            fails += 1
            if fails > 800:
                return None
            if what == "group":
                step = m.add_step(rng, g, kinds, theme)
                step = step[0] if step else None
            elif what == "belly":
                step = belly_step(rng, g, kinds)
            elif what == "pair":
                step = bubble_pair_step(rng, g, kinds)
            elif what == "carry":
                step = bubble_carry_step(rng, g, kinds)
            else:
                step = rainbow_step(rng, g, kinds)
            if step:
                g = step
                break
    while gate:
        fails += 1
        if fails > 800:
            return None
        step = gate_step(rng, g, kinds, gate)
        if step:
            return step
    return g


def good_start(g):
    return stable(g) and m.joined(g)


def hint_key(at, position):
    """The same as puzzleHintKey."""
    g, place = position
    h = at + 1
    for v in g:
        h = (h * 37 + v) % HINT_MOD
    return (h * 37 + place) % HINT_MOD


def show(g):
    letters = {0: "", 1: "b", 2: "o", 4: "r"}
    return "\n".join(" ".join(f"{kind(v)}{letters[v // 10]}".ljust(2) if v else ". " for v in g[y * W:(y + 1) * W]) for y in range(H))


def one_place(job):
    """The best of the boards tried for one place of the thirty, on this go."""
    place, go = job
    pops, bellies, pairs, carries, rainbows, gate, needed = SETS[place]
    rng = random.Random(20261101 + place + go * 1000)
    step = place
    for size, easiest, hardest in LOTS:
        if step < size:
            break
        step -= size
    wanted = easiest * (hardest / easiest) ** (step / (size - 1))
    best = None
    found = tries = 0
    start = time.time()
    while found < CANDIDATES and time.time() - start < SECONDS:
        tries += 1
        g = build(rng, pops, bellies, pairs, carries, rainbows, gate, m.CYCLE[tries % len(m.CYCLE)])
        if not g or not good_start(g):
            continue
        try:
            rated = solve(g)
        except TooBig:
            continue
        if rated is None or len(rated["line"]) > MOST_LINE or len(rated["line"]) < 3 or len(rated["hints"]) > MOST_HINTS:
            continue
        if needed and needed not in rated["needs"]:
            continue
        if (bellies or gate) and needed != "belly" and rated["timing"] < TIMING_WANTED:
            continue
        # the cursor starts in the middle of the floor, so it can be under a tent, and then the way out is the puzzle
        if gate and sealed(g) < 2:
            continue
        found += 1
        score = abs(math.log(max(rated["wins"], .0001) / wanted))
        if step < 3 and rated["free"] < 2:
            score += 5  # a board near the start of a lot never has only one way in
        if best is None or score < best[0]:
            best = (score, g, rated)
    print(f"place {place + 1}: {found} boards from {tries} tries in {time.time() - start:.0f}s", flush=True)
    return place, best


def bits(g, y, wanted):
    return int("".join("1" if wanted(v) else "0" for v in g[y * W:(y + 1) * W]))


def check_rows(boards):
    """(board, step, move, key of where it ends up, 1 if that is lost), for a few other moves from each step of a line.
    A move that clears the board is left out, because that is the end of the board."""
    rng = random.Random(20261102)
    rows = []
    for at, (g, rated) in enumerate(boards):
        b, c = list(g), start_cursor(g)
        for step, (how, a, n) in enumerate(rated["line"]):
            seen = {}
            for move in moves(b, c):
                if any(move[3]):
                    seen.setdefault(key(move[3], move[4]), move)
            picked = rng.sample(sorted(seen, key=lambda k: (seen[k][1], seen[k][2])), min(CHECKS_EACH_STEP, len(seen)))
            for k in picked:
                m_how, m_a, m_n = seen[k][:3]
                encoded = m_a * 10000 + m_n + (MOVE_FLAG if m_how == "touch" else 0)
                rows.append((at, step, encoded, hint_key(at, k), 0 if k in rated["hints"] else 1))
            b = do_swap(b, a, n)[0] if how == "swap" else do_touch(b, n)[0]
            c = n
    return rows


def read_boards():
    """The boards that are in fish_puzzles now."""
    text = OUT.read_text(encoding="utf-8")
    boards = {}
    for at, y, row in re.findall(r"puzzleRow\((\d+), (\d+), (\d+)\)", text):
        g = boards.setdefault(int(at), [0] * (W * H))
        for x, digit in enumerate(row.zfill(W)):
            g[int(y) * W + x] = int(digit)
    for name, flag in (("PUZZLE_BELLIES", BELLY), ("PUZZLE_BUBBLES", BUBBLE), ("PUZZLE_RAINBOWS", RAINBOW)):
        for at, y, row in re.findall(name + r"\((\d+) \* PUZZLE_SIDE \+ (\d+)\) = (\d+)", text):
            for x, digit in enumerate(row.zfill(W)):
                if digit == "1":
                    boards[int(at)][int(y) * W + x] += flag
    return [boards[at] for at in sorted(boards)]


def source(boards):
    lines = [
        "` MADE BY build/make_puzzle_boards.py. do not change this by hand: every board has been solved all the way, and",
        "` comes with a line of moves that clears it. see fish_headers_puzzle.",
        "goto puzzles_list_exit",
        "",
        "setup_puzzle_list:",
    ]
    for at, (g, rated) in enumerate(boards):
        for y in range(H):
            row = "".join(str(kind(v)) for v in g[y * W:(y + 1) * W])
            if int(row):
                lines.append(f"    puzzleRow({at}, {y}, {int(row)})")
            for name, wanted in (("PUZZLE_BELLIES", is_belly), ("PUZZLE_BUBBLES", is_bubble), ("PUZZLE_RAINBOWS", is_rainbow)):
                if bits(g, y, wanted):
                    lines.append(f"    {name}({at} * PUZZLE_SIDE + {y}) = {bits(g, y, wanted)}")
        for n, (how, a, b) in enumerate(rated["line"]):
            lines.append(f"    puzzleSwap({at}, {n}, {a}, {b + (MOVE_FLAG if how == 'touch' else 0)})")
    lines += ["return", "", "puzzles_list_exit:", ""]

    rows = {}
    for at, (_, rated) in enumerate(boards):
        for position, (how, a, b) in rated["hints"].items():
            k = hint_key(at, position)
            assert k not in rows, "two positions have the same key"
            rows[k] = a * 10000 + b + (MOVE_FLAG if how == "touch" else 0)
    rows = sorted(rows.items())
    if len(rows) > HINT_ROOM:
        raise SystemExit(f"{len(rows)} hints do not fit in PUZZLE_HINT_ROOM")
    print(f"{len(rows)} hints")
    parts = [rows[i:i + HINTS_EACH] for i in range(0, len(rows), HINTS_EACH)]
    lines.append("function puzzleHintList()")
    lines += [f"    puzzleHintList{i}()" for i in range(len(parts))]
    lines += ["endfunction", ""]
    for i, part in enumerate(parts):
        lines.append(f"function puzzleHintList{i}()")
        lines += [f"    puzzleHintRow({k}, {move})" for k, move in part]
        lines += ["endfunction", ""]

    checks = check_rows(boards)
    if len(checks) > CHECK_ROOM:
        raise SystemExit(f"{len(checks)} checks do not fit in PUZZLE_CHECK_AT")
    print(f"{len(checks)} checks, {sum(row[4] for row in checks)} of them lost")
    parts = [checks[i:i + HINTS_EACH] for i in range(0, len(checks), HINTS_EACH)]
    lines.append("function puzzleCheckList()")
    lines += [f"    puzzleCheckList{i}()" for i in range(len(parts))]
    lines += ["endfunction", ""]
    for i, part in enumerate(parts):
        lines.append(f"function puzzleCheckList{i}()")
        lines += [f"    puzzleCheck({at * 100 + step}, {move}, {k}, {lost})" for at, step, move, k, lost in part]
        lines += ["endfunction", ""]
    return "\n".join(lines)


def clashes(boards):
    """Whether two positions have the same key. The key has to fit in an int, so with this many it can happen."""
    keys = [hint_key(at, position) for at, (_, rated) in enumerate(boards) for position in rated["hints"]]
    return len(keys) != len(set(keys))


def apart(boards):
    """Where a board is goes into its keys, so a clash goes away when two boards change places. It is the two that are
    closest in how hard they are, and never the first of a lot, which is where a new fish comes in."""
    if not clashes(boards):
        return boards
    firsts = [sum(lot[0] for lot in LOTS[:n]) for n in NEW_FISH_LOTS]
    pairs = [at for at in range(len(boards) - 1) if at not in firsts and at + 1 not in firsts]
    for at in sorted(pairs, key=lambda at: boards[at][1]["wins"] / max(boards[at + 1][1]["wins"], .0001)):
        moved = list(boards)
        moved[at], moved[at + 1] = moved[at + 1], moved[at]
        if not clashes(moved):
            return moved
    raise SystemExit("two positions have the same key, wherever the boards go")


def write(boards):
    for i, (g, rated) in enumerate(boards):
        print(f"board {i + 1}: {fish_count(g)} fish, {len(rated['line'])} moves, random wins {rated['wins']:.1%}, "
              f"{rated['states']} positions, {rated['lost']} lost, {rated['free']} of {rated['choices']} first moves win, "
              f"needs {sorted(rated['needs'])}, timing {rated['timing']:.0%}, sealed {sealed(g)}", flush=True)
        print(show(g), flush=True)
    if "--check" not in sys.argv:
        OUT.write_text(source(boards), encoding="utf-8", newline="\n")
        print(f"wrote {len(boards)} boards to {OUT}")


def main():
    if "--again" in sys.argv:
        write([(g, solve(g)) for g in read_boards()])
        return
    found = {}
    if "--resume" in sys.argv and KEPT.exists():
        found = {place: best for place, (made_of, best) in pickle.loads(KEPT.read_bytes()).items() if made_of == SETS[place]}
    with multiprocessing.Pool(max(2, (multiprocessing.cpu_count() or 4) - 2)) as pool:
        for go in range(GOES):
            missing = [place for place in range(len(SETS)) if place not in found]
            if not missing:
                break
            for place, best in pool.imap_unordered(one_place, [(place, go) for place in missing]):
                if best:
                    found[place] = best
                    KEPT.write_bytes(pickle.dumps({at: (SETS[at], kept) for at, kept in found.items()}))
    missing = [place + 1 for place in range(len(SETS)) if place not in found]
    if missing:
        raise SystemExit(f"no board for {missing}")

    boards = []
    first = 0
    for at, (size, _, _) in enumerate(LOTS):
        lot = [found[place][1:] for place in range(first, first + size)]
        kept = 1 if at in NEW_FISH_LOTS else 0
        boards += lot[:kept] + sorted(lot[kept:], key=lambda board: -board[1]["wins"])
        first += size

    write(apart(boards))


if __name__ == "__main__":
    main()
