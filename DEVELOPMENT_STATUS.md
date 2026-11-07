# Indian Traffic Rush — Development Status

Engine: **Unity 6000.3.8f1** (Android module installed) · URP 17.3 · Input System 1.18 · uGUI · Test Framework 1.6
Last verified: 2026-11-06 · **Portrait-only (9:16), 1080×1920 UI reference**

## How to open & play
1. Open this folder in Unity Hub with **6000.3.8f1**.
2. (First time / after pulling) run **ITR ▸ Setup Project** — idempotent; creates any missing materials, ScriptableObjects, scenes and player settings without overwriting existing ones.
3. Set the Game view to a portrait size (e.g. 1080x1920), open `Assets/Scenes/MainMenu.unity` and press **Play**. Keyboard: ←/→ or A/D lanes, ↑/W gas, ↓/S brake, Space nitro, H horn, Esc pause.

## Architecture
Scenes contain a single bootstrap component; everything else is built at runtime from code + ScriptableObjects, so there are no fragile scene references.

| Folder | Responsibility |
|---|---|
| `Scripts/Core` | AppRoot (services, quality, faded scene loads), GameManager (state machine, explicit update order, scoring, crash/revive, commit to save), SaveSystem/SaveData (atomic JSON), GameConfig/GameAssets SOs, MeshAssembler + VehicleBuilder (procedural models), FloatingOrigin |
| `Scripts/Player` | PlayerCar (lanes, cruise/gas/brake, nitro, side-swipe vs crash, physics tumble), PlayerInputHandler (Input System: swipe, buttons, keyboard, gamepad), PlayerEffects (flames, sparks, smoke, engine pitch w/ fake gears, indicators, sirens), CameraRig (follow, FOV, speed lines, shake, crash orbit), ParticleFactory |
| `Scripts/Traffic` | TrafficManager (pooling, weighted spawning by difficulty, same-lane following, **anti-wall rule**, lane-weaving autos/bikes, overtakes & near misses, decorative oncoming traffic), TrafficVehicle, CoinManager, TrafficVehicleData/Catalog SOs |
| `Scripts/Environment` | RoadBuilder (3-lane road, markings, median with kerbs/shrubs/streetlights, opposite carriageway, footpaths), SceneryFactory (buildings w/ water tanks, shops w/ signboards & awnings, trees, palms, billboards, direction/speed/info signs), RoadManager (segment recycling + prop pools), EnvironmentTheme (Day / Sunset) |
| `Scripts/UI` | UIKit (generated sprites, consistent palette), GameUI (HUD, pedals, countdown, pause, game over), MainMenuUI, MenuShowroom (3D turntable), GarageUI, MissionsUI (+ DailyRewardUI), SettingsUI |
| `Scripts/Garage` | VehicleData/Catalog SOs, GarageService (buy/select/paint/upgrade) |
| `Scripts/Missions` | MissionManager (3 daily missions/day from 8 types + 15 achievements), DailyRewardManager (7-day streak), Clock |
| `Scripts/Economy` | Wallet, RewardCalculator |
| `Scripts/Audio` | AudioSynth (all SFX + 2 music loops synthesised at startup), AudioManager |
| `Scripts/Ads` | AdsConfig SO, IAdsProvider, NullAdsProvider, AdMobAdsProvider (`#if ADMOB_ENABLED`), AdsManager |
| `Scripts/Editor` | ProjectSetup, BuildScript |
| `Tests/EditMode`, `Tests/PlayMode` | automated verification (below) |

## Phase status
| Phase | Status |
|---|---|
| 1 Environment, road, player car, camera | ✅ Done, tested |
| 2 Traffic, lanes, collisions, coins | ✅ Done, tested |
| 3 Scoring, difficulty, pause, game over | ✅ Done, tested |
| 4 Menus, garage, selection, upgrades | ✅ Done, tested |
| 5 Missions, rewards, saving, audio | ✅ Done, tested (audio verified to generate/play without errors; listening is manual) |
| 6 Android optimisation & release prep | ✅ Settings, build script, docs. APK build: see "Android build" below |

