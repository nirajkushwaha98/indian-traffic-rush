using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Pooled spinning coins laid out in lines (straight or lane-switching) in lanes that are free of
    /// traffic when spawned. Pickup is a cheap distance check against the player.
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        public event Action<Vector3> Collected;

        GameConfig cfg;
        readonly List<Transform> active = new List<Transform>();
        readonly Stack<Transform> pool = new Stack<Transform>();
        Mesh coinMesh;
        Material[] coinMats;
        float nextLineZ;
        float spin;
        System.Random rng;
        ParticleSystem sparkle;

        public int ActiveCount => active.Count;

        public void Init(GameConfig config, int seed, float playerZ)
        {
            cfg = config;
            rng = new System.Random(seed);
            var a = new MeshAssembler();
            var gold = MaterialFactory.Lit(new Color(1f, 0.76f, 0.12f), 0.85f);
            var goldDark = MaterialFactory.Lit(new Color(0.9f, 0.55f, 0.05f), 0.7f);
            a.Cylinder(gold, Vector3.zero, 0.95f, 0.12f, new Vector3(90, 0, 0));
            a.Cylinder(goldDark, Vector3.zero, 0.62f, 0.15f, new Vector3(90, 0, 0));
            a.Box(gold, Vector3.zero, new Vector3(0.12f, 0.36f, 0.17f));
            coinMesh = a.BakeMesh("Coin", out coinMats);
            for (int i = 0; i < 40; i++) pool.Push(Create());
            nextLineZ = playerZ + 70f;
            sparkle = ParticleFactory.Sparkle(transform);
            FloatingOrigin.Shifted += OnShift;
        }

        void OnDestroy() => FloatingOrigin.Shifted -= OnShift;
        void OnShift(float dz) => nextLineZ -= dz;

        Transform Create()
        {
            var go = new GameObject("Coin");
            go.transform.SetParent(transform, false);
            MeshAssembler.Apply(go, coinMesh, coinMats, false);
            go.SetActive(false);
            return go.transform;
        }

        Transform Get() => pool.Count > 0 ? pool.Pop() : Create();

        void Release(int index)
        {
            var t = active[index];
            t.gameObject.SetActive(false);
            pool.Push(t);
            active.RemoveAt(index);
        }

        public void SpawnLine(int lane, float z, int count, int switchToLane = -1)
        {
            for (int i = 0; i < count; i++)
            {
                float x;
                int half = count / 2;
                if (switchToLane >= 0 && i >= half)
                {
                    float t = Mathf.Clamp01((i - half + 1) / 2f);
                    x = Mathf.Lerp(GameConstants.LaneX(lane), GameConstants.LaneX(switchToLane), t);
                }
                else x = GameConstants.LaneX(lane);
                var c = Get();
                c.position = new Vector3(x, 1.0f, z + i * cfg.coinSpacing);
                c.gameObject.SetActive(true);
                active.Add(c);
            }
        }

        public void Clear()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
        }

        public void Tick(float dt, PlayerCar player, TrafficManager traffic)
        {
            var pp = player.transform.position;

            // Spawn new lines ahead in a lane that is clear right now.
            if (nextLineZ < pp.z + 100f) nextLineZ = pp.z + 100f;
            if (nextLineZ < pp.z + 200f)
            {
                int count = cfg.coinsPerLine;
                float len = count * cfg.coinSpacing;
                int start = rng.Next(3);
                for (int k = 0; k < 3; k++)
                {
                    int lane = (start + k) % 3;
                    if (traffic == null || traffic.LaneClear(lane, nextLineZ + len * 0.5f, len, 12f))
                    {
                        int sw = -1;
                        if (rng.NextDouble() < 0.35)
                        {
                            int dir = lane == 0 ? 1 : (lane == 2 ? -1 : (rng.Next(2) == 0 ? -1 : 1));
                            if (traffic == null || traffic.LaneClear(lane + dir, nextLineZ + len * 0.5f, len, 12f)) sw = lane + dir;
                        }
                        SpawnLine(lane, nextLineZ, count, sw);
                        break;
                    }
                }
                nextLineZ += Mathf.Lerp(cfg.coinLineInterval.x, cfg.coinLineInterval.y, (float)rng.NextDouble());
            }

            spin = (spin + dt * 220f) % 360f;
            var rot = Quaternion.Euler(0f, spin, 0f);
            float r = cfg.coinPickupRadius;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var c = active[i];
                var p = c.position;
                if (!player.Crashed && Mathf.Abs(p.x - pp.x) < r * 0.85f && Mathf.Abs(p.z - pp.z) < r + player.Length * 0.4f)
                {
                    sparkle.transform.position = p;
                    sparkle.Emit(12);
                    Collected?.Invoke(p);
                    Release(i);
                    continue;
                }
                if (p.z < pp.z - 15f) { Release(i); continue; }
                c.rotation = rot;
            }
        }
    }
}
