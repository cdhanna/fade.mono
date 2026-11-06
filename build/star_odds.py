"""How possible is each constellation of the night? (see THE CONSTELLATIONS in fish_headers_squid)

It plays the real rules on random settled 9x9 boards with 7 kinds of fish:
  - the squid starts on a random fish that has none of its kind beside it
  - a push inks the squid's cell and sends it along the row or column, wrapping, as far as the number
    of kinds before a repeat, counted from the fish next to it (the one under it does not count)
  - a swap out of an inked cell inks the cell next to it, so a piece grows a cell at a time from any
    cell of it, and a push is the only way to start a new piece
A constellation is drawn by pushing the squid from one piece to the next, so every piece gets its
first cell from a push, and then growing each piece out. Pushes come first, while the board is as it
was dealt (a push changes no fish), so whether the jumps line up is decided by the board alone.

For every board it tries every turn of the shape, every order of its pieces and every cell of each
piece as the place the squid lands, and says the shape can be made when one of those works:
  exact   - the squid lands exactly on the next piece every time, no stray ink at all
  +1 hop  - one extra push is allowed on the way, which leaves one stray cell of ink, as long as it
            does not touch the shape (a move, and a cell, of waste)
It does not try swaps that only rearrange fish to change a jump, which a strong player would also use,
so these are floors, not ceilings.

    py -I build/star_odds.py [boards]
"""
import itertools, random, sys

N = 9
KINDS = 7
MOVES = 12


def settled_board(rng):
    """a board with no group of three of a kind, the way the board is once it has settled"""
    while True:
        b = [[0] * N for _ in range(N)]
        ok = True
        for y in range(N):
            for x in range(N):
                choices = list(range(1, KINDS + 1))
                rng.shuffle(choices)
                for k in choices:
                    b[y][x] = k
                    if group_size(b, x, y) < 3:
                        break
                else:
                    ok = False
        if ok:
            return b


def group_size(b, x, y):
    k = b[y][x]
    seen = {(x, y)}
    todo = [(x, y)]
    while todo:
        cx, cy = todo.pop()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < N and 0 <= ny < N and (nx, ny) not in seen and b[ny][nx] == k:
                seen.add((nx, ny))
                todo.append((nx, ny))
    return len(seen)


def lone_cells(b):
    out = []
    for y in range(N):
        for x in range(N):
            k = b[y][x]
            if all(not (0 <= x + dx < N and 0 <= y + dy < N and b[y + dy][x + dx] == k)
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out.append((x, y))
    return out


def jump(b, x, y, dx, dy):
    # the fish under the squid cannot be seen, so it does not count: the walk starts with the fish next to it
    seen = set()
    steps = 0
    cx, cy = x, y
    for _ in range(N - 1):
        cx, cy = (cx + dx) % N, (cy + dy) % N
        if b[cy][cx] in seen:
            break
        seen.add(b[cy][cx])
        steps += 1
    steps = max(steps, 1)
    return (x + dx * steps) % N, (y + dy * steps) % N


def pieces_of(cells):
    cells = set(cells)
    out = []
    while cells:
        c = cells.pop()
        piece = {c}
        todo = [c]
        while todo:
            cx, cy = todo.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (cx + dx, cy + dy)
                if n in cells:
                    cells.remove(n)
                    piece.add(n)
                    todo.append(n)
        out.append(sorted(piece))
    return out


def turns(cells):
    out = []
    cur = list(cells)
    for _ in range(4):
        mx = min(x for x, y in cur)
        my = min(y for x, y in cur)
        out.append([(x - mx, y - my) for x, y in cur])
        cur = [(-y, x) for x, y in cur]
    return out


def plans(cells):
    """every way to go from piece to piece: the shape turned, the order of the pieces, and the cell of
    each piece that the squid lands on. a plan is a list of cells, relative to the first one."""
    out = []
    for shape in turns(cells):
        ps = pieces_of(shape)
        for order in itertools.permutations(range(len(ps))):
            for seeds in itertools.product(*[ps[i] for i in order]):
                x0, y0 = seeds[0]
                out.append((shape, [(x - x0, y - y0) for x, y in seeds]))
    return out


def touches(cell, shape_set):
    x, y = cell
    if cell in shape_set:
        return True
    return any((x + dx, y + dy) in shape_set for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))


