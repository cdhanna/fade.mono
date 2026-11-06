"""
Writes the score font of the languages SuperCorn cannot draw (Assets/Fish/Fonts/ZeroCool-<family>.spritefont):

    cy   M PLUS Rounded 1c Black   Russian
    ja   M PLUS Rounded 1c Black   Japanese
    zh   Noto Sans SC Black        Chinese (simplified)
    ko   Jua                       Korean
    tr   SuperCorn, as ZeroCool    Turkish: the same font, but its small i is drawn as İ (fade:turkish),
                                   since SuperCorn draws small letters as capitals

The game loads the family its language needs over SCORE_FONT (fish_routines_menu.fbasic, applyLanguage),
and the build check knows which language each family draws from `fonts` in
Assets/Fish/Lang/strings.sheet.json. All three fonts are OFL (licenses/fonts/ at the root of the repo). Their sizes
hardly matter: the game sizes text by its line, and these fonts stand about as tall in their lines as
SuperCorn does (capitals about half the line), which is why they were picked.

Every family holds every character ZeroCool.spritefont bakes (so plain text the game draws works in any
language) plus every character of its languages' columns in Assets/Fish/Lang/*.csv: a CJK font holds only
the characters the text uses, since all of them would be tens of thousands. So after the Japanese,
Chinese or Korean text changes, run this again, then push the assets (build/push-assets.ps1):

    python Fade.MonoGame/Tools/make-fonts.py

The sizes are SuperCorn's 48 scaled so that capitals stand as tall, and each spritefont says fade:caps,
so small letters are drawn as capitals the way SuperCorn draws them (ComposedGlyphFontProcessor).
Only the standard library is used.
"""

import csv
import glob
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.dirname(HERE)
FONTS = os.path.join(GAME, "Assets", "Fish", "Fonts")
LANG = os.path.join(GAME, "Assets", "Fish", "Lang")
BASE = os.path.join(FONTS, "ZeroCool.spritefont")

# family: (font file in Assets/Fish/Fonts, size, languages whose text it must hold, extra ranges)
CJK_PUNCTUATION = [(0x3000, 0x303F), (0xFF01, 0xFF5E)]
FAMILIES = {
    "cy": ("MPLUSRounded1c-Black", 41, ["ru"], [(0x400, 0x45F)]),
    "ja": ("MPLUSRounded1c-Black", 41, ["ja"], CJK_PUNCTUATION),
    "zh": ("NotoSansSC-Black.otf", 41, ["zh-CN"], CJK_PUNCTUATION),
    "ko": ("Jua-Regular", 42, ["ko"], CJK_PUNCTUATION),
}


def base_regions():
    """the character regions of ZeroCool.spritefont"""
    with open(BASE, encoding="utf-8-sig") as f:
        text = f.read()
    out = []
    for a, b in re.findall(r"<Start>&#(\d+);</Start>\s*<End>&#(\d+);</End>", text):
        out.append((int(a), int(b)))
    return out


def used_characters(languages):
    """every character of these languages' columns in the string tables"""
    chars = set()
    for path in glob.glob(os.path.join(LANG, "*.csv")):
        with open(path, encoding="utf-8-sig", newline="") as f:
            for row in csv.DictReader(f):
                for language in languages:
                    chars.update(row.get(language) or "")
    return chars


def regions(fixed, chars):
    """the fixed ranges and the characters, as few ranges as they make"""
    codes = set()
    for a, b in fixed:
        codes.update(range(a, b + 1))
    codes.update(ord(c) for c in chars if 0x20 <= ord(c) <= 0xFFFF)
    out = []
    for code in sorted(codes):
        if out and code == out[-1][1] + 1:
            out[-1][1] = code
        else:
            out.append([code, code])
    return out


def write(family, font, size, ranges):
    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "<!-- Written by Tools/make-fonts.py: the score font for the %s languages. Do not edit; run it again." % family,
        "     fade:caps (small letters are drawn as capitals, like SuperCorn's) -->",
        '<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">',
        '  <Asset Type="Graphics:FontDescription">',
        "    <FontName>%s</FontName>" % font,
        "    <Size>%d</Size>" % size,
        "    <Spacing>2</Spacing>",
        "    <UseKerning>true</UseKerning>",
        "    <Style>Regular</Style>",
        "    <DefaultCharacter>?</DefaultCharacter>",
        "    <CharacterRegions>",
    ]
    for a, b in ranges:
        lines.append("      <CharacterRegion><Start>&#%d;</Start><End>&#%d;</End></CharacterRegion>" % (a, b))
    lines += ["    </CharacterRegions>", "  </Asset>", "</XnaContent>", ""]
    path = os.path.join(FONTS, "ZeroCool-%s.spritefont" % family)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))


def write_turkish():
    """ZeroCool.spritefont as it is, marked fade:turkish"""
    with open(BASE, encoding="utf-8-sig") as f:
        text = f.read()
    mark = ("<!-- Written by Tools/make-fonts.py from ZeroCool.spritefont, for Turkish. Do not edit; run it again.\n"
            "     fade:turkish (small i is drawn as İ) -->\n")
    at = text.index("<XnaContent")
    with open(os.path.join(FONTS, "ZeroCool-tr.spritefont"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text[:at] + mark + text[at:])
    print("tr: ZeroCool.spritefont, fade:turkish")


def main():
    write_turkish()
    base = base_regions()
    for family, (font, size, languages, extra) in FAMILIES.items():
        ranges = regions(base + extra, used_characters(languages))
        write(family, font, size, ranges)
        count = sum(b - a + 1 for a, b in ranges)
        print("%s: %s, %d characters in %d ranges" % (family, font, count, len(ranges)))


if __name__ == "__main__":
    main()
