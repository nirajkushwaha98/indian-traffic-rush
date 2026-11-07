using UnityEngine;

namespace ITR
{
    /// <summary>Visual and audio feedback for the player car: nitro flames, sparks, smoke, lights, engine.</summary>
    public class PlayerEffects : MonoBehaviour
    {
        PlayerCar car;
        ParticleSystem flameL, flameR, sparks, crashSmoke, tyreSmoke;
        AudioSource engine;
        AudioSource nitroLoop;
        float blink;
        float sirenT;
        bool sirenOn = true;

        public void Setup(PlayerCar c)
        {
            car = c;
            var v = car.Visual;
            float rearZ = -v.length * 0.5f - 0.15f;
            flameL = ParticleFactory.NitroFlame(v.body);
            flameL.transform.localPosition = new Vector3(-v.width * 0.25f, 0.32f, rearZ);
            flameR = ParticleFactory.NitroFlame(v.body);
            flameR.transform.localPosition = new Vector3(v.width * 0.25f, 0.32f, rearZ);

            sparks = ParticleFactory.Sparks(transform);
            crashSmoke = ParticleFactory.Smoke(v.body, new Color(0.18f, 0.18f, 0.18f, 0.8f), 1.6f, 18f);
            crashSmoke.transform.localPosition = new Vector3(0, 0.9f, v.length * 0.35f);
            tyreSmoke = ParticleFactory.Smoke(transform, new Color(0.85f, 0.85f, 0.85f, 0.5f), 1.2f, 0f);
            tyreSmoke.transform.localPosition = new Vector3(0, 0.2f, -v.length * 0.35f);

            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = AudioManager.I != null ? AudioManager.I.EngineClip : null;
            engine.loop = true;
            engine.spatialBlend = 0f;
            engine.volume = 0f;
            if (engine.clip != null) engine.Play();

            car.NitroStarted += OnNitro;
            v.SetHeadlights(EnvironmentTheme.HeadlightsOn);
        }

        void OnDestroy()
        {
            if (car != null) car.NitroStarted -= OnNitro;
        }

        void OnNitro()
        {
            AudioManager.I?.Play(Sfx.Nitro, 0.9f);
            Haptics.Buzz();
        }

        public void SparkBurst(Vector3 worldPos, int count)
        {
            sparks.transform.position = worldPos;
            sparks.Emit(count);
        }

        public void OnCrash(Vector3 contact)
        {
            SparkBurst(contact, 80);
            crashSmoke.Play();
            SetFlames(false);
            AudioManager.I?.Play(Sfx.Crash, 1f);
            Haptics.Buzz();
        }

        public void OnSideSwipe(Vector3 contact)
        {
            SparkBurst(contact, 35);
            tyreSmoke.Emit(12);
            AudioManager.I?.Play(Sfx.Scrape, 0.9f);
            Haptics.Buzz();
        }

        public void OnRevive()
        {
            crashSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void SetFlames(bool on)
        {
            if (on && !flameL.isPlaying) { flameL.Play(); flameR.Play(); }
            else if (!on && flameL.isPlaying) { flameL.Stop(); flameR.Stop(); }
        }

        public void SetEngineMuted(bool muted)
        {
            if (engine == null) return;
            if (muted) engine.Pause(); else engine.UnPause();
        }

        void Update()
        {
            if (car == null) return;
            float dt = Time.deltaTime;
            var v = car.Visual;

            SetFlames(car.NitroActive && !car.Crashed);

            if (car.Braking && car.SpeedKmh > 60f && !car.Crashed && Random.value < dt * 20f)
                tyreSmoke.Emit(1);

            // Engine: fake 5-speed gearbox for a convincing rev sound
            if (engine != null && engine.clip != null)
            {
                float sfx = AudioManager.I != null ? AudioManager.I.SfxVolume : 1f;
                float n = Mathf.Clamp01(car.Speed / Mathf.Max(1f, car.TopSpeed));
                float gears = 5f;
                float g = n * gears;
                float inGear = g - Mathf.Floor(Mathf.Min(g, gears - 0.001f));
                float pitch = 0.75f + inGear * 0.75f + n * 0.5f + (car.NitroActive ? 0.25f : 0f);
                engine.pitch = car.Crashed ? Mathf.Lerp(engine.pitch, 0.4f, dt * 2f) : Mathf.Lerp(engine.pitch, pitch, dt * 8f);
                float vol = car.Crashed ? 0f : (0.22f + n * 0.25f + (car.Accelerating ? 0.08f : 0f));
                engine.volume = Mathf.Lerp(engine.volume, vol * sfx, dt * 4f);
            }

            // Indicators while changing lanes
            blink += dt;
            bool blinkOn = (blink % 0.5f) < 0.25f;
            if (car.IsChangingLane && !car.Crashed)
            {
                float dir = GameConstants.LaneX(car.Lane) - car.transform.position.x;
                v.SetIndicators(dir < 0 && blinkOn, dir > 0 && blinkOn);
            }
            else v.SetIndicators(false, false);

            // Police siren lights
            if (v.sirenRed != null && sirenOn)
            {
                sirenT += dt;
                bool phase = (sirenT % 0.4f) < 0.2f;
                v.sirenRed.enabled = phase;
                v.sirenBlue.enabled = !phase;
            }

            // Invulnerability blink after a revive
            if (v.body != null)
            {
                // Blink only during the short post-revive grace period (long test immunity stays visible).
                bool blinking = car.Invulnerable && car.InvulnerableTime < 3f;
                bool visible = !blinking || (Time.time % 0.2f) < 0.12f;
                if (v.body.gameObject.activeSelf != visible) v.body.gameObject.SetActive(visible);
            }
        }
    }
}
