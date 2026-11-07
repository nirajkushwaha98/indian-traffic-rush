using UnityEngine;

namespace ITR
{
    /// <summary>All gameplay tuning in one asset (Assets/ScriptableObjects/GameConfig.asset).</summary>
    [CreateAssetMenu(menuName = "ITR/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player speed")]
        public float startCruiseKmh = 80f;
        public float maxCruiseKmh = 130f;
        public float minSpeedKmh = 35f;
        public float coastDecel = 3f;

        [Header("Difficulty")]
        [Tooltip("Seconds of driving until difficulty reaches 1.")]
        public float difficultyRampSeconds = 210f;

        [Header("Traffic")]
        public float spawnAhead = 340f;
        [Tooltip("Traffic never appears closer than this ahead of the player (it fades in through the distance haze).")]
        public float minSpawnDistance = 260f;
        public float despawnBehind = 25f;
        public int minActiveTraffic = 18;
        public int maxActiveTraffic = 24;
        public Vector2 rowGapEasy = new Vector2(22f, 30f);
        public Vector2 rowGapHard = new Vector2(18f, 26f);
        [Range(0f, 1f)] public float doubleRowChanceEasy = 0.35f;
        [Range(0f, 1f)] public float doubleRowChanceHard = 0.5f;
        [Tooltip("Minimum free bumper-to-bumper gap a vehicle keeps to the one ahead in its lane.")]
        public float followGap = 12f;
        [Tooltip("Longitudinal window in which all three lanes may never be blocked at once.")]
        public float wallWindow = 18f;
        [Tooltip("Traffic within this distance of the player never changes lane.")]
        public float laneChangeSafeDistance = 70f;

        [Header("Coins")]
        public Vector2 coinLineInterval = new Vector2(90f, 170f);
        public int coinsPerLine = 6;
        public float coinSpacing = 4f;
        public float coinPickupRadius = 1.6f;

        [Header("Scoring")]
        public int nearMissPoints = 100;
        public int overtakePoints = 10;
        public float nearMissClearance = 1.5f;
        public float nearMissMinSpeedKmh = 90f;
        public float comboWindow = 3f;

        [Header("Nitro")]
        public float nitroStart = 0.5f;
        public float nitroMinToActivate = 0.25f;
        public float nitroDrainPerSecond = 0.2f;
        public float nitroPerNearMiss = 0.18f;
        public float nitroPerOvertake = 0.03f;
        public float nitroPassivePerSecond = 0.01f;
        public float nitroSpeedMultiplier = 1.35f;
        public float nitroScoreMultiplier = 1.5f;

        [Header("Rewards")]
        public int coinsPerKm = 20;
        public int coinsPerNearMiss = 2;
        public int coinValue = 1;

        [Header("Ads")]
        public float reviveInvulnerability = 2.5f;

        [Header("Environment")]
        public float fogStart = 190f;
        public float fogEnd = 430f;

        public static GameConfig CreateDefault()
        {
            var c = CreateInstance<GameConfig>();
            c.name = "GameConfig (runtime default)";
            return c;
        }
    }
}
