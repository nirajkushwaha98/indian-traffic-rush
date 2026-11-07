namespace ITR
{
    /// <summary>
    /// 7-day login calendar. Claiming on consecutive days advances the streak; missing a day restarts at day 1.
    /// </summary>
    public static class DailyRewardManager
    {
        public static readonly int[] Rewards = { 100, 150, 200, 300, 400, 500, 1000 };

        static DailyRewardState State => SaveSystem.Data.dailyReward;

        public static bool CanClaim => State.lastClaimDate != Clock.TodayKey;

        /// <summary>True when the streak continues if claimed today (claimed yesterday).</summary>
        static bool StreakAlive => State.lastClaimDate == Clock.YesterdayKey;

        /// <summary>0-based calendar index of today's reward (claimable or already claimed today).</summary>
        public static int TodayIndex
        {
            get
            {
                if (!CanClaim) return (State.streak - 1 + Rewards.Length) % Rewards.Length;
                return StreakAlive ? State.streak % Rewards.Length : 0;
            }
        }

        /// <summary>Calendar index state: claimed (in current cycle), today, or upcoming.</summary>
        public static bool IsClaimedInCycle(int index)
        {
            int today = TodayIndex;
            if (index < today) return true;
            return index == today && !CanClaim;
        }

        public static int Claim()
        {
            if (!CanClaim) return 0;
            State.streak = StreakAlive ? State.streak + 1 : 1;
            State.lastClaimDate = Clock.TodayKey;
            int reward = Rewards[(State.streak - 1) % Rewards.Length];
            Wallet.Add(reward, false);
            SaveSystem.Save();
            return reward;
        }
    }
}
