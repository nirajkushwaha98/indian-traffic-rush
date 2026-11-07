using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    public enum VehicleShape
    {
        Hatchback, Sedan, SUV, Sports, Taxi, Police,
        Bus, Truck, Motorcycle, AutoRickshaw
    }

    /// <summary>A player-drivable car shown in the garage.</summary>
    [CreateAssetMenu(menuName = "ITR/Vehicle", fileName = "Vehicle")]
    public class VehicleData : ScriptableObject
    {
        public string id = "hatchback";
        public string displayName = "City Hatch";
        [TextArea] public string description = "";
        public VehicleShape shape = VehicleShape.Hatchback;
        public int price;

        [Header("Base stats")]
        public float topSpeedKmh = 150f;
        [Tooltip("m/s² while accelerating")] public float acceleration = 7f;
        [Tooltip("Lateral speed in m/s when switching lanes")] public float handling = 11f;
        [Tooltip("m/s² while braking")] public float braking = 14f;

        [Header("Upgrades (per level)")]
        public int maxUpgradeLevel = 5;
        public float speedPerLevelKmh = 7f;
        public float handlingPerLevel = 0.9f;
        public float brakingPerLevel = 2f;
        public int upgradeBaseCost = 300;

        [Header("Paint")]
        public Color[] colors = { new Color(0.85f, 0.1f, 0.1f), Color.white, new Color(0.1f, 0.3f, 0.8f) };

        [Tooltip("True while the model is the procedural placeholder built by VehicleBuilder.")]
        public bool placeholderModel = true;

        public int UpgradeCost(int currentLevel) =>
            currentLevel >= maxUpgradeLevel ? -1 : upgradeBaseCost * (currentLevel + 1) + (currentLevel * currentLevel) * 100;

        public float TopSpeedKmh(int lvl) => topSpeedKmh + speedPerLevelKmh * lvl;
        public float Handling(int lvl) => handling + handlingPerLevel * lvl;
        public float Braking(int lvl) => braking + brakingPerLevel * lvl;
        public float Acceleration(int lvl) => acceleration + 0.35f * lvl;

        public static VehicleData Make(string id, string name, VehicleShape shape, int price, float top, float acc,
            float handling, float braking, string desc, params Color[] colors)
        {
            var v = CreateInstance<VehicleData>();
            v.name = id;
            v.id = id; v.displayName = name; v.shape = shape; v.price = price;
            v.topSpeedKmh = top; v.acceleration = acc; v.handling = handling; v.braking = braking;
            v.description = desc;
            v.colors = colors;
            v.upgradeBaseCost = Mathf.Max(250, price / 10);
            return v;
        }
    }
}
