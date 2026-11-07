using UnityEngine;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>Base for full-screen menu pages (garage, missions, daily, settings).</summary>
    public abstract class MenuPage : MonoBehaviour
    {
        public UIPanel Panel { get; protected set; }
        protected MainMenuUI menu;
        public abstract void Build(MainMenuUI owner, Transform parent);
        public abstract void Refresh();

        protected RectTransform Header(Transform parent, string title)
        {
            var back = UIKit.Btn(parent, "BACK", new Color(0, 0, 0, 0.55f), () => menu.Open(MainMenuUI.Page.Home), 38);
            // Portrait header: BACK top-left beside the coin pill; title on its own row below.
            back.GetComponent<RectTransform>().Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(220, 84));
            var t = UIKit.Txt(parent, title, 68, Color.white, TextAnchor.MiddleLeft);
            t.Outlined(new Color(0, 0, 0, 0.5f), 3f);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -122), new Vector2(1000, 90));
            return t.rectTransform;
        }
    }

    /// <summary>Main menu: animated logo, 3D car preview, coin/best display and navigation to every page.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public enum Page { Home, Garage, Missions, Daily, Settings }
        public static Page OpenOnLoad = Page.Home;

        public static MainMenuUI I { get; private set; }
        public MenuShowroom Showroom { get; private set; }
        public Canvas Canvas { get; private set; }
        public Page Current { get; private set; } = Page.Home;
        public GarageUI Garage => garage;

        RectTransform safe, home, logo;
        Text coinText, bestText, carNameText;
        GameObject missionsBadge, dailyBadge;
        Text missionsBadgeText;
        Button playBtn, freeCoinsBtn;
        Image shine;
        GarageUI garage;
        MissionsUI missions;
        DailyRewardUI daily;
        SettingsUI settings;
        float t;

        public static MainMenuUI Build(MenuShowroom showroom)
        {
            var c = UIKit.CreateCanvas("MenuUI", 10);
            var ui = c.gameObject.AddComponent<MainMenuUI>();
            I = ui;
            ui.Canvas = c;
            ui.Showroom = showroom;
            ui.BuildAll();
            return ui;
        }

        void OnDestroy()
        {
            Wallet.Changed -= OnCoins;
            if (I == this) I = null;
        }

        void BuildAll()
        {
            safe = UIKit.Rect(Canvas.transform, "SafeArea").Stretch();
            safe.gameObject.AddComponent<SafeArea>();

            // Portrait: soft dark bands at the top (logo) and bottom (buttons) so text reads over the 3D scene.
            var top = UIKit.Img(safe, "ShadeTop", new Color(0.02f, 0.02f, 0.05f, 0.45f));
            top.rectTransform.anchorMin = new Vector2(0, 0.8f); top.rectTransform.anchorMax = Vector2.one;
            top.rectTransform.offsetMin = top.rectTransform.offsetMax = Vector2.zero;
            var bottom = UIKit.Img(safe, "ShadeBottom", new Color(0.02f, 0.02f, 0.05f, 0.5f));
            bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1, 0.34f);
            bottom.rectTransform.offsetMin = bottom.rectTransform.offsetMax = Vector2.zero;

            home = UIKit.Rect(safe, "Home").Stretch();
            BuildLogo();
            BuildHomeButtons();

            // Top bar: best score (left) + coins (right)
            bestPill = UIKit.RoundedPanel(safe, "BestPill", new Color(0, 0, 0, 0.45f)).gameObject;
            ((RectTransform)bestPill.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(400, 78));
            bestText = UIKit.Txt(bestPill.transform, "", 34, UIKit.Gold);
            bestText.rectTransform.Stretch(16, 16, 0, 0);
            coinText = UIKit.CoinPill(safe, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), 340f);

            garage = CreatePage<GarageUI>();
            missions = CreatePage<MissionsUI>();
            daily = CreatePage<DailyRewardUI>();
            settings = CreatePage<SettingsUI>();

            Wallet.Changed += OnCoins;
            RefreshTop();
            Showroom.ShowCar(GarageService.Selected, GarageService.PaintOf(GarageService.Selected));
            var start = OpenOnLoad;
            OpenOnLoad = Page.Home;
            Open(start);
        }

        GameObject bestPill;
        static readonly Vector2 LogoPos = new Vector2(0, -130);

        T CreatePage<T>() where T : MenuPage
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(safe, false);
            ((RectTransform)go.transform).Stretch();
            var p = go.AddComponent<T>();
            p.Build(this, go.transform);
            p.Panel.Hide(true);
            return p;
        }

        void BuildLogo()
        {
            logo = UIKit.Rect(home, "Logo").Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), LogoPos, new Vector2(1000, 320));
            // tricolour stripe
            Color[] tri = { UIKit.Saffron, Color.white, UIKit.Green };
            for (int i = 0; i < 3; i++)
            {
                var s = UIKit.RoundedPanel(logo, "Stripe" + i, tri[i], 4f);
                s.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((i - 1) * 74, -4), new Vector2(66, 12));
            }
            var a = UIKit.Txt(logo, "INDIAN", 100, UIKit.Saffron, TextAnchor.UpperCenter);
            a.Outlined(new Color(0.25f, 0.1f, 0f, 0.9f), 4f).Shadowed(new Color(0, 0, 0, 0.5f), 6f);
            a.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(1000, 112));
            var b = UIKit.Txt(logo, "TRAFFIC RUSH", 112, Color.white, TextAnchor.UpperCenter);
            b.fontStyle = FontStyle.BoldAndItalic;
            b.Outlined(new Color(0.05f, 0.25f, 0.1f, 0.95f), 5f).Shadowed(new Color(0, 0, 0, 0.5f), 7f);
            b.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -124), new Vector2(1040, 130));
            var mask = UIKit.Rect(logo, "ShineMask").Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(1000, 240));
            mask.gameObject.AddComponent<RectMask2D>();
            shine = UIKit.Img(mask, "Shine", new Color(1, 1, 1, 0.35f), UIKit.Shine);
            shine.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(120, 340));
            shine.rectTransform.localRotation = Quaternion.Euler(0, 0, -20);
            var tag = UIKit.Txt(logo, "DODGE • OVERTAKE • RUSH", 32, UIKit.Dim, TextAnchor.UpperCenter);
            tag.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -262), new Vector2(1000, 44));
        }

        void BuildHomeButtons()
        {
            // Bottom-anchored stack, all within one-thumb reach.
            var s = UIKit.Btn(home, "SETTINGS", new Color(0.3f, 0.33f, 0.42f), () => Open(Page.Settings), 38, "SettingsButton");
            s.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(620, 96));

            var g = UIKit.Btn(home, "GARAGE", UIKit.Saffron, () => Open(Page.Garage), 40, "GarageButton");
            g.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-340, 166), new Vector2(316, 130));
            var m = UIKit.Btn(home, "MISSIONS", UIKit.Blue, () => Open(Page.Missions), 40, "MissionsButton");
            m.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 166), new Vector2(316, 130));
            missionsBadgeText = UIKit.Badge(m.transform, new Vector2(-10, -10));
            missionsBadge = missionsBadgeText.transform.parent.gameObject;
            var d = UIKit.Btn(home, "DAILY", UIKit.Purple, () => Open(Page.Daily), 40, "DailyButton");
            d.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(340, 166), new Vector2(316, 130));
            dailyBadge = UIKit.Badge(d.transform, new Vector2(-10, -10)).transform.parent.gameObject;

            playBtn = UIKit.Btn(home, "PLAY", UIKit.Green, () => Play(), 84, "PlayButton");
            playBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(780, 180));

            var nameCard = UIKit.RoundedPanel(home, "CarName", new Color(0, 0, 0, 0.45f));
            nameCard.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 540), new Vector2(660, 116));
            carNameText = UIKit.Txt(nameCard.transform, "", 46, Color.white);
            carNameText.rectTransform.Stretch(10, 10, 8, 40);
            var hint = UIKit.Txt(nameCard.transform, "Drag the car to rotate • Upgrade in the Garage", 24, UIKit.Dim, TextAnchor.LowerCenter, FontStyle.Normal);
            hint.rectTransform.Stretch(10, 10, 0, 12);

            freeCoinsBtn = UIKit.Btn(home, "FREE COINS  (AD)", UIKit.Purple, OnFreeCoins, 32, "FreeCoinsButton");
            freeCoinsBtn.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -116), new Vector2(340, 80));
        }

        public void Open(Page p)
        {
            Current = p;
            home.gameObject.SetActive(p == Page.Home);
            bestPill.SetActive(p == Page.Home);
            if (p == Page.Home)
            {
                Tween.Slide(this, logo, LogoPos + new Vector2(0, 60), LogoPos, 0.4f);
                SaveSystem.Save();
            }
            Show(garage, p == Page.Garage);
            Show(missions, p == Page.Missions);
            Show(daily, p == Page.Daily);
            Show(settings, p == Page.Settings);
            Showroom.SetGarageView(p == Page.Garage);
            if (p != Page.Garage) Showroom.ShowCar(GarageService.Selected, GarageService.PaintOf(GarageService.Selected));
            RefreshTop();
        }

        void Show(MenuPage page, bool on)
        {
            if (on) { page.Refresh(); page.Panel.Show(); }
            else if (page.Panel.Visible) page.Panel.Hide();
        }

        void Play()
        {
            SaveSystem.Save();
            AppRoot.LoadScene(GameConstants.GameScene);
        }

        void OnFreeCoins()
        {
            int amount = GameAssets.I.ads.menuRewardCoins;
            AdsManager.ShowRewarded(ok =>
            {
                if (!ok) return;
                Wallet.Add(amount);
                AudioManager.I?.Play(Sfx.Reward);
            });
        }

        void OnCoins(int c) => RefreshTop();

        public void RefreshTop()
        {
            coinText.text = Wallet.Coins.ToString("N0");
            bestText.text = "BEST  " + Util.FormatScore(SaveSystem.Data.bestScore);
            int claim = MissionManager.ClaimableCount();
            missionsBadge.SetActive(claim > 0);
            missionsBadgeText.text = claim.ToString();
            dailyBadge.SetActive(DailyRewardManager.CanClaim);
            carNameText.text = GarageService.Selected.displayName.ToUpperInvariant();
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (logo != null && Current == Page.Home)
            {
                float s = 1f + Mathf.Sin(t * 2f) * 0.015f;
                logo.localScale = new Vector3(s, s, 1f);
                float sx = Mathf.Repeat(t * 0.45f, 1.6f) * 1200f - 200f;
                shine.rectTransform.anchoredPosition = new Vector2(sx, 0);
                float p = 1f + Mathf.Sin(t * 4f) * 0.025f;
                playBtn.transform.localScale = new Vector3(p, p, 1f);
            }
            freeCoinsBtn.gameObject.SetActive(AdsManager.RewardedReady && Current == Page.Home);
        }

        public void ShowToast(string msg, Color c) => FloatingText.Spawn(safe, msg, c, 52, new Vector2(0, 420), 1.4f);
    }
}
