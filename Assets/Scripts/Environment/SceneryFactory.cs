using UnityEngine;

namespace ITR
{
    public enum PropKind { Building, Shop, Tree, PalmTree, Billboard, DirectionSign, SpeedSign, WarningSign }

    /// <summary>Marker on every roadside prop so the pool knows its footprint.</summary>
    public class SceneryProp : MonoBehaviour
    {
        public PropKind kind;
        /// <summary>Length along the road (local X before rotation).</summary>
        public float width;
    }

    /// <summary>
    /// Procedural Indian roadside props. Built facing local +Z ("the road"), width along local X.
    /// Every prop is a single combined mesh plus optional 3D text.
    /// </summary>
    public static class SceneryFactory
    {
        static readonly Color[] WallColors =
        {
            Util.Hex("F2D6A2"), Util.Hex("E8B4A0"), Util.Hex("BFD8C8"), Util.Hex("F4E3C1"), Util.Hex("D7C4E2"),
            Util.Hex("F0C987"), Util.Hex("C9E0F0"), Util.Hex("E6E1D3"), Util.Hex("F5B971"), Util.Hex("B9D7A8")
        };
        static readonly Color[] Bright =
        {
            Util.Hex("E63946"), Util.Hex("F4A261"), Util.Hex("2A9D8F"), Util.Hex("264653"), Util.Hex("E9C46A"),
            Util.Hex("1D3557"), Util.Hex("8338EC"), Util.Hex("FF006E"), Util.Hex("06A77D"), Util.Hex("FB5607")
        };
        static readonly string[] ShopNames =
        {
            "GUPTA GENERAL STORE", "SHARMA SWEETS", "MOBILE REPAIR", "HIGHWAY DHABA", "CHAI STALL",
            "MEDICAL STORE", "KIRANA STORE", "TYRE PUNCTURE", "PATEL HARDWARE", "FRESH JUICE",
            "SAREE CENTRE", "PAAN SHOP", "AUTO PARTS", "BAKERY", "XEROX & PRINT", "NAMKEEN BHANDAR"
        };
        static readonly string[] Slogans =
        {
            "DRIVE SAFE\nARRIVE ALIVE", "SPEED THRILLS\nBUT KILLS", "WEAR YOUR\nSEAT BELT", "INDIAN\nTRAFFIC RUSH",
            "CHAI & SAMOSA\nOPEN 24x7", "SUPER GRIP\nTYRES", "LANE DRIVING\nIS SANE DRIVING", "DON'T DRINK\n& DRIVE",
            "HORN OK\nPLEASE", "MONSOON SALE\n50% OFF"
        };
        static readonly string[] Cities =
        {
            "MUMBAI", "PUNE", "DELHI", "JAIPUR", "AGRA", "NASHIK", "SURAT", "INDORE", "BHOPAL", "NAGPUR",
            "LUCKNOW", "KANPUR", "CHENNAI", "MYSURU", "GOA", "KOCHI"
        };

        public static GameObject Create(PropKind kind, System.Random rng)
        {
            GameObject go;
            float width;
            switch (kind)
            {
                case PropKind.Building: go = Building(rng, out width); break;
                case PropKind.Shop: go = Shop(rng, out width); break;
                case PropKind.Tree: go = Tree(rng, out width); break;
                case PropKind.PalmTree: go = Palm(rng, out width); break;
                case PropKind.Billboard: go = Billboard(rng, out width); break;
                case PropKind.DirectionSign: go = DirectionSign(rng, out width); break;
                case PropKind.SpeedSign: go = SpeedSign(rng, out width); break;
                default: go = WarningSign(rng, out width); break;
            }
            var p = go.AddComponent<SceneryProp>();
            p.kind = kind;
            p.width = width;
            return go;
        }