def makeable(b, start, plan_list, hops):
    sx, sy = start
    for shape, seeds in plan_list:
        # the shape, put where the squid starts
        x0 = sx - seeds[0][0]
        y0 = sy - seeds[0][1]
        placed = [(x0 + x, y0 + y) for x, y in shape]
        if not all(0 <= x < N and 0 <= y < N for x, y in placed):
            continue
        shape_set = set(placed)
        targets = [(sx + x, sy + y) for x, y in seeds]
        if walk(b, targets, 0, hops, shape_set):
            return True
    return False


def walk(b, targets, i, hops, shape_set):
    """is there a way to push from targets[i] to targets[i+1], and on to the end?"""
    if i == len(targets) - 1:
        return True
    x, y = targets[i]
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        nx, ny = jump(b, x, y, dx, dy)
        if (nx, ny) == targets[i + 1]:
            if walk(b, targets, i + 1, hops, shape_set):
                return True
        elif hops > 0 and not touches((nx, ny), shape_set):
            # a hop on the way: a push from (nx, ny) inks it, a stray cell, and goes on from there
            for ex, ey in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if jump(b, nx, ny, ex, ey) == targets[i + 1]:
                    if walk(b, targets, i + 1, hops - 1, shape_set):
                        return True
    return False


def odds(cells, boards):
    pl = plans(cells)
    exact = hop = 0
    for b, start in boards:
        if makeable(b, start, pl, 0):
            exact += 1
            hop += 1
        elif len(cells) + 1 <= MOVES and makeable(b, start, pl, 1):
            hop += 1
    return exact / len(boards), hop / len(boards)


def show(cells):
    w = max(x for x, y in cells) + 1
    h = max(y for x, y in cells) + 1
    ps = pieces_of(cells)
    mark = {}
    for i, p in enumerate(ps):
        for c in p:
            mark[c] = 'ABCDEFG'[i]
    return [''.join(mark.get((x, y), '.') for x in range(w)) for y in range(h)]


SHAPES = {}


def shape(name, tier, rows):
    cells = [(x, y) for y, r in enumerate(rows) for x, ch in enumerate(r) if ch != '.']
    SHAPES[name] = (tier, cells)


if __name__ == '__main__':
    count = int(sys.argv[1]) if len(sys.argv) > 1 else 400
    which = sys.argv[2] if len(sys.argv) > 2 else 'current'
    rng = random.Random(7)
    boards = []
    for _ in range(count):
        b = settled_board(rng)
        boards.append((b, rng.choice(lone_cells(b))))

    if which == 'current':
        shape('Mercury', 'easy', ['X.X', 'X..', 'XX.'])
        shape('Venus', 'easy', ['.X.', '...', 'XXX', '.X.'])
        shape('Mars', 'easy', ['..XX', 'X...', 'XX..'])
        shape('Earth', 'medium', ['.X..X', 'XXX.X', '.X...'])
        shape('Uranus', 'medium', ['XXX', '.X.', '.X.', '...', 'X.X'])
        shape('Pluto', 'medium', ['XX.', 'XX.', '...', 'XXX'])
        shape('Jupiter', 'hard', ['X.X.X', '..X..', '..X..', 'XXX..', '..X.X'])
        shape('Saturn', 'hard', ['....XX.', '..XX...', '..XX...', 'XX....X'])
        shape('Neptune', 'hard', ['X.X.X', 'X.X.X', '..X..', '..X..', '..X..', '.....', '..X..'])
    else:
        exec(open(which).read())

    for name, (tier, cells) in SHAPES.items():
        ps = pieces_of(cells)
        sizes = sorted(len(p) for p in ps)
        e, h = odds(cells, boards)
        print(f'{name:9} {tier:6} {len(cells):2} cells, pieces {sizes}: exact {e*100:5.1f}%   with one stray hop {h*100:5.1f}%')
        for r in show(cells):
            print('            ' + r)
