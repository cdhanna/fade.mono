"""writes the trailer_captions test at the end of Fade.MonoGame/fish_tests_trailer.fbasic, replacing it if it is
there: one short caption video for each line of CAPTIONS. run it, then render the test with
build/render-trailer.ps1 -Shot captions."""
import os
p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Fade.MonoGame", "fish_tests_trailer.fbasic")
s = open(p, encoding="utf-8").read()
if "test trailer_captions" in s:
    a = s.index("` CAPTIONS.")
    b = s.index("endtest", s.index("test trailer_captions")) + len("endtest\n")
    s = s[:a] + s[b:]

CAPTIONS = [
    ("cap_swap", ["Swap.", "Pop."], ["Take", "your", "time."]),
    ("cap_cards", ["A", "new", "card"], ["every", "round."]),
    ("cap_locals", ["Meet", "the", "locals."], ["They", "have", "favors", "to", "ask."]),
    ("cap_night", ["When", "night", "falls,"], ["nothing", "pops."]),
    ("cap_deep", ["Not", "everything"], ["down", "here", "is", "friendly."]),
    ("cap_moves", ["Thirty", "rounds."], ["How", "few", "moves", "will", "you", "need?"]),
    ("cap_listen", ["Listen", "closely."], ["The", "sea", "is", "full", "of", "secrets."]),
    ("cap_quiet", ["No", "timer."], ["No", "rush.", "Just", "the", "sea."]),
    ("cap_relax", ["30", "Rounds."], ["Relax,", "you", "cannot", "lose."], 320),
]

out = ['''` CAPTIONS. short lines for the cuts in build/cuts to put between their shots: the words rise in over
` the empty sea the way the title at the start does, stay a moment, and fade. one video for each, from
` one run. the words are written by build/trailer_captions.py, which writes this test.
test trailer_captions from trailerShot
    trailerNoUi()
    trailerFishAway()
    set sprite alpha SELECTION_SPRITE, 0
    for tCapF = 1 to 30
        hintShow(0)
        trailerHideBut(-1, -1, -1, -1)
        runto update_frame
    next
''']
for name, big, small, *hold in CAPTIONS:
    hold = hold[0] if hold else 200  # how many frames the words are up for, with their rise
    words = "".join(f'    trailerWordOn("{w}", 0)\n' for w in big) + "".join(f'    trailerWordSub("{w}", 1, 28)\n' for w in small)
    out.append(f'''
    ` "{" ".join(big)} {" ".join(small)}"
{words}    trailerTitleLayout(WORLD_HEIGHT / 2)
    trailerLineWait(1, 30)
    trailerRecord("trailer_{name}")
    for tCapF = 0 to {hold}
        hintShow(0)
        trailerHideBut(-1, -1, -1, -1)
        trailerRiseTitle(tCapF - 8, 12, 1)
        runto update_frame
    next
    for tCapF = 0 to 24
        tCapA# = tCapF
        tCapA# = 1 - tCapA# / 24.0
        hintShow(0)
        trailerHideBut(-1, -1, -1, -1)
        trailerRiseTitle(400, 12, tCapA#)
        runto update_frame
    next
    record stop
    for tCapN = 0 to TR_WORD_COUNT - 1
        hide text TR_WORD(tCapN)
    next
    TR_WORD_COUNT = 0
''')
out.append("    end\nendtest\n")
s = s.rstrip("\n") + "\n\n\n" + "".join(out)
open(p, "w", encoding="utf-8").write(s)
print("captions:", [c[0] for c in CAPTIONS])