        static T Pick<T>(System.Random r, T[] arr) => arr[r.Next(arr.Length)];
        static float Range(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        // --------------------------------------------------------------- building
        static GameObject Building(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = Range(r, 8f, 13f);
            float depth = Range(r, 7f, 11f);
            int floors = r.Next(2, 8);
            float fh = 3.1f;
            float h = floors * fh;
            var wall = MaterialFactory.Lit(Pick(r, WallColors), 0.05f);
            var trim = MaterialFactory.Lit(Color.Lerp(Pick(r, WallColors), Color.white, 0.4f), 0.05f);
            var window = MaterialFactory.Lit(new Color(0.12f, 0.16f, 0.22f), 0.8f);
            var frame = MaterialFactory.Lit(new Color(0.9f, 0.9f, 0.88f), 0.1f);
            var rail = MaterialFactory.Lit(new Color(0.25f, 0.25f, 0.27f), 0.4f);
            var tank = MaterialFactory.Lit(new Color(0.08f, 0.08f, 0.08f), 0.2f);

            a.Box(wall, new Vector3(0, h * 0.5f, -depth * 0.5f), new Vector3(width, h, depth));
            a.Box(trim, new Vector3(0, h + 0.4f, -depth * 0.5f), new Vector3(width + 0.2f, 0.8f, depth + 0.2f)); // parapet
            int cols = Mathf.Max(2, Mathf.FloorToInt(width / 2.6f));
            float spacing = width / cols;
            bool balconies = r.NextDouble() < 0.6;
            for (int f = 0; f < floors; f++)
            {
                float y = f * fh + fh * 0.55f;
                a.Box(trim, new Vector3(0, f * fh + 0.05f, 0.05f), new Vector3(width + 0.1f, 0.15f, 0.12f));
                for (int c = 0; c < cols; c++)
                {
                    float x = -width * 0.5f + spacing * (c + 0.5f);
                    if (f == 0 && c == cols / 2)
                    {
                        a.Box(rail, new Vector3(x, 1.1f, 0.02f), new Vector3(1.4f, 2.2f, 0.1f)); // door
                        continue;
                    }
                    a.Box(frame, new Vector3(x, y, 0.03f), new Vector3(1.35f, 1.5f, 0.08f));
                    a.Box(window, new Vector3(x, y, 0.07f), new Vector3(1.15f, 1.3f, 0.04f));
                    if (balconies && f > 0 && c % 2 == 0)
                    {
                        a.Box(trim, new Vector3(x, f * fh + 0.1f, 0.6f), new Vector3(spacing * 0.9f, 0.12f, 1.2f));
                        a.Box(rail, new Vector3(x, f * fh + 0.6f, 1.18f), new Vector3(spacing * 0.9f, 0.9f, 0.05f));
                    }
                }
            }
            // rooftop water tank on a stand (very common sight)
            float tx = Range(r, -width * 0.3f, width * 0.3f);
            a.Box(rail, new Vector3(tx, h + 0.5f, -depth * 0.5f), new Vector3(1.6f, 1f, 1.6f));
            a.Cylinder(tank, new Vector3(tx, h + 1.7f, -depth * 0.5f), 1.4f, 1.6f, Vector3.zero);
            if (r.NextDouble() < 0.5)
                a.Box(rail, new Vector3(-tx * 0.5f, h + 1.4f, -depth * 0.7f), new Vector3(0.05f, 2.8f, 0.05f)); // antenna
            return a.Build("Building", null, true);
        }

        // --------------------------------------------------------------- shop
        static GameObject Shop(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = Range(r, 6f, 10f);
            float depth = 6f;
            bool upper = r.NextDouble() < 0.7;
            float h = upper ? 6.4f : 3.6f;
            var wall = MaterialFactory.Lit(Pick(r, WallColors), 0.05f);
            var interior = MaterialFactory.Lit(new Color(0.12f, 0.1f, 0.09f), 0.05f);
            var shutter = MaterialFactory.Lit(new Color(0.6f, 0.62f, 0.64f), 0.4f);
            var signC = Pick(r, Bright);
            var sign = MaterialFactory.Lit(signC, 0.3f);
            var awningA = MaterialFactory.Lit(Pick(r, Bright), 0.1f);
            var awningB = MaterialFactory.Lit(new Color(0.97f, 0.96f, 0.92f), 0.1f);
            var window = MaterialFactory.Lit(new Color(0.12f, 0.16f, 0.22f), 0.8f);

            a.Box(wall, new Vector3(0, h * 0.5f, -depth * 0.5f), new Vector3(width, h, depth));
            a.Box(interior, new Vector3(0, 1.4f, 0.02f), new Vector3(width * 0.84f, 2.6f, 0.06f)); // open shop front
            a.Box(shutter, new Vector3(0, 2.62f, 0.06f), new Vector3(width * 0.84f, 0.3f, 0.06f));  // rolled shutter
            // goods on display
            for (int i = 0; i < 4; i++)
            {
                var g = MaterialFactory.Lit(Pick(r, Bright), 0.2f);
                float x = -width * 0.35f + i * width * 0.23f;
                a.Box(g, new Vector3(x, 0.45f + (i % 2) * 0.1f, 0.5f), new Vector3(width * 0.18f, 0.9f, 0.8f));
            }
            // striped awning
            int stripes = 8;
            for (int i = 0; i < stripes; i++)
            {
                float x = -width * 0.5f + width / stripes * (i + 0.5f);
                a.Box(i % 2 == 0 ? awningA : awningB, new Vector3(x, 3.0f, 0.8f), new Vector3(width / stripes, 0.06f, 1.7f), new Vector3(-18, 0, 0));
            }
            // sign board
            a.Box(sign, new Vector3(0, 3.65f, 0.12f), new Vector3(width * 0.92f, 0.9f, 0.12f));
            if (upper)
            {
                for (int c = 0; c < 2; c++)
                {
                    float x = (c == 0 ? -1 : 1) * width * 0.25f;
                    a.Box(window, new Vector3(x, 5.0f, 0.03f), new Vector3(1.2f, 1.2f, 0.06f));
                }
                a.Box(MaterialFactory.Lit(Color.Lerp(Pick(r, WallColors), Color.white, 0.5f)), new Vector3(0, h + 0.3f, -depth * 0.5f), new Vector3(width + 0.1f, 0.6f, depth + 0.1f));
            }
            var go = a.Build("Shop", null, true);
            var lum = signC.r * 0.3f + signC.g * 0.59f + signC.b * 0.11f;
            Util.Text3D(go.transform, Pick(r, ShopNames), new Vector3(0, 3.65f, 0.2f), new Vector3(0, 180, 0), 0.42f,
                lum > 0.55f ? new Color(0.1f, 0.05f, 0.05f) : Color.white);
            return go;
        }

        // --------------------------------------------------------------- trees
        static GameObject Tree(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = 4f;
            var bark = MaterialFactory.Lit(new Color(0.36f, 0.26f, 0.18f), 0.05f);
            float h = Range(r, 2.4f, 4f);
            a.Cylinder(bark, new Vector3(0, h * 0.5f, 0), 0.35f, h, Vector3.zero);
            // whitewashed base (common on Indian roadside trees)
            a.Cylinder(MaterialFactory.Lit(new Color(0.95f, 0.95f, 0.93f)), new Vector3(0, 0.5f, 0), 0.37f, 1f, Vector3.zero);
            a.Cylinder(MaterialFactory.Lit(new Color(0.8f, 0.15f, 0.12f)), new Vector3(0, 1.05f, 0), 0.375f, 0.12f, Vector3.zero);
            int blobs = r.Next(3, 6);
            for (int i = 0; i < blobs; i++)
            {
                var leaf = MaterialFactory.Lit(Color.Lerp(new Color(0.16f, 0.38f, 0.12f), new Color(0.32f, 0.52f, 0.18f), (float)r.NextDouble()), 0.05f);
                float s = Range(r, 2.4f, 3.6f);
                a.Sphere(leaf, new Vector3(Range(r, -1.2f, 1.2f), h + Range(r, 0.6f, 2f), Range(r, -1.2f, 1.2f)), new Vector3(s, s * 0.8f, s));
            }
            return a.Build("Tree", null, true);
        }

        static GameObject Palm(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = 3f;
            var bark = MaterialFactory.Lit(new Color(0.45f, 0.36f, 0.25f), 0.05f);
            var leaf = MaterialFactory.Lit(new Color(0.2f, 0.48f, 0.15f), 0.1f);
            float h = Range(r, 6f, 8.5f);
            float lean = Range(r, -6f, 6f);
            for (int i = 0; i < 4; i++)
            {
                float y = h / 4f * (i + 0.5f);
                a.Cylinder(bark, new Vector3(Mathf.Sin(lean * Mathf.Deg2Rad) * y, y, 0), 0.32f - i * 0.03f, h / 4f + 0.05f, new Vector3(0, 0, -lean));
            }
            var top = new Vector3(Mathf.Sin(lean * Mathf.Deg2Rad) * h, h, 0);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f + Range(r, -10, 10);
                var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                a.Box(leaf, top + dir * 1.3f + Vector3.down * 0.3f, new Vector3(0.5f, 0.06f, 2.8f), new Vector3(25, ang, 0));
            }
            a.Sphere(MaterialFactory.Lit(new Color(0.35f, 0.25f, 0.12f)), top, new Vector3(0.6f, 0.5f, 0.6f));
            return a.Build("Palm", null, true);
        }

