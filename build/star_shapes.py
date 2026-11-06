# the five constellations, one for every part of Neptune's trident, for build/star_odds.py:
#     py -I build/star_odds.py 400 build/star_shapes.py
# a letter is a cell, and each letter is a piece. the easy and middling ones are a single piece: one
# push starts it, and swaps out of the ink grow it. the hard one is two pieces, neither a single cell,
# which line up along a row or a column, one cell apart, which is how far the squid jumps most often. this is the table that
# setup_stars in fish_routines_squid is written from, in this order.

shape('Left prong', 'easy', [
    'AAA',
    'A.A',
    'A..'])
shape('Right prong', 'easy', [
    'AAA',
    'A.A',
    '..A'])
shape('Middle prong', 'medium', [
    'A.A',
    'AAA',
    '.A.',
    '.A.'])
shape('Crossbar', 'medium', [
    'AAAAA',
    'A...A'])
shape('Shaft', 'hard', [
    'A..',
    'A..',
    'A..',
    'A..',
    'A..',
    '...',
    'B..',
    'BBB'])
