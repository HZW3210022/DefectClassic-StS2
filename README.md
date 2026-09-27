# Defect Classic — for Slay the Spire 2

> **中文说明请看 [README.zh-CN.md](README.zh-CN.md)**

Brings the **original Defect** from *Slay the Spire 1* — all **75 cards**, the orb
system, Focus, and Cracked Core — into *Slay the Spire 2* as a **standalone 6th
character**, with the original card values and behaviour rather than STS2's reworked
Defect.

---

## Why a separate character?

STS2 already ships with a Defect (alongside Ironclad, Silent, Necrobinder and Regent),
but its card pool was **redesigned**: Focus is mostly temporary, a 5th orb type (Glass)
was added, and several numbers were rebalanced. Recreating the STS1 pool inside that
character would mean fighting the official pool, so this mod adds a **6th character**
that lives beside it. Both can coexist.

## Features

- **All 75 STS1 Defect cards.** Every card from `com.megacrit.cardcrawl.cards.blue.*`
  except `Impulse`, an unimplemented test card that has no localisation entries.
- **Original numbers.** Base values and upgrade deltas are read straight out of STS1's
  own compiled classes (decompiled from `desktop-1.0.jar`), not copied from STS2's
  reworked versions — e.g. 24-damage Sunder, 3-orb-slot Defect, permanent Focus.
- **Orb system fully reused.** `OrbCmd.Channel` / `Evoke`, all orb types,
  `OrbQueue`, the `ModifyOrbValue` hook and per-character `CardPoolModel` already exist
  in STS2 — no custom orb engine needed.
- **3 orb slots, 75 HP, Cracked Core**, starting deck 4×Strike / 4×Defend / Zap / Dualcast.
- **Official STS1 localisation** for English and Simplified Chinese (card names and
  descriptions as printed in the original game).
- **Harmony patches** for the two mechanics STS2's public API can't express directly:
  Electrodynamics (Lightning hits ALL enemies) and Lock-On (target takes +50% damage
  from orbs, decaying each round). Both patches fail soft — if they can't apply, the
  card degrades to vanilla behaviour and logs a warning instead of breaking the mod.
- **Zero custom art pipeline.** The character reuses STS2's own Defect visuals
  (portrait, energy dial, campfire, shop, SFX) via `PlaceholderCharacterModel`.
- **Built-in test command.** `dc pool` dumps the whole card pool into your draw pile so
  you can play through everything in a couple of turns (see *Debugging* below).

## Requirements

| | |
|---|---|
| **Slay the Spire 2** | v0.111.0 or newer (Early Access) |
| **BaseLib** | 3.4.7+ — subscribe on the Steam Workshop, ids in `DefectClassic.json` |
| **.NET 9 SDK** | only for building from source |

## Installation (players)

1. Subscribe to **BaseLib** on the Steam Workshop.
2. Create `…/steamapps/common/Slay the Spire 2/mods/DefectClassic/` and drop
   `DefectClassic.dll`, `DefectClassic.json` and `DefectClassic.pck` into it.
3. Launch the game, open **Settings → Modding**, make sure `DefectClassic` is enabled,
   and start a new run as **Defect Classic**.

> Modded and vanilla saves are kept in separate folders by the game, so your normal
> progress is untouched.

## Building from source

You need the **.NET 9 SDK** and — only if you are going to re-export the `.pck` —
**MegaDot 4.5.1**, Mega Crit's custom Godot build. Its version *must* match the game's
Godot version or the game will refuse to load the pack.

```
git clone https://github.com/HZW3210022/DefectClassic-StS2
cd DefectClassic-StS2

python pack.py            # compile only  → updates the .dll   (a few seconds)
python pack.py --pck      # compile + re-export the .pck        (after asset changes)
```

On Windows you can just double-click **`build.bat`** (`build.bat publish` for the pck).

