using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>Squash-on-press + click sound for every button.</summary>
    public class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        float target = 1f;
        Selectable sel;

        void Awake() => sel = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData e) { if (sel == null || sel.interactable) target = 0.93f; }
        public void OnPointerUp(PointerEventData e) => target = 1f;
        public void OnPointerExit(PointerEventData e) => target = 1f;
        public void OnPointerClick(PointerEventData e) { if (sel == null || sel.interactable) AudioManager.Click(); }

        void Update()
        {
            float s = transform.localScale.x;
            if (Mathf.Abs(s - target) > 0.001f)
            {
                s = Mathf.Lerp(s, target, 1f - Mathf.Exp(-25f * Time.unscaledDeltaTime));
                transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    /// <summary>Press-and-hold button (gas / brake pedals, steering arrows).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> onChange;
        public Action onPress;
        bool held;
        Image img;
        Color baseColor;

        void Awake()
        {
            img = GetComponent<Image>();
            if (img != null) baseColor = img.color;
        }

        public void OnPointerDown(PointerEventData e) { Set(true); onPress?.Invoke(); }
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) { if (!e.eligibleForClick) Set(false); }
        void OnDisable() => Set(false);

        void Set(bool v)
        {
            if (held == v) return;
            held = v;
            transform.localScale = v ? new Vector3(0.92f, 0.92f, 1f) : Vector3.one;
            if (img != null) img.color = v ? Color.Lerp(baseColor, Color.white, 0.25f) : baseColor;
            onChange?.Invoke(v);
        }
    }

    /// <summary>Keeps children inside the device safe area (notches, rounded corners).</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        RectTransform rt;

        void Awake() { rt = (RectTransform)transform; Apply(); }
        void Update() { if (Screen.safeArea != last) Apply(); }

        void Apply()
        {
            last = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var min = last.position; var max = last.position + last.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Unscaled-time tweens so menus animate even while the game is paused.</summary>
    public static class Tween
    {
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static IEnumerator Run(float duration, Action<float> step)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
        }

        public static Coroutine Pop(MonoBehaviour host, Transform target, float duration = 0.35f, float from = 0.6f)
        {
            if (host == null || !host.isActiveAndEnabled) { target.localScale = Vector3.one; return null; }
            return host.StartCoroutine(Run(duration, t =>
            {
                float s = Mathf.LerpUnclamped(from, 1f, EaseOutBack(t));
                target.localScale = new Vector3(s, s, 1f);
            }));
        }

        public static Coroutine Fade(MonoBehaviour host, CanvasGroup g, float to, float duration, Action done = null)
        {
            if (host == null || !host.isActiveAndEnabled) { g.alpha = to; done?.Invoke(); return null; }
            float from = g.alpha;
            return host.StartCoroutine(FadeRoutine(g, from, to, duration, done));
        }

        static IEnumerator FadeRoutine(CanvasGroup g, float from, float to, float duration, Action done)
        {
            yield return Run(duration, t => g.alpha = Mathf.Lerp(from, to, t));
            done?.Invoke();
        }

        public static Coroutine Slide(MonoBehaviour host, RectTransform rt, Vector2 from, Vector2 to, float duration)
        {
            if (host == null || !host.isActiveAndEnabled) { rt.anchoredPosition = to; return null; }
            return host.StartCoroutine(Run(duration, t => rt.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseOutCubic(t))));
        }
    }

    /// <summary>A full-screen panel that fades/pops in and out.</summary>
    public class UIPanel : MonoBehaviour
    {
        public CanvasGroup Group { get; private set; }
        public RectTransform Content;
        public Action onShown;
        bool visible;

        public bool Visible => visible;

        void Awake()
        {
            Group = UIKit.Group(gameObject);
        }

        public void Show()
        {
            if (Group == null) Group = UIKit.Group(gameObject);
            gameObject.SetActive(true);
            visible = true;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            Group.alpha = 0f;
            Tween.Fade(this, Group, 1f, 0.2f);
            if (Content != null) Tween.Pop(this, Content, 0.35f, 0.88f);
            onShown?.Invoke();
        }

        public void Hide(bool instant = false)
        {
            if (Group == null) Group = UIKit.Group(gameObject);
            visible = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            if (instant || !gameObject.activeInHierarchy) { Group.alpha = 0f; gameObject.SetActive(false); return; }
            Tween.Fade(this, Group, 0f, 0.15f, () => { if (!visible) gameObject.SetActive(false); });
        }

        public static UIPanel Create(Transform parent, string name, Color dim)
        {
            var bg = UIKit.Img(parent, name, dim);
            bg.rectTransform.Stretch();
            bg.raycastTarget = true;
            var p = bg.gameObject.AddComponent<UIPanel>();
            p.Group = UIKit.Group(bg.gameObject);
            return p;
        }
    }

    /// <summary>Short-lived floating text (near miss!, +coins…).</summary>
    public class FloatingText : MonoBehaviour
    {
        Text text;
        float life, t;
        Vector2 start;
        RectTransform rt;

        public static void Spawn(Transform parent, string msg, Color c, int size, Vector2 pos, float life = 1.1f)
        {
            var t = UIKit.Txt(parent, msg, size, c);
            t.Outlined(new Color(0, 0, 0, 0.6f), 3f);
            t.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(900, 120));
            var f = t.gameObject.AddComponent<FloatingText>();
            f.text = t; f.life = life; f.rt = t.rectTransform; f.start = pos;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = t / life;
            float s = k < 0.15f ? Mathf.Lerp(0.5f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            rt.localScale = new Vector3(s, s, 1);
            rt.anchoredPosition = start + new Vector2(0, k * 90f);
            var c = text.color; c.a = k < 0.7f ? 1f : Mathf.Clamp01(1f - (k - 0.7f) / 0.3f); text.color = c;
            if (k >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>Generic confirm / info dialog.</summary>
    public static class Dialog
    {
        public static void Show(Transform canvas, string title, string body, string ok, Action onOk, string cancel = null, Action onCancel = null)
        {
            var panel = UIPanel.Create(canvas, "Dialog", new Color(0, 0, 0, 0.7f));
            var box = UIKit.RoundedPanel(panel.transform, "Box", UIKit.Panel);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
            panel.Content = box.rectTransform;
            var tt = UIKit.Txt(box.transform, title, 58, UIKit.Saffron);
            tt.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(820, 80));
            var bt = UIKit.Txt(box.transform, body, 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            bt.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(800, 200));
            bool two = !string.IsNullOrEmpty(cancel);
            var okBtn = UIKit.Btn(box.transform, ok, UIKit.Green, () => { panel.Hide(); UnityEngine.Object.Destroy(panel.gameObject, 0.3f); onOk?.Invoke(); });
            okBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(two ? 190 : 0, 40), new Vector2(340, 110));
            if (two)
            {
                var c = UIKit.Btn(box.transform, cancel, new Color(0.35f, 0.37f, 0.45f), () => { panel.Hide(); UnityEngine.Object.Destroy(panel.gameObject, 0.3f); onCancel?.Invoke(); });
                c.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-190, 40), new Vector2(340, 110));
            }
            panel.Show();
        }
    }
}
