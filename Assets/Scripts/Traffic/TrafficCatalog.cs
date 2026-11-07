using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    [CreateAssetMenu(menuName = "ITR/Traffic Catalog", fileName = "TrafficCatalog")]
    public class TrafficCatalog : ScriptableObject
    {
        public List<TrafficVehicleData> types = new List<TrafficVehicleData>();

        public static List<TrafficVehicleData> DefaultTypes()
        {
            Color C(float r, float g, float b) => new Color(r, g, b);
            return new List<TrafficVehicleData>
            {
                TrafficVehicleData.Make("car", VehicleShape.Hatchback, new Vector2(38, 60), new Vector2(3f, 2.2f), 0.05f, 8,
                    C(0.92f,0.92f,0.92f), C(0.7f,0.72f,0.75f), C(0.6f,0.08f,0.08f), C(0.1f,0.25f,0.6f), C(0.2f,0.2f,0.2f), C(0.85f,0.5f,0.1f)),
                TrafficVehicleData.Make("sedan", VehicleShape.Sedan, new Vector2(42, 64), new Vector2(2f, 1.8f), 0.05f, 6,
                    C(0.95f,0.95f,0.95f), C(0.15f,0.15f,0.18f), C(0.45f,0.47f,0.5f), C(0.25f,0.1f,0.1f)),
                TrafficVehicleData.Make("taxi", VehicleShape.Taxi, new Vector2(34, 54), new Vector2(1.2f, 1.2f), 0.1f, 4,
                    C(0.1f,0.1f,0.1f)),
                TrafficVehicleData.Make("bus", VehicleShape.Bus, new Vector2(30, 46), new Vector2(0.6f, 1.2f), 0f, 4,
                    C(0.85f,0.25f,0.1f), C(0.15f,0.4f,0.7f), C(0.2f,0.55f,0.25f)),
                TrafficVehicleData.Make("truck", VehicleShape.Truck, new Vector2(26, 42), new Vector2(0.6f, 1.4f), 0f, 5,
                    C(0.95f,0.6f,0.05f), C(0.1f,0.45f,0.75f), C(0.8f,0.1f,0.15f), C(0.15f,0.55f,0.3f)),
                TrafficVehicleData.Make("motorcycle", VehicleShape.Motorcycle, new Vector2(35, 55), new Vector2(1.4f, 1.2f), 0.25f, 5,
                    C(0.1f,0.1f,0.1f), C(0.75f,0.05f,0.05f), C(0.1f,0.2f,0.6f)),
                TrafficVehicleData.Make("auto", VehicleShape.AutoRickshaw, new Vector2(22, 36), new Vector2(1.6f, 1.3f), 0.3f, 5,
                    C(0.1f,0.5f,0.18f)),
            };
        }
    }
}
