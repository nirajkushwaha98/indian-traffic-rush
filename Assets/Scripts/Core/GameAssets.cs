using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Root registry asset at Resources/GameAssets. Holding the base materials here guarantees their shaders
    /// are included in player builds. Created by ITR ▸ Setup Project.
    /// </summary>
    public class GameAssets : ScriptableObject
    {
        public GameConfig config;
        public VehicleCatalog vehicles;
        public TrafficCatalog traffic;
        public AdsConfig ads;

        [Header("Base materials")]
        public Material lit;
        public Material unlit;
        public Material particleAdditive;
        public Material particleAlpha;
        public Material skybox;

        [Header("Links")]
        public string privacyPolicyUrl = "https://example.com/indian-traffic-rush/privacy";

        static GameAssets instance;

        public static GameAssets I
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameAssets>("GameAssets");
                    if (instance == null) instance = CreateFallback();
                    instance.FillMissing();
                }
                return instance;
            }
        }

        static GameAssets CreateFallback()
        {
            Debug.LogWarning("[ITR] Resources/GameAssets missing - using runtime defaults. Run ITR > Setup Project.");
            return CreateInstance<GameAssets>();
        }

        void FillMissing()
        {
            if (config == null) config = GameConfig.CreateDefault();
            if (vehicles == null || vehicles.vehicles.Count == 0)
            {
                vehicles = CreateInstance<VehicleCatalog>();
                vehicles.vehicles = VehicleCatalog.DefaultVehicles();
            }
            if (traffic == null || traffic.types.Count == 0)
            {
                traffic = CreateInstance<TrafficCatalog>();
                traffic.types = TrafficCatalog.DefaultTypes();
            }
            if (ads == null) ads = CreateInstance<AdsConfig>();
            if (lit == null) lit = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            if (unlit == null) unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (particleAdditive == null) particleAdditive = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (particleAlpha == null) particleAlpha = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (skybox == null) skybox = new Material(Shader.Find("Skybox/Procedural"));
        }
    }
}
