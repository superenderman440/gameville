# Gameville

A minimal Minecraft-inspired Unity project scaffold with Lua integration for Linux.

## What this includes

- `Assets/Scripts/LuaManager.cs` — manages Lua execution and exposes block spawning to Lua.
- `Assets/Scripts/BlockSpawner.cs` — triggers Lua world generation.
- `Assets/Scripts/PlayerController.cs` — simple first-person movement and block placement.
- `Assets/StreamingAssets/Lua/game_logic.lua` — Lua-driven world generation.
- `Packages/manifest.json` — minimal Unity package manifest.
- `ProjectSettings/ProjectVersion.txt` — basic Unity project version marker.

## Setup

1. Install the Unity Editor for Linux.
2. Open this folder as a Unity project.
3. Open `Assets/Scenes/MainScene.unity` to load the sample scene.
4. Add MoonSharp to the project:
   - Import MoonSharp from OpenUPM or add the MoonSharp Unity package.
   - If you want to use the source directly, place it under `Assets/Plugins/MoonSharp`.
5. Use the included `Assets/Prefabs/Block.prefab` as the cube block prefab.
6. Select the `LuaGameManager` object in the scene:
   - Attach `LuaManager` and `BlockSpawner` if they are not already attached.
   - Assign `Assets/StreamingAssets/Lua/game_logic.lua` to the `luaScript` field.
   - Assign the `Block` prefab to `blockPrefab` and assign a `worldRoot` transform.
7. Select the `Player` object in the scene:
   - Attach `PlayerController` and `PlayerSkinController`.
   - Assign the `Camera` to `cameraTransform`, assign the `Block` prefab to `blockPrefab`, and assign a `worldRoot` transform.
8. Create a new empty GameObject named `MultiplayerManager` or reuse `LuaGameManager`:
   - Attach `MultiplayerManager`.
   - Set `localPlayer` to the `Player` object.
   - Set `playerPrefab` to the same `Player` object (or a dedicated player prefab).
   - Set `blockPrefab` and `worldRoot` to match the Lua world setup.
   - Use `isHost = true` on the machine that starts the game server, and `isHost = false` on connecting clients.
   - If `isHost = false`, set `connectAddress` to the host IP.
9. Optional: attach `SceneAutoSetup` to any object in the scene to auto-wire the Player, camera, world root, and multiplayer references.
10. Open `File > Build Settings`, select `Linux`, add the scene, and build.
   - Set `localPlayer` to the `Player` object.
   - Set `playerPrefab` to the same `Player` object (or a dedicated player prefab).
   - Set `blockPrefab` and `worldRoot` to match the Lua world setup.
   - Use `isHost = true` on the machine that starts the game server, and `isHost = false` on connecting clients.
   - If `isHost = false`, set `connectAddress` to the host IP.
8. Open `File > Build Settings`, select `Linux`, add the scene, and build.

## Controls

- `WASD` — move
- Mouse — look around
- `Left click` — place a block in the world
- `Tab` — cycle player skins
- `F1` — toggle network diagnostics panel (debug overlay)

## Included sample assets

- `Assets/Scenes/MainScene.unity` — sample scene with a Main Camera, Directional Light, Player, and LuaGameManager.
- `Assets/Prefabs/Block.prefab` — sample block prefab for Lua-driven block placement.
- `Assets/Scripts/SceneAutoSetup.cs` — optional runtime helper that auto-wires scene references for player, camera, world root, and multiplayer.
- `Assets/Scripts/GameLoader.cs` — game initialization and loader orchestrating all systems (items, mobs, abilities, skins).
- `Assets/Scripts/SkinManager.cs` — Tynker-compatible skin loader supporting both color-based and texture-based skins from JSON configuration.
- `Assets/Scripts/NetworkDiagnostics.cs` — optional debug overlay showing host/client mode and connection status; press F1 to toggle.
- `Assets/Scripts/Inventory.cs` — inventory management system with item stacking and JSON-based item database.
- `Assets/Scripts/AbilityManager.cs` — ability system with mana management, cooldown tracking, and JSON-based ability definitions.
- `Assets/Scripts/Mob.cs` — mob AI with health, damage, pathfinding, loot drops, and ability usage.
- `Assets/Scripts/MobSpawner.cs` — mob spawner with JSON-based mob definitions and randomized spawning.
- `Assets/Scripts/MountSystem.cs` — mount system for rideable mobs like the Shrieking Fence.
- `Assets/Scripts/PlayerHealth.cs` — player health tracking with damage and healing mechanics.
- `Assets/Scripts/ScreenShake.cs` — camera shake and tilt system for ability telegraphs.
- `Assets/StreamingAssets/Skins/skins.json` — skin configuration file defining available player skins (includes new Mojang Pink skin).
- `Assets/StreamingAssets/Skins/README.md` — instructions for adding custom texture skins.
- `Assets/StreamingAssets/Items/items.json` — item database with weapons, armor, consumables, and misc items.
- `Assets/StreamingAssets/Mobs/mobs.json` — mob definitions with stats, loot tables, abilities, boss encounters, and behavior parameters.
- `Assets/StreamingAssets/Abilities/abilities.json` — ability definitions with damage, cooldown, mana cost, and effects.

