using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ITR
{
    /// <summary>Chase camera with speed-based FOV, speed lines, shake and a crash orbit.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        PlayerCar target;
        ParticleSystem speedLines;
        float shakeAmp, shakeTime, shakeDur;
        float crashOrbit;
        Vector3 vel;
        // Portrait chase cam: high and behind, looking well ahead so the car sits in the lower-middle
        // of a tall screen and the road ahead fills the rest.
        // Elevated third-person rear-follow camera. At 9:16 this puts the player car ≈37% up the screen
        // (lower-middle, clear of the controls) and the horizon ≈63% up, so ~400 m of road is visible.
        // Telephoto chase: further back + higher + narrower lens. The player car keeps the same on-screen size,
        // while traffic 100–250 m ahead appears ~1.5x larger than with a wide lens, so it reads clearly.
        public Vector3 offset = new Vector3(0f, 10.5f, -22f);
        public Vector3 lookAhead = new Vector3(0f, 0f, 30f);
        /// <summary>
        /// Horizontal field of view kept constant across aspect ratios so all three lanes always fit
        /// (at the car the view is ~13.5 m wide vs a 10.8 m carriageway).
        /// </summary>
        public float baseHorizontalFov = 34f;
        float lastAspect = -1f;

        /// <summary>Converts a horizontal FOV into Unity's vertical FOV for the current aspect ratio.</summary>
        public static float VerticalFov(float horizontalFov, float aspect)
        {
            float v = 2f * Mathf.Atan(Mathf.Tan(horizontalFov * 0.5f * Mathf.Deg2Rad) / Mathf.Max(0.3f, aspect)) * Mathf.Rad2Deg;
            return Mathf.Clamp(v, 40f, 115f); // 20:9 phones need ~106° vertical for a 62° horizontal view
        }

        public static CameraRig Create(PlayerCar car)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 1f; // camera sits 22 m back; better depth precision over the long view
            cam.farClipPlane = 320f;
            cam.fieldOfView = 58f;
            cam.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            var rig = go.AddComponent<CameraRig>();
            rig.Cam = cam;
            rig.target = car;
            rig.speedLines = ParticleFactory.SpeedLines(go.transform);
            rig.Snap();
            return rig;
        }

        public void Snap()
        {
            if (target == null) return;
            var tp = target.transform.position;
            transform.position = new Vector3(tp.x * 0.45f, tp.y, tp.z) + offset;
            transform.LookAt(new Vector3(tp.x * 0.45f, 0f, tp.z) + lookAhead);
            if (Cam != null) Cam.fieldOfView = VerticalFov(baseHorizontalFov, Cam.aspect);
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmp * (shakeTime / Mathf.Max(0.01f, shakeDur))) return;
            shakeAmp = amplitude;
            shakeDur = shakeTime = duration;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            var tp = target.transform.position;

            if (!target.Crashed)
            {
                crashOrbit = 0f;
                // Stay mostly centred on the road (all lanes visible), drifting slightly with the car.
                var desired = new Vector3(tp.x * 0.45f, tp.y, tp.z) + offset;
                var p = transform.position;
                p.x = Util.Damp(p.x, desired.x, 6f, dt);
                p.y = Util.Damp(p.y, desired.y, 6f, dt);
                p.z = desired.z;
                transform.position = p;
                var look = new Vector3(tp.x * 0.45f, 0f, tp.z) + lookAhead;
                transform.rotation = Quaternion.LookRotation(look - transform.position);

                float n = Mathf.Clamp01((target.SpeedKmh - 60f) / 160f);
                float hfov = baseHorizontalFov + n * 4f + (target.NitroActive ? 3f : 0f); // subtle speed feel
                float targetFov = VerticalFov(hfov, Cam.aspect);
                // Snap when the screen shape changes (foldables, split screen); otherwise ease for speed effects.
                if (Mathf.Abs(Cam.aspect - lastAspect) > 0.01f) { Cam.fieldOfView = targetFov; lastAspect = Cam.aspect; }
                else Cam.fieldOfView = Util.Damp(Cam.fieldOfView, targetFov, 3f, dt);

                var em = speedLines.emission;
                // Light speed streaks only at high speed / nitro — never enough to hide traffic.
                em.rateOverTime = target.NitroActive ? 60f : Mathf.Max(0f, (target.SpeedKmh - 150f) * 0.4f);
            }
            else
            {
                // Slow orbit around the wreck
                crashOrbit += dt * 25f;
                var pivot = tp + Vector3.up * 1f;
                var dir = Quaternion.Euler(0, -crashOrbit, 0) * new Vector3(0.3f, 0.35f, -1f).normalized;
                var desired = pivot + dir * 9f;
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, 0.35f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pivot - transform.position), dt * 5f);
                var em = speedLines.emission;
                em.rateOverTime = 0f;
            }

            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = shakeAmp * Mathf.Clamp01(shakeTime / shakeDur);
                float t = Time.unscaledTime * 35f;
                transform.position += new Vector3((Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 2f * k, (Mathf.PerlinNoise(0.3f, t) - 0.5f) * 2f * k, 0f);
            }
        }
    }
}