`pack.py` auto-detects the .NET SDK, the game folder (Steam registry +
`libraryfolders.vdf`) and MegaDot. Override any of them with the environment variables
`DOTNET_ROOT`, `STS2_PATH`, `MEGADOT_PATH`, or with a `local.props` file:

```xml
<Project><PropertyGroup>
  <GodotPath>D:/tools/MegaDot_v4.5.1-stable_mono_win64.exe</GodotPath>
  <Sts2Path>D:/Steam/steamapps/common/Slay the Spire 2</Sts2Path>
</PropertyGroup></Project>
```

> **Close the game before building.** While it is running the files in the mods folder
> are locked and the `.pck` cannot be replaced. (This is the reason `pack.py` exists at
> all: the `GodotPublish` MSBuild target lets Godot write the `.pck` *directly* into the
> mods folder, and if that file is locked it retries forever and `dotnet publish` hangs
> without ever returning. `pack.py` exports to a staging file and swaps it atomically.)

## Debugging & testing

STS2 has a **full developer console** built in, and it is enabled automatically
whenever the game is running modded:

```csharp
bool shouldAllowDebugCommands = OS.HasFeature("editor")
    || TestMode.IsOn
    || ModManager.IsRunningModded()      // ← you are here
    || SaveManager.Instance.SettingsSave.FullConsole;
```

Press <kbd>`</kbd> (backquote, below <kbd>Esc</kbd>) in a run to open it.
<kbd>F11</kbd> fullscreen · <kbd>Esc</kbd> close · <kbd>Tab</kbd> autocomplete ·
<kbd>↑</kbd> previous command.

Useful vanilla commands: `godmode`, `energy 99`, `draw 10`, `upgrade 0`, `kill all`,
`card DEFECTCLASSIC-ZAP`, `help`.

**This mod also registers its own command:**

| Command | What it does |
|---|---|
| `dc list` | list every card ID from this mod |
| `dc pool [pile]` | put **all 75 cards** into a pile (default: draw pile) |
| `dc hand <card-id> [count]` | put N copies of a card into your hand (`dc hand claw 5`) |

Fastest way to test everything: `godmode` → `energy 99` → `dc pool` → `draw 10` →
play the hand → `draw 10` → repeat.

Card IDs are the character prefix plus the class name in screaming snake case, e.g.
`DEFECTCLASSIC-ZAP`, `DEFECTCLASSIC-BOOT_SEQUENCE`, `DEFECTCLASSIC-CREATIVE_A_I`.

## Project layout

```
DefectClassicCode/
  Cards/            75 card classes (one file each)
  Character/        the character + card/relic/potion pools
  Powers/           powers that STS2 does not provide
  Patches/          Harmony patches (Electrodynamics, Lock-On)
  Dev/              the `dc` console command
DefectClassic/
  images/           card portraits, character UI, power icons
  localization/     "The Architect" dialogue required by the mod analyzer
tools/
  extract_assets.py pulls the card portraits out of your own copy of STS1
```

## Credits and assets

- **Card code, character definition, patches, build scripts** — written for this mod.
- **Card names and descriptions** — the official Slay the Spire 1 localisation text.
- **Card portraits** — extracted from *Slay the Spire 1*. `Slay the Spire`, its cards,
  artwork and text are © **Mega Crit Games**. No ownership is claimed over them; they
  are redistributed here only so that the port is playable. If you own STS1 you can
  regenerate them yourself with `tools/extract_assets.py`.
- **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** by Alchyr — the modding library
  this project is built on (`CustomCardModel`, `PlaceholderCharacterModel`, automatic
  registration and localisation).
- **MegaDot** by Mega Crit — the Godot build used for `.pck` export.

## License

The **source code** in this repository is released under the **MIT License** — see
[LICENSE](LICENSE).

The **Slay the Spire 1 assets** (card artwork, card text) are **not** covered by that
license and remain the property of Mega Crit Games. This project is a fan work and is
not affiliated with or endorsed by Mega Crit.