## How it works

- `LuaManager` loads `game_logic.lua` at runtime.
- The Lua script calls `SpawnBlock(x, y, z)` to place blocks.
- `PlayerController` allows WASD movement and mouse look.
- `SkinManager` provides runtime skins with Tynker texture support and `Tab` cycles through available skins.
- `MultiplayerManager` shares player movement, skin changes, and block placement across connected clients.

## Tynker Skin Support & Custom Skins

Skins are defined in `Assets/StreamingAssets/Skins/skins.json` and support two types:

1. **Color-based skins** (type: 0): defined with RGBA values
   - Default White, Deep Red, Sky Blue, Forest Green, Purple

2. **Texture-based skins** (type: 1): references PNG files in the Skins directory
   - Tynker Texture
   - **Mojang Pink** (new!) — Pink background with magenta coloring

### Using Skins

Press Tab to cycle through all available skins. The game will automatically load both color and texture skins.

### Adding Custom Tynker Skins

1. Create a 64x64 PNG image and place it in `Assets/StreamingAssets/Skins/`
2. Edit `skins.json` to add entries
3. For the Mojang Pink skin, place `mojang_pink.png` in the Skins directory (see README.md in Skins folder for format details)
4. Each player can cycle through all available skins with `Tab` during gameplay

## Game Initialization & Loader

The `GameLoader` component orchestrates the initialization of all game systems. It loads configurations from JSON files and prepares the game for play.

### Setup

1. Create an empty GameObject in your scene (e.g., name it `GameManager`)
2. Attach the `GameLoader` component to it
3. Enable `autoLoadOnAwake` (checked by default)
4. Enable `debugLog` to see initialization messages in the console

### Features

- **Auto-initialization**: On scene start, `GameLoader` automatically:
  - Loads all abilities from `abilities.json`
  - Loads all items from `items.json`
  - Loads all mobs from `mobs.json`
  - Loads all skins from `skins.json`
  - Logs status messages to console

- **Manual trigger**: Call `gameLoaderInstance.LoadGame()` to reload all systems at runtime

### Console Output Example

```
[GameLoader] Starting game initialization...
[GameLoader] Loaded Abilities: 7 entries
[GameLoader] Loaded Items: 8 entries
[GameLoader] Loaded Mobs: 16 entries
[GameLoader] Loaded Skins: 7 entries
[GameLoader] ✓ Game initialization complete!
```

## Item System

Items are defined in `Assets/StreamingAssets/Items/items.json`. Each item has:

- `id` — unique identifier
- `name` — display name
- `type` — weapon, armor, consumable, tool, or misc
- `damage` — weapon damage value
- `defense` — armor defense value
- `healAmount` — consumable healing value
- `description` — item description

Consumables and misc items stack in inventory; weapons and armor do not.

## Mob System

Mobs are defined in `Assets/StreamingAssets/Mobs/mobs.json`. Each mob has:

- `id` — unique identifier
- `name` — display name
- `maxHealth` — mob health
- `damage` — attack damage
- `speed` — movement speed
- `attackRange` — melee attack range
- `sightRange` — detection range
- `experienceReward` — XP when defeated
- `abilities` — list of ability IDs this mob can use
- `lootTable` — list of possible drops with chances

### Boss Encounters (Strongest to Weakest)

