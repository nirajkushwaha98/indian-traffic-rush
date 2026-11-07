using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Generates every sound in the game at startup (original, royalty-free, no audio files needed).
    /// Replace with recorded assets later by assigning clips in <see cref="AudioManager"/>.
    /// </summary>
    public static class AudioSynth
    {
        public const int Rate = 22050;

        static AudioClip Make(string name, float[] data, bool loop = false)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Saw(float phase) => 2f * (phase - Mathf.Floor(phase + 0.5f));
        static float Square(float phase) => (phase - Mathf.Floor(phase)) < 0.5f ? 1f : -1f;
        static float Sin(float phase) => Mathf.Sin(phase * 2f * Mathf.PI);

        /// <summary>Seamless 1 s engine loop at 50 Hz fundamental; pitch is driven by speed.</summary>
        public static AudioClip Engine()
        {
            int n = Rate; // exactly 1 s -> 50 whole cycles -> seamless
            var d = new float[n];
            var rnd = new System.Random(1);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = 50f;
                float v = 0.55f * Saw(f * t) + 0.35f * Sin(2 * f * t) + 0.2f * Square(f * 0.5f * t) * 0.5f + 0.12f * Sin(3 * f * t);
                // firing pulses
                float pulse = Mathf.Pow(Mathf.Abs(Sin(f * 2f * t)), 6f) * 0.35f;
                float noise = (float)(rnd.NextDouble() * 2 - 1) * 0.08f;
                v = v * 0.6f + pulse + noise;
                lp += (v - lp) * 0.25f;
                d[i] = lp * 0.55f;
            }
            return Make("Engine", d, true);
        }

        public static AudioClip Horn()
        {
            int n = (int)(Rate * 0.55f);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((0.55f - t) / 0.06f);
                float v = 0.5f * Square(415f * t) + 0.5f * Square(520f * t);
                lp += (v - lp) * 0.35f;
                d[i] = lp * env * 0.35f;
            }
            return Make("Horn", d);
        }

        public static AudioClip Coin()
        {
            int n = (int)(Rate * 0.22f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = t < 0.06f ? 1318.5f : 1975.5f;
                float env = Mathf.Exp(-(t < 0.06f ? t : t - 0.06f) * 18f);
                d[i] = (Sin(f * t) * 0.7f + Square(f * t) * 0.15f) * env * 0.45f;
            }
            return Make("Coin", d);
        }

        public static AudioClip Crash()
        {
            int n = (int)(Rate * 1.3f);
            var d = new float[n];
            var rnd = new System.Random(7);
            float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(rnd.NextDouble() * 2 - 1);
                lp += (noise - lp) * 0.3f;
                lp2 += (noise - lp2) * 0.05f;
                float thump = Sin((60f - t * 30f) * t) * Mathf.Exp(-t * 7f);
                float metal = Sin(1730f * t) * Sin(2310f * t) * Mathf.Exp(-t * 5f) * 0.3f;
                float env = Mathf.Exp(-t * 3.2f);
                // debris clatter
                float clatter = (rnd.NextDouble() < 0.002 * Mathf.Exp(-t * 2f) * 50 ? 1f : 0f) * (float)rnd.NextDouble();
                d[i] = Mathf.Clamp((lp * 0.6f + lp2 * 1.2f) * env + thump * 0.9f + metal + clatter * 0.3f, -1f, 1f) * 0.8f;
            }
            return Make("Crash", d);
        }

        public static AudioClip Scrape()
        {
            int n = (int)(Rate * 0.5f);
            var d = new float[n];
            var rnd = new System.Random(9);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(rnd.NextDouble() * 2 - 1);
                lp += (noise - lp) * 0.5f;
                float squeal = Sin(2900f * t + Sin(30f * t) * 0.3f) * 0.25f;
                d[i] = (lp * 0.6f + squeal) * Mathf.Exp(-t * 5f) * 0.7f;
            }
            return Make("Scrape", d);
        }

        public static AudioClip Whoosh(float duration, float startCut, float endCut, string name)
        {
            int n = (int)(Rate * duration);
            var d = new float[n];
            var rnd = new System.Random(3);
            float lp = 0, hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float cut = Mathf.Lerp(startCut, endCut, t);
                float noise = (float)(rnd.NextDouble() * 2 - 1);
                lp += (noise - lp) * cut;
                hp = 0.9f * (hp + lp - prev);
                prev = lp;
                float env = Mathf.Sin(t * Mathf.PI);
                d[i] = (lp * 0.7f + hp * 0.5f) * env * 0.8f;
            }
            return Make(name, d);
        }

        public static AudioClip Click()
        {
            int n = (int)(Rate * 0.06f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = Sin(880f * t) * Mathf.Exp(-t * 70f) * 0.5f + Sin(1760f * t) * Mathf.Exp(-t * 90f) * 0.2f;
            }
            return Make("Click", d);
        }

        public static AudioClip Reward()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int per = (int)(Rate * 0.09f);
            int n = per * notes.Length + (int)(Rate * 0.4f);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                for (int i = 0; i < n - k * per; i++)
                {
                    float t = (float)i / Rate;
                    d[k * per + i] += (Sin(notes[k] * t) * 0.6f + Sin(notes[k] * 2 * t) * 0.15f) * Mathf.Exp(-t * 6f) * 0.3f;
                }
            }
            return Make("Reward", d);
        }

        public static AudioClip Countdown(bool go)
        {
            int n = (int)(Rate * (go ? 0.5f : 0.18f));
            var d = new float[n];
            float f = go ? 1046.5f : 659.25f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = (Sin(f * t) * 0.6f + Square(f * t) * 0.1f) * Mathf.Exp(-t * (go ? 4f : 12f)) * 0.45f;
            }
            return Make(go ? "Go" : "Beep", d);
        }

        /// <summary>
        /// Loopable arcade track with an Indian flavour: tabla-like percussion, drone and a melody in
        /// a Bhairav-inspired scale. <paramref name="bpm"/> sets energy (menu vs race).
        /// </summary>
        public static AudioClip Music(float bpm, int seed, bool intense)
        {
            int bars = 8;
            float beat = 60f / bpm;
            int n = (int)(Rate * beat * 4 * bars);
            var d = new float[n];
            var rnd = new System.Random(seed);
            // Scale degrees in semitones (Bhairav-like): S r G m P d N
            int[] scale = { 0, 1, 4, 5, 7, 8, 11, 12 };
            float root = 146.83f; // D3
            int[] progression = { 0, 0, 5, 5, 3, 3, 4, 0 }; // degree of bass per bar

            float Freq(float baseF, int semis) => baseF * Mathf.Pow(2f, semis / 12f);

            // Drone (tanpura-ish): root + fifth, slow shimmer
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float sh = 1f + 0.003f * Sin(0.3f * t);
                d[i] += (Sin(root * 0.5f * sh * t) * 0.08f + Sin(root * 0.75f * t) * 0.05f + Sin(root * t * 1.0f) * 0.03f);
            }

            int sixteenth = (int)(Rate * beat / 4f);
            for (int bar = 0; bar < bars; bar++)
            {
                int barStart = (int)(bar * beat * 4 * Rate);
                int deg = progression[bar];
                // bass on beats
                for (int b = 0; b < 4; b++)
                {
                    int s0 = barStart + (int)(b * beat * Rate);
                    float bf = Freq(root * 0.5f, scale[deg]);
                    int len = (int)(beat * Rate * 0.9f);
                    for (int i = 0; i < len && s0 + i < n; i++)
                    {
                        float t = (float)i / Rate;
                        d[s0 + i] += (Saw(bf * t) * 0.18f + Sin(bf * t) * 0.15f) * Mathf.Exp(-t * 3.5f);
                    }
                }
                // percussion on 16ths: "dha" (low pitch drop) and "na" (bright ring)
                for (int s = 0; s < 16; s++)
                {
                    int s0 = barStart + s * sixteenth;
                    bool dha = s % 8 == 0 || s == 6 || s == 11;
                    bool na = s % 4 == 2 || (intense && s % 2 == 1 && rnd.NextDouble() < 0.5);
                    bool hat = intense || s % 2 == 0;
                    int len = (int)(Rate * 0.25f);
                    for (int i = 0; i < len && s0 + i < n; i++)
                    {
                        float t = (float)i / Rate;
                        float v = 0f;
                        if (dha) v += Sin((110f * Mathf.Exp(-t * 9f) + 55f) * t) * Mathf.Exp(-t * 10f) * 0.45f;
                        if (na) v += (Sin(440f * t) * 0.5f + Sin(880f * 1.5f * t) * 0.2f) * Mathf.Exp(-t * 28f) * 0.28f;
                        if (hat && t < 0.03f) v += (float)(rnd.NextDouble() * 2 - 1) * Mathf.Exp(-t * 160f) * 0.08f;
                        d[s0 + i] += v;
                    }
                }
                // melody: 8th notes, random walk on the scale, rests sometimes
                int noteIdx = 4;
                for (int e = 0; e < 8; e++)
                {
                    if (rnd.NextDouble() < (intense ? 0.25 : 0.4)) continue;
                    noteIdx = Mathf.Clamp(noteIdx + rnd.Next(-2, 3), 0, scale.Length - 1);
                    float mf = Freq(root * 2f, scale[noteIdx]);
                    int s0 = barStart + e * sixteenth * 2;
                    int len = sixteenth * 2;
                    for (int i = 0; i < len && s0 + i < n; i++)
                    {
                        float t = (float)i / Rate;
                        float vib = 1f + 0.006f * Sin(6f * t);
                        float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 4f);
                        d[s0 + i] += (Sin(mf * vib * t) * 0.6f + Sin(mf * 2f * vib * t) * 0.25f + Saw(mf * t) * 0.08f) * env * 0.16f;
                    }
                }
            }
            // normalise and soften loop seam
            float max = 0.0001f;
            for (int i = 0; i < n; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
            float g = 0.8f / max;
            int fade = Rate / 50;
            for (int i = 0; i < n; i++)
            {
                float f = 1f;
                if (i < fade) f = (float)i / fade;
                else if (i > n - fade) f = (float)(n - i) / fade;
                d[i] *= g * f;
            }
            return Make(intense ? "MusicRace" : "MusicMenu", d, true);
        }
    }
}
