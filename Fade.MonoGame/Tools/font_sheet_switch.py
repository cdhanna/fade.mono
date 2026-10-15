"""fontexp.py in|out : switch the 16 and 20 pixel texts of the fish game to fonts baked at their
own size, drawn through TextCrispEffect. 'out' undoes exactly what 'in' did."""
import glob, os, re, sys
root = r"C:\proj\fade.mono\Fade.MonoGame"
mode = sys.argv[1]
files = sorted(f for f in glob.glob(root + r"\fish*.fbasic"))
CREATE = r"^([ \t]*text )([^,\r\n]+)(,[^\r\n]*?)\bSCORE_FONT\b(?=,)"
MARK = " `fontexp: a line of this font is 17 tall"

GLOBALS = """
` the same letters as SCORE_FONT, at the two sizes that most of the text is drawn at: a line that is
` 16 pixels tall, and one that is 20. these two are pictures, drawn a pixel at a time, so text in them
` is drawn at its own size and every pixel lands where it was drawn. the pictures are FontSmall and
` FontMid, in the textures. SCORE_FONT is baked big, for the titles, and for text that changes size.
` the three share one set of input icons.
global SCORE_FONT_16
global SCORE_FONT_20
"""
FONT_LINE = """    font SCORE_FONT, "Fish/Fonts/ZeroCool"
"""
LOAD = """
    ` the small fonts are pictures of 16 columns and 6 rows of letters, from the space to the ~.
    ` to change a letter, paint over it in the picture. the faint squares are only there to show
    ` where the cells are.
    SCORE_FONT_16 = 2
    texture reserve texture id(fontSmallTex), "Fish/Textures/FontSmall"
    font sheet SCORE_FONT_16, fontSmallTex, 16, 6
    SCORE_FONT_20 = 3
    texture reserve texture id(fontMidTex), "Fish/Textures/FontMid"
    font sheet SCORE_FONT_20, fontMidTex, 16, 6

    ` and they draw the icons of the big one. the set is shared, so the icons below go to all three.
    share font icons SCORE_FONT, SCORE_FONT_16
    share font icons SCORE_FONT, SCORE_FONT_20
"""
ICON_HEAD = "    ` every size of the font gets the same icons. see SCORE_FONT_16 in fish.\n    for iconFontI = 1 to SCORE_FONT_COUNT\n"

src = {}
for f in files:
    t = open(f, 'rb').read().decode('utf-8')
    src[f] = (t, '\r\n' if '\r\n' in t else '\n')

def L(s, n): return s.replace('\n', n)

if mode == 'in':
    assert not any('SCORE_FONT_16' in t for t, _ in src.values()), 'already in'
    sizes, plan = {}, {}
    for f, (t, n) in src.items():
        if 'fish_tests' in f: continue
        for m in re.finditer(r"^[ \t]*size text( [xy])? ([^,\r\n]+),[ \t]*([^`\r\n]*)", t, re.M):
            sizes.setdefault(m.group(2).strip(), set()).add(((m.group(1) or '').strip() + ' ' + m.group(3).strip()).strip())
    for i, s in sizes.items():
        want = {x for x in s if x in ('y 16', 'y 20')}
        if len(want) == 1 and not (s - want): plan[i] = next(iter(want))[2:]
    plan['moveAbility.keyTextId'] = '16'
    done = 0
    for f in files:
        t, n = src[f]; o = t; name = os.path.basename(f)
        if 'fish_tests' not in name:
            def rep(m):
                global done
                i = m.group(2).strip()
                if i not in plan: return m.group(0)
                done += 1
                return m.group(1) + m.group(2) + m.group(3) + 'SCORE_FONT_' + plan[i]
            t = re.sub(CREATE, rep, t, flags=re.M)
            # a line of the 20 font is 19 pixels tall, so those texts are sized to that, to be drawn 1:1
        if name == 'fish.fbasic':
            k = L("global SCORE_FONT\n", n); assert t.count(k) == 1
            t = t.replace(k, k + L(GLOBALS, n))
        if name == 'fish_routines.fbasic':
            k = L(FONT_LINE, n); assert t.count(k) == 1
            t = t.replace(k, k + L(LOAD, n))
        if t != o: open(f, 'wb').write(t.encode('utf-8'))
    print('switched', done, 'texts')
else:
    for f in files:
        t, n = src[f]; o = t; name = os.path.basename(f)
        t = re.sub(r"^([ \t]*text [^\r\n]*?)\bSCORE_FONT_(16|20),", r"\1SCORE_FONT,", t, flags=re.M)
        if name == 'fish.fbasic': t = t.replace(L(GLOBALS, n), '')
        if name == 'fish_routines.fbasic': t = t.replace(L(LOAD, n), '')
        if t != o:
            open(f, 'wb').write(t.encode('utf-8')); print('restored', name)
    left = [os.path.basename(f) for f in files if re.search(r'SCORE_FONT_|fontSmallTex', open(f, 'rb').read().decode('utf-8'))]
    print('left over:', left)
