using System;
using UnityEngine;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>Daily missions + achievements with progress bars and claim buttons.</summary>
    public class MissionsUI : MenuPage
    {
        RectTransform content;
        bool achievementsTab;
        Button dailyTab, achTab;
        Text footer;

        public override void Build(MainMenuUI owner, Transform parent)
        {
            menu = owner;
            var dim = parent.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
            Panel = parent.gameObject.AddComponent<UIPanel>();
            Header(parent, "MISSIONS");

            var box = UIKit.RoundedPanel(parent, "Box", new Color(UIKit.Panel.r, UIKit.Panel.g, UIKit.Panel.b, 0.975f));
            box.rectTransform.Stretch(24, 24, 230, 40); // portrait: full width below the header
            box.raycastTarget = true;
            Panel.Content = box.rectTransform;

            dailyTab = UIKit.Btn(box.transform, "DAILY", UIKit.Saffron, () => { achievementsTab = false; Refresh(); }, 38, "DailyTab");
            dailyTab.GetComponent<RectTransform>().Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(330, 96));
            achTab = UIKit.Btn(box.transform, "ACHIEVEMENTS", UIKit.Saffron, () => { achievementsTab = true; Refresh(); }, 34, "AchievementsTab");
            achTab.GetComponent<RectTransform>().Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(380, -30), new Vector2(420, 96));
            footer = UIKit.Txt(box.transform, "", 28, UIKit.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            footer.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -138), new Vector2(940, 44));

            // Scroll view
            var viewport = UIKit.Rect(box.transform, "Viewport").Stretch(20, 20, 196, 24);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.001f);
            content = UIKit.Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childControlHeight = true; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(4, 4, 4, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
        }

        public override void Refresh()
        {
            dailyTab.GetComponent<Image>().color = achievementsTab ? new Color(0.25f, 0.28f, 0.36f) : UIKit.Saffron;
            achTab.GetComponent<Image>().color = achievementsTab ? UIKit.Saffron : new Color(0.25f, 0.28f, 0.36f);
            foreach (Transform ch in content) Destroy(ch.gameObject);

            if (!achievementsTab)
            {
                var now = Clock.Now();
                var left = now.Date.AddDays(1) - now;
                footer.text = "New missions in " + left.Hours + "h " + left.Minutes + "m";
                foreach (var m in MissionManager.Daily)
                {
                    var mm = m;
                    bool done = MissionManager.IsComplete(m);
                    Row(MissionManager.Describe(m), "Daily mission", m.progress, m.target, m.reward, done, m.claimed,
                        () => { if (MissionManager.Claim(mm)) Claimed(mm.reward); });
                }
            }
            else
            {
                int claimed = 0;
                foreach (var a in MissionManager.Achievements) if (MissionManager.IsClaimed(a)) claimed++;
                footer.text = claimed + " / " + MissionManager.Achievements.Count + " claimed";
                foreach (var a in MissionManager.Achievements)
                {
                    var aa = a;
                    Row(a.title, a.description, MissionManager.Progress(a), a.target, a.reward, MissionManager.IsUnlocked(a), MissionManager.IsClaimed(a),
                        () => { if (MissionManager.Claim(aa)) Claimed(aa.reward); });
                }
            }
        }

        void Claimed(int reward)
        {
            AudioManager.I?.Play(Sfx.Reward);
            menu.ShowToast("+" + reward + " COINS", UIKit.Gold);
            menu.RefreshTop();
            Refresh();
        }

        void Row(string title, string sub, float progress, float target, int reward, bool done, bool claimed, Action claim)
        {
            var bg = UIKit.RoundedPanel(content, "Row", claimed ? new Color(1, 1, 1, 0.04f) : UIKit.PanelSoft);
            bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 170;
            var t = UIKit.Txt(bg.transform, title, 36, claimed ? UIKit.Dim : Color.white, TextAnchor.UpperLeft);
            t.rectTransform.Stretch(28, 290, 14, 100);
            var s = UIKit.Txt(bg.transform, sub, 24, UIKit.Dim, TextAnchor.UpperLeft, FontStyle.Normal);
            s.rectTransform.Stretch(28, 290, 68, 62);
            var bar = UIKit.Bar(bg.transform, "Progress", new Color(0, 0, 0, 0.35f), done ? UIKit.Green : UIKit.Saffron);
            var barRt = (RectTransform)bar.transform.parent;
            barRt.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 24), new Vector2(480, 22));
            bar.fillAmount = Mathf.Clamp01(progress / Mathf.Max(1f, target));
            string prog = target >= 1000 ? (Mathf.Min(progress, target) / 1000f).ToString("0.#") + "k / " + (target / 1000f).ToString("0.#") + "k"
                                         : Mathf.FloorToInt(Mathf.Min(progress, target)) + " / " + Mathf.FloorToInt(target);
            var pt = UIKit.Txt(bg.transform, prog, 24, UIKit.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            pt.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(520, 14), new Vector2(200, 40));

            var coin = UIKit.CoinImage(bg.transform, 50);
            coin.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-190, -16), new Vector2(46, 46));
            var rt = UIKit.Txt(bg.transform, reward.ToString("N0"), 36, UIKit.Gold, TextAnchor.MiddleRight);
            rt.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -12), new Vector2(160, 54));

            var btn = UIKit.Btn(bg.transform, claimed ? "CLAIMED" : (done ? "CLAIM" : "IN PROGRESS"),
                claimed ? new Color(0.25f, 0.28f, 0.36f) : (done ? UIKit.Green : new Color(0.25f, 0.28f, 0.36f)), claim, done && !claimed ? 36 : 26);
            btn.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 16), new Vector2(250, 84));
            btn.interactable = done && !claimed;
        }
    }

    /// <summary>7-day login calendar with streak.</summary>
    public class DailyRewardUI : MenuPage
    {
        RectTransform tiles;
        Button claimBtn;
        Text info;

        public override void Build(MainMenuUI owner, Transform parent)
        {
            menu = owner;
            var dim = parent.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
            Panel = parent.gameObject.AddComponent<UIPanel>();
            Header(parent, "DAILY REWARDS");

            var box = UIKit.RoundedPanel(parent, "Box", new Color(UIKit.Panel.r, UIKit.Panel.g, UIKit.Panel.b, 0.975f));
            box.rectTransform.Stretch(24, 24, 230, 40);
            box.raycastTarget = true;
            Panel.Content = box.rectTransform;
            var sub = UIKit.Txt(box.transform, "Log in every day — the 7th day pays big. Miss a day and the streak restarts.", 30, UIKit.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            sub.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(940, 100));
            tiles = UIKit.Rect(box.transform, "Tiles").Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1000, 560));
            info = UIKit.Txt(box.transform, "", 30, UIKit.Gold);
            info.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 190), new Vector2(960, 50));
            claimBtn = UIKit.Btn(box.transform, "CLAIM", UIKit.Green, OnClaim, 44, "ClaimDaily");
            claimBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(760, 120));
        }

        public override void Refresh()
        {
            foreach (Transform ch in tiles) Destroy(ch.gameObject);
            int today = DailyRewardManager.TodayIndex;
            bool can = DailyRewardManager.CanClaim;
            for (int i = 0; i < 7; i++)
            {
                bool claimed = DailyRewardManager.IsClaimedInCycle(i);
                bool isToday = i == today;
                bool big = i == 6;
                var tile = UIKit.RoundedPanel(tiles, "Day" + (i + 1),
                    claimed ? new Color(0.1f, 0.45f, 0.2f, 0.9f) : (isToday ? UIKit.Saffron : UIKit.PanelSoft));
                tile.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TilePos(i), new Vector2(big ? 260 : 220, 250));
                var d = UIKit.Txt(tile.transform, "DAY " + (i + 1), 30, Color.white);
                d.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(160, 40));
                var coin = UIKit.CoinImage(tile.transform, big ? 90 : 70);
                coin.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(big ? 90 : 70, big ? 90 : 70));
                var a = UIKit.Txt(tile.transform, DailyRewardManager.Rewards[i].ToString("N0"), 36, isToday && !claimed ? UIKit.Ink : UIKit.Gold);
                a.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(160, 50));
                if (claimed)
                {
                    var c = UIKit.Txt(tile.transform, "CLAIMED", 24, Color.white);
                    c.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(160, 30));
                }
                if (isToday && can) Tween.Pop(this, tile.transform, 0.5f, 0.8f);
            }
            claimBtn.interactable = can;
            claimBtn.Label().text = can ? "CLAIM  +" + DailyRewardManager.Rewards[today] : "CLAIMED TODAY";
            var now = Clock.Now();
            var left = now.Date.AddDays(1) - now;
            info.text = can ? "Day " + (today + 1) + " reward is ready!" : "Next reward in " + left.Hours + "h " + left.Minutes + "m";
        }

        /// <summary>Portrait 4 + 3 grid: days 1–4 on the top row, days 5–7 (day 7 wider) below.</summary>
        static Vector2 TilePos(int i)
        {
            if (i < 4) return new Vector2(-360f + i * 240f, 140f);
            float[] x = { -260f, -20f, 240f };
            return new Vector2(x[i - 4], -140f);
        }

        void OnClaim()
        {
            int r = DailyRewardManager.Claim();
            if (r <= 0) return;
            AudioManager.I?.Play(Sfx.Reward);
            menu.ShowToast("+" + r + " COINS", UIKit.Gold);
            menu.RefreshTop();
            Refresh();
        }
    }
}
