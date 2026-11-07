using UnityEngine;

namespace ITR
{
    /// <summary>
    /// The only component saved in Game.unity. Builds the whole race at runtime: lighting/sky, endless road,
    /// player car (selected garage car with its paint and upgrades), camera, traffic, coins, game manager and UI.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static int? ForcedSeed;          // tests
        public static TimeOfDay? ForcedTheme;   // tests / screenshots

        public GameManager Manager { get; private set; }

        void Awake()
        {
            AppRoot.Ensure();
            var cfg = GameAssets.I.config;
            int seed = ForcedSeed ?? System.Environment.TickCount;

            // Environment
            var sun = EnvironmentTheme.CreateSun();
            var tod = ForcedTheme ?? EnvironmentTheme.Resolve((ThemeChoice)SaveSystem.Data.settings.theme);
            var road = new GameObject("Road").AddComponent<RoadManager>();
            // Enough road to reach the far end of the haze, so the horizon is never empty.
            float viewDistance = Mathf.Max(cfg.fogEnd, cfg.spawnAhead) + 20f;
            road.Init(Mathf.CeilToInt((viewDistance + 80f) / GameConstants.SegmentLength) + 1, seed, -80f);
            EnvironmentTheme.Apply(tod, sun, cfg.fogStart, cfg.fogEnd);

            // Player
            var vehicle = GarageService.Selected;
            var playerGo = new GameObject("Player");
            playerGo.AddComponent<Rigidbody>();
            var player = playerGo.AddComponent<PlayerCar>();
            player.Setup(vehicle, GarageService.PaintOf(vehicle), cfg);
            var fx = playerGo.AddComponent<PlayerEffects>();
            fx.Setup(player);
            var input = playerGo.AddComponent<PlayerInputHandler>();

            var rig = CameraRig.Create(player);
            rig.Cam.farClipPlane = viewDistance + 40f;

            // Traffic & coins
            var traffic = new GameObject("Traffic").AddComponent<TrafficManager>();
            traffic.Init(cfg, GameAssets.I.traffic, seed + 1, 0f);
            traffic.Prewarm(player);
            var coins = new GameObject("Coins").AddComponent<CoinManager>();
            coins.Init(cfg, seed + 2, 0f);

            Manager = new GameObject("GameManager").AddComponent<GameManager>();
            Manager.Init(cfg, player, fx, input, traffic, coins, road, rig);

            GameUI.Build(Manager);
            AudioManager.I?.PlayMusic(true);
            Manager.Begin();
        }
    }
}
