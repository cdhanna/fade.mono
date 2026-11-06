"""
Writes the trailer experiments: one cut for each, into build/cuts/, made from the baseline cut
(build/trailer_cut.json) by moving, trimming, adding and taking away its shots. They all come out into
trailer-clips/experiments/.

    py -3 build/trailer_experiments.py                  write the cuts
    py -3 build/trailer_experiments.py --render         and make every one of them
    py -3 build/trailer_experiments.py --render e03     or only the ones whose names start with that

The captions are short videos of their own (see build/trailer_captions.py). EXPERIMENTS, at the bottom,
says what each experiment is for.
"""
import copy
import json
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
BASE = json.load(open(os.path.join(HERE, "trailer_cut.json"), encoding="utf-8"))
OUT = os.path.join(HERE, "cuts")


def base_shot(clip):
    for entry in BASE["shots"]:
        if entry.get("clip") == clip:
            return copy.deepcopy(entry)
    raise KeyError(clip)


def shot(clip, **changes):
    """a shot of the baseline, with changes. a change to None takes that setting away."""
    try:
        entry = base_shot("trailer_" + clip)
    except KeyError:
        entry = {"clip": "trailer_" + clip}
    for key, value in changes.items():
        if value is None:
            entry.pop(key, None)
        else:
            entry[key] = value
    return entry


def caption(name):
    return {"clip": "trailer_cap_" + name, "transition": "fade", "out": -6}


def faded(entry):
    """a shot that comes in with a fade, in place of whatever it came in with"""
    entry = dict(entry)
    entry["transition"] = "fade"
    entry.pop("sync", None)
    return entry


BASELINE = ["first_pop", "where_do_fish_live", "upgrade_rays", "stingray_chain", "night_held", "coral_sings",
            "quest_grid", "horror_ghosts", "info_horror", "angler_horror", "to_highscores", "fish_live_here"]


def baseline(*leave_out, **changes):
    """the shots of the baseline, in order, without the ones named, and with changes: name=dict"""
    shots = []
    for name in BASELINE:
        if name in leave_out:
            continue
        entry = shot(name)
        entry.update(changes.get(name, {}))
        shots.append(entry)
    return shots


def insert_before(shots, clip, *new):
    at = next(n for n, entry in enumerate(shots) if entry.get("clip") == "trailer_" + clip)
    return shots[:at] + list(new) + shots[at:]


def insert_after(shots, clip, *new):
    at = next(n for n, entry in enumerate(shots) if entry.get("clip") == "trailer_" + clip)
    return shots[:at + 1] + list(new) + shots[at + 1:]


TIGHT = dict(where_do_fish_live={"out": 640}, night_held={"in": 100}, to_highscores={"out": 175})

EXPERIMENTS = []


def experiment(name, title, goal, why, shots, music="exp_clair_echo", **cut):
    EXPERIMENTS.append({"name": name, "title": title, "goal": goal, "why": why, "shots": shots, "music": music, "cut": cut})


experiment("e01_hook_first", "Hook first", "appeal",
           "Store pages and feeds give a trailer about three seconds. The baseline opens on four fish on black, which is lovely but slow. "
           "This opens on the biggest thing the game does: a late-game board, multiplier high, a huge swap popping half the screen. "
           "Then it drops into the quiet first pop and 'Come, friend.', so the calm reads as a choice, not as all there is.",
           [shot("late_game_combo", **{"in": 60, "out": 230})] + [faded(shot("first_pop"))] + baseline("first_pop", "stingray_chain"))

experiment("e02_tight", "Tight cut", "appeal",
           "The same story, about 15 seconds shorter. The stingray chain, the info box and the angler go; the title, the night and the "
           "leaderboard are trimmed. Every remaining shot is one the trailer needs. A test of how much the length costs us.",
           baseline("stingray_chain", "info_horror", "angler_horror", **TIGHT))

experiment("e03_explain_the_loop", "Explain the loop", "accuracy",
           "A viewer of the baseline sees pretty fish, but not what you do. Short captions in the game's own title style, between the "
           "sections, say it plainly: a new card every round, at night nothing pops, meet the locals, not everything here is friendly, "
           "thirty rounds and how few moves. Built on the tight cut, without the coral shot, so it does not run long.",
           insert_before(insert_before(insert_before(insert_before(insert_before(
               baseline("stingray_chain", "coral_sings", "info_horror", "angler_horror", **TIGHT),
               "upgrade_rays", caption("cards")), "night_held", caption("night")), "quest_grid", caption("locals")),
               "horror_ghosts", caption("deep")), "fish_live_here", caption("moves")))

experiment("e04_power_climax", "Power climax", "accuracy",
           "A run is a climb: by round 20 the multiplier is huge and one swap clears the screen. The baseline never shows that, so the "
           "game reads as smaller than it is. This puts the late-game combo after the quest grid, as the peak, right before the horror turns it.",
           insert_after(baseline("stingray_chain"), "quest_grid", shot("late_game_combo", transition="bubbles", **{"in": 40, "out": 330})))

