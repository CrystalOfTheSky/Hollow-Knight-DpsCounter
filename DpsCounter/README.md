# DPS Counter

A Hollow Knight mod that shows the player's damage per second on screen.

## How it works

The mod hooks `HealthManager.TakeDamage` (the standard damage pipeline),
`ExtraDamageable.ApplyExtraDamageToHealthManager` (Spore/Dung charm ticks) and
`SpellFluke.DoDamage` (Flukenest), then keeps a rolling window of recent player
damage. Every frame it draws `DPS <value>` at the configured screen position.

By default, Nail, NailBeam, Spell and SharpShadow damage is counted. Damage
from player charms (Grimmchild, Weaversong, Dreamshield, Spore Shroom,
Defender's Crest, etc.) is detected automatically, including Spore/Dung extra
damage ticks. Unattributed "Generic" damage can additionally be enabled in
settings (`CountGenericDamage`).

Note: damage that bypasses `HealthManager.TakeDamage` (most notably individual
Flukenest projectiles) is handled separately so Flukenest damage is included.

When an attack hits multiple enemies, the default is to count one enemy's
damage; set `CountDamageToAllTargets` (or use the pause-menu option) to sum
damage across every enemy hit.

## Install

1. Make sure the game is launched modded (Modding API is installed).
2. Copy the `DpsCounter` folder into:
   `Hollow Knight/hollow_knight.app/Contents/Resources/Data/Managed/Mods/`
3. Launch the game; the counter appears when you enter a gameplay scene.

## Controls

- `F6` (default) toggles the HUD on/off while playing. The key can be changed
  in the in-game menu or in the settings JSON.
- `F7` (default) resets the peak DPS value shown under the main reading.

## HUD

The counter shows two lines:

- `DPS x.xx` - rolling average damage per second.
- `Max x.xx` - the highest rolling DPS reached since the last manual reset or
  since returning to the main menu.

Both values are formatted to two decimal places.

## Pause menu

Open the pause menu -> Mods -> DPS Counter. Available options:

- Mod on/off toggle (unloads the mod).
- Show HUD (what the hotkey changes).
- Toggle key (None / F5-F9 / Keypad0-9).
- Reset Max key (None / F5-F9 / Keypad0-9).
- Counting window (1s / 2s / 3s / 5s / 10s).
- Count Generic damage (some charms/companions).
- Multi-Target Damage (Single target / All targets).
- Debug Logging (logs every damage event to ModLog.txt).
- HUD corner (Top-Right / Top-Left / Bottom-Right / Bottom-Left).

## Settings

After the first run, `DpsCounter.GlobalSettings.json` is created next to the
game's saves. Edit it while the game is closed to change:

- `Enabled` - show/hide the HUD.
- `ToggleKey` - key used while playing to toggle the HUD.
- `ResetMaxKey` - key used while playing to reset the peak DPS.
- `WindowSeconds` - rolling window used for the DPS average (default 3).
- `CountGenericDamage` - also count Generic attack-type damage.
- `CountDamageToAllTargets` - count every enemy hit by one attack (`true`) or
  only one enemy (`false`, default).
- `DebugLogDamage` - log every observed damage event to ModLog.txt for
  diagnosing missing damage sources.
- `FontSize` - HUD text size.
- `HudAnchorX` / `HudAnchorY` - screen anchor (0/0 bottom-left, 1/1 top-right).
- `HudOffsetX` / `HudOffsetY` - pixel offset from the anchor.
