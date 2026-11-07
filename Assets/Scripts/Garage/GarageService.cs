namespace ITR
{
    public enum UpgradeType { Speed, Handling, Braking }

    public enum GarageResult { Ok, NotEnoughCoins, AlreadyOwned, NotOwned, MaxLevel, Invalid }

    /// <summary>Pure garage logic: buy, select, paint and upgrade cars. Every change is saved.</summary>
    public static class GarageService
    {
        public static VehicleCatalog Catalog => GameAssets.I.vehicles;

        public static VehicleData Selected => Catalog.Get(SaveSystem.Data.selectedVehicle);

        public static OwnedVehicle SelectedOwned => SaveSystem.Data.GetVehicle(SaveSystem.Data.selectedVehicle);

        public static bool IsOwned(VehicleData v) => v != null && SaveSystem.Data.Owns(v.id);

        public static GarageResult Buy(VehicleData v)
        {
            if (v == null) return GarageResult.Invalid;
            if (IsOwned(v)) return GarageResult.AlreadyOwned;
            if (!Wallet.TrySpend(v.price)) return GarageResult.NotEnoughCoins;
            SaveSystem.Data.vehicles.Add(new OwnedVehicle { id = v.id });
            SaveSystem.Save();
            return GarageResult.Ok;
        }

        public static GarageResult Select(VehicleData v)
        {
            if (!IsOwned(v)) return GarageResult.NotOwned;
            SaveSystem.Data.selectedVehicle = v.id;
            SaveSystem.Save();
            return GarageResult.Ok;
        }

        public static GarageResult SetColor(VehicleData v, int colorIndex)
        {
            var owned = v != null ? SaveSystem.Data.GetVehicle(v.id) : null;
            if (owned == null) return GarageResult.NotOwned;
            if (colorIndex < 0 || colorIndex >= v.colors.Length) return GarageResult.Invalid;
            owned.colorIndex = colorIndex;
            SaveSystem.Save();
            return GarageResult.Ok;
        }

        public static int Level(VehicleData v, UpgradeType type)
        {
            var o = v != null ? SaveSystem.Data.GetVehicle(v.id) : null;
            if (o == null) return 0;
            switch (type)
            {
                case UpgradeType.Speed: return o.speedLevel;
                case UpgradeType.Handling: return o.handlingLevel;
                default: return o.brakingLevel;
            }
        }

        public static int UpgradeCost(VehicleData v, UpgradeType type) => v.UpgradeCost(Level(v, type));

        public static GarageResult Upgrade(VehicleData v, UpgradeType type)
        {
            var o = v != null ? SaveSystem.Data.GetVehicle(v.id) : null;
            if (o == null) return GarageResult.NotOwned;
            int lvl = Level(v, type);
            if (lvl >= v.maxUpgradeLevel) return GarageResult.MaxLevel;
            int cost = v.UpgradeCost(lvl);
            if (!Wallet.TrySpend(cost)) return GarageResult.NotEnoughCoins;
            switch (type)
            {
                case UpgradeType.Speed: o.speedLevel++; break;
                case UpgradeType.Handling: o.handlingLevel++; break;
                default: o.brakingLevel++; break;
            }
            SaveSystem.Save();
            return GarageResult.Ok;
        }

        public static UnityEngine.Color PaintOf(VehicleData v)
        {
            var o = v != null ? SaveSystem.Data.GetVehicle(v.id) : null;
            int idx = o != null ? o.colorIndex : 0;
            if (v == null || v.colors == null || v.colors.Length == 0) return UnityEngine.Color.white;
            return v.colors[UnityEngine.Mathf.Clamp(idx, 0, v.colors.Length - 1)];
        }

        /// <summary>Final driving stats for a car including its upgrades.</summary>
        public struct Stats
        {
            public float topSpeedKmh, acceleration, handling, braking;
        }

        public static Stats GetStats(VehicleData v)
        {
            return new Stats
            {
                topSpeedKmh = v.TopSpeedKmh(Level(v, UpgradeType.Speed)),
                acceleration = v.Acceleration(Level(v, UpgradeType.Speed)),
                handling = v.Handling(Level(v, UpgradeType.Handling)),
                braking = v.Braking(Level(v, UpgradeType.Braking)),
            };
        }
    }
}
