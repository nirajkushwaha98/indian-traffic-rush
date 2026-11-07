using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>
    /// Persistent root: loads the save, starts audio/ads, applies quality, and performs faded scene loads.
    /// Every scene bootstrap calls <see cref="Ensure"/> so any scene can be played directly in the editor.
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        public static AppRoot I { get; private set; }
        Image fade;
        bool loading;

        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("AppRoot");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AppRoot>();
            I.Init();
        }

        void Init()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SaveSystem.Load();
            MainThread.Ensure();
            AudioManager.Ensure();
            AdsManager.Initialize();
            ApplyQuality();
            BuildFadeCanvas();
        }

        void BuildFadeCanvas()
        {
            var go = new GameObject("FadeCanvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var img = new GameObject("Fade").AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.color = new Color(0.03f, 0.03f, 0.05f, 0f);
            img.raycastTarget = false;
            fade = img;
        }

        public static void ApplyQuality()
        {
            bool high = SaveSystem.Data.settings.graphics == (int)GraphicsLevel.High;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            // Editing the pipeline asset in the editor would dirty the asset on disk, so only do it in players.
            if (urp != null && !Application.isEditor)
            {
                urp.renderScale = high ? 1f : 0.85f;
                urp.msaaSampleCount = high ? 4 : 2; // crisp edges and lane markings
                urp.shadowDistance = high ? 70f : 0f;
            }
            QualitySettings.shadows = high ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
        }

        public static void LoadScene(string scene)
        {
            Ensure();
            if (I.loading) return;
            I.StartCoroutine(I.LoadRoutine(scene));
        }

        IEnumerator LoadRoutine(string scene)
        {
            loading = true;
            fade.raycastTarget = true;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, t);
                yield return null;
            }
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, 1f);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            for (float t = 1; t > 0f; t -= Time.unscaledDeltaTime / 0.3f)
            {
                fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, t);
                yield return null;
            }
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, 0f);
            fade.raycastTarget = false;
            loading = false;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && SaveSystem.IsLoaded) SaveSystem.Save();
        }

        void OnApplicationQuit()
        {
            if (SaveSystem.IsLoaded) SaveSystem.Save();
        }
    }
}
