#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Defect Classic —— build / pack helper.

Why this exists (and why you should not just run `dotnet publish`):

    The .csproj defines a `GodotPublish` target that lets MegaDot write the final
    `.pck` **directly into the game's mods folder**:

        MegaDot --headless --export-pack "BasicExport" "<mods>/DefectClassic.pck"

    Godot's exporter writes `<name>.pck<random>.tmp` first and then renames it over
    the target. If that target is locked (game still running, antivirus scanning the
    ~12 MB file, a previous export process still alive), it retries forever and
    MSBuild hangs with it — `dotnet publish` simply never returns.

    This script takes over: compile with dotnet, export the .pck to a staging file
    *inside the same folder*, verify it, then atomically replace the target. It also
    salvages a leftover valid `*.tmp` from a previously stuck publish.

Usage:
    python pack.py            # compile only, update the .dll (seconds)
    python pack.py --pck      # compile + re-export the .pck (needed after asset changes)

Path discovery: everything below can be overridden with environment variables
(DOTNET_ROOT, MEGADOT_PATH, STS2_PATH) or a local.props file.
"""

import glob
import os
import re
import shutil
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.abspath(__file__))
MODID = "DefectClassic"
PRESET = "BasicExport"
BUILD_TIMEOUT = 420
EXPORT_TIMEOUT = 900

IS_WIN = os.name == "nt"


def log(msg=""):
    print(msg, flush=True)


def first_existing(paths):
    for p in paths:
        if p and os.path.exists(p):
            return p
    return None


# --------------------------------------------------------------- discovery

def _dotnet_exe(folder):
    return os.path.join(folder, "dotnet.exe" if IS_WIN else "dotnet")


def _has_net9(dotnet):
    """The game targets net9.0, so we need a 9.x SDK — a 8.x one will not build it."""
    try:
        out = subprocess.run([dotnet, "--list-sdks"], capture_output=True, text=True,
                             timeout=90).stdout or ""
    except Exception:
        return False
    return any(line.strip().startswith("9.") for line in out.splitlines())


def find_dotnet():
    home = os.path.expanduser("~")
    candidates = [
        os.environ.get("DOTNET_ROOT"),
        os.path.dirname(shutil.which("dotnet") or ""),
        r"C:\Program Files\dotnet",
        os.path.join(home, ".dotnet9"),
        os.path.join(home, ".dotnet"),
        "/usr/share/dotnet",
        "/usr/local/share/dotnet",
    ]
    seen = []
    for folder in candidates:
        if not folder:
            continue
        exe = _dotnet_exe(folder)
        if exe in seen or not os.path.exists(exe):
            continue
        seen.append(exe)
        if _has_net9(exe):
            return exe
    # Nothing had a 9.x SDK — return the first one that exists so the error is useful.
    return seen[0] if seen else None


def find_megadot():
    home = os.path.expanduser("~")
    names_win = ["MegaDot_v4.5.1-stable_mono_win64_console.exe",
                 "MegaDot_v4.5.1-stable_mono_win64.exe"]
    candidates = [os.environ.get("MEGADOT_PATH"), os.environ.get("GODOT_PATH")]
    for name in names_win:
        candidates += [
            os.path.join(ROOT, "megadot", name),
            os.path.join(ROOT, "tools", "megadot", name),
            r"C:\megadot" + "\\" + name,
        ]
    candidates += [
        os.path.join(home, "megadot", "MegaDot_v4.5.1-stable_mono_linux.x86_64"),
        "/usr/lib/megadot/MegaDot_v4.5.1-stable_mono_linux.x86_64",
        "/Applications/Megadot.app/Contents/MacOS/Godot",
    ]
    # Also honour whatever the csproj/props file says.
    try:
        props = open(os.path.join(ROOT, "Directory.Build.props"), encoding="utf-8").read()
        for m in re.finditer(r"<GodotPath>([^<]+)</GodotPath>", props):
            candidates.insert(0, m.group(1).replace("/", os.sep))
    except OSError:
        pass
    return first_existing(candidates)


def _steam_from_registry():
    """Steam's install folder, straight from the registry (Windows only)."""
    if not IS_WIN:
        return None
    try:
        import winreg
    except ImportError:
        return None
    hives = [
        (winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam"),
        (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam"),
        (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Valve\Steam"),
    ]
    for hive, sub in hives:
        for value in ("SteamPath", "InstallPath"):
            try:
                with winreg.OpenKey(hive, sub) as k:
                    path, _ = winreg.QueryValueEx(k, value)
                if path and os.path.isdir(path):
                    return path
            except OSError:
                continue
    return None


def _steam_libraries():
    """Best-effort list of `steamapps` folders, from libraryfolders.vdf if we can find it."""
    roots = []
    home = os.path.expanduser("~")
    vdf_candidates = []
    steam_root = _steam_from_registry()
    if steam_root:
        vdf_candidates.append(os.path.join(steam_root, "steamapps", "libraryfolders.vdf"))
    vdf_candidates += [
        r"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf",
        r"C:\Program Files\Steam\steamapps\libraryfolders.vdf",
        r"C:\SteamLibrary\steamapps\libraryfolders.vdf",
        os.path.join(home, ".steam", "steam", "steamapps", "libraryfolders.vdf"),
        os.path.join(home, ".local", "share", "Steam", "steamapps", "libraryfolders.vdf"),
        os.path.join(home, "Library", "Application Support", "Steam", "steamapps", "libraryfolders.vdf"),
    ]
    for vdf in vdf_candidates:
        if not os.path.exists(vdf):
            continue
        roots.append(os.path.dirname(vdf))
        try:
            text = open(vdf, encoding="utf-8", errors="replace").read()
        except OSError:
            continue
        for m in re.finditer(r'"path"\s+"([^"]+)"', text):
            roots.append(os.path.join(m.group(1).replace("\\\\", "\\"), "steamapps"))
    return roots


def _scan_common_roots():
    """Last resort: look for `<something>/steamapps/common/Slay the Spire 2` a few levels deep."""
    if not IS_WIN:
        return []
    seen = []
    for drive in "CDEFGH":
        for depth in range(0, 4):
            pattern = "%s:/%ssteamapps/common/Slay the Spire 2" % (drive, "*/" * depth)
            for hit in glob.glob(pattern):
                if hit not in seen:
                    seen.append(hit)
    return seen


def find_sts2():
    env = os.environ.get("STS2_PATH")
    if env and os.path.exists(env):
        return env
    for props in ("local.props", "Directory.Build.props"):
        try:
            text = open(os.path.join(ROOT, props), encoding="utf-8").read()
        except OSError:
            continue
        for m in re.finditer(r"<Sts2Path>([^<]+)</Sts2Path>", text):
            p = m.group(1).replace("/", os.sep)
            if os.path.exists(p):
                return p

    candidates = []
    for lib in _steam_libraries():
        candidates.append(os.path.join(lib, "common", "Slay the Spire 2"))
    candidates += _scan_common_roots()
    if not IS_WIN:
        home = os.path.expanduser("~")
        candidates += [
            os.path.join(home, ".local/share/Steam/steamapps/common/Slay the Spire 2"),
            os.path.join(home, "Library/Application Support/Steam/steamapps/common/Slay the Spire 2"),
        ]
    return first_existing(candidates)


# --------------------------------------------------------------- pck helpers

def pck_entries(path):
    try:
        data = open(path, "rb").read()
    except OSError:
        return []
    return [m.decode() for m in re.findall(rb"res://[ -~]+?\.(?:png|json|cfg)", data)]


def describe(path):
    if not os.path.exists(path):
        return "missing"
    entries = pck_entries(path)
    big = {e for e in entries if "/card_portraits/big/" in e}
    return "%s bytes / %d entries / %d big card portraits" % (
        format(os.path.getsize(path), ","), len(entries), len(big))


def salvage_pending_pck(mods):
    """If a previous publish died mid-rename, a complete .tmp is left behind. Use it."""
    tmps = glob.glob(os.path.join(mods, "*.pck*.tmp"))
    if not tmps:
        return False
    target = os.path.join(mods, MODID + ".pck")
    for t in tmps:
        try:
            with open(t, "rb") as fh:
                if fh.read(4) != b"GDPC":
                    log("  . skipping invalid leftover %s" % os.path.basename(t))
                    continue
            if os.path.exists(target) and os.path.getsize(t) < os.path.getsize(target):
                log("  . skipping smaller leftover %s" % os.path.basename(t))
                continue
            os.replace(t, target)
            log("  v salvaged: %s -> target pck" % os.path.basename(t))
            return True
        except OSError as e:
            log("  x could not replace %s: %s" % (os.path.basename(t), e))
    return False


# --------------------------------------------------------------- runner

def run(cmd, timeout, cwd=ROOT):
    env = os.environ.copy()
    dotnet = find_dotnet()
    if dotnet:
        env["DOTNET_ROOT"] = os.path.dirname(dotnet)
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_NOLOGO"] = "1"
    env["MSBUILDDISABLENODEREUSE"] = "1"
    try:
        p = subprocess.run(cmd, cwd=cwd, env=env, timeout=timeout,
                           stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                           text=True, encoding="utf-8", errors="replace")
        return p.returncode == 0, False, (p.stdout or "")[-4000:]
    except subprocess.TimeoutExpired as e:
        out = e.stdout or ""
        if isinstance(out, bytes):
            out = out.decode("utf-8", "replace")
        return False, True, out[-4000:]


def main():
    want_pck = "--pck" in sys.argv
    t0 = time.time()

    dotnet = find_dotnet()
    if not dotnet:
        log("[error] Could not find a .NET SDK. Install .NET 9 SDK and either put it on PATH")
        log("        or set DOTNET_ROOT.  https://dotnet.microsoft.com/download/dotnet/9.0")
        return 1

    sts2 = find_sts2()
    if not sts2:
        log("[error] Could not locate 'Slay the Spire 2'. Set the STS2_PATH environment")
        log("        variable, or uncomment <Sts2Path> in Directory.Build.props.")
        return 1
    mods = os.path.join(sts2, "mods", MODID)
    if not os.path.isdir(mods):
        try:
            os.makedirs(mods)
        except OSError as e:
            log("[error] Cannot create %s: %s" % (mods, e))
            return 1

    log("dotnet  : %s" % dotnet)
    log("game    : %s" % sts2)
    log("mods out: %s" % mods)

    log("\n=== 0/3 leftovers ===")
    if not salvage_pending_pck(mods):
        log("  . nothing to salvage")

    log("\n=== 1/3 build ===")
    ok, timed_out, out = run([dotnet, "build", "%s.csproj" % MODID, "-v:m",
                              "--no-incremental", "-p:UseSharedCompilation=false"],
                             BUILD_TIMEOUT)
    warns = [l for l in out.splitlines() if "warning" in l.lower()]
    if not ok:
        log("build failed:")
        for l in out.strip().splitlines()[-20:]:
            log("  " + l.strip())
        return 1
    log("  v build ok (%d warnings, %.0fs)" % (len(warns), time.time() - t0))
    for l in warns[:5]:
        log("    " + l.strip())

    if not want_pck:
        log("\n=== 2/3 pck export skipped (no --pck) ===")
    else:
        log("\n=== 2/3 export pck ===")
        megadot = find_megadot()
        if not megadot:
            log("  x MegaDot not found. Download it from https://megadot.megacrit.com/")
            log("    (version must be 4.5.1) and set MEGADOT_PATH or GodotPath in local.props.")
            return 1
        final = os.path.join(mods, MODID + ".pck")
        # Godot only accepts an export path ending in .pck, so the staging file needs that suffix too.
        staging = os.path.join(mods, MODID + "_building.pck")
        if os.path.exists(staging):
            try:
                os.remove(staging)
            except OSError:
                pass

        log("  exporting with %s ..." % os.path.basename(megadot))
        ok, timed_out, out = run([megadot, "--headless", "--path", ROOT,
                                  "--export-pack", PRESET, staging], EXPORT_TIMEOUT)
        if os.path.exists(staging) and os.path.getsize(staging) > 1024:
            log("  v exported: %s" % describe(staging))
            try:
                os.replace(staging, final)
                log("  v replaced %s.pck" % MODID)
            except OSError as e:
                log("  x replace failed (is the game still running?): %s" % e)
                return 1
        else:
            log("  x no staging pck produced (timeout=%s)" % timed_out)
            for l in out.strip().splitlines()[-12:]:
                log("    " + l.strip())
            if not salvage_pending_pck(mods):
                return 1

    log("\n=== 3/3 mods folder ===")
    for f in sorted(glob.glob(os.path.join(mods, "*"))):
        st = os.stat(f)
        extra = "  " + describe(f) if f.endswith(".pck") else ""
        log("  %12s  %s  %s%s" % (format(st.st_size, ","),
                                  time.strftime("%H:%M:%S", time.localtime(st.st_mtime)),
                                  os.path.basename(f), extra))

    log("\n[done] %.0fs. Launch the game and enable the mod in Settings -> Modding." % (time.time() - t0))
    return 0


if __name__ == "__main__":
    sys.exit(main())
