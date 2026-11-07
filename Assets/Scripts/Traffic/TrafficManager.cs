using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Spawns, pools and drives all AI traffic.
    /// Safety rules (checked at spawn time AND every frame):
    ///  • vehicles in the same lane keep a following gap (no overlaps, no rear-ending, also behind the player);
    ///  • all three lanes are never blocked inside one <see cref="GameConfig.wallWindow"/> — the rearmost
    ///    vehicle of any would-be "wall" holds back, so there is always an escape route;
    ///  • nothing spawns near or beside the player; lane changes only happen far ahead of the player.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
        public event Action<TrafficVehicle, bool, float> Overtaken; // vehicle, nearMiss, clearance

        GameConfig cfg;
        List<TrafficVehicleData> types;
        readonly Dictionary<TrafficVehicleData, List<TrafficVehicle>> pool = new Dictionary<TrafficVehicleData, List<TrafficVehicle>>();
        readonly Dictionary<TrafficVehicleData, int> built = new Dictionary<TrafficVehicleData, int>();
        readonly List<TrafficVehicle> active = new List<TrafficVehicle>();
        readonly List<TrafficVehicle> scratch = new List<TrafficVehicle>();
        readonly List<Transform> oncoming = new List<Transform>();
        readonly List<float> oncomingSpeed = new List<float>();
        System.Random rng;
        float nextRowZ;
        float time;
        public bool spawningEnabled = true;

        public IReadOnlyList<TrafficVehicle> Active => active;

        public void Init(GameConfig config, TrafficCatalog catalog, int seed, float playerZ)
        {
            cfg = config;
            types = catalog.types;
            rng = new System.Random(seed);
            foreach (var t in types)
            {
                pool[t] = new List<TrafficVehicle>();
                built[t] = 0;
                for (int i = 0; i < t.poolSize; i++) pool[t].Add(CreateVehicle(t));
            }
            nextRowZ = playerZ + 90f;
            BuildOncoming(playerZ);
            FloatingOrigin.Shifted += OnShift;
        }

        /// <summary>
        /// Fills the road ahead before the race is shown, so 4–6+ vehicles are already visible at the start
        /// instead of popping in during the countdown.
        /// </summary>
        public void Prewarm(PlayerCar player)
        {
            float pz = player.transform.position.z;
            nextRowZ = pz + 45f;
            int maxActive = cfg.minActiveTraffic;
            prewarming = true;
            int guard = 0;
            while (nextRowZ < pz + cfg.spawnAhead && active.Count < maxActive && guard++ < 40)
            {
                SpawnRow(nextRowZ, 0f, player);
                nextRowZ += Mathf.Lerp(cfg.rowGapEasy.x, cfg.rowGapEasy.y, (float)rng.NextDouble());
            }
            prewarming = false;
        }

        bool prewarming;

        /// <summary>Raised for every vehicle placed by the spawner (distance ahead of the player, prewarm flag).</summary>
        public event Action<TrafficVehicle, float, bool> Spawned;

        /// <summary>Number of vehicles between <paramref name="near"/> and <paramref name="far"/> metres ahead of z.</summary>
        public int CountAhead(float z, float near, float far)
        {
            int n = 0;
            foreach (var v in active) if (v.Z > z + near && v.Z < z + far) n++;
            return n;
        }

        void OnDestroy() => FloatingOrigin.Shifted -= OnShift;

        void OnShift(float dz) => nextRowZ -= dz;

        TrafficVehicle CreateVehicle(TrafficVehicleData t)
        {
            int idx = built[t]++;
            var color = t.colors[idx % t.colors.Length];
            var vis = VehicleBuilder.Build(t.shape, color, false, idx % 3).GetComponent<VehicleVisual>();
            var go = new GameObject("Traffic_" + t.id + "_" + idx);
            go.transform.SetParent(transform, false);
            var tv = go.AddComponent<TrafficVehicle>();
            tv.Init(t, vis);
            go.SetActive(false);
            return tv;
        }

        TrafficVehicle Acquire(TrafficVehicleData t)
        {
            var list = pool[t];
            if (list.Count == 0)
            {
                if (built[t] >= t.poolSize * 3) return null;
                return CreateVehicle(t);
            }
            int i = rng.Next(list.Count);
            var v = list[i];
            list[i] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return v;
        }

        void Release(TrafficVehicle v)
        {
            v.Deactivate();
            pool[v.Data].Add(v);
        }

        TrafficVehicleData PickType(float difficulty)
        {
            float total = 0f;
            foreach (var t in types) total += Mathf.Max(0f, Mathf.Lerp(t.spawnWeight.x, t.spawnWeight.y, difficulty));
            float r = (float)rng.NextDouble() * total;
            foreach (var t in types)
            {
                r -= Mathf.Max(0f, Mathf.Lerp(t.spawnWeight.x, t.spawnWeight.y, difficulty));
                if (r <= 0f) return t;
            }
            return types[types.Count - 1];
        }

        // ------------------------------------------------------------------ safety queries

        /// <summary>True if a vehicle of <paramref name="len"/> can sit in <paramref name="lane"/> at <paramref name="z"/> without overlapping anyone.</summary>
        public bool LaneClear(int lane, float z, float len, float gap, TrafficVehicle exclude = null)
        {
            foreach (var v in active)
            {
                if (v == exclude || !v.Occupies(lane)) continue;
                if (Mathf.Abs(v.Z - z) < (v.Length + len) * 0.5f + gap) return false;
            }
            return true;
        }

        /// <summary>True if placing a vehicle here would block all three lanes inside the wall window.</summary>
        public bool WouldWall(int lane, float z, float len, TrafficVehicle exclude = null)
        {
            float rear = z - len * 0.5f, front = z + len * 0.5f;
            int a = -1, b = -1;
            for (int l = 0; l < GameConstants.LaneCount; l++)
            {
                if (l == lane) continue;
                if (a < 0) a = l; else b = l;
            }
            float limit = cfg.wallWindow + 4f;
            foreach (var p in active)
            {
                if (p == exclude || !p.Occupies(a)) continue;
                if (Mathf.Abs(p.Z - z) > 60f) continue;
                foreach (var q in active)
                {
                    if (q == exclude || q == p || !q.Occupies(b)) continue;
                    float maxRear = Mathf.Max(rear, Mathf.Max(p.Rear, q.Rear));
                    float minFront = Mathf.Min(front, Mathf.Min(p.Front, q.Front));
                    if (maxRear - minFront <= limit) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds the minimum distance between the rears and fronts of any three vehicles in distinct lanes
        /// (debug/test helper). A value above wallWindow means the road is always passable.
        /// </summary>
        public float TightestWall(float fromZ, float toZ)
        {
            float best = float.MaxValue;
            foreach (var p in active)
            {
                if (!p.Occupies(0) || p.Z < fromZ || p.Z > toZ) continue;
                foreach (var q in active)
                {
                    if (!q.Occupies(1) || q == p) continue;
                    foreach (var r in active)
                    {
                        if (!r.Occupies(2) || r == p || r == q) continue;
                        float maxRear = Mathf.Max(p.Rear, Mathf.Max(q.Rear, r.Rear));
                        float minFront = Mathf.Min(p.Front, Mathf.Min(q.Front, r.Front));
                        best = Mathf.Min(best, maxRear - minFront);
                    }
                }
            }
            return best;
        }

        // ------------------------------------------------------------------ spawning

        void SpawnRow(float z, float difficulty, PlayerCar player)
        {
            float doubleChance = Mathf.Lerp(cfg.doubleRowChanceEasy, cfg.doubleRowChanceHard, difficulty);
            int count = rng.NextDouble() < doubleChance ? 2 : 1;
            int[] lanes = { 0, 1, 2 };
            for (int i = lanes.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = lanes[i]; lanes[i] = lanes[j]; lanes[j] = t; }
            int spawned = 0;
            foreach (int lane in lanes)
            {
                if (spawned >= count) break;
                var type = PickType(difficulty);
                var v = Acquire(type);
                if (v == null) continue;
                float zz = z + (float)rng.NextDouble() * 6f - 3f;
                if (!CanSpawn(lane, zz, v.Length, player)) { pool[type].Add(v); continue; }
                float kmh = Mathf.Lerp(type.speedKmh.x, type.speedKmh.y, (float)rng.NextDouble()) * (1f + difficulty * 0.12f);
                v.Activate(lane, zz, kmh * GameConstants.KmhToMs);
                v.Visual.SetHeadlights(EnvironmentTheme.HeadlightsOn);
                active.Add(v);
                spawned++;
                Spawned?.Invoke(v, player != null ? zz - player.transform.position.z : 0f, prewarming);
            }
        }

        bool CanSpawn(int lane, float z, float len, PlayerCar player)
        {
            if (player != null)
            {
                float pz = player.transform.position.z;
                // Prewarm may fill from 80 m (before the player sees anything); live spawns respect minSpawnDistance.
                if (z - pz < (prewarming ? 40f : cfg.minSpawnDistance - 5f)) return false;
            }
            return LaneClear(lane, z, len, cfg.followGap * 2f) && !WouldWall(lane, z, len);
        }

        /// <summary>Test/debug helper: place a vehicle of the given type exactly.</summary>
        public TrafficVehicle SpawnAt(int typeIndex, int lane, float z, float kmh)
        {
            var t = types[Mathf.Clamp(typeIndex, 0, types.Count - 1)];
            var v = Acquire(t);
            if (v == null) return null;
            v.Activate(lane, z, kmh * GameConstants.KmhToMs);
            active.Add(v);
            Physics.SyncTransforms();
            return v;
        }

        public void ClearAll()
        {
            foreach (var v in active) Release(v);
            active.Clear();
        }

        /// <summary>Removes traffic around a revive point so the player gets a fair restart.</summary>
        public void ClearAround(float z, float ahead)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var v = active[i];
                if (v.Z > z - 40f && v.Z < z + ahead)
                {
                    Release(v);
                    active.RemoveAt(i);
                }
            }
            nextRowZ = z + ahead;
        }

        // ------------------------------------------------------------------ per frame

        public void Tick(float dt, PlayerCar player, float difficulty)
        {
            time += dt;
            float pz = player.transform.position.z;
            float px = player.transform.position.x;

            if (spawningEnabled)
            {
                int maxActive = Mathf.RoundToInt(Mathf.Lerp(cfg.minActiveTraffic, cfg.maxActiveTraffic, difficulty));
                // Never spawn inside the clearly visible zone: new traffic always appears far ahead and
                // fades in through the distance haze, so the player always has reaction distance.
                float minSpawn = pz + cfg.minSpawnDistance;
                if (nextRowZ < minSpawn) nextRowZ = minSpawn;
                int guard = 0;
                while (nextRowZ < pz + cfg.spawnAhead && active.Count < maxActive && guard++ < 3)
                {
                    SpawnRow(nextRowZ, difficulty, player);
                    var gap = Vector2.Lerp(cfg.rowGapEasy, cfg.rowGapHard, difficulty);
                    nextRowZ += Mathf.Lerp(gap.x, gap.y, (float)rng.NextDouble());
                }
            }

            active.Sort((a, b) => a.Z.CompareTo(b.Z));
            int playerLane = GameConstants.NearestLane(px);

            for (int i = 0; i < active.Count; i++)
            {
                var v = active[i];
                if (v.wrecked)
                {
                    v.speed = Mathf.Max(0f, v.speed - 12f * dt);
                    v.Move(dt, time);
                    continue;
                }

                float desired = v.cruiseSpeed;

                // Same-lane leader (vehicles sorted by Z, so the first match ahead is the nearest).
                float leadRear = float.MaxValue, leadSpeed = 0f;
                for (int j = i + 1; j < active.Count; j++)
                {
                    var w = active[j];
                    if (w.Occupies(v.lane) || (v.changingLane && w.Occupies(v.fromLane)))
                    {
                        leadRear = w.Rear; leadSpeed = w.speed; break;
                    }
                }
                // The player is a leader too, so traffic never rear-ends a slow player.
                bool playerAhead = pz > v.Z && (Mathf.Abs(px - v.X) < (player.Width + v.Width) * 0.5f + 0.4f);
                if (playerAhead)
                {
                    float pr = pz - player.Length * 0.5f;
                    if (pr < leadRear) { leadRear = pr; leadSpeed = player.Crashed ? 0f : player.Speed; }
                }
                float gap = leadRear - v.Front;
                if (gap < cfg.followGap * 4f)
                    desired = Mathf.Min(desired, leadSpeed + (gap - cfg.followGap) * 0.9f);

                // Wall rule: hold back if the two other lanes are blocked close together just ahead.
                if (WallLead(i, out float wallRear, out float wallSpeed))
                {
                    float wgap = wallRear - (cfg.wallWindow + 6f) - v.Front;
                    if (wgap < 30f) desired = Mathf.Min(desired, wallSpeed + wgap * 0.7f);
                }

                desired = Mathf.Max(0f, desired);
                if (v.speed < desired) v.speed = Mathf.Min(desired, v.speed + 3.5f * dt);
                else v.speed = Mathf.Max(desired, v.speed - 10f * dt);
                if (gap < cfg.followGap * 0.4f) v.speed = Mathf.Min(v.speed, leadSpeed);

                // Occasional weaving (autos, bikes, taxis) — only well ahead of the player.
                if (v.Data.laneChangeChance > 0f && !v.changingLane)
                {
                    v.nextLaneDecision -= dt;
                    if (v.nextLaneDecision <= 0f)
                    {
                        v.nextLaneDecision = 3f + (float)rng.NextDouble() * 4f;
                        float closing = Mathf.Max(0f, player.Speed - v.speed);
                        bool farAhead = v.Z - pz > Mathf.Max(cfg.laneChangeSafeDistance, closing * 2.5f + 15f);
                        if (farAhead && rng.NextDouble() < v.Data.laneChangeChance * 3f)
                        {
                            int dir = rng.Next(2) == 0 ? -1 : 1;
                            int target = v.lane + dir;
                            if (target < 0 || target >= GameConstants.LaneCount) target = v.lane - dir;
                            if (target >= 0 && target < GameConstants.LaneCount &&
                                LaneClear(target, v.Z, v.Length, cfg.followGap * 2f, v) &&
                                !WouldWall(target, v.Z, v.Length, v))
                            {
                                v.StartLaneChange(target);
                            }
                        }
                    }
                }

                v.Move(dt, time);
            }

            // Overtakes, near misses, despawning
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var v = active[i];
                if (!v.passedByPlayer && !player.Crashed && pz > v.Front)
                {
                    v.passedByPlayer = true;
                    if (!v.hitByPlayer)
                    {
                        float clearance = Mathf.Abs(px - v.X) - (player.Width + v.Width) * 0.5f;
                        bool near = clearance < cfg.nearMissClearance && clearance > -0.2f && player.SpeedKmh >= cfg.nearMissMinSpeedKmh;
                        Overtaken?.Invoke(v, near, clearance);
                    }
                }
                else if (v.passedByPlayer && pz < v.Rear - 3f)
                {
                    v.passedByPlayer = false; // it re-passed the player; can be overtaken again
                }

                if (v.Front < pz - cfg.despawnBehind || v.Z > pz + cfg.spawnAhead + 140f)
                {
                    Release(v);
                    active.RemoveAt(i);
                }
            }

            TickOncoming(dt, pz);
        }

        bool WallLead(int index, out float rear, out float speed)
        {
            rear = 0f; speed = 0f;
            var v = active[index];
            TrafficVehicle p = null, q = null;
            int a = -1, b = -1;
            for (int l = 0; l < GameConstants.LaneCount; l++)
            {
                if (l == v.lane) continue;
                if (a < 0) a = l; else b = l;
            }
            for (int j = index + 1; j < active.Count && (p == null || q == null); j++)
            {
                var w = active[j];
                if (w.Z - v.Z > 90f) break;
                if (p == null && w.Occupies(a)) p = w;
                else if (q == null && w.Occupies(b)) q = w;
            }
            if (p == null || q == null) return false;
            float spread = Mathf.Max(p.Rear, q.Rear) - Mathf.Min(p.Front, q.Front);
            if (spread > cfg.wallWindow + 4f) return false;
            var far = p.Rear > q.Rear ? p : q;
            rear = far.Rear;
            speed = Mathf.Min(p.speed, q.speed);
            return true;
        }

        // ------------------------------------------------------------------ decorative oncoming traffic

        void BuildOncoming(float pz)
        {
            var root = new GameObject("Oncoming").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < 7; i++)
            {
                var t = types[rng.Next(types.Count)];
                var vis = VehicleBuilder.Build(t.shape, t.colors[rng.Next(t.colors.Length)], false, i % 3);
                vis.transform.SetParent(root, false);
                foreach (var c in vis.GetComponentsInChildren<Collider>()) Destroy(c);
                oncoming.Add(vis.transform);
                oncomingSpeed.Add(Mathf.Lerp(t.speedKmh.x, t.speedKmh.y, (float)rng.NextDouble()) * GameConstants.KmhToMs);
                PlaceOncoming(i, pz + 40f + i * 45f);
            }
        }

        void PlaceOncoming(int i, float z)
        {
            int lane = rng.Next(3);
            float x = RoadBuilder.OppositeInner - GameConstants.LaneWidth * (lane + 0.5f);
            oncoming[i].SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0, 180, 0));
            var vis = oncoming[i].GetComponent<VehicleVisual>();
            if (vis != null) vis.SetHeadlights(EnvironmentTheme.HeadlightsOn);
        }

        void TickOncoming(float dt, float pz)
        {
            for (int i = 0; i < oncoming.Count; i++)
            {
                var t = oncoming[i];
                var p = t.position;
                p.z -= oncomingSpeed[i] * dt;
                t.position = p;
                if (p.z < pz - 30f) PlaceOncoming(i, pz + cfg.spawnAhead + 40f + (float)rng.NextDouble() * 80f);
            }
        }
    }
}