## Verified by automated tests (all passing — 15 EditMode + 13 PlayMode)
Run: *Window ▸ General ▸ Test Runner*, or batch: `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode` (set `ITR_SHOTS=<dir>` to save screenshots).

- Save round-trip to disk, corrupt-file recovery, starter car/coins
- Wallet spend rules; garage buy/select/paint/upgrade/max-level, persisted
- Daily reward claim, same-day block, streak, missed-day reset, day-7 payout
- Daily missions distinct, progress, claim once, roll over on a new day; achievements unlock/claim once
- Ads disabled by default and **never** report a reward
- Race starts after countdown, car drives, score/distance increase, traffic spawns, road recycles
- Smooth lane changes and edge clamping; gas/brake/nitro change speed; nitro drains
- **Traffic safety:** 16 s at 2.5× speed — 0 same-lane overlaps, 0 three-lane walls (tightest gap 22 m vs 18 m window)
- Head-on hit → crash → game over; run counted once; best score saved and reloaded from disk
- Side-swipe bounces back to the original lane without ending the race
- Coin pickup; adjacent-lane high-speed pass counts as overtake + near miss
- Pause freezes the game, resume continues; revive path; restart reloads a fresh race; revived run not double-counted
- Menu: Garage purchase via UI button, Missions & Achievements tabs, Daily claim, Settings (controls/graphics) persisted, Play loads the race with the newly selected car

Screenshots of menu, garage, missions, daily, settings, race (day/sunset, swipe & button modes), nitro, crash, pause and game over were captured from these runs and visually checked.

## Placeholders (clearly original, replace for a commercial look)
- **All 3D models are procedural placeholders** built from primitives by `VehicleBuilder` / `SceneryFactory` (`VehicleData.placeholderModel = true`).
- App icon / splash logo: generated by `ProjectSetup` (`Assets/Textures/AppIcon.png`).
- Audio: synthesised; swap in recorded clips in `AudioManager.Init` if desired.
- Fonts: Unity's built-in LegacyRuntime font.
- Application ID `com.indiantrafficrush.game` and privacy-policy URL in `GameAssets.asset` are placeholders.

## Pending / needs a human
- [ ] Install & test on a physical Android device (touch feel, 60 FPS on low-end hardware, notch safe area).
- [ ] Final art (models, icon, feature graphic) and store listing — see `RELEASE_GUIDE.md`.
- [ ] Upload keystore + release AAB signing (env vars, see guide).
- [ ] AdMob: install plugin, add `ADMOB_ENABLED`, enter real IDs, add UMP consent — code path compiles only with the SDK so it is **not yet compiled or tested**.
- [ ] Publish a real privacy policy (template in `PRIVACY_POLICY_TEMPLATE.md`).

## Portrait mode (9:16)
- **Locked orientation:** Player settings use Portrait only, with landscape and upside-down disabled. The built APK's manifest contains `screenOrientation=portrait` and `resizeableActivity=false`, and the EditMode test `Build_IsLockedToPortrait` checks this.
- **UI scaling:** the reference resolution is 1080×1920 and the CanvasScaler uses **Expand**. The whole 9:16 design area is always visible: 19.5:9 and 20:9 phones get extra height, and tablets get extra width. Every screen also respects the notch/safe area.
- **Cameras:** the race and showroom cameras keep a constant **horizontal** field of view and work out the vertical one from the actual aspect ratio (`CameraRig.VerticalFov`). All three lanes therefore fit on every phone, and foldables snap to the new shape.
- **Race layout:**
  - Top: score and distance card, coins and pause.
  - Car: in the lower-middle, about 40% up the screen.
  - Bottom, Swipe mode: BRAKE (bottom-left), speedometer and nitro bar (centre), GAS (bottom-right), with HORN and NITRO above them.
  - Bottom, Buttons mode: steering arrows in the corners with BRAKE and GAS stacked above.
