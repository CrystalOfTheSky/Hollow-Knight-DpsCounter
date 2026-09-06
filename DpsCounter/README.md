# DPS Counter

A Hollow Knight mod that shows the player's damage per second on screen.

## How it works

The mod hooks `HealthManager.TakeDamage` (the same damage pipeline used by the
game) and keeps a rolling window of recent player damage. Every frame it draws
`DPS <value>` at the configured screen position.

By default, Nail, NailBeam, Spell and SharpShadow damage is counted. Damage
from charms/companions that is delivered with the "Generic" attack type can be
enabled in settings (`CountGenericDamage`).

Note: damage that bypasses `HealthManager.TakeDamage` (most notably individual
Flukenest projectiles) is not counted in this first version.

## Install

1. Make sure the game is launched modded (Modding API is installed).
2. Copy the `DpsCounter` folder into:
   `Hollow Knight/hollow_knight.app/Contents/Resources/Data/Managed/Mods/`
3. Launch the game; the counter appears when you enter a gameplay scene.

## Settings

After the first run, `DpsCounter.GlobalSettings.json` is created next to the
game's saves. Edit it while the game is closed to change:

- `Enabled` - show/hide the HUD.
- `WindowSeconds` - rolling window used for the DPS average (default 3).
- `CountGenericDamage` - also count Generic attack-type damage.
- `FontSize` - HUD text size.
- `HudAnchorX` / `HudAnchorY` - screen anchor (0/0 bottom-left, 1/1 top-right).
- `HudOffsetX` / `HudOffsetY` - pixel offset from the anchor.