        // --------------------------------------------------------------- billboard
        static GameObject Billboard(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = 9f;
            var steel = MaterialFactory.Lit(new Color(0.35f, 0.36f, 0.38f), 0.5f);
            var bgC = Pick(r, Bright);
            var board = MaterialFactory.Lit(bgC, 0.2f);
            var frame = MaterialFactory.Lit(new Color(0.95f, 0.95f, 0.95f), 0.3f);
            foreach (float x in new[] { -2.5f, 2.5f })
                a.Box(steel, new Vector3(x, 3.5f, 0), new Vector3(0.3f, 7f, 0.3f));
            a.Box(frame, new Vector3(0, 7.5f, 0.1f), new Vector3(8.6f, 3.6f, 0.2f));
            a.Box(board, new Vector3(0, 7.5f, 0.22f), new Vector3(8.2f, 3.2f, 0.06f));
            a.Box(steel, new Vector3(0, 5.6f, 0.6f), new Vector3(8f, 0.08f, 0.9f)); // walkway
            var go = a.Build("Billboard", null, true);
            var lum = bgC.r * 0.3f + bgC.g * 0.59f + bgC.b * 0.11f;
            var t = Util.Text3D(go.transform, Pick(r, Slogans), new Vector3(0, 7.5f, 0.27f), new Vector3(0, 180, 0), 0.85f,
                lum > 0.55f ? new Color(0.08f, 0.08f, 0.1f) : Color.white);
            t.lineSpacing = 0.85f;
            return go;
        }

