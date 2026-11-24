# NOT COMPILED

# Saranggola Royale · Laban ng Bayan

Unity migration source is in `unity/`. The archived Godot project, its JSON, design notes, and historical APKs are in `legacy_godot/`, which is write-protected in this workspace. The Unity editor and Android build modules were unavailable here. This handoff includes the Unity source and a standalone simulation report. The Unity editor has yet to compile or boot the project, and there is no Unity APK in this handoff.

## Open in Unity

1. In Unity Hub, install **Unity 6 LTS**. The project pins **6000.0.62f1**. Add the **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules if building for a phone.
2. Add the `unity/` directory as a project, open it, and let Package Manager resolve dependencies.
3. Run **Saranggola → Set Up Project**. It generates the URP renderer and quality assets, configures the Input System and Android player settings, saves `Assets/Generated/Boot.unity`, and adds Boot to the build list. If the editor prompts for an input backend restart, reopen the project.
4. Open **Boot** and press **Play**. Pick a hero, then choose **Play Duos**, **Play Solo**, **Tutorial**, or **Practice**.

Play Duos offers the skippable 60-second guide on your first match. Practice starts with three passive opponents. The mobile controls are the camera-relative stick, hold Attack, tap or drag skills, tap an enemy to lock, **Tumalon!** then **Dive**, objective interactions, two consumable slots, and minimap tapping for duo pings. Tapping a weapon label in reach opens a comparison when you already hold a weapon.

Desktop controls are **WASD** or arrows, **Space** to attack, **1–3** or **Q/E/R** for skills, **Shift + 1–3** to rank up, **4–5** for items, **F** to jump or dive, **G** to interact, **B** for a nearby store, **T** for duo quick chat, and **Esc** for settings.

## Command-line verification and APK

Run these from the repository root with `UNITY_EDITOR` set to the Unity executable. Use a licensed editor and close interactive instances of the same project before running batchmode.

```bash
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD/unity" \
  -executeMethod ProjectSetup.Run -logFile "$PWD/unity-setup.log"

"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD/unity" \
  -runTests -testPlatform EditMode -testResults "$PWD/editmode-results.xml" \
  -logFile "$PWD/unity-tests.log"

"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD/unity" \
  -executeMethod BalanceSim.Run -matches 200 -seed 1 -logFile "$PWD/unity-balance.log"

mkdir -p Build
"$UNITY_EDITOR" -batchmode -nographics -quit -buildTarget Android \
  -projectPath "$PWD/unity" -executeMethod ProjectSetup.BuildAndroid \
  -apk "$PWD/Build/SaranggolaRoyale-Unity.apk" -logFile "$PWD/unity-android.log"
```

The interactive APK command is **Saranggola → Build Android APK**. Setup selects landscape, ARM64, IL2CPP, ASTC and Android API 26 minimum. To install the resulting APK, transfer it to the phone and open it, or use `adb install -r Build/SaranggolaRoyale-Unity.apk`. An in-place upgrade of the archived APK requires the same signing certificate for `org.saranggola.royale`. Use a different application identifier in Project Setup for side-by-side testing.

Balance output defaults to `balance_report.md` at the repository root. Use `-report /absolute/path.md` to keep the checked-in standalone run when comparing Unity results. Full player matches append JSON records to Unity’s `Application.persistentDataPath/matches.jsonl`.

## Godot audit and milestone verification

The Godot simulation covered the hero roster, combat, storm, gear, folklore camps, and objective-seeking bots. Its match loop used structures, teams of two, and base respawns. The archive also supplied the warm palette and touch HUD arrangement. The migration keeps the seven saved hero names and their ability data, including Bato and Tala ng Langit, the weapon and consumable catalogs, mob stats, and store items other than Buhay-Pa. The Unity round uses last standing, Bagsak, Bangon, Tapos and Hatid-Buhay. Store, outpost and beacon interactions are neutral.

