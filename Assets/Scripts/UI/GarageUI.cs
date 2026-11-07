using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ITR
{
    /// <summary>Garage: browse the six cars, buy with coins, select, repaint and upgrade speed/handling/braking.</summary>
    public class GarageUI : MenuPage
    {
        int index;
        int previewColor;
        Text nameText, descText, countText, priceText, lockText;
        Image[] statFills = new Image[4];
        Text[] statValues = new Text[4];
        RectTransform upgradesRoot, colorsRoot;
        readonly List<Image> swatches = new List<Image>();
        readonly UpgradeRow[] rows = new UpgradeRow[3];
        Button actionBtn;

        class UpgradeRow
        {
            public Text label;
            public Image[] pips;
            public Button button;
        }

        public VehicleData Current => GarageService.Catalog.vehicles[index];

        public override void Build(MainMenuUI owner, Transform parent)
        {
            menu = owner;
            Panel = parent.gameObject.AddComponent<UIPanel>();
            Header(parent, "GARAGE");

            var card = UIKit.RoundedPanel(parent, "Card", new Color(UIKit.Panel.r, UIKit.Panel.g, UIKit.Panel.b, 0.985f));
            card.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1032, 1000));
            card.raycastTarget = true;
            Panel.Content = card.rectTransform;
            var c = card.transform;

            nameText = UIKit.Txt(c, "", 60, Color.white, TextAnchor.UpperLeft);
            nameText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -28), new Vector2(780, 76));
            countText = UIKit.Txt(c, "", 30, UIKit.Dim, TextAnchor.UpperRight);
            countText.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -42), new Vector2(160, 40));
            descText = UIKit.Txt(c, "", 28, UIKit.Dim, TextAnchor.UpperLeft, FontStyle.Normal);
            descText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -104), new Vector2(950, 70));

            string[] statNames = { "TOP SPEED", "ACCELERATION", "HANDLING", "BRAKING" };
            for (int i = 0; i < 4; i++)
            {
                float y = -190 - i * 62;
                var l = UIKit.Txt(c, statNames[i], 28, Color.white, TextAnchor.MiddleLeft);
                l.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, y), new Vector2(280, 44));
                statFills[i] = UIKit.Bar(c, "Stat" + i, new Color(1, 1, 1, 0.1f), UIKit.Saffron);
                ((RectTransform)statFills[i].transform.parent).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(330, y - 10), new Vector2(480, 26));
                statValues[i] = UIKit.Txt(c, "", 26, UIKit.Dim, TextAnchor.MiddleRight);
                statValues[i].rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, y), new Vector2(170, 44));
            }

            upgradesRoot = UIKit.Rect(c, "Upgrades").Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -450), new Vector2(1032, 240));
            string[] upNames = { "SPEED", "HANDLING", "BRAKING" };
            for (int i = 0; i < 3; i++)
            {
                var type = (UpgradeType)i;
                float y = -i * 78;
                var row = new UpgradeRow();
                row.label = UIKit.Txt(upgradesRoot, upNames[i], 28, Color.white, TextAnchor.MiddleLeft);
                row.label.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, y), new Vector2(200, 64));
                row.pips = new Image[5];
                for (int p = 0; p < 5; p++)
                {
                    row.pips[p] = UIKit.RoundedPanel(upgradesRoot, "Pip", new Color(1, 1, 1, 0.12f), 3f);
                    row.pips[p].rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(280 + p * 52, y - 18), new Vector2(42, 28));
                }
                row.button = UIKit.Btn(upgradesRoot, "UPGRADE", UIKit.Green, () => DoUpgrade(type), 26);
                row.button.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, y), new Vector2(330, 64));
                rows[i] = row;
            }

            colorsRoot = UIKit.Rect(c, "Colors").Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -700), new Vector2(1032, 80));
            var pl = UIKit.Txt(colorsRoot, "PAINT", 28, Color.white, TextAnchor.MiddleLeft);
            pl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(42, 0), new Vector2(150, 60));

            lockText = UIKit.Txt(c, "", 28, UIKit.Gold);
            lockText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(950, 40));
            actionBtn = UIKit.Btn(c, "SELECT", UIKit.Green, OnAction, 46, "ActionButton");
            actionBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(950, 110));
            priceText = actionBtn.Label();

            var prev = UIKit.Btn(parent, "<", new Color(0, 0, 0, 0.55f), () => Browse(-1), 80, "PrevCar");
            prev.GetComponent<RectTransform>().Place(new Vector2(0, 0.7f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(140, 140));
            var next = UIKit.Btn(parent, ">", new Color(0, 0, 0, 0.55f), () => Browse(1), 80, "NextCar");
            next.GetComponent<RectTransform>().Place(new Vector2(1, 0.7f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(140, 140));
        }

        public override void Refresh()
        {
            if (!Panel.Visible) index = GarageService.Catalog.IndexOf(SaveSystem.Data.selectedVehicle);
            var owned = SaveSystem.Data.GetVehicle(Current.id);
            previewColor = owned != null ? owned.colorIndex : 0;
            UpdateView(true);
        }

        public void Browse(int dir)
        {
            int n = GarageService.Catalog.vehicles.Count;
            index = (index + dir + n) % n;
            var owned = SaveSystem.Data.GetVehicle(Current.id);
            previewColor = owned != null ? owned.colorIndex : 0;
            UpdateView(true);
        }

        void UpdateView(bool rebuildCar)
        {
            var v = Current;
            bool owned = GarageService.IsOwned(v);
            bool selected = SaveSystem.Data.selectedVehicle == v.id;
            var stats = GarageService.GetStats(v);

            nameText.text = v.displayName.ToUpperInvariant();
            countText.text = (index + 1) + " / " + GarageService.Catalog.vehicles.Count;
            descText.text = v.description;
            float[] norm =
            {
                Mathf.InverseLerp(120f, 270f, stats.topSpeedKmh), Mathf.InverseLerp(5f, 15f, stats.acceleration),
                Mathf.InverseLerp(8f, 20f, stats.handling), Mathf.InverseLerp(10f, 32f, stats.braking)
            };
            string[] vals =
            {
                Mathf.RoundToInt(stats.topSpeedKmh) + " km/h", stats.acceleration.ToString("0.0"),
                stats.handling.ToString("0.0"), stats.braking.ToString("0.0")
            };
            for (int i = 0; i < 4; i++)
            {
                statFills[i].fillAmount = Mathf.Max(0.05f, norm[i]);
                statValues[i].text = vals[i];
            }

            upgradesRoot.gameObject.SetActive(owned);
            if (owned)
            {
                for (int i = 0; i < 3; i++)
                {
                    var type = (UpgradeType)i;
                    int lvl = GarageService.Level(v, type);
                    for (int p = 0; p < 5; p++)
                        rows[i].pips[p].color = p < lvl ? UIKit.Saffron : new Color(1, 1, 1, 0.12f);
                    int cost = GarageService.UpgradeCost(v, type);
                    bool max = cost < 0;
                    rows[i].button.interactable = !max && Wallet.CanAfford(cost);
                    rows[i].button.Label().text = max ? "MAX" : "UPGRADE  " + cost.ToString("N0");
                }
            }

            // Paint swatches
            foreach (var s in swatches) if (s != null) Destroy(s.gameObject);
            swatches.Clear();
            for (int i = 0; i < v.colors.Length; i++)
            {
                int ci = i;
                var sw = UIKit.Img(colorsRoot, "Swatch" + i, v.colors[i], UIKit.Circle);
                sw.raycastTarget = true;
                sw.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(240 + i * 82, 0), new Vector2(i == previewColor ? 72 : 58, i == previewColor ? 72 : 58));
                sw.Outlined(i == previewColor ? Color.white : new Color(0, 0, 0, 0.5f), i == previewColor ? 4f : 2f);
                var b = sw.gameObject.AddComponent<Button>();
                b.targetGraphic = sw;
                sw.gameObject.AddComponent<ButtonFx>();
                b.onClick.AddListener(() => PickColor(ci));
                swatches.Add(sw);
            }

            var img = actionBtn.GetComponent<Image>();
            if (!owned)
            {
                bool afford = Wallet.CanAfford(v.price);
                priceText.text = "BUY   " + v.price.ToString("N0") + " COINS";
                img.color = afford ? UIKit.Gold : new Color(0.4f, 0.4f, 0.45f);
                priceText.color = afford ? UIKit.Ink : Color.white;
                actionBtn.interactable = afford;
                lockText.text = afford ? "LOCKED • Preview only" : "LOCKED • Need " + (v.price - Wallet.Coins).ToString("N0") + " more coins";
            }
            else
            {
                priceText.color = Color.white;
                img.color = selected ? new Color(0.3f, 0.33f, 0.42f) : UIKit.Green;
                priceText.text = selected ? "IN USE" : "SELECT";
                actionBtn.interactable = !selected;
                lockText.text = "";
            }

            if (rebuildCar) menu.Showroom.ShowCar(v, v.colors[Mathf.Clamp(previewColor, 0, v.colors.Length - 1)]);
        }

        void PickColor(int ci)
        {
            previewColor = ci;
            var v = Current;
            if (GarageService.IsOwned(v)) GarageService.SetColor(v, ci);
            UpdateView(true);
        }

        void OnAction()
        {
            var v = Current;
            if (!GarageService.IsOwned(v))
            {
                var r = GarageService.Buy(v);
                if (r == GarageResult.Ok)
                {
                    GarageService.SetColor(v, previewColor);
                    GarageService.Select(v);
                    AudioManager.I?.Play(Sfx.Reward);
                    menu.ShowToast(v.displayName.ToUpperInvariant() + " UNLOCKED!", UIKit.Gold);
                }
                else menu.ShowToast("Not enough coins", UIKit.Red);
            }
            else
            {
                GarageService.Select(v);
                AudioManager.I?.Play(Sfx.Coin, 0.6f);
            }
            menu.RefreshTop();
            UpdateView(false);
        }

        void DoUpgrade(UpgradeType type)
        {
            var r = GarageService.Upgrade(Current, type);
            if (r == GarageResult.Ok)
            {
                AudioManager.I?.Play(Sfx.Reward, 0.7f);
                menu.ShowToast(type.ToString().ToUpperInvariant() + " UPGRADED!", UIKit.Green);
            }
            else if (r == GarageResult.NotEnoughCoins) menu.ShowToast("Not enough coins", UIKit.Red);
            menu.RefreshTop();
            UpdateView(false);
        }
    }
}
