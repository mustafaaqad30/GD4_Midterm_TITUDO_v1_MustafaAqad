# TITUDØ — Multiplayer Card Game

**TITUDØ** is a two-player online multiplayer card game developed using Unity 6 and C# as a university Game Design 4 midterm project.

The game combines number cards, strategic decision-making, special abilities, and a boxing mechanic in a competitive multiplayer experience.

## 1. Project Overview

Players compete across a match consisting of 8 rounds. The objective is to use number cards strategically while managing the target number, special abilities, and mana.

Players must consider the risk of drawing additional cards and avoid exceeding the target number.

### Main Features

- Two-player online multiplayer
- Eight-round match system
- Target number selection
- Heads or Tails coin toss
- Number card drawing
- Mana-based skill system
- Six special abilities
- Player scores and round tracking
- Boxing mechanic affecting the losing player
- Multiplayer lobby and ready system

## 2. Gameplay

### Starting a Match

1. Both players connect to the online multiplayer lobby.
2. Each player indicates that they are ready.
3. The match begins when both players are ready.
4. A target number is selected.
5. A Heads or Tails coin toss determines the starting player.
6. Players begin drawing cards and using their available abilities.

### Number Cards

Players draw number cards that contribute to their current total.

The objective is to manage the total strategically without exceeding the target number.

### Mana and Skills

Mana is used to activate special abilities during gameplay.

The game includes six skills:

| Skill | Description |
|---|---|
| Skill Destroyer | Interferes with an opponent's skill |
| Target Breaker | Modifies the target number |
| Double Draw | Provides an additional card-drawing effect |
| Turn Block | Restricts an opponent's turn |
| Total Booster | Increases a player's total |
| Second Chance | Provides protection against exceeding the target on a draw |

Skill activation depends on available mana and gameplay conditions.

### Rounds and Scoring

The match consists of 8 rounds.

Player scores, card totals, the target number, and the current round are displayed in the gameplay interface.

The game also includes a boxing mechanic associated with round outcomes.

## 3. Technologies Used

- Unity 6
- C#
- Unity Netcode for GameObjects
- Unity Relay
- Unity Multiplayer Services
- Blender and Autodesk Maya for 3D assets

## 4. How to Run the Project

### Requirements

- Unity Hub
- A compatible Unity 6 Editor installation
- Internet connection for online multiplayer
- Two game instances or two devices for multiplayer testing

### Opening the Project

1. Download or clone this GitHub repository.
2. Open Unity Hub.
3. Select **Add → Add project from disk**.
4. Select the folder containing `Assets`, `Packages`, and `ProjectSettings`.
5. Open the project using the Unity version specified in `ProjectSettings/ProjectVersion.txt`.
6. Allow Unity to import the project assets and resolve dependencies.
7. Check the Unity Console for errors before starting.

### Running the Game

1. Open the `MainMenu` scene.
2. Press Play in the Unity Editor or launch a compatible build.
3. Complete any required Unity Services authentication.
4. Create or join a multiplayer session using the available lobby controls.
5. Share the session join code with the second player when prompted.
6. Both players enter the lobby and mark themselves ready.
7. Start the match and play the card game.

**Important:** Online multiplayer requires correctly configured Unity Gaming Services and Relay access. The project may require service configuration before it can be tested from a different Unity account.

## 5. Known Issues

The following issues were observed during development and multiplayer testing.

### 5.1 Gray Overlay Covering the Gameplay

**Issue:** A gray, semi-transparent overlay made the 3D environment and cards difficult to see.

**Identified cause:** The background Image component of the GamePanel.

**Workaround:** Set the GamePanel Image color Alpha to 0 while keeping the panel and its child UI elements active.

**Status:** Fix applied during development; final multiplayer verification is recommended.

### 5.2 Target Selection Repeats

**Issue:** The target selection interface may appear again in later rounds, although the intended design is to select the target once at the beginning of the match.

**Status:** Known gameplay-flow issue pending verification or correction.

### 5.3 Coin Toss Repeats

**Issue:** The Heads or Tails sequence may repeat between rounds.

**Expected behavior:** The coin toss should occur only once at the beginning of the match.

**Status:** Known gameplay-flow issue pending verification or correction.

### 5.4 UI Layout and Readability

**Issue:** Some interface elements may have alignment or readability problems depending on screen resolution and gameplay state.

**Status:** Prototype UI; additional layout refinement may be required.

## 6. Project Status

TITUDØ is a university midterm prototype. Its primary focus is implementing and demonstrating the multiplayer gameplay loop, card mechanics, player interactions, and skill system.

Further improvements may include gameplay balancing, interface refinement, visual polish, and additional multiplayer testing.

## 7. Developer

**Mustafa Aqad**

Game Design 4 — Midterm Project

Developed with Unity 6 and C#.