- **One-hand play:** besides swipes, a quick **tap on the left or right half** of the road changes lanes in either control mode.
- **Menus:**
  - Home: centred logo at the top, car in the middle, and PLAY, GARAGE, MISSIONS, DAILY and SETTINGS stacked at the bottom.
  - Garage: car at the top with ‹ › arrows beside it, and a full-width stats/upgrade card at the bottom.
  - Missions, Daily (4+3 grid) and Settings: full-width cards.
  - Game over: a vertical stack with the actions at the bottom.
- **Verified:** automated screenshots at **9:16, 20:9 and 3:4**. An automated **no-clipping check** fails the tests if any visible button or label falls off-screen in any capture.

## Camera & visibility (2026-11-06)
- **Camera:** an elevated third-person rear-follow "telephoto" chase cam, 22 m behind and 10.5 m up with a 34° horizontal lens, looking 30 m ahead. On a 9:16 screen the player car sits **30% up the screen** (lower-middle, above the controls) and the horizon **68% up**. All lanes stay in view on every aspect ratio, because the horizontal FOV is held constant.
- **View distance:** light haze runs from 190–430 m instead of 70–240 m. The camera's far clip and the endless road now reach the end of the haze (about 450 m), and lane dashes are wider and longer.
- **Traffic visibility:**
  - New vehicles spawn only **260–340 m ahead** (measured nearest: about 300 m) and fade in through the haze, so nothing pops up in front of the player.
  - The road is prewarmed with traffic before the race appears.
  - Traffic is denser, with a 12 m bumper gap, and realistic Indian highway speeds (autos 22–36, trucks 26–42, cars 38–60 km/h) that are slower than the player, so you steadily close in and overtake.
- **Measured in the automated test** (12 s of driving, 9:16):
  - **About 12 vehicles visible ahead** on average (≥0.5% of screen height and not hazed out), with 6 or more in 100% of frames.
  - **About 6 clearly readable vehicles** on average (≥1% of screen height, about 19 px on 1080×1920), with 4 or more in ~85% of frames.
  - The overlap and no-three-lane-wall safety checks still pass.
- **Effects toned down:** camera shake is roughly halved, speed streaks appear only at high speed or with nitro, and the FOV widens only slightly with speed.
- **Lighting:**
  - Brighter daylight sun and fill light, with lighter shadows.
  - Sunset is brightened.
  - **Auto** environment now picks daylight 75% of the time, and new installs default to **Day**.
- **Sharper image:** the mobile URP asset now uses render scale 1.0 (was 0.8) and 2× MSAA. On devices, High graphics uses 4× MSAA and Low uses 0.85 render scale with 2× MSAA.
- **Not changed:** models still use flat colours, not textures (they're procedural placeholders). Higher-quality textured models remain a pending art task.

## Android build
- ✅ **Portrait APK rebuilt 2026-11-06** (0 errors). Before that, a landscape APK ran on the Pixel 10 Pro emulator (Android 17 preview, 16 KB pages) and rendered and played correctly. That run exposed a code-stripping error from `CreatePrimitive`, which is now fixed by loading the built-in meshes directly.
- ❌ **The portrait APK hasn't been installed on a device or emulator yet.**
- ✅ **Test APK built successfully** in batch mode (IL2CPP, ARM64 + ARMv7, debug keystore): `Builds/Android/IndianTrafficRush-1.0.0.apk`, 50 MB (each ABI's share is roughly 25 MB, and an AAB downloads only one ABI per device). It took 5m45s and finished with 0 errors.
- Built with `ITR_JDK` pointing to Homebrew `openjdk@17`, because the Hub's embedded JDK folder is missing on this machine.
- ❌ **Not yet installed on a physical device** because none was connected, so on-device touch input and performance have not been checked yet.
- ⏳ **The release AAB hasn't been built yet** because it needs your upload keystore. See `RELEASE_GUIDE.md`.
- Install the APK on a device with: `adb install -r Builds/Android/IndianTrafficRush-1.0.0.apk`
