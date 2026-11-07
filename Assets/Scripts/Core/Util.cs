using UnityEngine;

namespace ITR
{
    public static class Util
    {
        /// <summary>Destroy that also works in edit mode (EditMode tests, editor tooling).</summary>
        public static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        static Font font;

        /// <summary>Unity's built-in runtime font (no external font assets needed).</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>World-space 3D text (signs, shop boards, truck art).</summary>
        public static TextMesh Text3D(Transform parent, string text, Vector3 localPos, Vector3 euler, float height, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = height / 6.4f; // ≈ cap height in metres for fontSize 64
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        public static float Damp(float current, float target, float lambda, float dt) =>
            Mathf.Lerp(current, target, 1f - Mathf.Exp(-lambda * dt));

        public static Vector3 Damp(Vector3 current, Vector3 target, float lambda, float dt) =>
            Vector3.Lerp(current, target, 1f - Mathf.Exp(-lambda * dt));

        public static string FormatScore(long v) => v.ToString("N0");

        public static string FormatDistance(float meters) =>
            meters < 1000f ? Mathf.FloorToInt(meters) + " m" : (meters / 1000f).ToString("0.00") + " km";
    }
}
