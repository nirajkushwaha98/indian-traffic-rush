using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>
    /// Code-first uGUI toolkit: canvases, rounded panels, buttons, bars, sliders and generated sprites.
    /// Keeps typography, colours and spacing consistent across every screen.
    /// </summary>
    public static class UIKit
    {
        // Palette: saffron / white / green accents on deep navy glass panels.
        public static readonly Color Saffron = Util.Hex("FF8A1F");
        public static readonly Color SaffronDark = Util.Hex("D96400");
        public static readonly Color Green = Util.Hex("19A74E");
        public static readonly Color Blue = Util.Hex("2F80ED");
        public static readonly Color Red = Util.Hex("E5393B");
        public static readonly Color Gold = Util.Hex("FFC83D");
        public static readonly Color Purple = Util.Hex("8E44EC");
        public static readonly Color Panel = new Color(0.06f, 0.07f, 0.12f, 0.92f);
        public static readonly Color PanelSoft = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color Dim = new Color(1f, 1f, 1f, 0.62f);
        public static readonly Color Ink = Util.Hex("10131C");

        /// <summary>Portrait 9:16 design resolution.</summary>
        public const float RefWidth = 1080f, RefHeight = 1920f;

        static Sprite rounded, circle, ring, coin, shine, softGlow;

        public static Sprite Rounded => rounded != null ? rounded : (rounded = MakeRounded(64, 26));
        public static Sprite Circle => circle != null ? circle : (circle = MakeCircle(128, 0f, false));
        public static Sprite Ring => ring != null ? ring : (ring = MakeCircle(256, 0.78f, false));
        public static Sprite CoinIcon => coin != null ? coin : (coin = MakeCoin(96));
        public static Sprite Shine => shine != null ? shine : (shine = MakeShine());
        public static Sprite Glow => softGlow != null ? softGlow : (softGlow = MakeCircle(128, 0f, true));

        // ------------------------------------------------------------------ sprite generation

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = radius + 1;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite MakeCircle(int size, float inner, bool soft)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a;
                    if (soft) a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d);
                    else
                    {
                        a = Mathf.Clamp01((1f - d) * r);
                        if (inner > 0f) a *= Mathf.Clamp01((d - inner) * r);
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeCoin(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            Color outer = Util.Hex("F5A300"), face = Util.Hex("FFD54A"), hi = Util.Hex("FFF1A8");
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(p, new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01((1f - d) * r);
                    Color c = d > 0.78f ? outer : face;
                    // simple embossed "rupee-like" bar mark (original glyph, not a currency logo)
                    float nx = (p.x - r) / r, ny = (p.y - r) / r;
                    bool mark = (Mathf.Abs(nx) < 0.1f && Mathf.Abs(ny) < 0.42f) || (Mathf.Abs(ny - 0.25f) < 0.07f && Mathf.Abs(nx) < 0.35f);
                    if (mark && d < 0.7f) c = outer;
                    if (d < 0.7f && nx + ny > 0.55f && nx + ny < 0.75f) c = Color.Lerp(c, hi, 0.6f);
                    c.a = a;
                    px[y * size + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeShine()
        {
            int w = 64, h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                byte a = (byte)(Mathf.Sin(t * Mathf.PI) * Mathf.Sin(t * Mathf.PI) * 255);
                for (int y = 0; y < h; y++) px[y * w + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------ building blocks

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = false;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(RefWidth, RefHeight);
            // Expand: the full 1080x1920 design area is always visible. Taller phones (19.5:9, 20:9…) get
            // extra height, wider screens (tablets) extra width — so nothing is ever clipped.
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchor at a point (0..1), offset in reference pixels.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Img(Transform parent, string name, Color c, Sprite sprite = null, float cornerScale = 1f)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.sprite = sprite;
            if (sprite == Rounded)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = cornerScale;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image RoundedPanel(Transform parent, string name, Color c, float cornerScale = 1f) =>
            Img(parent, name, c, Rounded, cornerScale);

        public static Text Txt(Transform parent, string text, int size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Util.Font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static T Shadowed<T>(this T g, Color c, float dist = 3f) where T : Graphic
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = c;
            s.effectDistance = new Vector2(dist, -dist);
            return g;
        }

        public static T Outlined<T>(this T g, Color c, float dist = 2f) where T : Graphic
        {
            var s = g.gameObject.AddComponent<Outline>();
            s.effectColor = c;
            s.effectDistance = new Vector2(dist, -dist);
            return g;
        }

        /// <summary>Rounded, coloured button with a label, press animation and click sound.</summary>
        public static Button Btn(Transform parent, string label, Color color, Action onClick, int fontSize = 42, string name = null)
        {
            var img = RoundedPanel(parent, name ?? ("Btn_" + label), color);
            img.raycastTarget = true;
            img.Shadowed(new Color(0, 0, 0, 0.35f), 5f);
            // top highlight
            var hi = RoundedPanel(img.transform, "Highlight", new Color(1, 1, 1, 0.14f));
            hi.rectTransform.anchorMin = new Vector2(0, 0.5f);
            hi.rectTransform.anchorMax = new Vector2(1, 1);
            hi.rectTransform.offsetMin = new Vector2(6, 0);
            hi.rectTransform.offsetMax = new Vector2(-6, -5);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var t = Txt(img.transform, label, fontSize, Color.white);
            t.rectTransform.Stretch(12, 12, 4, 4);
            t.Shadowed(new Color(0, 0, 0, 0.35f), 2f);
            img.gameObject.AddComponent<ButtonFx>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Text Label(this Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Horizontal progress bar; returns the fill image (Filled, horizontal).</summary>
        public static Image Bar(Transform parent, string name, Color bg, Color fill)
        {
            var back = RoundedPanel(parent, name, bg, 2f);
            var f = RoundedPanel(back.transform, "Fill", fill, 2f);
            f.rectTransform.Stretch(4, 4, 4, 4);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillOrigin = 0;
            f.fillAmount = 0.5f;
            // Filled+sprite: use a plain sprite so the rounded caps don't stretch oddly
            f.sprite = Rounded;
            return f;
        }

        public static Slider SliderCtl(Transform parent, string name, float value, Action<float> onChange)
        {
            var root = Rect(parent, name);
            var slider = root.gameObject.AddComponent<Slider>();
            var bg = RoundedPanel(root, "Track", new Color(1, 1, 1, 0.15f), 2f);
            bg.rectTransform.anchorMin = new Vector2(0, 0.35f);
            bg.rectTransform.anchorMax = new Vector2(1, 0.65f);
            bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            var fillArea = Rect(root, "FillArea");
            fillArea.anchorMin = new Vector2(0, 0.35f);
            fillArea.anchorMax = new Vector2(1, 0.65f);
            fillArea.offsetMin = new Vector2(0, 0);
            fillArea.offsetMax = new Vector2(0, 0);
            var fill = RoundedPanel(fillArea, "Fill", Saffron, 2f);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect(root, "HandleArea").Stretch(20, 20);
            var handle = Img(handleArea, "Handle", Color.white, Circle);
            handle.rectTransform.sizeDelta = new Vector2(52, 52);
            handle.preserveAspect = true; // the slider stretches the handle to its height — keep it round
            handle.raycastTarget = true;
            handle.Shadowed(new Color(0, 0, 0, 0.4f), 3f);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        public static Image CoinImage(Transform parent, float size)
        {
            var i = Img(parent, "Coin", Color.white, CoinIcon);
            i.rectTransform.sizeDelta = new Vector2(size, size);
            i.preserveAspect = true;
            return i;
        }

        /// <summary>Pill with coin icon + amount (top bars).</summary>
        public static Text CoinPill(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, float width = 300f)
        {
            var pill = RoundedPanel(parent, "CoinPill", new Color(0, 0, 0, 0.45f));
            pill.rectTransform.Place(anchor, pivot, pos, new Vector2(width, 78));
            var icon = CoinImage(pill.transform, 58);
            icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(58, 58));
            var t = Txt(pill.transform, "0", 40, Color.white, TextAnchor.MiddleRight);
            t.rectTransform.Stretch(80, 22, 0, 0);
            return t;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            return g;
        }

        /// <summary>Simple vector-ish icons drawn with UI images (pause bars, arrows, gear…).</summary>
        public static void PauseIcon(Transform parent, Color c)
        {
            for (int i = 0; i < 2; i++)
            {
                var b = RoundedPanel(parent, "Bar" + i, c, 4f);
                b.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -12 : 12, 0), new Vector2(14, 44));
            }
        }

        public static Text Badge(Transform parent, Vector2 pos)
        {
            var b = Img(parent, "Badge", Red, Circle);
            b.rectTransform.Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), pos, new Vector2(48, 48));
            b.Shadowed(new Color(0, 0, 0, 0.4f), 2f);
            var t = Txt(b.transform, "!", 28, Color.white);
            t.rectTransform.Stretch();
            return t;
        }
    }
}
