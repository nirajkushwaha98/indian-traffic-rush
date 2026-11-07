using UnityEngine;

namespace ITR
{
    /// <summary>Code-built particle systems (nitro flames, sparks, smoke, speed lines, coin sparkle).</summary>
    public static class ParticleFactory
    {
        static ParticleSystem Base(string name, Transform parent, bool additive, int max)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = additive ? GameAssets.I.particleAdditive : GameAssets.I.particleAlpha;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void Gradient(ParticleSystem ps, Color a, Color b, float alphaStart = 1f)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                      new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaStart * 0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        public static ParticleSystem NitroFlame(Transform parent)
        {
            var ps = Base("NitroFlame", parent, true, 120);
            var main = ps.main;
            main.startLifetime = 0.18f;
            main.startSpeed = 9f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 140f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.05f;
            ps.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));
            Gradient(ps, new Color(0.5f, 0.75f, 1f), new Color(1f, 0.45f, 0.1f));
            return ps;
        }

        public static ParticleSystem Sparks(Transform parent)
        {
            var ps = Base("Sparks", parent, true, 200);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.gravityModifier = 1.5f;
            main.startColor = new Color(1f, 0.8f, 0.3f);
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 2f;
            Gradient(ps, new Color(1f, 0.9f, 0.5f), new Color(1f, 0.35f, 0.05f));
            return ps;
        }

        public static ParticleSystem Smoke(Transform parent, Color c, float size, float rate)
        {
            var ps = Base("Smoke", parent, false, 150);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.gravityModifier = -0.08f;
            main.startColor = c;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
            Gradient(ps, c, c * 0.8f, 0.6f);
            return ps;
        }

        public static ParticleSystem SpeedLines(Transform cameraParent)
        {
            var ps = Base("SpeedLines", cameraParent, true, 120);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.35f;
            main.startSpeed = -60f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 4.5f;
            sh.radiusThickness = 0.35f;
            ps.transform.localPosition = new Vector3(0, 0, 18f);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 1f;
            Gradient(ps, Color.white, new Color(0.7f, 0.85f, 1f), 0.6f);
            ps.Play();
            return ps;
        }

        public static ParticleSystem Sparkle(Transform parent)
        {
            var ps = Base("CoinSparkle", parent, true, 120);
            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(1f, 0.85f, 0.25f);
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            Gradient(ps, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.1f));
            return ps;
        }
    }
}
