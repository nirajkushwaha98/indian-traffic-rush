using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    [CreateAssetMenu(menuName = "ITR/Vehicle Catalog", fileName = "VehicleCatalog")]
    public class VehicleCatalog : ScriptableObject
    {
        public List<VehicleData> vehicles = new List<VehicleData>();

        public VehicleData Get(string id)
        {
            for (int i = 0; i < vehicles.Count; i++)
                if (vehicles[i] != null && vehicles[i].id == id) return vehicles[i];
            return vehicles.Count > 0 ? vehicles[0] : null;
        }

        public int IndexOf(string id)
        {
            for (int i = 0; i < vehicles.Count; i++)
                if (vehicles[i] != null && vehicles[i].id == id) return i;
            return 0;
        }

        /// <summary>The six garage cars. Used by the editor setup and as a runtime fallback.</summary>
        public static List<VehicleData> DefaultVehicles()
        {
            Color C(float r, float g, float b) => new Color(r, g, b);
            return new List<VehicleData>
            {
                VehicleData.Make("hatchback", "City Hatch", VehicleShape.Hatchback, 0, 150, 7f, 11f, 14f,
                    "Nimble starter hatch. Cheap to run, easy to love.",
                    C(0.85f,0.12f,0.1f), C(0.95f,0.95f,0.95f), C(0.1f,0.35f,0.85f), C(1f,0.6f,0.05f), C(0.15f,0.15f,0.17f)),
                VehicleData.Make("sedan", "Metro Sedan", VehicleShape.Sedan, 2500, 168, 8f, 11.5f, 16f,
                    "Comfortable family sedan with a strong highway pull.",
                    C(0.75f,0.77f,0.8f), C(0.08f,0.18f,0.4f), C(0.5f,0.05f,0.08f), C(0.95f,0.95f,0.95f), C(0.1f,0.1f,0.1f)),
                VehicleData.Make("taxi", "Kaali-Peeli Taxi", VehicleShape.Taxi, 3500, 160, 7.5f, 13f, 15f,
                    "The legendary black-and-yellow city cab. Knows every gap.",
                    C(0.1f,0.1f,0.1f), C(0.05f,0.35f,0.15f), C(0.12f,0.12f,0.35f)),
                VehicleData.Make("suv", "Ghat Runner SUV", VehicleShape.SUV, 5500, 176, 8.5f, 10.5f, 18f,
                    "Tall, tough and planted. Brakes like an anchor.",
                    C(0.12f,0.12f,0.12f), C(0.95f,0.95f,0.95f), C(0.35f,0.25f,0.15f), C(0.6f,0.05f,0.05f), C(0.2f,0.35f,0.25f)),
                VehicleData.Make("police", "Highway Patrol", VehicleShape.Police, 9000, 195, 10f, 14f, 20f,
                    "Interceptor with sirens. Fast hands, faster feet.",
                    C(0.96f,0.96f,0.96f), C(0.1f,0.2f,0.45f)),
                VehicleData.Make("sports", "Rush GT", VehicleShape.Sports, 15000, 230, 12f, 15f, 22f,
                    "Low, loud and lightning quick. The king of the expressway.",
                    C(1f,0.45f,0f), C(0.9f,0.05f,0.1f), C(0.95f,0.85f,0.1f), C(0.1f,0.6f,0.3f), C(0.1f,0.1f,0.12f), C(0.2f,0.4f,1f)),
            };
        }
    }
}
