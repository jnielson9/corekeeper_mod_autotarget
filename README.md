# AutoTarget

A combat targeting mod for Core Keeper that adds auto-aim and manual target locking.

## Features

**Auto-Aim** — when active, all attacks automatically aim toward the nearest valid enemy within range. Works with ranged weapons, beams, and lunge attacks. Melee facing direction can also be overridden (configurable).

**Target Lock** — manually pin a specific enemy by pressing the lock key while hovering your cursor over it. All attacks aim at the locked target until you release it or the target dies. The lock clears automatically on death or despawn.

**Visual Rings** — active targets are highlighted with a coloured circle ring:
- Gold ring: manually locked target
- Cyan ring: auto-aim target (hidden while a lock is held)

Friendly entities are never targeted — this includes minions, pets, and summons that share your faction (such as the Phantom Spark ghost).

## Installation

To install the mod you can either subscribe to it on mod.io or download and install it manually.

### Mod.io installation

Subscribe either through the website or the in-game [mods] tab.

This mod requires Elevated Permissions to work — you will be prompted with that information after restarting the game.

If you subscribe on the mod.io website, you still need to open the [mods] tab in game for it to download and install.

#### Updating

Go into the game's [mods] tab and click [collection] at the top of the screen. The [AutoTarget] mod should begin updating automatically. Restart the game once it finishes.

### Manual installation

Download the file from the mod.io website, unpack it as a folder into:

```
CoreKeeper_Data\StreamingAssets\Mods
```

inside your Core Keeper installation directory.

#### Updating

Follow the installation instructions above.

## Issues

Please report any issues here: https://github.com/jnielson9/corekeeper_mod_autotarget/issues

## Default Keybinds

| Key | Action |
|-----|--------|
| `F` | Toggle auto-aim on / off |
| `T` | Lock enemy under cursor / release lock |

## Configuration

Settings are saved to `AutoTarget_config.json` in the game's persistent data folder:

```
%AppData%\..\LocalLow\Pugstorm\Core Keeper\
```

The file is created automatically on first run with default values. Edit it with any text editor while the game is closed.

| Option | Default | Description |
|--------|---------|-------------|
| `autoAimEnabled` | `false` | Whether auto-aim starts enabled when you load the game |
| `targetingRange` | `15` | Radius in world units (~tiles) to scan for enemies |
| `highlightEnabled` | `true` | Show the circle rings on targets |
| `overrideMeleeFacing` | `true` | Also redirect melee facing direction toward the target |
| `toggleAutoAimKeyCode` | `70` | KeyCode int for the auto-aim toggle (70 = F) |
| `lockTargetKeyCode` | `84` | KeyCode int for target lock (84 = T) |
| `debug` | `false` | Log targeting events to the game log for troubleshooting |

Keybinds use Unity [KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) integer values.

## Contribution
To work on the mod from the CoreKeeperModSDK, add this project as a submodule\clone into `CoreKeeperModSDK/Assets/AutoTarget`:

```
git submodule add https://github.com/jnielson9/corekeeper_mod_autotarget.git .\Assets\AutoTarget
```

## Notes

- The mod is client-side only; it does not affect other players.
- Auto-aim state (on/off) is remembered between sessions.
- Target lock is cleared when switching worlds or returning to the menu.
