using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ITR.Tests
{
    /// <summary>
    /// End-to-end tests that load the real scenes and drive the real systems.
    /// Set the environment variable ITR_SHOTS to a folder to also save screenshots.
    /// </summary>
    public class GameplayTests
    {
        string savePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savePath = Path.Combine(Application.temporaryCachePath, "itr_play_" + Guid.NewGuid().ToString("N") + ".json");
            SaveSystem.OverridePath = savePath;
            SaveSystem.Unload();
            SaveSystem.Load();
            SaveSystem.Data.stats.runs = 10; // hide first-run hints
            GameBootstrap.ForcedSeed = 42;
            GameBootstrap.ForcedTheme = TimeOfDay.Day;
            Time.timeScale = 1f;
            Screenshots.ResetClipping();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            Assert.IsEmpty(Screenshots.Clipped, "UI clipped off-screen:\n" + string.Join("\n", Screenshots.Clipped));
            GameBootstrap.ForcedSeed = null;
            GameBootstrap.ForcedTheme = null;
            SaveSystem.Unload();
            if (File.Exists(savePath)) File.Delete(savePath);
            SaveSystem.OverridePath = null;
            yield return null;
        }

        static IEnumerator LoadGame()
        {
            SceneManager.LoadScene(GameConstants.GameScene);
            yield return null;
            yield return null;
            Assert.IsNotNull(GameManager.I, "GameManager should be built by GameBootstrap");
        }

        static IEnumerator WaitFor(Func<bool> cond, float timeout, string what)
        {
            float t = 0f;
            while (!cond() && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(cond(), "Timed out waiting for: " + what);
        }

        static IEnumerator WaitPlaying()
        {
            yield return WaitFor(() => GameManager.I != null && GameManager.I.State == GameState.Playing, 6f, "race to start");
        }

        [UnityTest]
        public IEnumerator Race_StartsDrivesAndScores()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            Assert.AreEqual(GameState.Countdown, gm.State);
            yield return WaitPlaying();
            gm.Player.SetInvulnerable(30f);
            float z0 = gm.Player.transform.position.z;
            yield return new WaitForSeconds(3f);
            Assert.Greater(gm.Stats.distance, 30f, "player should move forward");
            Assert.Greater(gm.Player.transform.position.z, z0 + 30f);
            Assert.Greater(gm.Stats.Score, 0, "score should increase with distance");
            Assert.Greater(gm.Player.SpeedKmh, 40f);
            yield return WaitFor(() => gm.Traffic.Active.Count > 0, 6f, "traffic to spawn");
            Assert.Greater(gm.Road.SegmentCount, 5);
            yield return Screenshots.Capture("02_race_day");
            yield return Screenshots.Capture("02b_race_tall_20x9", 720, 1600);
            yield return Screenshots.Capture("02c_race_tablet_3x4", 1200, 1600);
        }

        [UnityTest]
        public IEnumerator Player_ChangesLanesSmoothly()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Player.SetInvulnerable(30f);
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            Assert.AreEqual(1, gm.Player.Lane);
            gm.Player.RequestLane(1);
            yield return new WaitForSeconds(0.1f);
            float midX = gm.Player.transform.position.x;
            Assert.Greater(midX, 0.05f, "moving right");
            Assert.Less(midX, GameConstants.LaneX(2) - 0.05f, "not teleported");
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(GameConstants.LaneX(2), gm.Player.transform.position.x, 0.15f);
            gm.Player.RequestLane(1); // already at right edge: stays
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(2, gm.Player.Lane);
            gm.Player.RequestLane(-1);
            gm.Player.RequestLane(-1);
            yield return new WaitForSeconds(0.9f);
            Assert.AreEqual(0, gm.Player.Lane);
            Assert.AreEqual(GameConstants.LaneX(0), gm.Player.transform.position.x, 0.15f);
        }

        [UnityTest]
        public IEnumerator Player_BrakeGasAndNitroChangeSpeed()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Player.SetInvulnerable(60f);
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            gm.Input.gasHeld = true;
            yield return new WaitForSeconds(4f);
            float gasSpeed = gm.Player.SpeedKmh;
            Assert.Greater(gasSpeed, gm.Player.CruiseKmh, "gas accelerates above cruise");
            gm.Input.gasHeld = false;
            gm.Input.brakeHeld = true;
            yield return new WaitForSeconds(1.5f);
            Assert.Less(gm.Player.SpeedKmh, gasSpeed - 30f, "brake slows the car");
            Assert.IsTrue(gm.Player.Braking);
            gm.Input.brakeHeld = false;
            gm.Player.AddNitro(1f);
            Assert.IsTrue(gm.Player.TryNitro());
            float before = gm.Player.SpeedKmh;
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(gm.Player.NitroActive);
            Assert.Greater(gm.Player.SpeedKmh, before + 20f, "nitro boosts speed");
            Assert.Less(gm.Player.Nitro, 1f, "nitro drains");
            yield return Screenshots.Capture("03_nitro");
        }

        [UnityTest]
        public IEnumerator Traffic_NeverOverlapsOrBlocksAllLanes()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Player.SetInvulnerable(999f);
            var cfg = gm.Cfg;
            int overlaps = 0, walls = 0, frames = 0, maxActive = 0;
            float tightest = float.MaxValue;
            Time.timeScale = 2.5f;
            float end = Time.unscaledTime + 16f;
            while (Time.unscaledTime < end)
            {
                gm.Player.SetInvulnerable(999f);
                var list = gm.Traffic.Active;
                maxActive = Mathf.Max(maxActive, list.Count);
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var a = list[i]; var b = list[j];
                        bool sameLane = Mathf.Abs(a.X - b.X) < (a.Width + b.Width) * 0.5f;
                        if (sameLane && Mathf.Abs(a.Z - b.Z) < (a.Length + b.Length) * 0.5f) overlaps++;
                    }
                float pz = gm.Player.transform.position.z;
                float w = gm.Traffic.TightestWall(pz + 5f, pz + 400f);
                tightest = Mathf.Min(tightest, w);
                if (w <= cfg.wallWindow) walls++;
                frames++;
                yield return null;
            }
            Time.timeScale = 1f;
            Debug.Log($"[ITR] traffic test: frames={frames} maxActive={maxActive} overlaps={overlaps} walls={walls} tightest={tightest:0.0}");
            Assert.Greater(maxActive, 3, "traffic should be spawning");
            Assert.AreEqual(0, overlaps, "vehicles in the same lane must never overlap");
            Assert.AreEqual(0, walls, "all three lanes must never be blocked within the wall window");
        }

        [UnityTest]
        public IEnumerator HeadOnCollision_CrashesAndSavesBestScore()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            yield return new WaitForSeconds(1f);
            var p = gm.Player.transform.position;
            var truck = gm.Traffic.SpawnAt(4, gm.Player.Lane, p.z + 18f, 15f);
            Assert.IsNotNull(truck);
            yield return WaitFor(() => gm.State == GameState.Crashing || gm.State == GameState.GameOver, 4f, "crash");
            Assert.IsTrue(gm.Player.Crashed);
            yield return new WaitForSeconds(0.6f);
            yield return Screenshots.Capture("04_crash");
            yield return WaitFor(() => gm.State == GameState.GameOver, 6f, "game over screen");
            yield return new WaitForSeconds(0.5f);
            yield return Screenshots.Capture("05_game_over");

            var d = SaveSystem.Data;
            Assert.AreEqual(1, d.stats.runs - 10, "run counted once");
            Assert.AreEqual(gm.Stats.Score, d.bestScore, "best score saved");
            Assert.Greater(d.coins, 500 - 1, "rewards granted");
            Assert.IsTrue(File.Exists(savePath));
            int best = d.bestScore;
            SaveSystem.Unload(); SaveSystem.Load();
            Assert.AreEqual(best, SaveSystem.Data.bestScore, "best score persisted to disk");
        }

        [UnityTest]
        public IEnumerator SideSwipe_BouncesBackWithoutCrash()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            yield return new WaitForSeconds(0.5f);
            var p = gm.Player.transform.position;
            // A car exactly beside the player in the right lane, same speed.
            gm.Traffic.SpawnAt(1, 2, p.z, gm.Player.SpeedKmh);
            yield return null;
            gm.Player.RequestLane(1);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(GameState.Playing, gm.State, "side-swipe must not end the race");
            Assert.AreEqual(1, gm.Stats.sideHits);
            Assert.AreEqual(1, gm.Player.Lane, "bounced back to the original lane");
        }

        [UnityTest]
        public IEnumerator Coins_AreCollected()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Player.SetInvulnerable(60f);
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            gm.Coins.Clear();
            var p = gm.Player.transform.position;
            int before = gm.Stats.coins;
            gm.Coins.SpawnLine(gm.Player.Lane, p.z + 20f, 5);
            yield return new WaitForSeconds(2.5f);
            Assert.GreaterOrEqual(gm.Stats.coins - before, 5, "all coins in the lane collected");
        }

        [UnityTest]
        public IEnumerator Overtake_InAdjacentLaneAtSpeed_CountsNearMiss()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            gm.Player.SetStartSpeed(130f);
            gm.Input.gasHeld = true;
            var p = gm.Player.transform.position;
            gm.Traffic.SpawnAt(3, 2, p.z + 40f, 30f); // bus in the right lane
            yield return WaitFor(() => gm.Stats.overtakes >= 1, 5f, "overtake");
            Assert.GreaterOrEqual(gm.Stats.nearMisses, 1, "close pass of a wide bus at speed is a near miss");
            Assert.Greater(gm.Stats.Score, 100);
        }

        [UnityTest]
        public IEnumerator PauseResume_And_Revive_And_Restart()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Pause();
            Assert.AreEqual(GameState.Paused, gm.State);
            Assert.AreEqual(0f, Time.timeScale);
            float z = gm.Player.transform.position.z;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(z, gm.Player.transform.position.z, 1e-3, "frozen while paused");
            yield return Screenshots.Capture("06_paused");
            gm.Resume();
            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.AreEqual(1f, Time.timeScale);

            // Crash, then revive (as a verified rewarded ad would)
            gm.Traffic.ClearAll();
            gm.Traffic.spawningEnabled = false;
            var p = gm.Player.transform.position;
            gm.Traffic.SpawnAt(3, gm.Player.Lane, p.z + 15f, 10f);
            yield return WaitFor(() => gm.State == GameState.GameOver, 8f, "game over");
            Assert.IsFalse(gm.CanRevive, "no ad provider configured, so revive must not be offered");
            gm.DoRevive();
            Assert.IsFalse(gm.Player.Crashed);
            yield return WaitPlaying();
            Assert.AreEqual(Quaternion.identity, gm.Player.transform.rotation);

            // Restart reloads a fresh race
            gm.Restart();
            yield return WaitFor(() => GameManager.I != null && GameManager.I != gm, 5f, "scene reload");
            Assert.AreEqual(GameState.Countdown, GameManager.I.State);
            Assert.AreEqual(1, SaveSystem.Data.stats.runs - 10, "revived run counted once");
        }

        [UnityTest]
        public IEnumerator Visibility_TrafficAheadAndFarSpawns()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            var cfg = gm.Cfg;
            float minLiveSpawn = float.MaxValue;
            int liveSpawns = 0;
            gm.Traffic.Spawned += (v, dist, prewarm) => { if (!prewarm) { liveSpawns++; minLiveSpawn = Mathf.Min(minLiveSpawn, dist); } };

            // Prewarmed: traffic is already on the road ahead when the race appears.
            float pz0 = gm.Player.transform.position.z;
            int atStart = gm.Traffic.CountAhead(pz0, 15f, cfg.fogEnd);
            Assert.GreaterOrEqual(atStart, 4, "at least 4 vehicles visible ahead at the start");

            yield return WaitPlaying();
            var cam = gm.Rig.Cam;
            cam.aspect = 9f / 16f; // phone portrait
            Time.timeScale = 2f;
            int frames = 0, framesWith4 = 0, framesVisible6 = 0, minAhead = int.MaxValue, minReadable = int.MaxValue;
            long sumReadable = 0, sumVisible = 0;
            float end = Time.unscaledTime + 12f;
            while (Time.unscaledTime < end)
            {
                gm.Player.SetInvulnerable(999f);
                float pz = gm.Player.transform.position.z;
                minAhead = Mathf.Min(minAhead, gm.Traffic.CountAhead(pz, 15f, cfg.fogEnd));
                int readable = ReadableAhead(gm, cam);
                frames++;
                sumReadable += readable;
                if (readable >= 4) framesWith4++;
                int visible = OnScreenAhead(gm, cam, 0.005f);
                sumVisible += visible;
                if (visible >= 6) framesVisible6++;
                minReadable = Mathf.Min(minReadable, readable);
                yield return null;
            }
            Time.timeScale = 1f;
            cam.ResetAspect();
            float ratio = framesWith4 / (float)frames;
            float visRatio = framesVisible6 / (float)frames;
            float avgReadable = sumReadable / (float)frames;
            Debug.Log($"[ITR] visibility: start={atStart} minAhead={minAhead} visible(>=0.5%) avg={sumVisible / (float)frames:0.0} frames6+={visRatio:P0} | readable(>=1%) avg={avgReadable:0.0} min={minReadable} frames4+={ratio:P0} | liveSpawns={liveSpawns} nearestLiveSpawn={minLiveSpawn:0}m");
            Assert.GreaterOrEqual(visRatio, 0.9f, "6+ vehicles visible on screen ahead at least 90% of the time");
            Assert.GreaterOrEqual(avgReadable, 4f, "on average 4+ vehicles large enough to read clearly");
            Assert.Greater(liveSpawns, 0);
            Assert.GreaterOrEqual(minLiveSpawn, cfg.minSpawnDistance - 5f, "traffic never spawns close in front of the player");
        }

        /// <summary>
        /// Vehicles ahead that a player can actually read: inside the viewport, at least 1% of screen height tall
        /// (≈19 px on a 1080x1920 phone) and no more than half hidden by the distance haze.
        /// </summary>
        static int ReadableAhead(GameManager gm, Camera cam) => OnScreenAhead(gm, cam, 0.01f);

        /// <summary>Vehicles ahead inside the viewport, at least <paramref name="minHeight"/> of screen height tall, ≤50% hazed.</summary>
        static int OnScreenAhead(GameManager gm, Camera cam, float minHeight)
        {
            int n = 0;
            float pz = gm.Player.transform.position.z;
            float hazeLimit = gm.Cfg.fogStart + (gm.Cfg.fogEnd - gm.Cfg.fogStart) * 0.5f;
            foreach (var v in gm.Traffic.Active)
            {
                if (v.Z < pz + 10f) continue;
                if (Vector3.Distance(cam.transform.position, v.transform.position) > hazeLimit) continue;
                var bottom = cam.WorldToViewportPoint(v.transform.position);
                var top = cam.WorldToViewportPoint(v.transform.position + Vector3.up * v.Visual.height);
                if (bottom.z <= 0f || bottom.x < 0f || bottom.x > 1f || top.y > 1f || bottom.y < 0f) continue;
                if (top.y - bottom.y >= minHeight) n++;
            }
            return n;
        }

        [UnityTest]
        public IEnumerator Camera_PortraitFraming()
        {
            yield return LoadGame();
            var gm = GameManager.I;
            yield return WaitPlaying();
            gm.Player.SetInvulnerable(30f);
            var cam = gm.Rig.Cam;
            cam.aspect = 9f / 16f;
            yield return null;
            yield return null;
            var p = gm.Player.transform.position;
            var car = cam.WorldToViewportPoint(p + Vector3.up * 0.7f);
            var horizon = cam.WorldToViewportPoint(new Vector3(0f, 0f, p.z + 5000f));
            var far = cam.WorldToViewportPoint(new Vector3(GameConstants.LaneX(0), 0.5f, p.z + 250f));
            var farR = cam.WorldToViewportPoint(new Vector3(GameConstants.LaneX(2), 0.5f, p.z + 250f));
            var nearL = cam.WorldToViewportPoint(new Vector3(-GameConstants.RoadHalfWidth, 0f, p.z + 8f));
            var nearR = cam.WorldToViewportPoint(new Vector3(GameConstants.RoadHalfWidth, 0f, p.z + 8f));
            Debug.Log($"[ITR] camera: car.y={car.y:0.00} horizon.y={horizon.y:0.00} far250.y={far.y:0.00} fovV={cam.fieldOfView:0.0} pitch={cam.transform.eulerAngles.x:0.0}");
            Assert.That(car.y, Is.InRange(0.28f, 0.45f), "player car in the lower-middle of the screen");
            Assert.That(car.x, Is.InRange(0.35f, 0.65f));
            Assert.That(horizon.y, Is.InRange(0.55f, 0.75f), "elevated view: horizon in the upper part, road fills the middle");
            Assert.Greater(far.z, 0f);
            Assert.That(far.y, Is.InRange(car.y, horizon.y), "road 250 m ahead visible between car and horizon");
            Assert.That(far.x, Is.InRange(0f, 1f)); Assert.That(farR.x, Is.InRange(0f, 1f));
            Assert.That(nearL.x, Is.InRange(0f, 1f), "all lanes visible just ahead of the car (left edge)");
            Assert.That(nearR.x, Is.InRange(0f, 1f), "all lanes visible just ahead of the car (right edge)");
            Assert.GreaterOrEqual(cam.farClipPlane, gm.Cfg.fogEnd, "camera draws to the end of the haze");
            cam.ResetAspect();
        }

        [UnityTest]
        public IEnumerator Sunset_Theme_Renders()
        {
            GameBootstrap.ForcedTheme = TimeOfDay.Sunset;
            yield return LoadGame();
            yield return WaitPlaying();
            GameManager.I.Player.SetInvulnerable(30f);
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(TimeOfDay.Sunset, EnvironmentTheme.Current);
            yield return Screenshots.Capture("07_race_sunset");
        }
    }

    public class MenuTests
    {
        string savePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savePath = Path.Combine(Application.temporaryCachePath, "itr_menu_" + Guid.NewGuid().ToString("N") + ".json");
            Screenshots.ResetClipping();
            SaveSystem.OverridePath = savePath;
            SaveSystem.Unload();
            SaveSystem.Load();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Assert.IsEmpty(Screenshots.Clipped, "UI clipped off-screen:\n" + string.Join("\n", Screenshots.Clipped));
            SaveSystem.Unload();
            if (File.Exists(savePath)) File.Delete(savePath);
            SaveSystem.OverridePath = null;
            yield return null;
        }

        static Button FindButton(string name)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.name == name) return b;
            Assert.Fail("Button not found: " + name);
            return null;
        }

        [UnityTest]
        public IEnumerator Menu_NavigationGaragePurchaseDailyAndPlay()
        {
            SceneManager.LoadScene(GameConstants.MenuScene);
            yield return null;
            yield return null;
            var menu = MainMenuUI.I;
            Assert.IsNotNull(menu);
            yield return new WaitForSeconds(1f);
            yield return Screenshots.Capture("01_main_menu");
            yield return Screenshots.Capture("01b_menu_tall_20x9", 720, 1600);
            yield return Screenshots.Capture("01c_menu_tablet_3x4", 1200, 1600);

            // Garage: buy + select the sedan
            FindButton("GarageButton").onClick.Invoke();
            Assert.AreEqual(MainMenuUI.Page.Garage, menu.Current);
            SaveSystem.Data.coins = 50000;
            var garage = menu.Garage;
            while (garage.Current.id != "sedan") garage.Browse(1);
            FindButton("ActionButton").onClick.Invoke();
            Assert.IsTrue(SaveSystem.Data.Owns("sedan"));
            Assert.AreEqual("sedan", SaveSystem.Data.selectedVehicle);
            Assert.AreEqual(50000 - garage.Current.price, SaveSystem.Data.coins);
            yield return new WaitForSeconds(0.8f);
            yield return Screenshots.Capture("08_garage");

            // Missions
            menu.Open(MainMenuUI.Page.Home);
            FindButton("MissionsButton").onClick.Invoke();
            Assert.AreEqual(MainMenuUI.Page.Missions, menu.Current);
            yield return new WaitForSeconds(0.5f);
            yield return Screenshots.Capture("09_missions");
            FindButton("AchievementsTab").onClick.Invoke();
            yield return null;

            // Daily reward
            menu.Open(MainMenuUI.Page.Daily);
            int coins = SaveSystem.Data.coins;
            FindButton("ClaimDaily").onClick.Invoke();
            Assert.AreEqual(coins + DailyRewardManager.Rewards[0], SaveSystem.Data.coins);
            Assert.IsFalse(DailyRewardManager.CanClaim);
            yield return new WaitForSeconds(0.5f);
            yield return Screenshots.Capture("10_daily");

            // Settings: switch to button controls and low graphics
            menu.Open(MainMenuUI.Page.Settings);
            FindButton("controls_BUTTONS").onClick.Invoke();
            FindButton("graphics_LOW").onClick.Invoke();
            Assert.AreEqual((int)ControlMode.Buttons, SaveSystem.Data.settings.controlMode);
            Assert.AreEqual((int)GraphicsLevel.Low, SaveSystem.Data.settings.graphics);
            yield return new WaitForSeconds(0.5f);
            yield return Screenshots.Capture("11_settings");
            menu.Open(MainMenuUI.Page.Home);
            SaveSystem.Unload(); SaveSystem.Load();
            Assert.AreEqual((int)ControlMode.Buttons, SaveSystem.Data.settings.controlMode, "settings persisted");
            Assert.AreEqual("sedan", SaveSystem.Data.selectedVehicle);

            // Play → race with the newly selected car and button controls
            GameBootstrap.ForcedSeed = 7;
            GameBootstrap.ForcedTheme = TimeOfDay.Sunset;
            FindButton("PlayButton").onClick.Invoke();
            float t = 0;
            while (GameManager.I == null && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(GameManager.I, "Play loads the race");
            Assert.AreEqual("sedan", GameManager.I.Player.Data.id);
            yield return new WaitForSeconds(4.5f);
            GameManager.I.Player.SetInvulnerable(10f);
            yield return Screenshots.Capture("12_race_buttons_mode");
            GameBootstrap.ForcedSeed = null;
            GameBootstrap.ForcedTheme = null;
        }
    }

    /// <summary>Renders the camera + overlay UI into a PNG (works in batch mode).</summary>
    public static class Screenshots
    {
        /// <summary>UI elements found partly off-screen by the most recent captures (cleared by <see cref="ResetClipping"/>).</summary>
        public static readonly System.Collections.Generic.List<string> Clipped = new System.Collections.Generic.List<string>();

        public static void ResetClipping() => Clipped.Clear();

        /// <summary>
        /// Renders the camera + overlay UI at a portrait resolution (default 900x1600 = 9:16), checks that every
        /// visible button and label lies fully on screen, and saves a PNG when ITR_SHOTS is set.
        /// </summary>
        public static IEnumerator Capture(string name, int w = 900, int h = 1600)
        {
            string dir = Environment.GetEnvironmentVariable("ITR_SHOTS");
            var cam = Camera.main;
            if (cam == null) yield break;

            // Emulate the device: camera aspect (drives the aspect-aware FOV code) and canvas size (drives CanvasScaler).
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.aspect = w / (float)h;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (canvases[i].isRootCanvas && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = cam;
                    canvases[i].planeDistance = cam.nearClipPlane + 0.05f + (1000 - canvases[i].sortingOrder) * 0.0001f;
                }
            }
            // Let CanvasScaler / layout / camera FOV update for the new size (WaitForEndOfFrame never fires in batch mode).
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            CheckClipping(name, cam, w, h);

            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = prev;
            cam.ResetAspect();
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i] != null) canvases[i].renderMode = modes[i];
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            }
            UnityEngine.Object.Destroy(tex);
            rt.Release();
            UnityEngine.Object.Destroy(rt);
        }

        static void CheckClipping(string shot, Camera cam, int w, int h)
        {
            var corners = new Vector3[4];
            const float tol = 3f;
            foreach (var g in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled || g.canvas == null) continue;
                bool interactive = g.GetComponent<Selectable>() != null;
                var text = g as Text;
                if (!interactive && (text == null || string.IsNullOrEmpty(text.text))) continue;
                if (g.GetComponentInParent<FloatingText>() != null) continue;
                var group = g.GetComponentInParent<CanvasGroup>();
                if (group != null && group.alpha < 0.05f) continue;
                if (text != null && text.color.a < 0.05f) continue;
                g.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    var s = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                    if (s.x < -tol || s.y < -tol || s.x > w + tol || s.y > h + tol)
                    {
                        Clipped.Add(shot + ": " + NodePath(g.transform) + " at " + s);
                        break;
                    }
                }
            }
        }

        static string NodePath(Transform t) => t.parent == null ? t.name : NodePath(t.parent) + "/" + t.name;
    }
}
