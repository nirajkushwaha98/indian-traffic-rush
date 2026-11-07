using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ITR.Tests
{
    /// <summary>Pure-logic tests: save system, wallet, garage, daily rewards, missions, ads safety.</summary>
    public class LogicTests
    {
        string path;

        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Application.temporaryCachePath, "itr_test_" + Guid.NewGuid().ToString("N") + ".json");
            SaveSystem.OverridePath = path;
            SaveSystem.Unload();
            SaveSystem.Load();
            Clock.Now = () => new DateTime(2026, 3, 10, 12, 0, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Clock.Now = () => DateTime.Now;
            SaveSystem.Unload();
            if (File.Exists(path)) File.Delete(path);
            SaveSystem.OverridePath = null;
        }

        [Test]
        public void NewSave_HasStarterCarAndCoins()
        {
            var d = SaveSystem.Data;
            Assert.IsTrue(d.Owns("hatchback"));
            Assert.AreEqual("hatchback", d.selectedVehicle);
            Assert.AreEqual(500, d.coins);
        }

        [Test]
        public void Save_RoundTripsThroughDisk()
        {
            var d = SaveSystem.Data;
            d.coins = 1234;
            d.bestScore = 98765;
            d.settings.musicVolume = 0.25f;
            d.settings.controlMode = (int)ControlMode.Buttons;
            d.vehicles.Add(new OwnedVehicle { id = "sedan", speedLevel = 2, colorIndex = 3 });
            d.claimedAchievements.Add("km_5");
            SaveSystem.Save();
            Assert.IsTrue(File.Exists(path));

            SaveSystem.Unload();
            SaveSystem.Load();
            var r = SaveSystem.Data;
            Assert.AreEqual(1234, r.coins);
            Assert.AreEqual(98765, r.bestScore);
            Assert.AreEqual(0.25f, r.settings.musicVolume, 1e-4);
            Assert.AreEqual((int)ControlMode.Buttons, r.settings.controlMode);
            Assert.AreEqual(2, r.GetVehicle("sedan").speedLevel);
            Assert.AreEqual(3, r.GetVehicle("sedan").colorIndex);
            Assert.Contains("km_5", r.claimedAchievements);
        }

        [Test]
        public void CorruptSave_FallsBackToFreshData()
        {
            File.WriteAllText(path, "{ this is not json");
            SaveSystem.Unload();
            SaveSystem.Load();
            Assert.IsTrue(SaveSystem.Data.Owns("hatchback"));
        }

        [Test]
        public void Wallet_SpendRequiresFunds()
        {
            SaveSystem.Data.coins = 100;
            Assert.IsFalse(Wallet.TrySpend(150));
            Assert.AreEqual(100, Wallet.Coins);
            Assert.IsTrue(Wallet.TrySpend(60));
            Assert.AreEqual(40, Wallet.Coins);
            Wallet.Add(10);
            Assert.AreEqual(50, Wallet.Coins);
        }

        [Test]
        public void Garage_BuySelectPaintUpgrade()
        {
            var sedan = GarageService.Catalog.Get("sedan");
            Assert.IsNotNull(sedan);
            SaveSystem.Data.coins = 100;
            Assert.AreEqual(GarageResult.NotEnoughCoins, GarageService.Buy(sedan));
            Assert.IsFalse(GarageService.IsOwned(sedan));
            Assert.AreEqual(GarageResult.NotOwned, GarageService.Select(sedan));

            SaveSystem.Data.coins = 100000;
            Assert.AreEqual(GarageResult.Ok, GarageService.Buy(sedan));
            Assert.AreEqual(100000 - sedan.price, Wallet.Coins);
            Assert.AreEqual(GarageResult.AlreadyOwned, GarageService.Buy(sedan));
            Assert.AreEqual(GarageResult.Ok, GarageService.Select(sedan));
            Assert.AreEqual("sedan", SaveSystem.Data.selectedVehicle);
            Assert.AreEqual(GarageResult.Ok, GarageService.SetColor(sedan, 2));
            Assert.AreEqual(sedan.colors[2], GarageService.PaintOf(sedan));

            var before = GarageService.GetStats(sedan);
            int coins = Wallet.Coins;
            int cost = GarageService.UpgradeCost(sedan, UpgradeType.Speed);
            Assert.AreEqual(GarageResult.Ok, GarageService.Upgrade(sedan, UpgradeType.Speed));
            Assert.AreEqual(coins - cost, Wallet.Coins);
            Assert.AreEqual(1, GarageService.Level(sedan, UpgradeType.Speed));
            Assert.Greater(GarageService.GetStats(sedan).topSpeedKmh, before.topSpeedKmh);

            for (int i = 0; i < 10; i++) GarageService.Upgrade(sedan, UpgradeType.Braking);
            Assert.AreEqual(sedan.maxUpgradeLevel, GarageService.Level(sedan, UpgradeType.Braking));
            Assert.AreEqual(GarageResult.MaxLevel, GarageService.Upgrade(sedan, UpgradeType.Braking));

            // persisted
            SaveSystem.Unload(); SaveSystem.Load();
            Assert.IsTrue(SaveSystem.Data.Owns("sedan"));
            Assert.AreEqual(1, SaveSystem.Data.GetVehicle("sedan").speedLevel);
        }

        [Test]
        public void DailyReward_StreakAndReset()
        {
            int start = Wallet.Coins;
            Assert.IsTrue(DailyRewardManager.CanClaim);
            Assert.AreEqual(0, DailyRewardManager.TodayIndex);
            Assert.AreEqual(100, DailyRewardManager.Claim());
            Assert.IsFalse(DailyRewardManager.CanClaim);
            Assert.AreEqual(0, DailyRewardManager.Claim());
            Assert.AreEqual(start + 100, Wallet.Coins);

            Clock.Now = () => new DateTime(2026, 3, 11, 9, 0, 0);
            Assert.AreEqual(1, DailyRewardManager.TodayIndex);
            Assert.AreEqual(150, DailyRewardManager.Claim());

            Clock.Now = () => new DateTime(2026, 3, 13, 9, 0, 0); // skipped a day
            Assert.AreEqual(0, DailyRewardManager.TodayIndex);
            Assert.AreEqual(100, DailyRewardManager.Claim());
        }

        [Test]
        public void DailyReward_SeventhDayPaysBig()
        {
            for (int d = 0; d < 7; d++)
            {
                Clock.Now = () => new DateTime(2026, 3, 10).AddDays(d);
                int r = DailyRewardManager.Claim();
                Assert.AreEqual(DailyRewardManager.Rewards[d], r);
            }
            Assert.AreEqual(1000, DailyRewardManager.Rewards[6]);
        }

        [Test]
        public void Missions_ProgressClaimAndDailyRollover()
        {
            var daily = MissionManager.Daily;
            Assert.AreEqual(3, daily.Count);
            var types = new System.Collections.Generic.HashSet<int>();
            foreach (var m in daily) types.Add(m.type);
            Assert.AreEqual(3, types.Count, "missions must be distinct");

            foreach (var m in daily) Assert.IsFalse(MissionManager.Claim(m), "cannot claim incomplete");

            var run = new RunStats { distance = 100000, coins = 1000, nearMisses = 100, overtakes = 500, nitroUses = 50, score = 1000000 };
            var done = MissionManager.ReportRun(run);
            Assert.AreEqual(3, done.Count);
            int coins = Wallet.Coins;
            int total = 0;
            foreach (var m in daily) { Assert.IsTrue(MissionManager.Claim(m)); total += m.reward; }
            Assert.AreEqual(coins + total, Wallet.Coins);
            foreach (var m in daily) Assert.IsFalse(MissionManager.Claim(m), "cannot claim twice");

            Clock.Now = () => new DateTime(2026, 3, 11, 9, 0, 0);
            var next = MissionManager.Daily;
            Assert.AreEqual(3, next.Count);
            foreach (var m in next) { Assert.IsFalse(m.claimed); Assert.AreEqual(0, m.progress); }
        }

        [Test]
        public void Achievements_UnlockFromStatsAndClaimOnce()
        {
            var a = MissionManager.Achievements.Find(x => x.id == "km_5");
            Assert.IsFalse(MissionManager.IsUnlocked(a));
            SaveSystem.Data.stats.totalDistance = 6000;
            Assert.IsTrue(MissionManager.IsUnlocked(a));
            int c = Wallet.Coins;
            Assert.IsTrue(MissionManager.Claim(a));
            Assert.AreEqual(c + a.reward, Wallet.Coins);
            Assert.IsFalse(MissionManager.Claim(a));
        }

        [Test]
        public void Ads_DisabledByDefault_NeverGrantsRewards()
        {
            AdsManager.Initialize();
            Assert.IsFalse(GameAssets.I.ads.adsEnabled, "ads must ship disabled");
            Assert.IsFalse(AdsManager.RewardedReady);
            bool? result = null;
            AdsManager.ShowRewarded(ok => result = ok);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Rewards_ScaleWithPerformance()
        {
            var cfg = GameAssets.I.config;
            var low = RewardCalculator.Calculate(new RunStats { distance = 500, coins = 3 }, cfg);
            var high = RewardCalculator.Calculate(new RunStats { distance = 5000, coins = 40, nearMisses = 20 }, cfg);
            Assert.Greater(high.Total, low.Total);
            Assert.AreEqual(40 * cfg.coinValue + 5 * cfg.coinsPerKm + 20 * cfg.coinsPerNearMiss, high.Total);
        }

        [Test]
        public void Lanes_AreSymmetricAndRoundTrip()
        {
            Assert.AreEqual(-GameConstants.LaneWidth, GameConstants.LaneX(0), 1e-4);
            Assert.AreEqual(0f, GameConstants.LaneX(1), 1e-4);
            Assert.AreEqual(GameConstants.LaneWidth, GameConstants.LaneX(2), 1e-4);
            for (int l = 0; l < 3; l++) Assert.AreEqual(l, GameConstants.NearestLane(GameConstants.LaneX(l) + 0.4f));
        }

        [Test]
        public void Build_IsLockedToPortrait()
        {
            Assert.AreEqual(UnityEditor.UIOrientation.Portrait, UnityEditor.PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(UnityEditor.PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(UnityEditor.PlayerSettings.allowedAutorotateToLandscapeRight);
            Assert.IsFalse(UnityEditor.PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.AreEqual(1080f, UIKit.RefWidth);
            Assert.AreEqual(1920f, UIKit.RefHeight);
        }

        [Test]
        public void PortraitCamera_KeepsAllLanesInViewOnAnyAspect()
        {
            // Horizontal FOV must stay the same for 9:16, 20:9 and 3:4 so the road always fits the width.
            foreach (float aspect in new[] { 9f / 16f, 9f / 20f, 3f / 4f })
            {
                float v = ITR.CameraRig.VerticalFov(62f, aspect);
                float h = 2f * Mathf.Atan(Mathf.Tan(v * 0.5f * Mathf.Deg2Rad) * aspect) * Mathf.Rad2Deg;
                Assert.AreEqual(62f, h, 0.5f, "aspect " + aspect);
            }
        }

        [Test]
        public void Catalog_HasSixGarageCarsAndSixTrafficKinds()
        {
            Assert.AreEqual(6, GameAssets.I.vehicles.vehicles.Count);
            var shapes = new System.Collections.Generic.HashSet<VehicleShape>();
            foreach (var t in GameAssets.I.traffic.types) shapes.Add(t.shape);
            Assert.IsTrue(shapes.Contains(VehicleShape.Bus));
            Assert.IsTrue(shapes.Contains(VehicleShape.Truck));
            Assert.IsTrue(shapes.Contains(VehicleShape.Motorcycle));
            Assert.IsTrue(shapes.Contains(VehicleShape.AutoRickshaw));
            Assert.IsTrue(shapes.Contains(VehicleShape.Taxi));
        }
    }
}
