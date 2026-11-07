using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>
    /// In-race UI: HUD (score, distance, speedometer, coins, nitro, pause), touch controls, countdown,
    /// pause screen and game-over screen with rewards, revive and double-coins (rewarded ads only).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        GameManager gm;
        Canvas canvas;
        RectTransform safe;

        // HUD
        RectTransform hud;
        Text scoreText, distanceText, coinText, speedText, comboText, hintText, countdownText;
        Image speedFill, nitroFill, nitroBtnImg, comboFill;
        Text nitroBtnLabel;
        int shownScore = -1, shownCoins = -1, shownSpeed = -1, shownDist = -1;
        GameObject buttonsModeControls;

        // Screens
        UIPanel pausePanel, overPanel;
        Text overTitle, overScore, overBest, overStats, overReward, overMissions;
        Button reviveBtn, doubleBtn;
        GameObject newBestRibbon;

        public static GameUI Build(GameManager gm)
        {
            var c = UIKit.CreateCanvas("GameUI", 10);
            var ui = c.gameObject.AddComponent<GameUI>();
            ui.gm = gm;
            ui.canvas = c;
            ui.BuildAll();
            return ui;
        }

        void BuildAll()
        {
            safe = UIKit.Rect(canvas.transform, "SafeArea");
            safe.Stretch();
            safe.gameObject.AddComponent<SafeArea>();
            BuildHud();
            BuildControls();
            BuildCountdown();
            BuildPause();
            BuildGameOver();
            gm.StateChanged += OnState;
            gm.CountdownTick += OnCountdown;
            gm.NearMiss += OnNearMiss;
            gm.SideHit += () => FloatingText.Spawn(hud, "SCRAPE!", UIKit.Red, 64, new Vector2(0, 200), 0.8f);
            gm.Player.NitroStarted += () => FloatingText.Spawn(hud, "NITRO!", new Color(0.5f, 0.8f, 1f), 80, new Vector2(0, 150), 0.9f);
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.StateChanged -= OnState;
            gm.CountdownTick -= OnCountdown;
            gm.NearMiss -= OnNearMiss;
        }

        // ------------------------------------------------------------------ HUD

        void BuildHud()
        {
            hud = UIKit.Rect(safe, "HUD");
            hud.Stretch();

            // ---- Portrait layout: score + coins + pause across the top, controls along the bottom.
            // Score (top-left)
            var scoreCard = UIKit.RoundedPanel(hud, "ScoreCard", new Color(0, 0, 0, 0.42f));
            scoreCard.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(470, 170));
            var sl = UIKit.Txt(scoreCard.transform, "SCORE", 30, UIKit.Saffron, TextAnchor.UpperLeft);
            sl.rectTransform.Stretch(28, 20, 14, 0);
            scoreText = UIKit.Txt(scoreCard.transform, "0", 72, Color.white, TextAnchor.MiddleLeft);
            scoreText.rectTransform.Stretch(26, 20, 40, 40);
            scoreText.Shadowed(new Color(0, 0, 0, 0.5f), 3f);
            distanceText = UIKit.Txt(scoreCard.transform, "0 m", 32, UIKit.Dim, TextAnchor.LowerLeft, FontStyle.Normal);
            distanceText.rectTransform.Stretch(28, 20, 0, 14);
            var best = UIKit.Txt(scoreCard.transform, "BEST " + Util.FormatScore(SaveSystem.Data.bestScore), 26, UIKit.Gold, TextAnchor.LowerRight);
            best.rectTransform.Stretch(20, 24, 0, 16);

            // Coins + pause (top-right)
            coinText = UIKit.CoinPill(hud, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -34), 250f);
            var pause = UIKit.Btn(hud, "", new Color(0, 0, 0, 0.5f), () => gm.Pause(), 10, "PauseButton");
            pause.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(110, 110));
            UIKit.PauseIcon(pause.transform, Color.white);

            // Speedometer: bottom centre, between the pedals and below the car
            var gauge = UIKit.Rect(hud, "Speedometer").Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(230, 230));
            var gbg = UIKit.Img(gauge, "Back", new Color(0, 0, 0, 0.5f), UIKit.Circle);
            gbg.rectTransform.Stretch();
            var ring = UIKit.Img(gauge, "Ring", new Color(1, 1, 1, 0.12f), UIKit.Ring);
            ring.rectTransform.Stretch(10, 10, 10, 10);
            speedFill = UIKit.Img(gauge, "Fill", UIKit.Saffron, UIKit.Ring);
            speedFill.rectTransform.Stretch(10, 10, 10, 10);
            speedFill.type = Image.Type.Filled;
            speedFill.fillMethod = Image.FillMethod.Radial360;
            speedFill.fillOrigin = (int)Image.Origin360.Bottom;
            speedFill.fillClockwise = true;
            speedText = UIKit.Txt(gauge, "0", 76, Color.white);
            speedText.rectTransform.Stretch(0, 0, 40, 60);
            speedText.Shadowed(new Color(0, 0, 0, 0.5f), 3f);
            var kmh = UIKit.Txt(gauge, "km/h", 30, UIKit.Dim);
            kmh.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(200, 40));

            // Nitro meter under the gauge
            var nl = UIKit.Txt(hud, "NITRO", 22, new Color(0.55f, 0.8f, 1f));
            nl.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(200, 30));
            nitroFill = UIKit.Bar(hud, "NitroBar", new Color(0, 0, 0, 0.5f), new Color(0.3f, 0.65f, 1f));
            ((RectTransform)nitroFill.transform.parent).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(300, 26));

            // Combo bar (appears during near-miss streaks) — just under the top bar
            comboText = UIKit.Txt(hud, "", 54, UIKit.Gold);
            comboText.Outlined(new Color(0, 0, 0, 0.6f), 3f);
            comboText.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -214), new Vector2(700, 70));
            comboFill = UIKit.Bar(hud, "ComboBar", new Color(0, 0, 0, 0.4f), UIKit.Gold);
            ((RectTransform)comboFill.transform.parent).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(320, 18));

            hintText = UIKit.Txt(hud, "", 38, Color.white);
            hintText.Outlined(new Color(0, 0, 0, 0.6f), 2f);
            hintText.lineSpacing = 1.15f;
            hintText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(980, 220));
            bool swipe = SaveSystem.Data.settings.controlMode == (int)ControlMode.Swipe;
            hintText.text = swipe ? "SWIPE or TAP left / right to change lanes\nSWIPE UP for nitro  •  HOLD GAS to speed up"
                                  : "Use the arrow buttons (or tap left / right)\nNITRO for a boost  •  HOLD GAS to speed up";
        }

        void BuildControls()
        {
            var input = gm.Input;
            bool buttons = SaveSystem.Data.settings.controlMode == (int)ControlMode.Buttons;

            // Brake pedal
            // Portrait, thumb-reachable bottom corners.
            //  Swipe mode:   [BRAKE]  (speedo)  [GAS]      — horn above brake, nitro above gas
            //  Buttons mode: [ < ]    (speedo)  [ > ]      — brake/gas stacked above the arrows
            float pedalY = buttons ? 256f : 30f;
            Vector2 pedalSize = buttons ? new Vector2(200, 200) : new Vector2(200, 230);
            float upperY = pedalY + pedalSize.y + 30f;

            var brake = Pedal("BRAKE", UIKit.Red, v => input.brakeHeld = v);
            brake.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, pedalY), pedalSize);
            input.RegisterBlocker(brake);

            // Gas pedal
            var gas = Pedal("GAS", UIKit.Green, v => input.gasHeld = v);
            gas.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, pedalY), pedalSize);
            input.RegisterBlocker(gas);

            // Nitro button
            var nitro = UIKit.Img(safe, "NitroButton", new Color(0.2f, 0.5f, 1f), UIKit.Circle);
            nitro.raycastTarget = true;
            nitro.Shadowed(new Color(0, 0, 0, 0.4f), 4f);
            nitro.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-44, upperY), new Vector2(170, 170));
            nitroBtnImg = nitro;
            nitroBtnLabel = UIKit.Txt(nitro.transform, "NITRO", 34, Color.white);
            nitroBtnLabel.rectTransform.Stretch();
            var nh = nitro.gameObject.AddComponent<HoldButton>();
            nh.onPress = input.UiNitro;
            input.RegisterBlocker(nitro.rectTransform);

            // Horn
            var horn = UIKit.Img(safe, "HornButton", new Color(0, 0, 0, 0.45f), UIKit.Circle);
            horn.raycastTarget = true;
            horn.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(60, upperY + 20f), new Vector2(120, 120));
            var hl = UIKit.Txt(horn.transform, "HORN", 26, Color.white);
            hl.rectTransform.Stretch();
            horn.gameObject.AddComponent<HoldButton>().onPress = input.UiHorn;
            input.RegisterBlocker(horn.rectTransform);

            // Steering arrows (Buttons control mode)
            buttonsModeControls = UIKit.Rect(safe, "SteeringButtons").gameObject;
            ((RectTransform)buttonsModeControls.transform).Stretch();
            var left = Arrow(buttonsModeControls.transform, -1);
            left.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 30), new Vector2(200, 200));
            var right = Arrow(buttonsModeControls.transform, 1);
            right.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 30), new Vector2(200, 200));
            input.RegisterBlocker(left);
            input.RegisterBlocker(right);
            buttonsModeControls.SetActive(buttons);
        }

        RectTransform Pedal(string label, Color c, System.Action<bool> onChange)
        {
            var img = UIKit.RoundedPanel(safe, label, new Color(c.r, c.g, c.b, 0.85f));
            img.raycastTarget = true;
            img.Shadowed(new Color(0, 0, 0, 0.4f), 5f);
            for (int i = 0; i < 4; i++)
            {
                var stripe = UIKit.RoundedPanel(img.transform, "Grip", new Color(0, 0, 0, 0.18f), 3f);
                stripe.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70 + i * 34), new Vector2(130, 14));
            }
            var t = UIKit.Txt(img.transform, label, 32, Color.white);
            t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(180, 44));
            img.gameObject.AddComponent<HoldButton>().onChange = onChange;
            return img.rectTransform;
        }

        RectTransform Arrow(Transform parent, int dir)
        {
            var img = UIKit.Img(parent, dir < 0 ? "Left" : "Right", new Color(0, 0, 0, 0.45f), UIKit.Circle);
            img.raycastTarget = true;
            for (int i = 0; i < 2; i++)
            {
                var bar = UIKit.RoundedPanel(img.transform, "Chevron", Color.white, 4f);
                bar.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-dir * 4f, i == 0 ? 20 : -20), new Vector2(70, 18));
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, (i == 0 ? -45 : 45) * dir);
            }
            var input = gm.Input;
            img.gameObject.AddComponent<HoldButton>().onPress = () => input.UiSteer(dir);
            return img.rectTransform;
        }

        void BuildCountdown()
        {
            countdownText = UIKit.Txt(safe, "", 240, Color.white);
            countdownText.Outlined(new Color(0, 0, 0, 0.6f), 5f);
            countdownText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 300));
        }

        void OnCountdown(int n)
        {
            countdownText.text = n > 0 ? n.ToString() : "GO!";
            countdownText.color = n > 0 ? Color.white : UIKit.Green;
            Tween.Pop(this, countdownText.transform, 0.4f, 1.8f);
        }

        void OnNearMiss(int combo, int points)
        {
            string msg = combo > 1 ? "NEAR MISS x" + combo + "  +" + points : "NEAR MISS  +" + points;
            FloatingText.Spawn(hud, msg, combo > 3 ? UIKit.Gold : Color.white, combo > 3 ? 68 : 56, new Vector2(Random.Range(-60f, 60f), 230), 1f);
        }

        void Update()
        {
            if (gm == null) return;
            var s = gm.Stats;
            if (s.Score != shownScore) { shownScore = s.Score; scoreText.text = Util.FormatScore(shownScore); }
            int dist = Mathf.FloorToInt(s.distance);
            if (dist != shownDist) { shownDist = dist; distanceText.text = Util.FormatDistance(dist); }
            if (s.coins != shownCoins) { shownCoins = s.coins; coinText.text = shownCoins.ToString(); }
            int spd = Mathf.RoundToInt(gm.Player.SpeedKmh);
            if (spd != shownSpeed) { shownSpeed = spd; speedText.text = spd.ToString(); }
            float n = Mathf.Clamp01(gm.Player.SpeedKmh / 260f) * 0.75f;
            speedFill.fillAmount = Mathf.Lerp(speedFill.fillAmount, n, Time.unscaledDeltaTime * 8f);
            speedFill.color = gm.Player.NitroActive ? new Color(0.4f, 0.75f, 1f) : Color.Lerp(UIKit.Saffron, UIKit.Red, Mathf.Clamp01((gm.Player.SpeedKmh - 120f) / 100f));
            nitroFill.fillAmount = gm.Player.Nitro;
            bool ready = gm.Player.Nitro >= gm.Cfg.nitroMinToActivate && !gm.Player.NitroActive;
            nitroBtnImg.color = gm.Player.NitroActive ? new Color(0.6f, 0.85f, 1f) : (ready ? new Color(0.2f, 0.5f, 1f) : new Color(0.25f, 0.3f, 0.4f, 0.8f));
            if (ready) { float p = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f; nitroBtnImg.transform.localScale = new Vector3(p, p, 1); }

            bool combo = s.combo > 1 && gm.ComboTimeLeft > 0f;
            comboText.gameObject.SetActive(combo);
            comboFill.transform.parent.gameObject.SetActive(combo);
            if (combo)
            {
                comboText.text = "COMBO x" + s.combo;
                comboFill.fillAmount = gm.ComboTimeLeft / gm.Cfg.comboWindow;
            }
        }

        void OnState(GameState st)
        {
            bool racing = st == GameState.Playing || st == GameState.Countdown;
            hud.gameObject.SetActive(st != GameState.GameOver);
            hintText.gameObject.SetActive(st == GameState.Countdown && SaveSystem.Data.stats.runs < 5);
            if (st == GameState.Playing) Tween.Fade(this, UIKit.Group(countdownText.gameObject), 0f, 0.6f);
            else if (st == GameState.Countdown) UIKit.Group(countdownText.gameObject).alpha = 1f;
            foreach (Transform t in safe)
                if (t.GetComponent<HoldButton>() != null || t.gameObject == buttonsModeControls)
                    t.gameObject.SetActive(racing && (t.gameObject != buttonsModeControls || SaveSystem.Data.settings.controlMode == (int)ControlMode.Buttons));

            if (st == GameState.Paused) pausePanel.Show(); else if (pausePanel.Visible) pausePanel.Hide();
            if (st == GameState.GameOver) ShowGameOver(); else if (overPanel.Visible) overPanel.Hide(true);
        }

        // ------------------------------------------------------------------ pause

        void BuildPause()
        {
            pausePanel = UIPanel.Create(canvas.transform, "PausePanel", new Color(0.02f, 0.03f, 0.06f, 0.75f));
            var box = UIKit.RoundedPanel(pausePanel.transform, "Box", UIKit.Panel);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
            pausePanel.Content = box.rectTransform;
            var title = UIKit.Txt(box.transform, "PAUSED", 84, UIKit.Saffron);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(700, 110));

            float y = -180;
            var resume = UIKit.Btn(box.transform, "RESUME", UIKit.Green, () => gm.Resume(), 48);
            resume.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(720, 120));
            y -= 140;
            var restart = UIKit.Btn(box.transform, "RESTART", UIKit.Saffron, () => gm.Restart(), 44);
            restart.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(720, 110));
            y -= 140;

            var ml = UIKit.Txt(box.transform, "MUSIC", 30, UIKit.Dim, TextAnchor.MiddleLeft);
            ml.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-230, y - 10), new Vector2(240, 60));
            var ms = UIKit.SliderCtl(box.transform, "MusicSlider", SaveSystem.Data.settings.musicVolume, v =>
            {
                SaveSystem.Data.settings.musicVolume = v; AudioManager.I?.ApplyVolumes();
            });
            ((RectTransform)ms.transform).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(140, y - 10), new Vector2(420, 60));
            y -= 80;
            var sl = UIKit.Txt(box.transform, "SOUND FX", 30, UIKit.Dim, TextAnchor.MiddleLeft);
            sl.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-230, y - 10), new Vector2(240, 60));
            var ss = UIKit.SliderCtl(box.transform, "SfxSlider", SaveSystem.Data.settings.sfxVolume, v => SaveSystem.Data.settings.sfxVolume = v);
            ((RectTransform)ss.transform).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(140, y - 10), new Vector2(420, 60));
            y -= 120;

            var home = UIKit.Btn(box.transform, "MAIN MENU", new Color(0.33f, 0.36f, 0.45f), () => { SaveSystem.Save(); gm.Home(); }, 40);
            home.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(720, 100));
            pausePanel.Hide(true);
        }

        // ------------------------------------------------------------------ game over

        void BuildGameOver()
        {
            // Portrait stack: title → score card → reward card … action buttons at the bottom (thumb reach).
            overPanel = UIPanel.Create(canvas.transform, "GameOverPanel", new Color(0.02f, 0.03f, 0.06f, 0.72f));
            var root = UIKit.Rect(overPanel.transform, "Content").Stretch();
            root.gameObject.AddComponent<SafeArea>();
            overPanel.Content = root;

            overTitle = UIKit.Txt(root, "CRASHED!", 96, UIKit.Red);
            overTitle.Outlined(new Color(0, 0, 0, 0.6f), 4f);
            overTitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1040, 140));

            // Score card
            var left = UIKit.RoundedPanel(root, "ScoreCard", UIKit.Panel);
            left.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(960, 540));
            var sl = UIKit.Txt(left.transform, "SCORE", 36, UIKit.Saffron);
            sl.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(600, 50));
            overScore = UIKit.Txt(left.transform, "0", 110, Color.white);
            overScore.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(900, 130));
            overBest = UIKit.Txt(left.transform, "", 34, UIKit.Gold);
            overBest.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(900, 50));
            newBestRibbon = UIKit.RoundedPanel(left.transform, "NewBest", UIKit.Gold).gameObject;
            ((RectTransform)newBestRibbon.transform).Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-120, -20), new Vector2(240, 70));
            newBestRibbon.transform.localRotation = Quaternion.Euler(0, 0, -12);
            var nbt = UIKit.Txt(newBestRibbon.transform, "NEW BEST!", 34, UIKit.Ink);
            nbt.rectTransform.Stretch();
            overStats = UIKit.Txt(left.transform, "", 34, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
            overStats.lineSpacing = 1.25f;
            overStats.rectTransform.Stretch(70, 50, 270, 20);

            // Reward card (+ missions completed)
            var right = UIKit.RoundedPanel(root, "RewardCard", UIKit.Panel);
            right.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -790), new Vector2(960, 480));
            var rl = UIKit.Txt(right.transform, "COINS EARNED", 36, UIKit.Saffron);
            rl.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(600, 50));
            overReward = UIKit.Txt(right.transform, "", 34, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
            overReward.lineSpacing = 1.2f;
            overReward.rectTransform.Stretch(70, 50, 90, 170);
            overMissions = UIKit.Txt(right.transform, "", 28, new Color(0.6f, 1f, 0.7f), TextAnchor.UpperLeft, FontStyle.Normal);
            overMissions.rectTransform.Stretch(70, 40, 320, 10);

            // Buttons (bottom up): HOME | GARAGE, RACE AGAIN, REVIVE | 2X COINS (ads, only when loaded)
            var home = UIKit.Btn(root, "HOME", new Color(0.33f, 0.36f, 0.45f), () => gm.Home(), 44);
            home.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-245, 40), new Vector2(470, 120));
            var garage = UIKit.Btn(root, "GARAGE", UIKit.Blue, () => { MainMenuUI.OpenOnLoad = MainMenuUI.Page.Garage; gm.Home(); }, 44);
            garage.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(245, 40), new Vector2(470, 120));
            var restart = UIKit.Btn(root, "RACE AGAIN", UIKit.Green, () => gm.Restart(), 60);
            restart.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 190), new Vector2(960, 150));

            reviveBtn = UIKit.Btn(root, "REVIVE  (AD)", UIKit.Purple, OnRevive, 40);
            reviveBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-245, 370), new Vector2(470, 120));
            doubleBtn = UIKit.Btn(root, "2X COINS  (AD)", UIKit.Purple, OnDouble, 40);
            doubleBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(245, 370), new Vector2(470, 120));
            overPanel.Hide(true);
        }

        void ShowGameOver()
        {
            var s = gm.Stats;
            var d = SaveSystem.Data;
            overTitle.text = gm.NewBest ? "NEW HIGH SCORE!" : "CRASHED!";
            overTitle.color = gm.NewBest ? UIKit.Gold : UIKit.Red;
            overScore.text = Util.FormatScore(s.Score);
            overBest.text = "BEST  " + Util.FormatScore(d.bestScore);
            newBestRibbon.SetActive(gm.NewBest);
            overStats.text =
                "Distance            <b>" + Util.FormatDistance(s.distance) + "</b>\n" +
                "Top speed           <b>" + Mathf.RoundToInt(s.topSpeedKmh) + " km/h</b>\n" +
                "Near misses         <b>" + s.nearMisses + "</b>   (best combo x" + s.maxCombo + ")\n" +
                "Overtakes           <b>" + s.overtakes + "</b>";
            RefreshReward();

            var sb = new StringBuilder();
            if (gm.CompletedMissions.Count > 0)
            {
                sb.Append("<color=#FFC83D><b>MISSIONS COMPLETED</b></color>\n");
                foreach (var m in gm.CompletedMissions) sb.Append("• ").Append(MissionManager.Describe(m)).Append("  (+").Append(m.reward).Append(")\n");
                sb.Append("<size=24>Claim rewards in Missions</size>");
            }
            overMissions.text = sb.ToString();

            reviveBtn.gameObject.SetActive(gm.CanRevive);
            doubleBtn.gameObject.SetActive(gm.CanDoubleCoins);
            overPanel.Show();
            Tween.Pop(this, overTitle.transform, 0.5f, 0.3f);
        }

        void RefreshReward()
        {
            var r = gm.Reward;
            overReward.text =
                "Coins collected      <b>+" + r.collected + "</b>\n" +
                "Distance bonus       <b>+" + r.distanceBonus + "</b>\n" +
                "Near-miss bonus      <b>+" + r.nearMissBonus + "</b>\n" +
                (gm.CoinsDoubled ? "Ad bonus (2x)        <b>+" + gm.CoinsEarnedTotal + "</b>\n" : "") +
                "<color=#FFC83D><size=44><b>TOTAL  +" + (gm.CoinsEarnedTotal * (gm.CoinsDoubled ? 2 : 1)) + "</b></size></color>";
        }

        void OnRevive()
        {
            reviveBtn.interactable = false;
            gm.RequestRevive(ok =>
            {
                if (reviveBtn != null) reviveBtn.interactable = true;
                if (!ok) reviveBtn.gameObject.SetActive(false);
            });
        }

        void OnDouble()
        {
            doubleBtn.interactable = false;
            gm.RequestDoubleCoins(ok =>
            {
                doubleBtn.gameObject.SetActive(false);
                RefreshReward();
            });
        }
    }
}
