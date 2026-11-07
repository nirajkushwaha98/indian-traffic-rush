using System;
using System.Collections.Generic;

namespace ITR
{
    /// <summary>Everything that persists between sessions. Serialized with JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int coins = 500;
        public int bestScore;
        public float bestDistance;
        public float bestSpeedKmh;
        public string selectedVehicle = "hatchback";
        public List<OwnedVehicle> vehicles = new List<OwnedVehicle>();
        public SettingsData settings = new SettingsData();
        public DailyMissionState dailyMissions = new DailyMissionState();
        public DailyRewardState dailyReward = new DailyRewardState();
        public LifetimeStats stats = new LifetimeStats();
        public List<string> claimedAchievements = new List<string>();
        public bool tutorialSeen;

        public OwnedVehicle GetVehicle(string id)
        {
            for (int i = 0; i < vehicles.Count; i++)
                if (vehicles[i].id == id) return vehicles[i];
            return null;
        }

        public bool Owns(string id) => GetVehicle(id) != null;
    }

    [Serializable]
    public class OwnedVehicle
    {
        public string id;
        public int colorIndex;
        public int speedLevel;
        public int handlingLevel;
        public int brakingLevel;
    }

    public enum ControlMode { Swipe = 0, Buttons = 1 }
    public enum GraphicsLevel { Low = 0, High = 1 }
    public enum ThemeChoice { Auto = 0, Day = 1, Sunset = 2 }

    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 0.6f;
        public float sfxVolume = 1f;
        public bool vibration = true;
        public int controlMode = (int)ControlMode.Swipe;
        public int graphics = (int)GraphicsLevel.High;
        public int theme = (int)ThemeChoice.Day; // clear daylight by default
    }

    [Serializable]
    public class MissionProgress
    {
        public string id;
        public int type;
        public int target;
        public int reward;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    public class DailyMissionState
    {
        public string date = "";
        public List<MissionProgress> missions = new List<MissionProgress>();
    }

    [Serializable]
    public class DailyRewardState
    {
        public string lastClaimDate = "";
        /// <summary>Number of consecutive days claimed (1..n). 0 = never claimed.</summary>
        public int streak;
    }

    [Serializable]
    public class LifetimeStats
    {
        public float totalDistance;
        public int totalCoins;
        public int totalNearMisses;
        public int totalOvertakes;
        public int runs;
        public int nitroUses;
        public int bestNearMissCombo;
    }
}