experiment("e05_meet_the_locals", "Meet the locals", "accuracy",
           "The quests are the game's personality: creatures that ask for things. The grid shows sixteen at once, but small. This adds two "
           "of them at full size, in play, first: the shark that hunts the cursor, and the tail that the cursor drags. Then the grid.",
           insert_before(baseline("stingray_chain", "info_horror", **TIGHT), "quest_grid",
                         caption("locals"), faded(shot("shark", sync={"mark": "swap"}, **{"in": 40})),
                         shot("tail", transition="bubbles", **{"in": 60, "out": 330})))

experiment("e06_the_diver", "The diver", "accuracy",
           "The diver is the one human down here, and part of the game's story: the diver comes at night, and takes pictures. The "
           "baseline only shows the diver far off in the night shot. This gives the diver a shot of their own, taking a photo, just before the night.",
           insert_before(baseline("stingray_chain"), "night_held", shot("diver_photo", transition="bubbles", sync={"mark": "swap"})))

experiment("e07_quiet_promise", "The quiet promise", "appeal",
           "The game's best promise is that it does not rush you: there is no timer. Saying that early, right after 'Come, friend.', tells "
           "the people who want a calm game that this is theirs, before the horror turns up later to surprise them.",
           insert_after(baseline("stingray_chain"), "where_do_fish_live", caption("quiet")))

experiment("e08_secrets_forward", "Secrets forward", "accuracy",
           "The secrets are what people will talk about. 'Listen closely. The sea is full of secrets.' comes in just as the Listen card is "
           "taken, so the next shot, the hint going along the reef, reads as the first secret. 'Not everything down here is friendly' "
           "sets up the horror.",
           insert_before(insert_before(baseline("stingray_chain", **TIGHT), "coral_sings", caption("listen")),
                         "horror_ghosts", caption("deep")))

experiment("e09_vertical_30", "Vertical, 30 seconds", "appeal",
           "For phones: Shorts, TikTok, Reels. 9 by 16, the board in the middle and a blurred sea around it, about thirty seconds: the first "
           "pop and the title, an upgrade, Finn the shark talking, the horror, and the end card.",
           [shot("first_pop"), shot("where_do_fish_live", out=420),
            shot("upgrade_rays", **{"in": {"mark": "wave", "before": 20}, "out": 450}),
            shot("quest_01", transition="bubbles", out=230),
            shot("horror_ghosts"),
            shot("fish_live_here", transition="fade", **{"in": 420})],
           vertical=True)

experiment("e10_bumper_15", "Fifteen-second bumper", "appeal",
           "For ads and the top of a store page: the late-game pop, Finn talking, the horror, and the end card, in about fifteen seconds.",
           [shot("late_game_combo", **{"in": 70, "out": 190}),
            shot("quest_01", transition="fade", **{"in": 20, "out": 170}),
            shot("horror_ghosts", transition="bubbles", **{"in": 40, "out": 170}),
            shot("fish_live_here", transition="fade", **{"in": 600})],
           music="exp_clair_echo_late")

experiment("e11_sound_forward", "Sound forward", "accuracy",
           "In the game, the sound is the pops, the bubbles and the creatures, with the music under them. In the baseline the music leads. "
           "This turns the music down and the game up, so it sounds like playing it.",
           baseline("stingray_chain"), music_gain=0.55, sfx_volume=0.85)

experiment("e12_how_few_moves", "How few moves", "accuracy",
           "The score is how few moves a run takes, and that is not obvious. The leaderboard stays up longer, with its Moves column, and "
           "'Thirty rounds. How few moves will you need?' follows it, before the ocean title.",
           insert_after(baseline("stingray_chain", to_highscores={"out": 260}), "to_highscores", caption("moves")))


def write():
    os.makedirs(OUT, exist_ok=True)
    for exp in EXPERIMENTS:
        cut = {key: copy.deepcopy(value) for key, value in BASE.items() if key not in ("shots", "output", "clips", "music_variant")}
        cut["about"] = {"title": exp["title"], "goal": exp["goal"], "why": exp["why"]}
        cut["clips"] = "../../trailer-clips"
        cut["output"] = f"../../trailer-clips/experiments/{exp['name']}.mp4"
        cut["music_variant"] = f"../music/{exp['music']}.json"
        for key, value in exp["cut"].items():
            if key == "music_gain":
                cut["music"]["music_gain"] = value
            else:
                cut[key] = value
        cut["shots"] = exp["shots"]
        with open(os.path.join(OUT, exp["name"] + ".json"), "w", encoding="utf-8") as f:
            json.dump(cut, f, indent=4)
            f.write("\n")
    print(f"{len(EXPERIMENTS)} experiments -> {OUT}")


if __name__ == "__main__":
    write()
    if "--render" in sys.argv:
        only = [a for a in sys.argv[1:] if not a.startswith("--")]
        for exp in EXPERIMENTS:
            if only and not any(exp["name"].startswith(o) for o in only):
                continue
            print("==", exp["name"])
            subprocess.run([sys.executable, os.path.join(HERE, "trailer_cut.py"), "--cut", os.path.join(OUT, exp["name"] + ".json")])
