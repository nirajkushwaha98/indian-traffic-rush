using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Hands out shared material instances keyed by colour so identical colours share one material
    /// (keeps the SRP batcher happy and avoids per-object material leaks).
    /// </summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<int, Material> lit = new Dictionary<int, Material>();
        static readonly Dictionary<int, Material> unlit = new Dictionary<int, Material>();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

        static int Key(Color c, float s)
        {
            Color32 c32 = c;
            return (c32.r << 24) ^ (c32.g << 16) ^ (c32.b << 8) ^ c32.a ^ (Mathf.RoundToInt(s * 20f) << 4) * 7919;
        }

        /// <summary>Opaque lit material (URP Simple Lit).</summary>
        public static Material Lit(Color c, float smoothness = 0.2f)
        {
            int k = Key(c, smoothness);
            if (lit.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(GameAssets.I.lit) { name = "Lit_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor(BaseColor, c);
            if (m.HasProperty(Smoothness)) m.SetFloat(Smoothness, smoothness);
            lit[k] = m;
            return m;
        }

        /// <summary>Unlit material for lamps, signs and glowing bits.</summary>
        public static Material Unlit(Color c)
        {
            int k = Key(c, 0f);
            if (unlit.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(GameAssets.I.unlit) { name = "Unlit_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor(BaseColor, c);
            unlit[k] = m;
            return m;
        }

        public static void SetColor(Material m, Color c) => m.SetColor(BaseColor, c);
    }
}
