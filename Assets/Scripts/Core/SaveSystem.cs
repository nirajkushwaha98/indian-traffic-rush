using System;
using System.IO;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// JSON save file in Application.persistentDataPath. Writes go to a temp file first and are then
    /// swapped in, so a crash mid-write never corrupts the existing save.
    /// </summary>
    public static class SaveSystem
    {
        static SaveData data;

        /// <summary>Set by tests so they never touch the player's real save.</summary>
        public static string OverridePath;

        public static event Action Changed;

        public static string FilePath =>
            !string.IsNullOrEmpty(OverridePath) ? OverridePath : Path.Combine(Application.persistentDataPath, "itr_save.json");

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static bool IsLoaded => data != null;

        public static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(FilePath))
                    data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ITR] Save file unreadable, backing up and starting fresh: " + e.Message);
                TryBackupCorrupt();
            }

            if (data == null) data = new SaveData();
            Validate(data);
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                string path = FilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError("[ITR] Failed to save: " + e.Message);
            }
            Changed?.Invoke();
        }

        /// <summary>Wipes all progress (Settings → Reset Progress).</summary>
        public static void ResetAll()
        {
            var keepSettings = data != null ? data.settings : new SettingsData();
            data = new SaveData { settings = keepSettings };
            Validate(data);
            Save();
        }

        /// <summary>Drops the in-memory copy (tests).</summary>
        public static void Unload() => data = null;

        static void Validate(SaveData d)
        {
            if (d.vehicles == null) d.vehicles = new System.Collections.Generic.List<OwnedVehicle>();
            if (d.settings == null) d.settings = new SettingsData();
            if (d.dailyMissions == null) d.dailyMissions = new DailyMissionState();
            if (d.dailyMissions.missions == null) d.dailyMissions.missions = new System.Collections.Generic.List<MissionProgress>();
            if (d.dailyReward == null) d.dailyReward = new DailyRewardState();
            if (d.stats == null) d.stats = new LifetimeStats();
            if (d.claimedAchievements == null) d.claimedAchievements = new System.Collections.Generic.List<string>();
            if (d.coins < 0) d.coins = 0;

            // The starter car is always owned.
            if (!d.Owns("hatchback")) d.vehicles.Add(new OwnedVehicle { id = "hatchback" });
            if (string.IsNullOrEmpty(d.selectedVehicle) || !d.Owns(d.selectedVehicle)) d.selectedVehicle = "hatchback";
        }

        static void TryBackupCorrupt()
        {
            try { File.Copy(FilePath, FilePath + ".corrupt", true); } catch { /* ignore */ }
        }
    }
}
