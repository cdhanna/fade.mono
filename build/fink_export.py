"""Exports the dive boat of the main menu out of its Fink project, art/dive_boat.fink, into what the game loads.

    py -3 build/fink_export.py

Run it whenever the boat is redrawn in Fink, then push the pictures (build/push-assets.ps1) and commit the
.fink, the .fbasic and build/assets.lock. The game never reads the .fink, and nothing in CI runs Fink or this.

It writes, for every tag of the asset (one place the man can be on the boat):
    Fade.MonoGame/Assets/Fish/Textures/Menu/dive_boat_<tag>.png       in the evening (the base palette)
    Fade.MonoGame/Assets/Fish/Textures/Menu/dive_boat_<tag>_day.png   in the day (the variant "day")
and Fade.MonoGame/fish_art_boat.fbasic: the tags, in order, and where the point "man" is on each, in the
picture's own pixels. The hearts come up from there (see THE ENDING in fish_headers_diver).

A tag is drawn as its first frame. The layers are drawn bottom first, those that are hidden are left out,
and a layer that follows another (a fill under its lines) shows the drawings that one shows. This reads
the .fink as DESIGN.md in C:/proj/fink describes it (version 1).
"""
import json
import os
import sys
import zipfile

from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FINK = os.path.join(REPO, "art", "dive_boat.fink")
ASSET = "dive_boat"
POINT = "man"
DAY = "day"
TEXTURES = os.path.join(REPO, "Fade.MonoGame", "Assets", "Fish", "Textures", "Menu")
CODE = os.path.join(REPO, "Fade.MonoGame", "fish_art_boat.fbasic")
CONTENT = "Fish/Textures/Menu"


def rgba(hexa):
    h = hexa.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4, 6))


def main():
    z = zipfile.ZipFile(FINK)
    project = json.loads(z.read("fink.json"))
    if project.get("version") != 1:
        raise SystemExit(f"{FINK} is version {project.get('version')} of the .fink format, and this reads version 1")
    palettes = {"": [rgba(e) for e in project["palette"]["entries"]]}
    for variant in project["palette"].get("variants", []):
        if variant["name"] == DAY:
            palettes["_day"] = [rgba(e) for e in variant["entries"]]
    if "_day" not in palettes:
        raise SystemExit(f'{FINK} has no palette variant "{DAY}"')

    index = project["assets"].index(ASSET)
    asset = json.loads(z.read(f"assets/{index}/asset.json"))
    w, h = asset["width"], asset["height"]
    layers = asset["layers"]

    def drawing(li, frame):
        layer = layers[li]
        if layer.get("exposures") is None:
            follows = layer.get("follows")
            return None if follows is None else drawing(follows, frame)
        return layer["exposures"][frame]

    point = next((p for p in asset.get("points", []) if p["name"] == POINT), None)
    if point is None:
        raise SystemExit(f'{ASSET} has no point "{POINT}"')

    def point_at(frame):
        # a drawing that does not place the point takes it from the nearest frame before it that does, then after
        layer = layers[point["layer"]]
        placed = layer.get("points", {})
        order = list(range(frame, -1, -1)) + list(range(frame + 1, asset["frames"]))
        for f in order:
            d = drawing(point["layer"], f)
            spot = placed.get(str(d), {}).get(str(point["id"])) if d is not None else None
            if spot:
                return spot
        raise SystemExit(f'the point "{POINT}" is not placed on any drawing')

    tags = sorted(asset["tags"], key=lambda t: t["from"])
    if not tags:
        raise SystemExit(f"{ASSET} has no tags")
    places = []
    for tag in tags:
        frame = tag["from"]
        for ending, pal in palettes.items():
            im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
            px = im.load()
            for li, layer in enumerate(layers):
                if not layer.get("visible", True):
                    continue
                d = drawing(li, frame)
                if d is None:
                    continue
                cells = z.read(f"assets/{index}/L{li}_{d}.bin")
                for i, c in enumerate(cells):
                    if c:
                        px[i % w, i // w] = pal[c]
            path = os.path.join(TEXTURES, f"{ASSET}_{tag['name']}{ending}.png")
            im.save(path)
            print(os.path.relpath(path, REPO))
        places.append((tag["name"], point_at(frame)))

    lines = [
        f"` THE DIVE BOAT of the main menu, out of art/{os.path.basename(FINK)}. made by build/fink_export.py: run that",
        "` again, rather than editing this. every tag of the boat is a place on it that the man can be, and its",
        f"` pictures are {CONTENT}/{ASSET}_<place>.png, and _day.png for the middle of the day. BOAT_MAN_X and",
        f"` BOAT_MAN_Y are where the point \"{POINT}\" is on each, in the picture's own pixels ({w} by {h}).",
        f"#constant BOAT_PLACES {len(places)}",
        f"global dim BOAT_PLACE$({len(places)})",
        f"global dim BOAT_MAN_X({len(places)})",
        f"global dim BOAT_MAN_Y({len(places)})",
        f"global BOAT_ART_W = {w}",
        f"global BOAT_ART_H = {h}",
    ]
    for i, (name, (x, y)) in enumerate(places):
        lines += [f'BOAT_PLACE$({i}) = "{name}"', f"BOAT_MAN_X({i}) = {x}", f"BOAT_MAN_Y({i}) = {y}"]
    with open(CODE, "w", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    print(os.path.relpath(CODE, REPO), [p[0] for p in places])


if __name__ == "__main__":
    main()