| Milestone | Source and verification |
| --- | --- |
| U0 · Skeleton | Separate Sim, View, Editor and EditMode assemblies. Boot and generated project settings are implemented through Editor APIs. Unity import and boot pending. |
| U1 · Rules | Archived JSON plus fixed-rate damage, status, skill and economy code. Standalone C# compilation covered the Sim assembly. |
| U2 · Map | 200 × 200 m XZ map, rotational checkpoints, road speed, river crossings, projectile cover, camera, joystick and procedural terrain. Unity rendering and route playthrough pending. |
| U3 · Heroes and aim | Seven kits, chibi models, procedural skill icons, tap/drag aim, rank pips and radial cooldowns. Standalone smoke exercised all 21 active casts. Unity input and visuals pending. |
| U4 · Royale | Saranggola carrier and glide, four Bagyo phases, defeat and recovery channels, outposts and stores. Standalone tests covered damage interruption, revive health, soul pickup and beacon return, storm timing and solo elimination. |
| U5 · Loot and mobs | Ten weapons, four consumables, finite durability, gear swap, supply boxes, seven regular mob types and Bakunawa. The bench exercised the economy and all eight mob types. Tactical Android playtesting pending. |
| U6 · Bots and balance | Staggered utility AI, three difficulty profiles, fog and objective selection. The identical-seed test passed in standalone C#. The included report covers 200 Normal Duos seeds. |
| U7 · Presentation | Regional landmarks, weather, animated surfaces, audio and haptics, onboarding, controls/settings, results, recap and telemetry. Unity UI and mobile device checks pending. |
| U8 · Android and profiling | Editor generation and APK export commands included. Unity compilation, player build, device install and profiling pending. |

Unity could not compile or boot any of the milestones in this workspace. All ten EditMode fixture methods passed with standalone .NET 8 and NUnit assertions. Unity Test Runner execution is pending. Roslyn parsed all C# sources using C# 9.

## Assumptions and adjustments

- Local paths in the request are relative to the repository root. The Unity project contains C#, HLSL shaders, assembly definitions, the manifest and the editor version pin. Project Setup generates Unity scenes and assets. The verbatim archived JSON is embedded in `LegacyTables.cs` because the source file constraint excludes runtime JSON assets. The typed catalog parses it with Newtonsoft. `Catalog.cs` holds the new mode’s JSON overrides for the storm, roster timing, difficulty and XP curve.
- The six-beacon layout uses a four-way outer set and an opposed central pair. Six distinct points cannot make a four-way rotational orbit on a square. Stores, outposts, initial loot and regular mob spawn sites are placed in groups of four. Each loot quartet shares a rarity template sampled using the archived odds, and camp quartets share a mob type so biome art confers no starting resource advantage.
- Duos can finish downed opponents or rescue their teammate in the first two storm phases. A bleed-out creates the recoverable orb. Phase 3 shuts recovery down for both new and existing downs. Solo eliminates at zero HP. The storm starts sudden death at 8:00. If the tiny ring leaves multiple teams at 9:00, remaining health, damage and kills decide first place.
- Normal, Easy, and Hard use 6, 12, and 4 ticks for reaction time. Hard rounds 120 ms to the nearest available 30 Hz step. Aim error scales the bot’s direction jitter. Bots navigate via the bridge/ford network with local obstacle steering. The position hash is a same-runtime regression check and may vary across runtimes or CPU architectures.
- Regular camps can respawn once before Phase 3. Bakunawa spawns at 3:30. The progression targets are evaluated for efficient heroes by the report’s fastest quartile. Few bots reach level 8, and few clear Bakunawa before they need to rotate. Legacy health, damage and cooldown values remain the benchmark baseline.
- The scenery pass uses batched procedural props, a landmark in each quadrant, animated water/lava/paddies/rice, a cloud belt and rain. Smaller decorative extras such as laundry, the basketball court, firefly detail and dense market dressing were cut. Map connectivity and typical road-versus-flank travel times still need a walk-through.
- Ultimate hit-stop pauses visual poses for 0.3 seconds. The 30 Hz simulation clock continues through the effect. Damage, controls and audio presentation are handled in View. Graphics can fall back to flat URP/Lit if the toon shader reports that it is unsupported.

## What still needs Unity or a phone

- Batchmode import, assembly references, package resolution, shader compilation, and Editor API generation of the Boot scene, Input System setting, and quality tiers. The largest setup risks are the pinned URP and uGUI package APIs and Unity’s serialized Player/Quality/Graphics Settings fields.
- Boot through the graphical lobby, all seven heroes in the live match, both win/loss screens, multitouch drag and cancel, shop and swap panels, minimap chat, tutorial, audio and haptics. The standalone tests verify a winner and losing teams in simulation, plus rule-level skill and recovery behavior.
- Android IL2CPP Newtonsoft reflection and stripping, APK signing, install and cold start. Device temperatures, memory and battery load, 60 FPS on mid-range Android, render scale and dynamic downgrade, the ≤150 draw calls and ≤120k visible triangles budgets, and allocation measurements require the Unity Profiler and a physical device. The source pools shots, ground pickups, effects and damage numbers, combines static props, and caps the weather particle system at 135. HUD text updates still allocate periodically.
