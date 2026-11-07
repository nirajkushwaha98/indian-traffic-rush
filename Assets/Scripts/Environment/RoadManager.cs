using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Endless road: a ring of 40 m segments. When a segment falls behind the player it jumps to the front
    /// and gets fresh, pooled roadside scenery, so the road never ends and nothing is allocated per frame.
    /// </summary>
    public class RoadManager : MonoBehaviour
    {
        class Segment
        {
            public Transform root;
            public readonly List<SceneryProp> props = new List<SceneryProp>();
        }

        readonly List<Segment> segments = new List<Segment>();
        readonly Dictionary<PropKind, List<SceneryProp>> pool = new Dictionary<PropKind, List<SceneryProp>>();
        System.Random rng;
        Transform propRoot;
        public float keepBehind = 45f;
        public bool sceneryEnabled = true;

        public int SegmentCount => segments.Count;
        public float FrontZ => segments.Count == 0 ? 0f : segments[segments.Count - 1].root.position.z + GameConstants.SegmentLength;

        public void Init(int segmentCount, int seed, float startZ)
        {
            rng = new System.Random(seed);
            propRoot = new GameObject("PropPool").transform;
            propRoot.SetParent(transform, false);
            for (int i = 0; i < segmentCount; i++)
            {
                var seg = new Segment { root = new GameObject("Segment" + i).transform };
                seg.root.SetParent(transform, false);
                seg.root.position = new Vector3(0, 0, startZ + i * GameConstants.SegmentLength);
                RoadBuilder.CreateSegmentGeometry(seg.root);
                segments.Add(seg);
                Populate(seg);
            }
        }

        public void Tick(float playerZ)
        {
            if (segments.Count == 0) return;
            // Recycle at most a couple per frame to avoid spikes.
            for (int n = 0; n < 2; n++)
            {
                var first = segments[0];
                if (first.root.position.z + GameConstants.SegmentLength >= playerZ - keepBehind) break;
                float newZ = FrontZ;
                segments.RemoveAt(0);
                first.root.position = new Vector3(0, 0, newZ);
                segments.Add(first);
                Populate(first);
            }
        }

        void Populate(Segment seg)
        {
            foreach (var p in seg.props) Release(p);
            seg.props.Clear();
            if (!sceneryEnabled) return;

            FillSide(seg, true);
            FillSide(seg, false);

            // Occasional road sign on the near footpath, facing the driver.
            if (rng.NextDouble() < 0.35)
            {
                var kind = Pick(new[] { PropKind.DirectionSign, PropKind.SpeedSign, PropKind.WarningSign, PropKind.WarningSign });
                var p = Acquire(kind);
                Place(seg, p, new Vector3(RoadBuilder.FootpathRight + 1.6f, 0.2f, (float)rng.NextDouble() * 30f + 5f), 180f);
            }
        }

        void FillSide(Segment seg, bool right)
        {
            float z = (float)rng.NextDouble() * 2f;
            int guard = 0;
            while (z < GameConstants.SegmentLength - 2f && guard++ < 12)
            {
                double roll = rng.NextDouble();
                PropKind kind = roll < 0.34 ? PropKind.Building
                              : roll < 0.68 ? PropKind.Shop
                              : roll < 0.84 ? PropKind.Tree
                              : roll < 0.9 ? PropKind.PalmTree
                              : PropKind.Billboard;
                var p = Acquire(kind);
                float w = p.width;
                if (z + w > GameConstants.SegmentLength + 4f) { Release(p); break; }
                bool isTree = kind == PropKind.Tree || kind == PropKind.PalmTree;
                float baseX = right ? RoadBuilder.SceneryRight : RoadBuilder.SceneryLeft;
                float x = baseX + (right ? 1f : -1f) * (isTree ? -1.2f : (float)rng.NextDouble() * 3f);
                float yaw = right ? -90f : 90f;
                if (isTree) yaw = (float)rng.NextDouble() * 360f;
                Place(seg, p, new Vector3(x, 0f, z + w * 0.5f), yaw);
                z += w + 0.5f + (float)rng.NextDouble() * 3f;

                // Street trees between buildings and footpath.
                if (!isTree && rng.NextDouble() < 0.35)
                {
                    var t = Acquire(rng.NextDouble() < 0.8 ? PropKind.Tree : PropKind.PalmTree);
                    Place(seg, t, new Vector3(baseX + (right ? -2.4f : 2.4f), 0f, Mathf.Min(z, 39f)), (float)rng.NextDouble() * 360f);
                }
            }
        }

        void Place(Segment seg, SceneryProp p, Vector3 localPos, float yaw)
        {
            p.transform.SetParent(seg.root, false);
            p.transform.localPosition = localPos;
            p.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            p.gameObject.SetActive(true);
            seg.props.Add(p);
        }

        T Pick<T>(T[] arr) => arr[rng.Next(arr.Length)];

        SceneryProp Acquire(PropKind kind)
        {
            if (!pool.TryGetValue(kind, out var list)) pool[kind] = list = new List<SceneryProp>();
            // Grow the pool a bit for variety; afterwards reuse a random free instance.
            if (list.Count > 0 && (list.Count > 3 || rng.NextDouble() < 0.5))
            {
                int i = rng.Next(list.Count);
                var p = list[i];
                list[i] = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return p;
            }
            var go = SceneryFactory.Create(kind, rng);
            return go.GetComponent<SceneryProp>();
        }

        void Release(SceneryProp p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            p.transform.SetParent(propRoot, false);
            if (!pool.TryGetValue(p.kind, out var list)) pool[p.kind] = list = new List<SceneryProp>();
            list.Add(p);
        }
    }
}
