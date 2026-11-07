using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    public enum MissionType
    {
        DriveDistance = 0,     // cumulative metres today
        CollectCoins = 1,      // cumulative today
        NearMisses = 2,        // cumulative today
        Overtakes = 3,         // cumulative today
        ScoreInRun = 4,        // best single run today
        PlayRuns = 5,          // cumulative today
        UseNitro = 6,          // cumulative today
        DistanceInRun = 7,     // best single run today (metres)
    }

    public class AchievementDef
    {
        public string id, title, description;
        public float target;
        public int reward;
        public System.Func<SaveData, float> value;
    }

    /// <summary>Daily missions (3 new ones every day) and lifetime achievements, both claimable for coins.</summary>
    public static class MissionManager
    {
        public const int DailyCount = 3;

        struct Template
        {
            public MissionType type;
            public int[] targets;
            public int[] rewards;
        }

        static readonly Template[] Pool =
        {
            new Template { type = MissionType.DriveDistance, targets = new[] { 3000, 5000, 8000 }, rewards = new[] { 150, 250, 400 } },
            new Template { type = MissionType.CollectCoins, targets = new[] { 50, 100, 160 }, rewards = new[] { 120, 220, 350 } },
            new Template { type = MissionType.NearMisses, targets = new[] { 10, 20, 35 }, rewards = new[] { 150, 250, 400 } },
            new Template { type = MissionType.Overtakes, targets = new[] { 40, 80, 130 }, rewards = new[] { 120, 220, 350 } },
            new Template { type = MissionType.ScoreInRun, targets = new[] { 3000, 6000, 10000 }, rewards = new[] { 150, 300, 500 } },
            new Template { type = MissionType.PlayRuns, targets = new[] { 3, 5, 8 }, rewards = new[] { 100, 180, 280 } },
            new Template { type = MissionType.UseNitro, targets = new[] { 4, 8, 12 }, rewards = new[] { 100, 180, 280 } },
            new Template { type = MissionType.DistanceInRun, targets = new[] { 1500, 2500, 4000 }, rewards = new[] { 150, 260, 420 } },
        };

        public static readonly List<AchievementDef> Achievements = new List<AchievementDef>
        {
            A("km_5", "Road Rookie", "Drive 5 km in total", 5000, 250, d => d.stats.totalDistance),
            A("km_50", "Highway Hero", "Drive 50 km in total", 50000, 1000, d => d.stats.totalDistance),
            A("km_250", "Expressway Legend", "Drive 250 km in total", 250000, 3000, d => d.stats.totalDistance),
            A("runs_25", "Daily Commuter", "Play 25 races", 25, 400, d => d.stats.runs),
            A("coins_1000", "Coin Collector", "Earn 1,000 coins", 1000, 300, d => d.stats.totalCoins),
            A("coins_10000", "Treasure Hunter", "Earn 10,000 coins", 10000, 1500, d => d.stats.totalCoins),
            A("nm_50", "Close Call", "Perform 50 near misses", 50, 300, d => d.stats.totalNearMisses),
            A("nm_500", "Daredevil", "Perform 500 near misses", 500, 1500, d => d.stats.totalNearMisses),
            A("combo_8", "Combo Master", "Reach a x8 near-miss combo", 8, 600, d => d.stats.bestNearMissCombo),
            A("ot_200", "Overtake King", "Overtake 200 vehicles", 200, 500, d => d.stats.totalOvertakes),
            A("score_10k", "Score Chaser", "Score 10,000 in one race", 10000, 500, d => d.bestScore),
            A("score_50k", "Score Master", "Score 50,000 in one race", 50000, 2000, d => d.bestScore),
            A("speed_200", "Speed Demon", "Reach 200 km/h", 200, 800, d => d.bestSpeedKmh),
            A("cars_3", "Car Collector", "Own 3 cars", 3, 500, d => d.vehicles.Count),
            A("cars_6", "Full Garage", "Own every car", 6, 3000, d => d.vehicles.Count),
        };

        static AchievementDef A(string id, string t, string desc, float target, int reward, System.Func<SaveData, float> v) =>
            new AchievementDef { id = id, title = t, description = desc, target = target, reward = reward, value = v };

        // ------------------------------------------------------------------ daily missions

        /// <summary>Makes sure today's missions exist (rolls new ones on a new day).</summary>
        public static List<MissionProgress> Daily
        {
            get
            {
                EnsureToday();
                return SaveSystem.Data.dailyMissions.missions;
            }
        }

        public static void EnsureToday()
        {
            var state = SaveSystem.Data.dailyMissions;
            string today = Clock.TodayKey;
            if (state.date == today && state.missions.Count == DailyCount) return;

            state.date = today;
            state.missions.Clear();
            int hash = unchecked((int)2166136261);
            foreach (char ch in today) hash = unchecked((hash ^ ch) * 16777619); // stable FNV-1a
            var rng = new System.Random(hash);
            var indices = new List<int>();
            for (int i = 0; i < Pool.Length; i++) indices.Add(i);
            for (int k = 0; k < DailyCount; k++)
            {
                int pick = rng.Next(indices.Count);
                var t = Pool[indices[pick]];
                indices.RemoveAt(pick);
                int tier = rng.Next(t.targets.Length);
                state.missions.Add(new MissionProgress
                {
                    id = today + "_" + (int)t.type,
                    type = (int)t.type,
                    target = t.targets[tier],
                    reward = t.rewards[tier],
                });
            }
            SaveSystem.Save();
        }

        public static string Describe(MissionProgress m)
        {
            switch ((MissionType)m.type)
            {
                case MissionType.DriveDistance: return "Drive " + (m.target / 1000f).ToString("0.#") + " km today";
                case MissionType.CollectCoins: return "Collect " + m.target + " coins";
                case MissionType.NearMisses: return "Perform " + m.target + " near misses";
                case MissionType.Overtakes: return "Overtake " + m.target + " vehicles";
                case MissionType.ScoreInRun: return "Score " + m.target.ToString("N0") + " in one race";
                case MissionType.PlayRuns: return "Play " + m.target + " races";
                case MissionType.UseNitro: return "Use nitro " + m.target + " times";
                case MissionType.DistanceInRun: return "Drive " + m.target.ToString("N0") + " m in one race";
                default: return "Mission";
            }
        }

        public static bool IsComplete(MissionProgress m) => m.progress >= m.target;

        /// <summary>Applies a finished run to today's missions and lifetime stats. Returns missions completed by this run.</summary>
        public static List<MissionProgress> ReportRun(RunStats run) => ReportRun(run, run, true);

        /// <summary>
        /// <paramref name="delta"/> = amounts not reported yet (after a revive only the new part),
        /// <paramref name="total"/> = whole run (for "in one race" missions).
        /// </summary>
        public static List<MissionProgress> ReportRun(RunStats delta, RunStats total, bool countRun)
        {
            EnsureToday();
            var completed = new List<MissionProgress>();
            foreach (var m in SaveSystem.Data.dailyMissions.missions)
            {
                bool wasDone = IsComplete(m);
                int add = 0, best = -1;
                switch ((MissionType)m.type)
                {
                    case MissionType.DriveDistance: add = Mathf.FloorToInt(delta.distance); break;
                    case MissionType.CollectCoins: add = delta.coins; break;
                    case MissionType.NearMisses: add = delta.nearMisses; break;
                    case MissionType.Overtakes: add = delta.overtakes; break;
                    case MissionType.PlayRuns: add = countRun ? 1 : 0; break;
                    case MissionType.UseNitro: add = delta.nitroUses; break;
                    case MissionType.ScoreInRun: best = total.Score; break;
                    case MissionType.DistanceInRun: best = Mathf.FloorToInt(total.distance); break;
                }
                if (best >= 0) m.progress = Mathf.Max(m.progress, best);
                else m.progress = Mathf.Min(m.target, m.progress + add);
                if (m.progress > m.target) m.progress = m.target;
                if (!wasDone && IsComplete(m)) completed.Add(m);
            }
            SaveSystem.Save();
            return completed;
        }

        public static bool Claim(MissionProgress m)
        {
            if (m == null || m.claimed || !IsComplete(m)) return false;
            m.claimed = true;
            Wallet.Add(m.reward, false);
            SaveSystem.Save();
            return true;
        }

        // ------------------------------------------------------------------ achievements

        public static float Progress(AchievementDef a) => Mathf.Min(a.value(SaveSystem.Data), a.target);
        public static bool IsUnlocked(AchievementDef a) => a.value(SaveSystem.Data) >= a.target;
        public static bool IsClaimed(AchievementDef a) => SaveSystem.Data.claimedAchievements.Contains(a.id);

        public static bool Claim(AchievementDef a)
        {
            if (a == null || IsClaimed(a) || !IsUnlocked(a)) return false;
            SaveSystem.Data.claimedAchievements.Add(a.id);
            Wallet.Add(a.reward, false);
            SaveSystem.Save();
            return true;
        }

        /// <summary>Number of rewards waiting to be claimed (menu badge).</summary>
        public static int ClaimableCount()
        {
            int n = 0;
            foreach (var m in Daily) if (IsComplete(m) && !m.claimed) n++;
            foreach (var a in Achievements) if (IsUnlocked(a) && !IsClaimed(a)) n++;
            return n;
        }
    }
}
