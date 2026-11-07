using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>One kind of AI traffic (car, bus, truck, bike, auto-rickshaw, taxi).</summary>
    [CreateAssetMenu(menuName = "ITR/Traffic Vehicle", fileName = "Traffic")]
    public class TrafficVehicleData : ScriptableObject
    {
        public string id = "car";
        public VehicleShape shape = VehicleShape.Sedan;
        public Vector2 speedKmh = new Vector2(45f, 70f);
        [Tooltip("Relative spawn weight at difficulty 0 and at difficulty 1.")]
        public Vector2 spawnWeight = new Vector2(1f, 1f);
        [Range(0f, 1f)] public float laneChangeChance;
        public Color[] colors = { Color.white };
        [Tooltip("How many differently coloured meshes are pre-built for pooling.")]
        public int poolSize = 6;

        public static TrafficVehicleData Make(string id, VehicleShape shape, Vector2 speed, Vector2 weight,
            float laneChange, int pool, params Color[] colors)
        {
            var t = CreateInstance<TrafficVehicleData>();
            t.name = id;
            t.id = id; t.shape = shape; t.speedKmh = speed; t.spawnWeight = weight;
            t.laneChangeChance = laneChange; t.poolSize = pool; t.colors = colors;
            return t;
        }
    }
}
