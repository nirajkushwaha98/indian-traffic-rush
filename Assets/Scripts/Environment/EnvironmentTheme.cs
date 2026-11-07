using UnityEngine;
using UnityEngine.Rendering;

namespace ITR
{
    public enum TimeOfDay { Day, Sunset }

    /// <summary>Applies lighting, sky, fog and lamp glow for the Day or Sunset environment.</summary>
    public static class EnvironmentTheme
    {
        public static TimeOfDay Current { get; private set; }
        static Material skyInstance;

        public static TimeOfDay Resolve(ThemeChoice choice)
        {
            switch (choice)
            {
                case ThemeChoice.Day: return TimeOfDay.Day;
                case ThemeChoice.Sunset: return TimeOfDay.Sunset;
                default: return Random.value < 0.75f ? TimeOfDay.Day : TimeOfDay.Sunset; // Auto favours clear daylight
            }
        }

        public static Light CreateSun()
        {
            var go = new GameObject("Sun");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.7f;
            return l;
        }

        public static void Apply(TimeOfDay tod, Light sun, float fogStart, float fogEnd)
        {
            Current = tod;
            bool sunset = tod == TimeOfDay.Sunset;
            if (skyInstance == null) skyInstance = new Material(GameAssets.I.skybox) { name = "SkyInstance" };

            if (sun != null)
            {
                sun.transform.rotation = sunset ? Quaternion.Euler(18f, -35f, 0f) : Quaternion.Euler(55f, -28f, 0f);
                sun.color = sunset ? new Color(1f, 0.7f, 0.48f) : new Color(1f, 0.97f, 0.9f);
                sun.intensity = sunset ? 1.25f : 1.5f;
                sun.shadowStrength = 0.55f; // readable shadows, never pitch black
                bool shadows = SaveSystem.Data.settings.graphics == (int)GraphicsLevel.High;
                sun.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                RenderSettings.sun = sun;
            }

            if (skyInstance.HasProperty("_SkyTint"))
            {
                skyInstance.SetColor("_SkyTint", sunset ? new Color(0.85f, 0.45f, 0.4f) : new Color(0.45f, 0.6f, 0.85f));
                skyInstance.SetColor("_GroundColor", sunset ? new Color(0.45f, 0.3f, 0.25f) : new Color(0.55f, 0.55f, 0.5f));
                skyInstance.SetFloat("_AtmosphereThickness", sunset ? 1.6f : 0.9f);
                skyInstance.SetFloat("_Exposure", sunset ? 1.2f : 1.3f);
                skyInstance.SetFloat("_SunSize", sunset ? 0.07f : 0.04f);
            }
            RenderSettings.skybox = skyInstance;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sunset ? new Color(0.72f, 0.58f, 0.58f) : new Color(0.72f, 0.8f, 0.92f);
            RenderSettings.ambientEquatorColor = sunset ? new Color(0.64f, 0.5f, 0.42f) : new Color(0.68f, 0.68f, 0.64f);
            RenderSettings.ambientGroundColor = sunset ? new Color(0.34f, 0.27f, 0.24f) : new Color(0.42f, 0.4f, 0.35f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            // Light haze matching the horizon sky: hides far spawns gently without darkening the road.
            RenderSettings.fogColor = sunset ? new Color(0.95f, 0.72f, 0.58f) : new Color(0.8f, 0.86f, 0.93f);

            if (RoadBuilder.LampMaterial != null)
                MaterialFactory.SetColor(RoadBuilder.LampMaterial, sunset ? new Color(1f, 0.85f, 0.5f) : new Color(0.82f, 0.82f, 0.78f));
        }

        public static bool HeadlightsOn => Current == TimeOfDay.Sunset;
    }
}