        // --------------------------------------------------------------- signs (face local +Z = towards approaching driver after rotation)
        static GameObject DirectionSign(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = 4f;
            var steel = MaterialFactory.Lit(new Color(0.55f, 0.56f, 0.58f), 0.5f);
            var green = MaterialFactory.Lit(new Color(0.05f, 0.42f, 0.2f), 0.3f);
            var white = MaterialFactory.Lit(Color.white, 0.3f);
            a.Box(steel, new Vector3(-1.4f, 2.2f, 0), new Vector3(0.15f, 4.4f, 0.15f));
            a.Box(steel, new Vector3(1.4f, 2.2f, 0), new Vector3(0.15f, 4.4f, 0.15f));
            a.Box(white, new Vector3(0, 4.2f, 0.1f), new Vector3(3.7f, 2.1f, 0.06f));
            a.Box(green, new Vector3(0, 4.2f, 0.14f), new Vector3(3.55f, 1.95f, 0.04f));
            var go = a.Build("DirectionSign", null, true);
            int c1 = r.Next(Cities.Length), c2 = (c1 + 1 + r.Next(Cities.Length - 1)) % Cities.Length;
            string txt = Cities[c1] + "  " + r.Next(12, 90) + " km\n" + Cities[c2] + "  " + r.Next(100, 480) + " km";
            var t = Util.Text3D(go.transform, txt, new Vector3(0, 4.2f, 0.17f), new Vector3(0, 180, 0), 0.36f, Color.white);
            t.lineSpacing = 1.1f;
            return go;
        }

        static GameObject SpeedSign(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = 2f;
            var steel = MaterialFactory.Lit(new Color(0.55f, 0.56f, 0.58f), 0.5f);
            a.Box(steel, new Vector3(0, 1.5f, 0), new Vector3(0.1f, 3f, 0.1f));
            a.Cylinder(MaterialFactory.Lit(new Color(0.85f, 0.08f, 0.08f)), new Vector3(0, 3.2f, 0.06f), 1.0f, 0.05f, new Vector3(90, 0, 0));
            a.Cylinder(MaterialFactory.Lit(Color.white), new Vector3(0, 3.2f, 0.09f), 0.76f, 0.05f, new Vector3(90, 0, 0));
            var go = a.Build("SpeedSign", null, true);
            int[] limits = { 60, 80, 100, 120 };
            Util.Text3D(go.transform, limits[r.Next(limits.Length)].ToString(), new Vector3(0, 3.2f, 0.13f), new Vector3(0, 180, 0), 0.36f, Color.black);
            return go;
        }

        static GameObject WarningSign(System.Random r, out float width)
        {
            var a = new MeshAssembler();
            width = 2f;
            var steel = MaterialFactory.Lit(new Color(0.55f, 0.56f, 0.58f), 0.5f);
            a.Box(steel, new Vector3(0, 1.4f, 0), new Vector3(0.1f, 2.8f, 0.1f));
            a.Box(MaterialFactory.Lit(new Color(0.05f, 0.25f, 0.6f)), new Vector3(0, 3.1f, 0.06f), new Vector3(1.4f, 1.0f, 0.05f));
            var go = a.Build("InfoSign", null, true);
            string[] msgs = { "SLOW\nDOWN", "SCHOOL\nAHEAD", "NO\nOVERTAKING", "SPEED\nCAMERA", "PETROL\n2 km", "FOOD\n500 m" };
            var t = Util.Text3D(go.transform, msgs[r.Next(msgs.Length)], new Vector3(0, 3.1f, 0.1f), new Vector3(0, 180, 0), 0.22f, Color.white);
            t.lineSpacing = 0.9f;
            return go;
        }
    }
}
