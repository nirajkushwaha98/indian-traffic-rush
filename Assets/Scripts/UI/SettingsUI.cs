using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>Audio, controls, graphics, environment, vibration, privacy policy and progress reset.</summary>
    public class SettingsUI : MenuPage
    {
        Slider music, sfx;
        const float ControlX = 330f, ControlW = 666f; // portrait: label column + control column
        readonly Dictionary<string, List<Button>> groups = new Dictionary<string, List<Button>>();
        RectTransform body;

        public override void Build(MainMenuUI owner, Transform parent)
        {
            menu = owner;
            var dim = parent.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
            Panel = parent.gameObject.AddComponent<UIPanel>();
            Header(parent, "SETTINGS");

            var box = UIKit.RoundedPanel(parent, "Box", new Color(UIKit.Panel.r, UIKit.Panel.g, UIKit.Panel.b, 0.975f));
            box.rectTransform.Stretch(24, 24, 230, 40); // portrait: full width below the header
            box.raycastTarget = true;
            Panel.Content = box.rectTransform;
            body = box.rectTransform;

            var s = SaveSystem.Data.settings;
            float y = -70;
            Label("MUSIC", y);
            music = UIKit.SliderCtl(body, "Music", s.musicVolume, v => { SaveSystem.Data.settings.musicVolume = v; AudioManager.I?.ApplyVolumes(); });
            ((RectTransform)music.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(ControlX, y - 4), new Vector2(ControlW, 70));
            y -= 150;
            Label("SOUND FX", y);
            sfx = UIKit.SliderCtl(body, "Sfx", s.sfxVolume, v => SaveSystem.Data.settings.sfxVolume = v);
            ((RectTransform)sfx.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(ControlX, y - 4), new Vector2(ControlW, 70));
            y -= 150;

            Label("CONTROLS", y);
            Segmented("controls", y, new[] { "SWIPE", "BUTTONS" }, () => s.controlMode, i => s.controlMode = i);
            y -= 150;
            Label("GRAPHICS", y);
            Segmented("graphics", y, new[] { "LOW", "HIGH" }, () => s.graphics, i => { s.graphics = i; AppRoot.ApplyQuality(); });
            y -= 150;
            Label("ENVIRONMENT", y);
            Segmented("theme", y, new[] { "AUTO", "DAY", "SUNSET" }, () => s.theme, i => s.theme = i);
            y -= 150;
            Label("VIBRATION", y);
            Segmented("vibe", y, new[] { "ON", "OFF" }, () => s.vibration ? 0 : 1, i => { s.vibration = i == 0; if (s.vibration) Haptics.Buzz(); });

            var privacy = UIKit.Btn(body, "PRIVACY POLICY", UIKit.Blue, () => Application.OpenURL(GameAssets.I.privacyPolicyUrl), 32, "PrivacyButton");
            privacy.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(36, 110), new Vector2(470, 96));
            var reset = UIKit.Btn(body, "RESET PROGRESS", UIKit.Red, ConfirmReset, 32, "ResetButton");
            reset.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(526, 110), new Vector2(470, 96));
            var ver = UIKit.Txt(body, "v" + Application.version, 28, UIKit.Dim, TextAnchor.MiddleRight, FontStyle.Normal);
            ver.alignment = TextAnchor.MiddleCenter;
            ver.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(400, 50));
        }

        void Label(string text, float y)
        {
            var t = UIKit.Txt(body, text, 34, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, y), new Vector2(300, 70));
        }

        void Segmented(string key, float y, string[] options, Func<int> get, Action<int> set)
        {
            var list = new List<Button>();
            float w = ControlW / options.Length - 12f;
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var b = UIKit.Btn(body, options[i], new Color(0.25f, 0.28f, 0.36f), () =>
                {
                    set(idx);
                    Paint(key, get());
                }, 30, key + "_" + options[i]);
                b.GetComponent<RectTransform>().Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(ControlX + i * (w + 12f), y), new Vector2(w, 74));
                list.Add(b);
            }
            groups[key] = list;
            getters[key] = get;
            Paint(key, get());
        }

        readonly Dictionary<string, Func<int>> getters = new Dictionary<string, Func<int>>();

        void Paint(string key, int selected)
        {
            var list = groups[key];
            for (int i = 0; i < list.Count; i++)
                list[i].GetComponent<Image>().color = i == selected ? UIKit.Saffron : new Color(0.25f, 0.28f, 0.36f);
        }

        public override void Refresh()
        {
            var s = SaveSystem.Data.settings;
            music.SetValueWithoutNotify(s.musicVolume);
            sfx.SetValueWithoutNotify(s.sfxVolume);
            foreach (var kv in getters) Paint(kv.Key, kv.Value());
        }

        void ConfirmReset()
        {
            Dialog.Show(menu.Canvas.transform, "RESET PROGRESS?",
                "All coins, cars, upgrades, missions and records will be erased. This cannot be undone.",
                "RESET", () =>
                {
                    SaveSystem.ResetAll();
                    menu.RefreshTop();
                    menu.ShowToast("Progress reset", UIKit.Red);
                    menu.Open(MainMenuUI.Page.Home);
                }, "CANCEL");
        }
    }
}
