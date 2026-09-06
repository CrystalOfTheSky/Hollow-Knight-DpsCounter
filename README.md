# Hollow Knight – DPS Counter

*：The entire project is done by vibe coding (DeepSeek-v4-Flash).

A Hollow Knight mod that reads the damage the player deals to enemies and shows
a rolling damage-per-second value on screen, with a peak-DPS line underneath.

Tested against Hollow Knight **1.5.78.11833** with the matching Modding API
build (v77).

## Features

- Counts Nail, NailBeam, Spell and SharpShadow damage dealt to enemies.
- Flukenest projectiles are counted through their own damage path.
- Optional "Generic" attack-type damage (some charms/companions), opt-in via
  settings because it can also cover non-player damage.
- Rolling DPS window, displayed to two decimal places (default 3 s).
- Peak DPS shown in a smaller second line, resettable with a hotkey.
- Hotkeys to show/hide the HUD and to reset the peak.
- Config screen inside the pause menu (Mods → DPS Counter).
- Position presets for all four screen corners, plus JSON-level control over
  anchor, offset and font size.

## Install

1. Make sure the game is launched modded (Modding API installed, e.g. via
   Lumafly).
2. Copy the `DpsCounter` folder (containing `DpsCounter.dll`) into the mods
   folder:

   ```text
   Hollow Knight/hollow_knight_Data/Managed/Mods/          (Windows/Linux)
   Hollow Knight/hollow_knight.app/Contents/Resources/Data/Managed/Mods/   (macOS)
   ```

3. Launch the game and enter a gameplay scene; the HUD appears at the chosen
   screen corner.

## Controls

| Key (default) | Action |
| --- | --- |
| `F6` | Show/hide the HUD |
| `F7` | Reset the peak DPS shown under the main reading |

Both keys can be changed in the pause-menu config screen or in the settings
JSON (`ToggleKey`, `ResetMaxKey`).

## HUD

```text
DPS 21.50
Max 28.33
```

`Max` is the highest rolling DPS reached since the last manual reset or since
returning to the main menu.

## Pause menu configuration

Open **Pause → Mods → DPS Counter**:

- Mod on/off toggle (unloads the mod).
- Show HUD.
- Toggle key (None / F5–F9 / Keypad0–9).
- Reset Max key.
- Counting window (1 s / 2 s / 3 s / 5 s / 10 s).
- Count Generic damage.
- HUD corner (Top-Right / Top-Left / Bottom-Right / Bottom-Left).

## Building from source

Requirements:

- .NET SDK 8+ (`dotnet --version`)
- A Hollow Knight installation with the Modding API installed; the modded
  `Assembly-CSharp.dll` (and `MMHOOK_Assembly-CSharp.dll`) are referenced at
  build time.

The csproj looks for the game's `Managed` folder in common Steam/GOG
installations automatically. If it can't find your install, pass the path
explicitly:

```bash
dotnet build DpsCounter/DpsCounter.csproj -c Release \
  -p:HollowKnightManaged="/absolute/path/to/hollow_knight_Data/Managed"
```

The built DLL is written to:

```text
DpsCounter/bin/Release/net472/DpsCounter.dll
```

## Settings file

After the first run, the mod writes `DpsCounter.GlobalSettings.json` next to the
game's saves (`~/Library/Application Support/unity.Team Cherry.Hollow Knight/`
on macOS). Usable keys:

- `Enabled`
- `ToggleKey` / `ResetMaxKey`
- `WindowSeconds`
- `CountGenericDamage`
- `FontSize`
- `HudAnchorX` / `HudAnchorY` / `HudOffsetX` / `HudOffsetY`

## Repository layout

```text
DpsCounter/
  DpsCounter.csproj        # mod project (net472)
  DpsCounterMod.cs         # hooks, HUD, hotkeys, pause menu
  DpsCounterSettings.cs    # persistent settings model
  README.md                # mod-local install/build notes
```

## License

MIT — see [LICENSE](LICENSE).
