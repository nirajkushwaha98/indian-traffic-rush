using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Builds the static geometry for one 40 m road segment: three-lane carriageway, painted markings,
    /// median divider with kerbs, shrubs and streetlights, opposite carriageway, footpaths and ground.
    /// The mesh is identical for every segment, so it is built once and shared.
    /// </summary>
    public static class RoadBuilder
    {
        public const float MedianInner = -GameConstants.RoadHalfWidth - 0.35f;   // -5.75
        public const float MedianOuter = MedianInner - 1.6f;                     // -7.35
        public const float OppositeInner = MedianOuter - 0.35f;                  // -7.7
        public const float OppositeOuter = OppositeInner - GameConstants.LaneWidth * 3f; // -18.5
        public const float FootpathRight = GameConstants.RoadHalfWidth + 1.8f;   // 7.2
        public const float SceneryRight = 11f;
        public const float SceneryLeft = OppositeOuter - 4.5f;                   // -23

        static Mesh shared;
        static Material[] sharedMats;

        /// <summary>Material whose colour the theme changes (lamp heads glow at sunset).</summary>
        public static Material LampMaterial { get; private set; }

        public static void Invalidate() { shared = null; }

        public static GameObject CreateSegmentGeometry(Transform parent)
        {
            if (shared == null) Bake();
            var go = new GameObject("RoadGeometry");
            go.transform.SetParent(parent, false);
            MeshAssembler.Apply(go, shared, sharedMats, false);
            return go;
        }

        static void Bake()
        {
            const float L = GameConstants.SegmentLength;
            const float hw = GameConstants.RoadHalfWidth;
            float midZ = L * 0.5f;
            var a = new MeshAssembler();

            var asphalt = MaterialFactory.Lit(new Color(0.2f, 0.2f, 0.22f), 0.15f);
            var asphaltOld = MaterialFactory.Lit(new Color(0.27f, 0.26f, 0.26f), 0.1f);
            var white = MaterialFactory.Lit(new Color(0.92f, 0.92f, 0.9f), 0.2f);
            var yellow = MaterialFactory.Lit(new Color(0.98f, 0.78f, 0.1f), 0.2f);
            var black = MaterialFactory.Lit(new Color(0.08f, 0.08f, 0.08f), 0.1f);
            var concrete = MaterialFactory.Lit(new Color(0.68f, 0.66f, 0.62f), 0.1f);
            var paver = MaterialFactory.Lit(new Color(0.62f, 0.45f, 0.38f), 0.1f);
            var soil = MaterialFactory.Lit(new Color(0.47f, 0.36f, 0.24f), 0.05f);
            var ground = MaterialFactory.Lit(new Color(0.55f, 0.5f, 0.34f), 0.02f);
            var grass = MaterialFactory.Lit(new Color(0.33f, 0.47f, 0.2f), 0.05f);
            var shrub = MaterialFactory.Lit(new Color(0.18f, 0.42f, 0.16f), 0.05f);
            var flowerPink = MaterialFactory.Lit(new Color(0.95f, 0.45f, 0.65f), 0.1f);
            var flowerWhite = MaterialFactory.Lit(new Color(0.97f, 0.95f, 0.9f), 0.1f);
            var pole = MaterialFactory.Lit(new Color(0.5f, 0.52f, 0.55f), 0.5f);
            LampMaterial = MaterialFactory.Unlit(new Color(0.85f, 0.85f, 0.8f));

            // Carriageways (thin boxes so they never z-fight with the ground)
            a.Box(asphalt, new Vector3(0, -0.05f, midZ), new Vector3(hw * 2f, 0.1f, L));
            a.Box(asphaltOld, new Vector3((hw + FootpathRight) * 0.5f, -0.05f, midZ), new Vector3(FootpathRight - hw, 0.1f, L));
            a.Box(asphalt, new Vector3((OppositeInner + OppositeOuter) * 0.5f, -0.05f, midZ), new Vector3(OppositeInner - OppositeOuter, 0.1f, L));
            a.Box(asphaltOld, new Vector3((MedianInner + hw * -1f) * 0.5f, -0.05f, midZ), new Vector3(0.35f, 0.1f, L));
            a.Box(asphaltOld, new Vector3((MedianOuter + OppositeInner) * 0.5f, -0.05f, midZ), new Vector3(0.35f, 0.1f, L));

            // Lane markings: dashed white between lanes, solid edge lines
            for (int line = 1; line < GameConstants.LaneCount; line++)
            {
                float x = -hw + line * GameConstants.LaneWidth;
                for (float z = 1f; z < L; z += 10f)
                    a.Box(white, new Vector3(x, 0.005f, z + 2.25f), new Vector3(0.2f, 0.02f, 5f));
            }
            a.Box(yellow, new Vector3(-hw + 0.15f, 0.005f, midZ), new Vector3(0.15f, 0.02f, L));
            a.Box(white, new Vector3(hw - 0.15f, 0.005f, midZ), new Vector3(0.15f, 0.02f, L));
            // opposite carriageway markings
            for (int line = 1; line < 3; line++)
            {
                float x = OppositeInner - line * GameConstants.LaneWidth;
                for (float z = 6f; z < L; z += 10f)
                    a.Box(white, new Vector3(x, 0.005f, z + 2.25f), new Vector3(0.2f, 0.02f, 5f));
            }

            // Median: kerbs painted black/yellow, soil, shrubs with oleander flowers
            float medC = (MedianInner + MedianOuter) * 0.5f;
            float medW = MedianInner - MedianOuter;
            a.Box(soil, new Vector3(medC, 0.12f, midZ), new Vector3(medW - 0.3f, 0.24f, L));
            for (float z = 0f; z < L; z += 1f)
            {
                var m = ((int)z % 2 == 0) ? yellow : black;
                a.Box(m, new Vector3(MedianInner - 0.12f, 0.15f, z + 0.5f), new Vector3(0.24f, 0.3f, 1f));
                a.Box(m, new Vector3(MedianOuter + 0.12f, 0.15f, z + 0.5f), new Vector3(0.24f, 0.3f, 1f));
            }
            for (float z = 2f; z < L; z += 4f)
            {
                if (Mathf.Abs(z - midZ) < 1.5f) continue; // leave room for the lamp post
                a.Sphere(shrub, new Vector3(medC, 0.75f, z), new Vector3(1.1f, 1.1f, 1.6f));
                var f = (((int)z / 4) % 2 == 0) ? flowerPink : flowerWhite;
                a.Sphere(f, new Vector3(medC + 0.25f, 1.15f, z + 0.3f), new Vector3(0.35f, 0.3f, 0.35f));
                a.Sphere(f, new Vector3(medC - 0.3f, 1.05f, z - 0.35f), new Vector3(0.3f, 0.28f, 0.3f));
            }

            // Streetlight on the median (double arm)
            a.Cylinder(pole, new Vector3(medC, 4.5f, midZ), 0.2f, 9f, Vector3.zero);
            a.Box(concrete, new Vector3(medC, 0.35f, midZ), new Vector3(0.5f, 0.5f, 0.5f));
            foreach (float sx in new[] { -1f, 1f })
            {
                a.Box(pole, new Vector3(medC + sx * 1.3f, 8.85f, midZ), new Vector3(2.6f, 0.1f, 0.1f), new Vector3(0, 0, sx * -8f));
                a.Box(pole, new Vector3(medC + sx * 2.55f, 8.95f, midZ), new Vector3(0.75f, 0.14f, 0.32f));
                a.Box(LampMaterial, new Vector3(medC + sx * 2.55f, 8.86f, midZ), new Vector3(0.6f, 0.05f, 0.24f));
            }

            // Right side: kerb, footpath, ground
            a.Box(concrete, new Vector3(FootpathRight + 0.1f, 0.12f, midZ), new Vector3(0.2f, 0.25f, L));
            a.Box(paver, new Vector3(FootpathRight + 1.45f, 0.1f, midZ), new Vector3(2.5f, 0.2f, L));
            for (float z = 0; z < L; z += 2f)
                a.Box(concrete, new Vector3(FootpathRight + 1.45f, 0.201f, z + 1f), new Vector3(2.5f, 0.005f, 0.06f));
            a.Box(ground, new Vector3(FootpathRight + 2.7f + 30f, -0.06f, midZ), new Vector3(60f, 0.1f, L));
            a.Box(grass, new Vector3(FootpathRight + 3.3f, -0.0f, midZ), new Vector3(1.2f, 0.06f, L));

            // Left side beyond the opposite carriageway
            a.Box(concrete, new Vector3(OppositeOuter - 0.1f, 0.12f, midZ), new Vector3(0.2f, 0.25f, L));
            a.Box(paver, new Vector3(OppositeOuter - 1.45f, 0.1f, midZ), new Vector3(2.5f, 0.2f, L));
            a.Box(ground, new Vector3(OppositeOuter - 2.7f - 30f, -0.06f, midZ), new Vector3(60f, 0.1f, L));
            a.Box(grass, new Vector3(OppositeOuter - 3.3f, 0f, midZ), new Vector3(1.2f, 0.06f, L));

            shared = a.BakeMesh("RoadSegment", out sharedMats);
        }
    }
}
