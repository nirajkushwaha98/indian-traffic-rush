using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ITR.EditorTools
{
    /// <summary>
    /// ITR ▸ Setup Project — idempotent, never overwrites existing assets:
    /// materials, ScriptableObjects, Resources/GameAssets, MainMenu + Game scenes, build settings,
    /// Android player settings, generated (placeholder) app icon and splash logo.
    /// Run from the menu or via -executeMethod ITR.EditorTools.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        public const string BundleId = "com.indiantrafficrush.game";
        const string Mat = "Assets/Materials";
        const string SO = "Assets/ScriptableObjects";
        const string Tex = "Assets/Textures";
        const string Scenes = "Assets/Scenes";

        [MenuItem("ITR/Setup Project", priority = 0)]
        public static void SetupFromMenu()
        {
            Run();
            EditorUtility.DisplayDialog("Indian Traffic Rush", "Project setup complete.\nOpen Assets/Scenes/MainMenu.unity and press Play.", "OK");
        }

        public static void RunBatch()
        {
            Run();
            Debug.Log("[ITR] Setup complete");
        }

        public static void Run()
        {
            Dir(Mat); Dir(SO); Dir(SO + "/Vehicles"); Dir(SO + "/Traffic"); Dir(Tex); Dir(Scenes); Dir("Assets/Resources");

            var soft = SoftParticleTexture();
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>("Assets/Resources/GameAssets.asset");
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(assets, "Assets/Resources/GameAssets.asset");
            }

            assets.lit = assets.lit != null ? assets.lit : MakeMat("M_Lit", "Universal Render Pipeline/Simple Lit", null);
            assets.unlit = assets.unlit != null ? assets.unlit : MakeMat("M_Unlit", "Universal Render Pipeline/Unlit", null);
            assets.particleAdditive = assets.particleAdditive != null ? assets.particleAdditive : MakeParticleMat("M_ParticleAdditive", true, soft);
            assets.particleAlpha = assets.particleAlpha != null ? assets.particleAlpha : MakeParticleMat("M_ParticleAlpha", false, soft);
            assets.skybox = assets.skybox != null ? assets.skybox : MakeMat("M_Skybox", "Skybox/Procedural", null);

            assets.config = assets.config != null ? assets.config : CreateOrLoad<GameConfig>(SO + "/GameConfig.asset");
            assets.ads = assets.ads != null ? assets.ads : CreateOrLoad<AdsConfig>(SO + "/AdsConfig.asset");

            if (assets.vehicles == null)
            {
                var cat = CreateOrLoad<VehicleCatalog>(SO + "/VehicleCatalog.asset");
                if (cat.vehicles.Count == 0)
                {
                    foreach (var v in VehicleCatalog.DefaultVehicles())
                    {
                        string p = SO + "/Vehicles/" + v.id + ".asset";
                        var existing = AssetDatabase.LoadAssetAtPath<VehicleData>(p);
                        if (existing == null) { AssetDatabase.CreateAsset(v, p); existing = v; }
                        cat.vehicles.Add(existing);
                    }
                    EditorUtility.SetDirty(cat);
                }
                assets.vehicles = cat;
            }
            if (assets.traffic == null)
            {
                var cat = CreateOrLoad<TrafficCatalog>(SO + "/TrafficCatalog.asset");
                if (cat.types.Count == 0)
                {
                    foreach (var t in TrafficCatalog.DefaultTypes())
                    {
                        string p = SO + "/Traffic/" + t.id + ".asset";
                        var existing = AssetDatabase.LoadAssetAtPath<TrafficVehicleData>(p);
                        if (existing == null) { AssetDatabase.CreateAsset(t, p); existing = t; }
                        cat.types.Add(existing);
                    }
                    EditorUtility.SetDirty(cat);
                }
                assets.traffic = cat;
            }
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();

            CreateScene(Scenes + "/MainMenu.unity", typeof(MenuBootstrap));
            CreateScene(Scenes + "/Game.unity", typeof(GameBootstrap));
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Scenes + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(Scenes + "/Game.unity", true),
            };

            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ helpers

        static void Dir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                Dir(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static Material MakeMat(string name, string shader, Texture tex)
        {
            string path = Mat + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("[ITR] Shader not found: " + shader); return null; }
            m = new Material(sh) { name = name };
            if (tex != null) m.SetTexture("_BaseMap", tex);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material MakeParticleMat(string name, bool additive, Texture tex)
        {
            string path = Mat + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Texture2D SoftParticleTexture()
        {
            string path = Tex + "/T_SoftParticle.png";
            if (!File.Exists(path))
            {
                int s = 64;
                var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        t.SetPixel(x, y, new Color(1, 1, 1, a * a));
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void CreateScene(string path, System.Type bootstrap)
        {
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent(bootstrap);
            EditorSceneManager.SaveScene(scene, path);
        }

        // ------------------------------------------------------------------ player settings

        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Indian Traffic Rush";
            PlayerSettings.productName = "Indian Traffic Rush";
            var android = NamedBuildTarget.Android;
            if (PlayerSettings.GetApplicationIdentifier(android).StartsWith("com.Unity") ||
                PlayerSettings.GetApplicationIdentifier(android).StartsWith("com.DefaultCompany") ||
                string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(android)))
                PlayerSettings.SetApplicationIdentifier(android, BundleId);
            if (PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "0.1.0" || string.IsNullOrEmpty(PlayerSettings.bundleVersion))
                PlayerSettings.bundleVersion = "1.0.0";
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            // Portrait only (9:16). A fixed orientation means the activity is locked and never rotates.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.resizeableActivity = false;
            PlayerSettings.runInBackground = false;

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.SetMobileMTRendering(android, true);

            var icon = GenerateIcon();
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                var splash = AssetDatabase.LoadAssetAtPath<Sprite>(Tex + "/SplashLogo.png");
                if (splash != null)
                {
                    PlayerSettings.SplashScreen.show = true;
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                    PlayerSettings.SplashScreen.backgroundColor = new Color(0.04f, 0.05f, 0.09f);
                    PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
                    PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, splash) };
                }
            }
        }

        /// <summary>Draws an original placeholder icon (road + car, no text) and a splash logo.</summary>
        static Texture2D GenerateIcon()
        {
            string iconPath = Tex + "/AppIcon.png";
            string splashPath = Tex + "/SplashLogo.png";
            if (!File.Exists(iconPath))
            {
                var t = DrawIcon(1024);
                File.WriteAllBytes(iconPath, t.EncodeToPNG());
                File.WriteAllBytes(splashPath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(iconPath);
                AssetDatabase.ImportAsset(splashPath);
                var si = (TextureImporter)AssetImporter.GetAtPath(splashPath);
                si.textureType = TextureImporterType.Sprite;
                si.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        static Texture2D DrawIcon(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color top = new Color(1f, 0.55f, 0.12f), bottom = new Color(0.55f, 0.1f, 0.25f);
            Color road = new Color(0.16f, 0.16f, 0.19f), white = Color.white;
            for (int y = 0; y < s; y++)
            {
                float v = y / (float)s; // 0 bottom .. 1 top
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)s;
                    Color c = Color.Lerp(bottom, top, v);
                    // sun
                    float sd = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.68f));
                    if (sd < 0.16f) c = Color.Lerp(new Color(1f, 0.92f, 0.5f), c, Mathf.Clamp01((sd - 0.12f) / 0.04f));
                    // road trapezoid from bottom (wide) to horizon at v=0.6
                    if (v < 0.6f)
                    {
                        float k = v / 0.6f;
                        float half = Mathf.Lerp(0.48f, 0.03f, k);
                        if (Mathf.Abs(u - 0.5f) < half)
                        {
                            c = road;
                            float lane = (u - 0.5f) / half; // -1..1
                            bool dash = Mathf.Repeat(Mathf.Pow(1f - k, 1.6f) * 10f, 1f) < 0.5f;
                            if (dash && (Mathf.Abs(Mathf.Abs(lane) - 0.33f) < 0.025f)) c = white;
                            if (Mathf.Abs(lane) > 0.93f) c = new Color(1f, 0.8f, 0.1f);
                        }
                    }
                    px[y * s + x] = c;
                }
            }
            // Car (rear view) — rounded body, window, lights, wheels
            void Rect(float x0, float y0, float x1, float y1, Color c, float r = 0f)
            {
                for (int y = (int)(y0 * s); y < (int)(y1 * s); y++)
                    for (int x = (int)(x0 * s); x < (int)(x1 * s); x++)
                    {
                        if (x < 0 || y < 0 || x >= s || y >= s) continue;
                        float cx = Mathf.Clamp(x, x0 * s + r * s, x1 * s - r * s);
                        float cy = Mathf.Clamp(y, y0 * s + r * s, y1 * s - r * s);
                        if (r > 0 && Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) > r * s) continue;
                        px[y * s + x] = c;
                    }
            }
            Rect(0.25f, 0.1f, 0.35f, 0.22f, new Color(0.05f, 0.05f, 0.05f), 0.02f);
            Rect(0.65f, 0.1f, 0.75f, 0.22f, new Color(0.05f, 0.05f, 0.05f), 0.02f);
            Rect(0.22f, 0.16f, 0.78f, 0.36f, new Color(0.95f, 0.95f, 0.97f), 0.05f);
            Rect(0.3f, 0.34f, 0.7f, 0.5f, new Color(0.95f, 0.95f, 0.97f), 0.06f);
            Rect(0.34f, 0.37f, 0.66f, 0.47f, new Color(0.12f, 0.18f, 0.28f), 0.03f);
            Rect(0.25f, 0.26f, 0.37f, 0.31f, new Color(0.95f, 0.1f, 0.1f), 0.015f);
            Rect(0.63f, 0.26f, 0.75f, 0.31f, new Color(0.95f, 0.1f, 0.1f), 0.015f);
            Rect(0.42f, 0.19f, 0.58f, 0.23f, new Color(1f, 0.85f, 0.1f), 0.01f);
            // tricolour band across the boot
            Rect(0.22f, 0.32f, 0.78f, 0.333f, new Color(1f, 0.55f, 0.12f));
            Rect(0.22f, 0.307f, 0.78f, 0.32f, Color.white);
            Rect(0.22f, 0.294f, 0.78f, 0.307f, new Color(0.07f, 0.55f, 0.2f));
            // speed streaks
            for (int i = 0; i < 4; i++)
                Rect(0.04f + i * 0.02f, 0.25f + i * 0.08f, 0.16f + i * 0.02f, 0.262f + i * 0.08f, new Color(1, 1, 1, 0.9f), 0.006f);
            for (int i = 0; i < 4; i++)
                Rect(0.84f - i * 0.02f, 0.25f + i * 0.08f, 0.96f - i * 0.02f, 0.262f + i * 0.08f, new Color(1, 1, 1, 0.9f), 0.006f);
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