The game features 8 epic boss battles with escalating difficulty:

| Boss Name | Health | DMG | Abilities | Experience | Special |
|---|---|---|---|---|---|
| **Owl Eye** | 350 | 55 | All 5 | 2,000 | Ultimate boss |
| **All Seeing Eye** | 300 | 48 | 3 damage | 1,500 | Massive sight |
| **The Deepslate Monstrosity** | 260 | 42 | 3 mixed | 1,200 | Heavy hitter |
| **The Omni Dragon** | 220 | 38 | All damage | 1,000 | Flying threat |
| **The Omni Golem** | 180 | 32 | 3 abilities | 800 | Ranged attacks |
| **Colossal Alex** | 140 | 28 | Dash/Ambush | 600 | Quick strikes |
| **Giant Steve** | 120 | 25 | 3 abilities | 500 | Melee combat |
| **Shrieking Fence** | 80 | 12 | Dash/Fireball | 200 | **MOUNTABLE!** |

Each boss drops significant loot (5-100 coins + rare items) and grants substantial experience rewards!

### Mount System

The **Shrieking Fence (Warden Nautilus)** is a special mountable mob that serves as both a boss encounter and a travel mount:

- **How to Mount:** Approach the mob and press V (customizable keybind)
- **Speed Boost:** Gain 1.5x movement speed while mounted
- **How to Dismount:** Press V again while mounted
- **Combat Use:** Tactical positioning and rapid escape
- **Implementation:** See `Assets/Scripts/MountSystem.cs`

Enable rapid traversal across the map for strategic repositioning!

### Adding Custom Bosses & Mobs

Edit `mobs.json` to add new mob types:

```json
{
  "id": 5,
  "name": "Dragon",
  "maxHealth": 200,
  "damage": 30,
  "speed": 3.0,
  "attackRange": 3.0,
  "sightRange": 30.0,
  "experienceReward": 500,
  "abilities": [2, 5],
  "lootTable": [
    {
      "itemId": 3,
      "dropChance": 0.5,
      "minQuantity": 1,
      "maxQuantity": 1
    }
  ]
}
```

## Ability System

The game features a flexible ability system with 6 abilities available to players and mobs:

### Player Abilities (Keybindings)
- **Q** — Dash (Utility) - Quick movement ability
- **E** — Fireball (Damage) - Deal fire damage at range
- **F** — Makeshift Muffin (Heal) - Restore health
- **R** — Ambush (Damage) - High damage strike
- **T** — Rage of Ao (Buff) - Massive power boost
- **Y** — Blade of Retribution (Damage) - Swift blade strike

### Ability Stats
Each ability has:
- `damage` — Damage dealt
- `healing` — Health restored
- `cooldown` — Seconds before reuse
- `cost` — Mana cost (players start with 100 mana, regenerate 10/sec)
- `range` — Effect range for targeting
- `duration` — Buff/debuff duration
- `description` — Ability description

### Mob Abilities
Mobs have 30% chance to use an ability during combat instead of basic attacks. Each mob type uses specific abilities:
- **Goblin** — Dash (1), Ambush (4)
- **Orc** — Fireball (2), Rage of Ao (5), Blade of Retribution (6)
- **Skeleton** — Fireball (2), Blade of Retribution (6)
- **Zombie** — Ambush (4), Rage of Ao (5)

### Customizing Abilities

Edit `Assets/StreamingAssets/Abilities/abilities.json` to modify ability stats or add new ones:

```json
{
  "id": 7,
  "name": "Ice Storm",
  "type": "damage",
  "damage": 45,
  "healing": 0,
  "cooldown": 8.0,
  "cost": 50,
  "range": 20.0,
  "duration": 0,
  "description": "Summon a devastating ice storm"
}
```

## Usage

1. **Inventory**: Attach `Inventory` component to the player to enable item collection.
2. **Mobs**: Create a mob prefab with `Mob` component; attach `MobSpawner` to spawn them.
3. **Player Health**: Attach `PlayerHealth` to the player for damage/healing mechanics.
4. **Abilities**: Press Q/E/F/R/T/Y to cast abilities; requires sufficient mana and cooldown ready.

## Notes

- This scaffold is designed for Linux and Unity Editor usage.
- You can extend the Lua script to add more terrain, inventory, and block rules.
