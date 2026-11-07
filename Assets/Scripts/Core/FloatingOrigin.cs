using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ITR
{
    /// <summary>
    /// Keeps world coordinates small on very long runs by shifting every root object back along Z.
    /// Distance is tracked separately, so the shift is invisible to gameplay.
    /// </summary>
    public static class FloatingOrigin
    {
        public const float Threshold = 1600f;
        public static event Action<float> Shifted;

        public static bool ShouldShift(float playerZ) => playerZ > Threshold;

        public static void Shift(float dz)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var offset = new Vector3(0, 0, dz);
            foreach (var r in roots) r.transform.position -= offset;
            Physics.SyncTransforms();
            Shifted?.Invoke(dz);
        }
    }

    public static class Haptics
    {
        public static void Buzz()
        {
            if (!SaveSystem.Data.settings.vibration) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
