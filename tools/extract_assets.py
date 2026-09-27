#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Extract the Defect card portraits out of your own copy of Slay the Spire 1.

The card artwork in this mod comes from Slay the Spire 1 (it is (c) Mega Crit
Games, see LICENSE). The repository ships the extracted files so the port is
playable out of the box, but you can also regenerate them from a copy of the
game you own — either to replace them, or because your clone deliberately does
not contain them.

What it does:
    reads <SlayTheSpire>/desktop-1.0.jar
    ->  images/1024Portraits/blue/*.png   (76 Defect card portraits)
    ->  DefectClassic/images/card_portraits/{,big}/<slug>.png

The target file name is derived from the mod's card class names using the same
rule the game uses (MegaCrit.Sts2.Core.Helpers.StringHelper.Slugify):
    CamelCaseRegex.Replace(name, "$1_$2").ToUpperInvariant()  ->  lowercase
so `BallLightning` -> `ball_lightning.png`, `FTL` -> `f_t_l.png`,
`CreativeAI` -> `creative_a_i.png`.

Usage:
    python tools/extract_assets.py                 # auto-detect the game
    python tools/extract_assets.py --jar <path>    # point at desktop-1.0.jar
    python tools/extract_assets.py --check         # only verify what is present
"""

import argparse
import glob
import os
import re
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
CARDS_DIR = os.path.join(ROOT, "DefectClassicCode", "Cards")
OUT_DIRS = [
    os.path.join(ROOT, "DefectClassic", "images", "card_portraits"),
    os.path.join(ROOT, "DefectClassic", "images", "card_portraits", "big"),
]

# STS1 jar name -> this mod's name. Anything not listed here keeps its own name.
ALIASES = {
    "lock_on": "bullseye",            # the STS1 "LockOn" card is called Bullseye here
    "conserve_battery": "charge_battery",
    "hyper_beam": "hyperbeam",
    "forcefield": "force_field",
    "multicast": "multi_cast",
    "creative_ai": "creative_a_i",
    "ftl": "f_t_l",
    "gash": "claw",                   # STS1's internal name for Claw
}

IGNORE = {"lightning_mastery"}        # STS1 asset with no counterpart card here


def slug(name):
    """Verbatim reimplementation of MegaCrit.Sts2.Core.Helpers.StringHelper.Slugify."""
    s = name.strip()
    out, i, n, last_end = [], 0, len(s), 0
    while i < n:
        if i + 1 < n and s[i].isalnum() and s[i + 1].isupper():
            out.append(s[i] + "_" + s[i + 1])
            i += 2
            last_end = i
        elif i == last_end and i != 0 and s[i].isupper():
            out.append("_" + s[i])
            i += 1
            last_end = i
        else:
            out.append(s[i])
            i += 1
    return "".join(out).upper()


def wanted_names():
    """{expected png stem} for every card class in the project."""
    names = {}
    for fn in sorted(os.listdir(CARDS_DIR)):
        if not fn.endswith(".cs") or fn == "DefectClassicCard.cs":
            continue
        cls = fn[:-3]
        names[slug(cls).lower()] = cls
    return names


def find_jar():
    """Locate desktop-1.0.jar via Steam library folders."""
    candidates = []
    try:
        import winreg
        for hive, sub in [(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam"),
                          (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam"),
                          (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Valve\Steam")]:
            for value in ("SteamPath", "InstallPath"):
                try:
                    with winreg.OpenKey(hive, sub) as k:
                        p, _ = winreg.QueryValueEx(k, value)
                    if p and os.path.isdir(p):
                        candidates.append(os.path.join(p, "steamapps", "libraryfolders.vdf"))
                except OSError:
                    continue
    except ImportError:
        pass
    home = os.path.expanduser("~")
    candidates += [
        r"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf",
        r"C:\Program Files\Steam\steamapps\libraryfolders.vdf",
        os.path.join(home, ".steam", "steam", "steamapps", "libraryfolders.vdf"),
        os.path.join(home, ".local", "share", "Steam", "steamapps", "libraryfolders.vdf"),
        os.path.join(home, "Library", "Application Support", "Steam", "steamapps", "libraryfolders.vdf"),
    ]
    libraries = []
    for vdf in candidates:
        if not os.path.exists(vdf):
            continue
        libraries.append(os.path.dirname(vdf))
        try:
            text = open(vdf, encoding="utf-8", errors="replace").read()
        except OSError:
            continue
        for m in re.finditer(r'"path"\s+"([^"]+)"', text):
            libraries.append(os.path.join(m.group(1).replace("\\\\", "\\"), "steamapps"))
    if os.name == "nt":
        for drive in "CDEFGH":
            for depth in range(0, 3):
                libraries += glob.glob("%s:/%ssteamapps" % (drive, "*/" * depth))
    for lib in libraries:
        jar = os.path.join(lib, "common", "SlayTheSpire", "desktop-1.0.jar")
        if os.path.exists(jar):
            return jar
    return None


def check():
    names = wanted_names()
    missing_total = 0
    for d in OUT_DIRS:
        have = {f[:-4].lower() for f in os.listdir(d) if f.endswith(".png")} if os.path.isdir(d) else set()
        missing = sorted(cls for stem, cls in names.items() if stem not in have)
        rel = os.path.relpath(d, ROOT)
        if missing:
            missing_total += len(missing)
            print("  %-46s missing %d: %s" % (rel, len(missing), ", ".join(missing[:8]) + (" ..." if len(missing) > 8 else "")))
        else:
            print("  %-46s all %d portraits present" % (rel, len(names)))
    return missing_total


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--jar", help="path to Slay the Spire 1's desktop-1.0.jar")
    ap.add_argument("--check", action="store_true", help="only report which portraits are present")
    args = ap.parse_args()

    if args.check:
        print("Checking card portraits:")
        missing = check()
        return 0 if missing == 0 else 1

    jar = args.jar or find_jar()
    if not jar or not os.path.exists(jar):
        print("[error] Could not find desktop-1.0.jar (Slay the Spire 1).")
        print("        Pass it explicitly:  python tools/extract_assets.py --jar <path>")
        return 1

    print("source : %s" % jar)
    names = wanted_names()
    extracted, skipped = 0, []

    with zipfile.ZipFile(jar) as z:
        entries = [n for n in z.namelist()
                   if n.startswith("images/1024Portraits/blue/") and n.endswith(".png")]
        for entry in entries:
            stem = os.path.basename(entry)[:-4]
            if stem in IGNORE:
                continue
            target = ALIASES.get(stem, stem)
            if target not in names:
                skipped.append(stem)
                continue
            data = z.read(entry)
            for d in OUT_DIRS:
                if not os.path.isdir(d):
                    os.makedirs(d)
                with open(os.path.join(d, target + ".png"), "wb") as fh:
                    fh.write(data)
            extracted += 1

    print("wrote  : %d portraits -> %s" % (extracted, ", ".join(
        os.path.relpath(d, ROOT) for d in OUT_DIRS)))
    if skipped:
        print("skipped: %s (no matching card in this mod)" % ", ".join(sorted(skipped)))

    print("\nVerifying:")
    missing = check()
    if missing:
        print("\n[warning] %d portraits still missing — cards without art will render blank." % missing)
        return 1
    print("\nDone.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
