# Cuphead 3-Player Co-op Mod

An advanced, balanced, crash-free 3-player cooperative multiplayer modification for Cuphead and The Delicious Last Course DLC.

Built from scratch through two years of reverse-engineering Cuphead's engine assembly and Rewired input pipeline. Battle-tested end-to-end: completed a full 3-player playthrough from Inkwell Isle I through the Devil and Chef Saltbaker with zero crashes.

[Polska wersja instrukcji (README_PL.md)](README_PL.md)

---

## Features

Cuphead includes early internal code for extra player slots, but lacks balance, controller separation, Player 3 loadout management, and multiplayer crash fixes. This mod provides those missing systems:

* **3-Player Co-op Anywhere**: Play locally on one PC, or remotely using Steam Remote Play Together or Parsec.
* **Controller Routing**: Assigns pads automatically without Rewired conflicts. Works with 3 gamepads or 1 keyboard plus 2 gamepads.
* **Dynamic Damage Scaling**: Player damage scales with surviving teammates (0.35x for 3 players, 0.50x for 2 players, 1.00x for 1 player). When a teammate dies, surviving players immediately deal more damage.
* **Hardcore Co-op Mode**: If a fallen player's ghost floats off-screen without being parried, the level restarts automatically.
* **Boss Timeline Card**: In Hardcore mode, shows the boss progress timeline card for 3 seconds before restarting so the team sees how close they were.
* **In-Game Settings Menu [F5]**: Chalkboard menu navigable with keyboard, gamepad, or mouse. Configure Player 3 weapons, charms (including Ms. Chalice), base model, and starting HP (1 to 3 HP per player).
* **Live Stats, MVP Scorecard and 187 Achievements**: Tracks damage dealt, parries, revives, supers, deaths, and victories. Awards post-fight MVP honors (Golden Ram, Guardian Angel, Iron Shell).
* **Quick Restart [R] / [L3]**: Hold Left Stick (L3) or keyboard [R] for 1 second to restart a fight without opening the pause menu.
* **Vanilla Bug Fixes**: Fixes boss targeting crashes (KeyNotFoundException in PlayerManager), Chess Castle King's Leap interactions, Mr. Wheezy hazard spawns, DLC boat transition softlocks, and overworld Chalice victory portal errors.

---

## Controls

| Key | Action | Description |
| :--- | :--- | :--- |
| **[F1]** | Toggle P1 Keyboard-Only | Detaches gamepads from Player 1, freeing controllers for P2 and P3. |
| **[F2]** | Force Join Player 2 | Forces Player 2 (Mugman) to join on the first free gamepad. |
| **[F3]** | Force Join Player 3 | Forces Player 3 (Ms. Chalice / Green Tint) to join on the next gamepad. |
| **[F4]** | Auto Re-pair Controllers | Resets controller bindings: P1=KB/Pad1, P2=Pad2, P3=Pad3. |
| **[F5]** | Settings Menu | Opens the loadout, difficulty, and stats configuration menu. |
| **[F6]** | Toggle Hardcore Mode | Toggles automatic level restart when a ghost floats off-screen. |
| **[R] / [L3]** | Quick Restart | Hold Left Stick (L3) or [R] for 1 second to restart the boss fight. |
| **[V]** | Boss Victory Scorecard | Re-opens the last boss victory scorecard on the overworld map. |

---

## Hardware and Controller Setup

### Setup 1: 3 Gamepads (Local, Parsec, or Remote Play)
1. Plug in 3 controllers (Xbox, PlayStation, or generic direct-input controllers).
2. Launch Cuphead.
3. If button mappings cross over, press **[F4]** to map:
   * Gamepad 1 -> Player 1 (Cuphead / Red)
   * Gamepad 2 -> Player 2 (Mugman / Blue)
   * Gamepad 3 -> Player 3 (Ms. Chalice / Green Tint)

### Setup 2: 1 Keyboard + 2 Gamepads
1. Player 1 uses keyboard.
2. Press **[F1]** once. Toast notification confirms: `Cuphead (P1) na KLAWIATURZE. Pady zwolnione dla P2 i P3!`.
3. Players 2 and 3 connect their controllers and press any button (or press **[F2]** / **[F3]**).
4. Player 2 controls Mugman, and Player 3 controls the third character.

---

## In-Game Settings Menu [F5]

Press **[F5]** during any level or on the overworld map to open the menu:

### Tab 1: Player 3 Loadout and Health
* **Shot-A and Shot-B**: Peashooter, Spread, Chaser, Lobber, Charge, Roundabout, Crackshot, Converge, Twist-Up.
* **Super Art**: Super I (Beam), Super II (Invincibility), Super III (Giant Ghost).
* **Charm**: Smoke Bomb, Heart, Coffee, P. Sugar, Twin Heart, Astral Cookie (Ms. Chalice), Whetstone, Heart Ring, Broken / Cursed / Divine Relic.
* **Ms. Chalice Toggle**: Instant switch to Ms. Chalice with Astral Cookie.
* **Base Character Model**: Select Cuphead or Mugman animation model for Player 3 when not playing as Chalice.
* **Starting Health**: Set starting HP (1, 2, or 3 HP) individually or with 1-click presets.
* **Copy Loadout**: Synchronize Player 3 equipment with Player 1.

### Tab 2: Gameplay and Modifiers
* **Damage Presets**:
  * *Easy*: 2 Players = 0.75x, 3 Players = 0.50x.
  * *Normal (Default)*: 2 Players = 0.50x, 3 Players = 0.35x.
  * *Hard*: 2 Players = 0.35x, 3 Players = 0.25x.
* **Parry Time Stop**: Toggle the micro-freeze during parries on or off for smoother co-op.
* **Hardcore Mode**: Toggle level restart when a ghost escapes off-screen.
* **Boss Timeline Preview**: Set how long (in seconds) the boss timeline displays before auto-restarting.
* **Quick Restart Duration**: Set hold time for [L3] / [R] restarts (0.4s to 3.0s).

### Tab 3: Stats and 187 Achievements
* Tracks damage dealt, parries, revives, deaths, supers, and boss knockouts.
* 187 achievements covering team rescues, expert challenges, and boss scenarios.
* Review the last boss victory scorecard or reset saved stats.

---

## Installation

1. Install **BepInEx 5.4.x (x64)** into your Cuphead game folder (where `Cuphead.exe` is located).
2. Download the latest release from the [Releases](https://github.com/Meloon33/Cuphead-3-Player-Coop/releases) page.
3. Extract `Cuphead3PCoop.dll` into `Cuphead/BepInEx/plugins/`.
4. Launch `Cuphead.exe`.

---

## Building from Source

This project compiles with the Roslyn C# compiler (`csc.exe`) without package manager dependencies:

1. Clone this repository:
   ```bash
   git clone https://github.com/Meloon33/Cuphead-3-Player-Coop.git
   ```
2. Open `src/build.bat`. If your Cuphead folder is in a custom path, set `CUPHEAD_GAME_DIR`:
   ```cmd
   set CUPHEAD_GAME_DIR=C:\Path\To\Cuphead
   build.bat
   ```
3. Compiled output writes to `src/Cuphead3PCoop.dll` and `release/`.

---

## License

Distributed under the [MIT License](LICENSE).  
Cuphead is a registered trademark of Studio MDHR Entertainment Inc. This project is an independent open-source fan mod not affiliated with Studio MDHR.
