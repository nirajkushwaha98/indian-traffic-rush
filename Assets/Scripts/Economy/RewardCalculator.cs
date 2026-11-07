using UnityEngine;

namespace ITR
{
    /// <summary>Coins earned at the end of a run: collected coins + distance bonus + skill bonus.</summary>
    public struct RunReward
    {
        public int collected;
        public int distanceBonus;
        public int nearMissBonus;
        public int Total => collected + distanceBonus + nearMissBonus;
    }

    public static class RewardCalculator
    {
        public static RunReward Calculate(RunStats stats, GameConfig cfg)
        {
            return new RunReward
            {
                collected = stats.coins * cfg.coinValue,
                distanceBonus = Mathf.FloorToInt(stats.distance / 1000f * cfg.coinsPerKm),
                nearMissBonus = stats.nearMisses * cfg.coinsPerNearMiss,
            };
        }
    }
}
