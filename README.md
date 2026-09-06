# Hollow Knight – DPS Counter

A small Hollow Knight mod that reads the damage the player deals to enemies and
shows a rolling damage-per-second value on screen.

Tested against Hollow Knight 1.5.78.11833 with the matching Modding API build.

## Features

- Counts Nail, NailBeam, Spell and SharpShadow damage dealt to enemies.
- Optional support for "Generic" attack-type damage (some charms/companions) via
  settings.
- Rolling DPS window (default 3 seconds).
- On-screen text whose anchor, offset and font size are editable in a JSON
  settings file.

## Install from a release

Copy the `DpsCounter` folder (containing `DpsCounter.dll`) into the mods folder:

```text
Hollow Knight/hollow_knight_Data/Managed/Mods/   (Windows/Linux)
Hollow Knight/hollow_knight.app/Contents/Resources/Data/Managed/Mods/   (macOS)
```

Launch the game with Modding Support active (e.g. through Lumafly) and enter a
gameplay scene. The HUD appears at the configured screen position.

## Building from source

Requirements:

- .NET SDK 8+ (`dotnet --version`)
- A Hollow Knight installation with the Modding API installed (its modded
  `Assembly-CSharp.dll` is referenced at build time)

The project locates the game's `Managed` folder automatically for common Steam
locations. If it can't find your install, pass the path explicitly:

```bash
dotnet build DpsCounter/DpsCounter.csproj -c Release -p:HollowKnightManaged="/absolute/path/to/hollow_knight_Data/Managed"
```

The built DLL is written to:

```text
DpsCounter/bin/Release/net472/DpsCounter.dll
```

## Settings

After the first run, the mod writes `DpsCounter.GlobalSettings.json` next to the
game's saves (`~/Library/Application Support/unity.Team Cherry.Hollow Knight/`
on macOS). Edit it while the game is closed to change:

- `Enabled`
- `WindowSeconds`
- `CountGenericDamage`
- `FontSize`
- `HudAnchorX` / `HudAnchorY` / `HudOffsetX` / `HudOffsetY`

## Known limitations

- Individual Flukenest projectiles bypass `HealthManager.TakeDamage` and are not
  counted in this version.
- "Generic" attack type is opt-in because it also covers some non-player damage.

## License

MIT (add a LICENSE file with your name/year before publishing if you keep this
header).
