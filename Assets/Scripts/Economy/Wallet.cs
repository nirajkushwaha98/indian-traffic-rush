using System;

namespace ITR
{
    /// <summary>The player's coin balance. All coin changes go through here.</summary>
    public static class Wallet
    {
        public static event Action<int> Changed;

        public static int Coins => SaveSystem.Data.coins;

        public static void Add(int amount, bool save = true)
        {
            if (amount <= 0) return;
            var d = SaveSystem.Data;
            d.coins = (int)Math.Min((long)d.coins + amount, int.MaxValue);
            d.stats.totalCoins += amount;
            if (save) SaveSystem.Save();
            Changed?.Invoke(d.coins);
        }

        public static bool CanAfford(int amount) => amount >= 0 && Coins >= amount;

        public static bool TrySpend(int amount)
        {
            if (!CanAfford(amount)) return false;
            SaveSystem.Data.coins -= amount;
            SaveSystem.Save();
            Changed?.Invoke(SaveSystem.Data.coins);
            return true;
        }
    }
}
