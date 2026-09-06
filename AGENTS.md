# Hollow Knight DPS Counter — project notes

This file is read by Codex at the start of future sessions in this repository.
Keep it up to date when the project changes.

## What this project is

A Hollow Knight mod that shows the player's rolling damage per second on screen,
plus a peak-DPS line. Source of truth lives in this Git repo and on GitHub:

- GitHub: `CrystalOfTheSky/Hollow-Knight-DpsCounter`
- Current released version: `v0.3.1`

## Compatibility (important)

- Game: Hollow Knight **1.5.78.11833** (Steam)
- Modding API: **1.5.78.11833-77** (v77), merged into the game's
  `Assembly-CSharp.dll`; there is no separate `Modding.Modding.dll` in modern
  API builds.
- Mods compile as C# / .NET Framework class libraries (`net472`), referencing:
  `Assembly-CSharp.dll`, `MMHOOK_Assembly-CSharp.dll`, and the UnityEngine
  assemblies from the game's `Managed` folder.

Do not include Hollow Knight game files, decompiled game source, or the Modding
API source in the repository. The local `work/` and `outputs/` folders are
gitignored for exactly this reason.

## Building

The csproj auto-detects common Steam/GOG `Managed` paths, or override:

```bash
dotnet build DpsCounter/DpsCounter.csproj -c Release \
  -p:HollowKnightManaged="/absolute/path/to/hollow_knight_Data/Managed"
```

Output: `DpsCounter/bin/Release/net472/DpsCounter.dll`.

Version bumps are made in `DpsCounter/DpsCounter.csproj`
(`<Version>` / `<AssemblyVersion>` / `<FileVersion>`).

## Installing for a test

Copy the whole `DpsCounter` folder into:

```text
Hollow Knight/hollow_knight.app/Contents/Resources/Data/Managed/Mods/
```

On Windows/Linux the equivalent path is `hollow_knight_Data/Managed/Mods/`.

## Where to look for problems

- Mod log: `~/Library/Application Support/unity.Team Cherry.Hollow Knight/ModLog.txt`
- Global settings: `~/Library/Application Support/unity.Team Cherry.Hollow Knight/DpsCounter.GlobalSettings.json`

Ask the tester for these files when debugging.

## Architecture notes

- Damage is collected in `DpsCounter/DpsCounterMod.cs`:
  - `On.HealthManager.TakeDamage` handles the standard damage pipeline
    (Nail, NailBeam, Spell, SharpShadow, optional Generic).
  - `On.SpellFluke.DoDamage` handles Flukenest, which bypasses
    `HealthManager.TakeDamage`; the hook counts only the outermost call and
    mirrors vanilla's per-parent-chain damage checks to avoid double counting.
  - `On.ExtraDamageable.ApplyExtraDamageToHealthManager` handles Spore Shroom
    and Defender's Crest extra-damage ticks, which bypass the standard damage
    pipeline.
  - Generic damage is attributed to player charms via a HeroController parent
    check or known charm object names; `CountGenericDamage` opts into all
    Generic damage as a fallback.
- DPS is a rolling window (default 3 s): samples are `(Time.time, damage)`,
  old samples are pruned every frame, and
  `DPS = damage in window / window length`.
- Multi-target mode: when `CountDamageToAllTargets` is false (default), hits
  from the same source in the same frame are collapsed into one sample.
- The HUD is created with `CanvasUtil`: a corner-anchored container holds two
  `Text` rows (`DPS x.xx`, smaller `Max x.xx`, both two decimals).
- Peak DPS resets on the `ResetMaxKey` hotkey (default F7) or when returning
  to the main menu. `ToggleKey` (default F6) hides/shows the HUD.
- Pause-menu configuration uses the Modding API's `IMenuMod`
  (`GetMenuData`), with `ITogglableMod` for the in-menu on/off toggle.
- Persistent settings live in `DpsCounterSettings.cs` and are serialized to
  `DpsCounter.GlobalSettings.json` by the mod base class.

## Testing feedback received

- v0.3.0 behavior was verified by the user: general DPS readings match
  expectations; hotkey toggle/reset and the pause-menu config screen are in
  use. Fix regressions before changing behavior.
- v0.3.1 adds charm damage coverage and multi-target counting; it needs a user
  play test before being treated as verified.

## Release workflow

1. Build Release, copy `DpsCounter.dll`/`.pdb`/`README.md`/`LICENSE` into
   `outputs/DpsCounter/`.
2. Zip that folder as `outputs/DpsCounter-vX.Y.Z.zip`.
3. Commit and push source.
4. Tag `vX.Y.Z` and push the tag.
5. Create the GitHub Release from the pushed tag and attach the zip.
