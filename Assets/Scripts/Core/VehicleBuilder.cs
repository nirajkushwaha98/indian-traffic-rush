using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Builds every vehicle in the game procedurally from primitives (ORIGINAL PLACEHOLDER MODELS).
    /// Each vehicle body becomes one combined mesh; player cars get separate spinning wheels and light renderers.
    /// Local space: +Z forward, origin on the ground at the vehicle centre.
    /// </summary>
    public static class VehicleBuilder
    {
        struct CarSpec
        {
            public float L, W, bodyH, clearance, cabinL, cabinZ, cabinH, wheelD;
        }

        static readonly Dictionary<string, (Mesh mesh, Material[] mats, Vector3 size)> cache =
            new Dictionary<string, (Mesh, Material[], Vector3)>();

        static Material Glass => MaterialFactory.Lit(new Color(0.08f, 0.12f, 0.18f), 0.9f);
        static Material Tyre => MaterialFactory.Lit(new Color(0.06f, 0.06f, 0.06f), 0.05f);
        static Material Hub => MaterialFactory.Lit(new Color(0.65f, 0.67f, 0.7f), 0.6f);
        static Material Trim => MaterialFactory.Lit(new Color(0.12f, 0.12f, 0.13f), 0.3f);
        static Material Chrome => MaterialFactory.Lit(new Color(0.8f, 0.8f, 0.82f), 0.8f);
        static Material HeadLamp => MaterialFactory.Unlit(new Color(1f, 0.97f, 0.85f));
        static Material TailLamp => MaterialFactory.Unlit(new Color(0.5f, 0.02f, 0.02f));
        static Material BrakeLamp => MaterialFactory.Unlit(new Color(1f, 0.08f, 0.05f));
        static Material Amber => MaterialFactory.Unlit(new Color(1f, 0.6f, 0.05f));
        static Material PlateWhite => MaterialFactory.Lit(new Color(0.95f, 0.95f, 0.92f), 0.2f);
        static Material PlateYellow => MaterialFactory.Lit(new Color(0.98f, 0.82f, 0.1f), 0.2f);

        static CarSpec Spec(VehicleShape s)
        {
            switch (s)
            {
                case VehicleShape.Sedan: return new CarSpec { L = 4.5f, W = 1.78f, bodyH = 0.58f, clearance = 0.24f, cabinL = 2.2f, cabinZ = -0.2f, cabinH = 0.56f, wheelD = 0.64f };
                case VehicleShape.Taxi: return new CarSpec { L = 3.95f, W = 1.62f, bodyH = 0.62f, clearance = 0.26f, cabinL = 1.95f, cabinZ = -0.25f, cabinH = 0.64f, wheelD = 0.6f };
                case VehicleShape.SUV: return new CarSpec { L = 4.6f, W = 1.9f, bodyH = 0.8f, clearance = 0.36f, cabinL = 2.75f, cabinZ = -0.35f, cabinH = 0.72f, wheelD = 0.8f };
                case VehicleShape.Police: return new CarSpec { L = 4.55f, W = 1.8f, bodyH = 0.6f, clearance = 0.24f, cabinL = 2.2f, cabinZ = -0.2f, cabinH = 0.56f, wheelD = 0.66f };
                case VehicleShape.Sports: return new CarSpec { L = 4.4f, W = 1.95f, bodyH = 0.46f, clearance = 0.16f, cabinL = 1.75f, cabinZ = -0.45f, cabinH = 0.44f, wheelD = 0.68f };
                default: return new CarSpec { L = 3.8f, W = 1.7f, bodyH = 0.6f, clearance = 0.25f, cabinL = 2.0f, cabinZ = -0.35f, cabinH = 0.62f, wheelD = 0.62f };
            }
        }

        public static bool IsCar(VehicleShape s) =>
            s == VehicleShape.Hatchback || s == VehicleShape.Sedan || s == VehicleShape.Taxi ||
            s == VehicleShape.SUV || s == VehicleShape.Police || s == VehicleShape.Sports;

        /// <summary>
        /// Builds a vehicle. <paramref name="detailed"/> = player/showroom car with spinning wheels and light renderers.
        /// Traffic (detailed = false) shares cached meshes per shape+colour.
        /// </summary>
        public static GameObject Build(VehicleShape shape, Color paint, bool detailed, int variantSeed = 0)
        {
            var root = new GameObject(shape.ToString());
            var vis = root.AddComponent<VehicleVisual>();
            vis.shape = shape;
            var bodyPivot = new GameObject("Body").transform;
            bodyPivot.SetParent(root.transform, false);
            vis.body = bodyPivot;

            string key = shape + "_" + ColorUtility.ToHtmlStringRGB(paint) + "_" + variantSeed;
            Vector3 size;
            if (!detailed && cache.TryGetValue(key, out var hit) && hit.mesh != null)
            {
                var go = new GameObject("Mesh");
                go.transform.SetParent(bodyPivot, false);
                MeshAssembler.Apply(go, hit.mesh, hit.mats, true);
                size = hit.size;
                AddExtras(shape, go.transform, variantSeed);
            }
            else
            {
                var asm = new MeshAssembler();
                var wheelPositions = new List<Vector3>();
                float wheelD;
                size = Assemble(shape, paint, asm, wheelPositions, out wheelD, variantSeed);

                if (!detailed)
                {
                    foreach (var p in wheelPositions) asm.Wheel(Tyre, Hub, p, wheelD, WheelWidth(shape));
                }

                var mesh = asm.BakeMesh(key, out var mats);
                var go = new GameObject("Mesh");
                go.transform.SetParent(bodyPivot, false);
                MeshAssembler.Apply(go, mesh, mats, true);
                AddExtras(shape, go.transform, variantSeed);

                if (detailed)
                {
                    vis.wheelRadius = wheelD * 0.5f;
                    var wheels = new List<Transform>();
                    Mesh wheelMesh = null; Material[] wheelMats = null;
                    foreach (var p in wheelPositions)
                    {
                        if (wheelMesh == null)
                        {
                            var wa = new MeshAssembler();
                            wa.Wheel(Tyre, Hub, Vector3.zero, wheelD, WheelWidth(shape));
                            // Spokes so the rotation is visible.
                            wa.Box(Trim, Vector3.zero, new Vector3(WheelWidth(shape) * 1.1f, wheelD * 0.5f, 0.06f));
                            wa.Box(Trim, Vector3.zero, new Vector3(WheelWidth(shape) * 1.1f, 0.06f, wheelD * 0.5f));
                            wheelMesh = wa.BakeMesh("Wheel", out wheelMats);
                        }
                        var w = new GameObject("Wheel");
                        w.transform.SetParent(root.transform, false);
                        w.transform.localPosition = p;
                        MeshAssembler.Apply(w, wheelMesh, wheelMats, true);
                        wheels.Add(w.transform);
                    }
                    vis.wheels = wheels.ToArray();
                    AddLightRenderers(shape, vis, size);
                }
                else
                {
                    cache[key] = (mesh, mats, size);
                }
            }

            vis.length = size.z; vis.width = size.x; vis.height = size.y;
            if (!detailed && (shape == VehicleShape.AutoRickshaw || shape == VehicleShape.Motorcycle || IsCar(shape)))
                AddIndicators(vis, size);

            var exhaust = new GameObject("Exhaust").transform;
            exhaust.SetParent(root.transform, false);
            exhaust.localPosition = new Vector3(size.x * 0.25f, 0.3f, -size.z * 0.5f - 0.1f);
            vis.exhaust = exhaust;
            return root;
        }

        static float WheelWidth(VehicleShape s) =>
            s == VehicleShape.Motorcycle ? 0.12f : (s == VehicleShape.Bus || s == VehicleShape.Truck ? 0.35f : (s == VehicleShape.AutoRickshaw ? 0.14f : 0.24f));

        static Vector3 Assemble(VehicleShape shape, Color paint, MeshAssembler a, List<Vector3> wheels, out float wheelD, int seed)
        {
            switch (shape)
            {
                case VehicleShape.Bus: return BuildBus(paint, a, wheels, out wheelD);
                case VehicleShape.Truck: return BuildTruck(paint, a, wheels, out wheelD, seed);
                case VehicleShape.Motorcycle: return BuildBike(paint, a, wheels, out wheelD, seed);
                case VehicleShape.AutoRickshaw: return BuildAuto(paint, a, wheels, out wheelD, seed);
                default: return BuildCar(shape, paint, a, wheels, out wheelD);
            }
        }

        // ------------------------------------------------------------------ cars
        static Vector3 BuildCar(VehicleShape shape, Color paint, MeshAssembler a, List<Vector3> wheels, out float wheelD)
        {
            var s = Spec(shape);
            wheelD = s.wheelD;
            float smooth = shape == VehicleShape.Sports ? 0.75f : 0.55f;
            var body = MaterialFactory.Lit(paint, smooth);
            var roofMat = body;
            if (shape == VehicleShape.Taxi) roofMat = MaterialFactory.Lit(new Color(1f, 0.82f, 0.05f), 0.5f);

            float yBody = s.clearance + s.bodyH * 0.5f;
            float topBody = s.clearance + s.bodyH;
            float halfL = s.L * 0.5f;

            // Lower body + slightly narrower hood/boot cap for a chamfered look
            a.Box(body, new Vector3(0, yBody, 0), new Vector3(s.W, s.bodyH, s.L));
            a.Box(body, new Vector3(0, topBody + 0.03f, 0.02f), new Vector3(s.W * 0.94f, 0.06f, s.L * 0.96f));

            // Cabin: glass greenhouse + pillars + roof
            float cabY = topBody + s.cabinH * 0.5f;
            a.Box(Glass, new Vector3(0, cabY, s.cabinZ), new Vector3(s.W * 0.8f, s.cabinH, s.cabinL));
            float px = s.W * 0.4f;
            float pz = s.cabinL * 0.5f;
            foreach (float sx in new[] { -1f, 1f })
            {
                a.Box(roofMat, new Vector3(sx * px, cabY, s.cabinZ + pz - 0.05f), new Vector3(0.07f, s.cabinH, 0.1f));
                a.Box(roofMat, new Vector3(sx * px, cabY, s.cabinZ - pz + 0.05f), new Vector3(0.07f, s.cabinH, 0.14f));
                a.Box(roofMat, new Vector3(sx * px, cabY, s.cabinZ), new Vector3(0.07f, s.cabinH, 0.1f));
                // mirrors
                a.Box(body, new Vector3(sx * (s.W * 0.5f + 0.09f), topBody + 0.12f, s.cabinZ + pz + 0.05f), new Vector3(0.14f, 0.1f, 0.1f));
                // door handle line
                a.Box(Trim, new Vector3(sx * (s.W * 0.5f + 0.005f), yBody + 0.08f, s.cabinZ), new Vector3(0.02f, 0.03f, s.cabinL * 0.9f));
            }
            a.Box(roofMat, new Vector3(0, topBody + s.cabinH, s.cabinZ), new Vector3(s.W * 0.82f, 0.08f, s.cabinL * 0.92f));

            // Bumpers, grille, lamps, plates
            a.Box(Trim, new Vector3(0, s.clearance + 0.1f, halfL + 0.04f), new Vector3(s.W * 1.01f, 0.22f, 0.16f));
            a.Box(Trim, new Vector3(0, s.clearance + 0.1f, -halfL - 0.04f), new Vector3(s.W * 1.01f, 0.22f, 0.16f));
            a.Box(Trim, new Vector3(0, yBody + 0.06f, halfL + 0.005f), new Vector3(s.W * 0.42f, s.bodyH * 0.3f, 0.04f));
            a.Box(Chrome, new Vector3(0, yBody + 0.06f, halfL + 0.012f), new Vector3(s.W * 0.42f, 0.025f, 0.03f));
            foreach (float sx in new[] { -1f, 1f })
            {
                a.Box(HeadLamp, new Vector3(sx * s.W * 0.34f, yBody + 0.1f, halfL + 0.01f), new Vector3(s.W * 0.2f, 0.12f, 0.04f));
                a.Box(TailLamp, new Vector3(sx * s.W * 0.36f, yBody + 0.12f, -halfL - 0.01f), new Vector3(s.W * 0.22f, 0.12f, 0.04f));
            }
            bool commercial = shape == VehicleShape.Taxi;
            a.Box(commercial ? PlateYellow : PlateWhite, new Vector3(0, s.clearance + 0.12f, halfL + 0.13f), new Vector3(0.5f, 0.12f, 0.02f));
            a.Box(commercial ? PlateYellow : PlateWhite, new Vector3(0, s.clearance + 0.12f, -halfL - 0.13f), new Vector3(0.5f, 0.12f, 0.02f));

            float roofTop = topBody + s.cabinH + 0.04f;
            switch (shape)
            {
                case VehicleShape.Taxi:
                    a.Box(PlateYellow, new Vector3(0, roofTop + 0.1f, s.cabinZ), new Vector3(0.55f, 0.16f, 0.22f));
                    break;
                case VehicleShape.Police:
                    var stripe = MaterialFactory.Lit(new Color(0.08f, 0.2f, 0.55f), 0.4f);
                    a.Box(stripe, new Vector3(0, yBody + 0.02f, 0), new Vector3(s.W * 1.006f, 0.14f, s.L * 0.86f));
                    a.Box(Trim, new Vector3(0, roofTop + 0.04f, s.cabinZ), new Vector3(s.W * 0.7f, 0.06f, 0.26f));
                    a.Box(MaterialFactory.Lit(new Color(0.7f, 0.05f, 0.05f), 0.6f), new Vector3(-s.W * 0.18f, roofTop + 0.12f, s.cabinZ), new Vector3(s.W * 0.32f, 0.1f, 0.22f));
                    a.Box(MaterialFactory.Lit(new Color(0.05f, 0.15f, 0.7f), 0.6f), new Vector3(s.W * 0.18f, roofTop + 0.12f, s.cabinZ), new Vector3(s.W * 0.32f, 0.1f, 0.22f));
                    // push bar
                    a.Box(Trim, new Vector3(0, s.clearance + 0.3f, halfL + 0.18f), new Vector3(s.W * 0.6f, 0.35f, 0.06f));
                    break;
                case VehicleShape.SUV:
                    foreach (float sx in new[] { -1f, 1f })
                        a.Box(Chrome, new Vector3(sx * s.W * 0.36f, roofTop + 0.05f, s.cabinZ), new Vector3(0.05f, 0.06f, s.cabinL * 0.85f));
                    // spare wheel on the tailgate
                    a.Cylinder(Tyre, new Vector3(0, yBody + 0.1f, -halfL - 0.15f), s.wheelD * 0.9f, 0.22f, new Vector3(90, 0, 0));
                    // fender cladding
                    foreach (float sx in new[] { -1f, 1f })
                        a.Box(Trim, new Vector3(sx * (s.W * 0.5f + 0.01f), s.clearance + 0.1f, 0), new Vector3(0.04f, 0.2f, s.L * 0.9f));
                    break;
                case VehicleShape.Sports:
                    a.Box(Trim, new Vector3(0, topBody + 0.32f, -halfL + 0.2f), new Vector3(s.W * 0.92f, 0.05f, 0.32f));
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        a.Box(Trim, new Vector3(sx * s.W * 0.32f, topBody + 0.16f, -halfL + 0.25f), new Vector3(0.06f, 0.3f, 0.12f));
                        a.Box(Trim, new Vector3(sx * (s.W * 0.5f + 0.005f), yBody, 0.3f), new Vector3(0.03f, 0.16f, 0.7f));
                    }
                    a.Box(Trim, new Vector3(0, s.clearance + 0.02f, halfL - 0.05f), new Vector3(s.W * 0.95f, 0.04f, 0.4f));
                    break;
            }

            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                wheels.Add(new Vector3(sx * (s.W * 0.5f - 0.1f), s.wheelD * 0.5f, sz * (halfL - s.wheelD * 0.95f)));

            // Dark wheel arches
            foreach (var w in wheels)
                a.Box(Trim, new Vector3(w.x * 1.01f, w.y + 0.05f, w.z), new Vector3(0.05f, s.wheelD * 0.4f, s.wheelD * 1.15f));

            return new Vector3(s.W, roofTop, s.L);
        }

        // ------------------------------------------------------------------ bus
        static Vector3 BuildBus(Color paint, MeshAssembler a, List<Vector3> wheels, out float wheelD)
        {
            float L = 11f, W = 2.5f, clr = 0.4f;
            wheelD = 1.0f;
            var lower = MaterialFactory.Lit(paint, 0.4f);
            var upper = MaterialFactory.Lit(new Color(0.93f, 0.89f, 0.76f), 0.3f);
            a.Box(lower, new Vector3(0, clr + 0.6f, 0), new Vector3(W, 1.2f, L));
            a.Box(upper, new Vector3(0, clr + 1.9f, 0), new Vector3(W, 1.4f, L));
            a.Box(Glass, new Vector3(0, clr + 1.95f, -0.3f), new Vector3(W * 1.012f, 0.85f, L * 0.86f));
            a.Box(Glass, new Vector3(0, clr + 1.75f, L * 0.5f + 0.01f), new Vector3(W * 0.9f, 1.15f, 0.04f));
            a.Box(Glass, new Vector3(0, clr + 2.05f, -L * 0.5f - 0.01f), new Vector3(W * 0.8f, 0.7f, 0.04f));
            a.Box(MaterialFactory.Lit(new Color(0.55f, 0.55f, 0.55f)), new Vector3(0, clr + 2.65f, 0), new Vector3(W * 0.96f, 0.14f, L * 0.98f));
            a.Box(MaterialFactory.Unlit(new Color(1f, 0.55f, 0.05f)), new Vector3(0, clr + 2.48f, L * 0.5f + 0.02f), new Vector3(W * 0.7f, 0.22f, 0.04f));
            // stripe along the side
            a.Box(MaterialFactory.Lit(new Color(0.95f, 0.75f, 0.1f)), new Vector3(0, clr + 1.22f, 0), new Vector3(W * 1.008f, 0.12f, L * 0.99f));
            a.Box(Trim, new Vector3(0, clr + 0.12f, L * 0.5f + 0.06f), new Vector3(W, 0.25f, 0.12f));
            a.Box(Trim, new Vector3(0, clr + 0.12f, -L * 0.5f - 0.06f), new Vector3(W, 0.25f, 0.12f));
            foreach (float sx in new[] { -1f, 1f })
            {
                a.Box(HeadLamp, new Vector3(sx * W * 0.38f, clr + 0.5f, L * 0.5f + 0.01f), new Vector3(0.3f, 0.18f, 0.04f));
                a.Box(TailLamp, new Vector3(sx * W * 0.4f, clr + 0.6f, -L * 0.5f - 0.01f), new Vector3(0.22f, 0.3f, 0.04f));
                a.Box(Trim, new Vector3(sx * (W * 0.5f + 0.12f), clr + 2.0f, L * 0.5f - 0.1f), new Vector3(0.06f, 0.4f, 0.12f));
            }
            a.Box(PlateWhite, new Vector3(0, clr + 0.15f, L * 0.5f + 0.13f), new Vector3(0.55f, 0.14f, 0.02f));
            foreach (float sx in new[] { -1f, 1f })
            {
                wheels.Add(new Vector3(sx * (W * 0.5f - 0.2f), wheelD * 0.5f, L * 0.5f - 2.1f));
                wheels.Add(new Vector3(sx * (W * 0.5f - 0.2f), wheelD * 0.5f, -L * 0.5f + 2.4f));
            }
            return new Vector3(W, clr + 2.72f, L);
        }

        // ------------------------------------------------------------------ truck
        static readonly Color[] TruckArt =
        {
            new Color(0.95f, 0.15f, 0.2f), new Color(0.1f, 0.6f, 0.9f), new Color(1f, 0.8f, 0.1f),
            new Color(0.2f, 0.75f, 0.3f), new Color(0.95f, 0.45f, 0.05f), new Color(0.7f, 0.2f, 0.75f)
        };

        static Vector3 BuildTruck(Color paint, MeshAssembler a, List<Vector3> wheels, out float wheelD, int seed)
        {
            float L = 8.6f, W = 2.45f, clr = 0.55f;
            wheelD = 1.0f;
            var rng = new System.Random(seed * 31 + 7);
            var cab = MaterialFactory.Lit(paint, 0.4f);
            var cargo = MaterialFactory.Lit(TruckArt[rng.Next(TruckArt.Length)], 0.3f);
            // chassis
            a.Box(Trim, new Vector3(0, clr + 0.05f, 0), new Vector3(W * 0.7f, 0.3f, L * 0.98f));
            // cab
            float cabZ = L * 0.5f - 1.05f;
            a.Box(cab, new Vector3(0, clr + 1.0f, cabZ), new Vector3(W, 1.7f, 2.1f));
            a.Box(Glass, new Vector3(0, clr + 1.45f, cabZ + 1.06f), new Vector3(W * 0.88f, 0.75f, 0.04f));
            foreach (float sx in new[] { -1f, 1f })
                a.Box(Glass, new Vector3(sx * (W * 0.5f + 0.005f), clr + 1.45f, cabZ + 0.3f), new Vector3(0.02f, 0.6f, 0.9f));
            // decorated crown over the windshield (Indian truck style)
            a.Box(MaterialFactory.Lit(TruckArt[rng.Next(TruckArt.Length)]), new Vector3(0, clr + 2.0f, cabZ + 0.85f), new Vector3(W * 1.02f, 0.32f, 0.4f));
            a.Box(MaterialFactory.Lit(TruckArt[rng.Next(TruckArt.Length)]), new Vector3(0, clr + 2.2f, cabZ + 0.95f), new Vector3(W * 0.7f, 0.12f, 0.2f));
            // bumper & lamps
            a.Box(MaterialFactory.Lit(new Color(0.85f, 0.85f, 0.85f), 0.6f), new Vector3(0, clr + 0.2f, L * 0.5f + 0.05f), new Vector3(W * 1.02f, 0.3f, 0.14f));
            foreach (float sx in new[] { -1f, 1f })
                a.Box(HeadLamp, new Vector3(sx * W * 0.36f, clr + 0.55f, L * 0.5f - 0.0f), new Vector3(0.3f, 0.2f, 0.04f));
            // cargo body
            float boxL = L - 2.3f;
            float boxZ = -L * 0.5f + boxL * 0.5f;
            a.Box(cargo, new Vector3(0, clr + 1.45f, boxZ), new Vector3(W, 2.4f, boxL));
            // painted bands
            for (int i = 0; i < 3; i++)
            {
                var band = MaterialFactory.Lit(TruckArt[rng.Next(TruckArt.Length)], 0.3f);
                a.Box(band, new Vector3(0, clr + 0.55f + i * 0.85f, boxZ), new Vector3(W * 1.01f, 0.14f, boxL * 1.002f));
            }
            // rear: tail lamps + tassel bar
            foreach (float sx in new[] { -1f, 1f })
                a.Box(TailLamp, new Vector3(sx * W * 0.4f, clr + 0.25f, -L * 0.5f - 0.01f), new Vector3(0.25f, 0.18f, 0.04f));
            a.Box(MaterialFactory.Lit(new Color(0.1f, 0.1f, 0.1f)), new Vector3(0, clr + 0.1f, -L * 0.5f - 0.05f), new Vector3(W, 0.12f, 0.08f));

            foreach (float sx in new[] { -1f, 1f })
            {
                wheels.Add(new Vector3(sx * (W * 0.5f - 0.22f), wheelD * 0.5f, L * 0.5f - 1.2f));
                wheels.Add(new Vector3(sx * (W * 0.5f - 0.22f), wheelD * 0.5f, -L * 0.5f + 1.3f));
                wheels.Add(new Vector3(sx * (W * 0.5f - 0.22f), wheelD * 0.5f, -L * 0.5f + 2.4f));
            }
            return new Vector3(W, clr + 2.7f, L);
        }

        // ------------------------------------------------------------------ motorcycle
        static readonly Color[] Shirts =
        {
            new Color(0.9f, 0.9f, 0.9f), new Color(0.2f, 0.35f, 0.7f), new Color(0.75f, 0.15f, 0.15f),
            new Color(0.95f, 0.75f, 0.2f), new Color(0.2f, 0.55f, 0.35f), new Color(0.45f, 0.3f, 0.6f)
        };

        static Vector3 BuildBike(Color paint, MeshAssembler a, List<Vector3> wheels, out float wheelD, int seed)
        {
            var rng = new System.Random(seed * 17 + 3);
            wheelD = 0.62f;
            var body = MaterialFactory.Lit(paint, 0.6f);
            var skin = MaterialFactory.Lit(new Color(0.55f, 0.38f, 0.26f));
            var shirt = MaterialFactory.Lit(Shirts[rng.Next(Shirts.Length)]);
            var pants = MaterialFactory.Lit(new Color(0.15f, 0.17f, 0.25f));
            var helmet = MaterialFactory.Lit(Shirts[rng.Next(Shirts.Length)], 0.7f);

            a.Box(body, new Vector3(0, 0.68f, 0.15f), new Vector3(0.3f, 0.28f, 0.7f));                 // tank
            a.Box(Trim, new Vector3(0, 0.5f, -0.05f), new Vector3(0.22f, 0.3f, 0.8f));                  // engine
            a.Box(Trim, new Vector3(0, 0.82f, -0.35f), new Vector3(0.28f, 0.1f, 0.6f));                 // seat
            a.Box(body, new Vector3(0, 0.7f, -0.75f), new Vector3(0.24f, 0.12f, 0.35f));                // tail
            a.Box(Chrome, new Vector3(0.12f, 0.42f, -0.6f), new Vector3(0.08f, 0.08f, 0.6f));           // exhaust
            a.Box(Chrome, new Vector3(0, 0.75f, 0.62f), new Vector3(0.05f, 0.55f, 0.05f), new Vector3(-20, 0, 0)); // fork
            a.Box(Trim, new Vector3(0, 1.02f, 0.55f), new Vector3(0.65f, 0.04f, 0.04f));                // handlebar
            a.Box(HeadLamp, new Vector3(0, 0.9f, 0.72f), new Vector3(0.14f, 0.12f, 0.06f));
            a.Box(TailLamp, new Vector3(0, 0.75f, -0.94f), new Vector3(0.12f, 0.06f, 0.03f));
            // rider
            a.Box(pants, new Vector3(0, 0.95f, -0.3f), new Vector3(0.38f, 0.18f, 0.4f));
            foreach (float sx in new[] { -1f, 1f })
            {
                a.Box(pants, new Vector3(sx * 0.2f, 0.7f, -0.05f), new Vector3(0.12f, 0.45f, 0.14f), new Vector3(-25, 0, 0));
                a.Box(shirt, new Vector3(sx * 0.24f, 1.3f, 0.2f), new Vector3(0.1f, 0.1f, 0.5f), new Vector3(25, 0, 0));
            }
            a.Box(shirt, new Vector3(0, 1.32f, -0.15f), new Vector3(0.42f, 0.6f, 0.26f), new Vector3(15, 0, 0));
            a.Sphere(skin, new Vector3(0, 1.7f, -0.03f), new Vector3(0.22f, 0.24f, 0.22f));
            a.Sphere(helmet, new Vector3(0, 1.74f, -0.05f), new Vector3(0.3f, 0.3f, 0.32f));

            wheels.Add(new Vector3(0, wheelD * 0.5f, 0.68f));
            wheels.Add(new Vector3(0, wheelD * 0.5f, -0.68f));
            return new Vector3(0.75f, 1.9f, 2.0f);
        }

        // ------------------------------------------------------------------ auto-rickshaw
        static Vector3 BuildAuto(Color paint, MeshAssembler a, List<Vector3> wheels, out float wheelD, int seed)
        {
            var rng = new System.Random(seed * 13 + 5);
            float L = 2.7f, W = 1.35f;
            wheelD = 0.5f;
            var body = MaterialFactory.Lit(paint, 0.5f);
            var canopy = MaterialFactory.Lit(new Color(1f, 0.82f, 0.08f), 0.3f);
            var canvas = MaterialFactory.Lit(new Color(0.08f, 0.08f, 0.08f), 0.1f);
            var skin = MaterialFactory.Lit(new Color(0.5f, 0.34f, 0.22f));
            var shirt = MaterialFactory.Lit(rng.Next(2) == 0 ? new Color(0.85f, 0.8f, 0.65f) : new Color(0.4f, 0.5f, 0.7f));

            a.Box(body, new Vector3(0, 0.45f, -0.15f), new Vector3(W, 0.5f, L * 0.75f));                     // floor tub
            a.Box(body, new Vector3(0, 0.62f, L * 0.5f - 0.3f), new Vector3(0.62f, 0.75f, 0.5f));            // nose
            a.Box(canopy, new Vector3(0, 1.0f, L * 0.5f - 0.32f), new Vector3(0.5f, 0.12f, 0.45f));
            a.Box(Glass, new Vector3(0, 1.32f, L * 0.5f - 0.55f), new Vector3(W * 0.82f, 0.55f, 0.04f), new Vector3(-8, 0, 0));
            a.Box(canopy, new Vector3(0, 1.72f, -0.2f), new Vector3(W * 1.04f, 0.1f, L * 0.82f));          // roof
            a.Box(canvas, new Vector3(0, 1.62f, -0.2f), new Vector3(W * 1.02f, 0.12f, L * 0.8f));
            a.Box(canopy, new Vector3(0, 1.2f, -L * 0.5f + 0.06f), new Vector3(W, 1.0f, 0.08f));             // back
            a.Box(canvas, new Vector3(0, 1.25f, -L * 0.5f + 0.02f), new Vector3(W * 0.5f, 0.25f, 0.04f));     // back window
            foreach (float sx in new[] { -1f, 1f })
            {
                a.Box(canvas, new Vector3(sx * W * 0.5f, 1.15f, L * 0.5f - 0.55f), new Vector3(0.05f, 1.1f, 0.05f));
                a.Box(canopy, new Vector3(sx * W * 0.5f, 1.15f, -0.85f), new Vector3(0.06f, 1.1f, 0.5f));     // side panels behind
                a.Box(TailLamp, new Vector3(sx * W * 0.4f, 0.55f, -L * 0.5f - 0.01f), new Vector3(0.12f, 0.1f, 0.04f));
            }
            a.Box(HeadLamp, new Vector3(0, 0.82f, L * 0.5f - 0.04f), new Vector3(0.16f, 0.14f, 0.04f));
            a.Box(PlateYellow, new Vector3(0, 0.35f, -L * 0.5f + 0.15f), new Vector3(0.4f, 0.12f, 0.02f));
            // driver + passengers seat
            a.Box(canvas, new Vector3(0, 0.82f, -0.75f), new Vector3(W * 0.9f, 0.18f, 0.45f));
            a.Box(shirt, new Vector3(0, 1.1f, 0.15f), new Vector3(0.38f, 0.5f, 0.25f));
            a.Sphere(skin, new Vector3(0, 1.48f, 0.17f), new Vector3(0.22f, 0.25f, 0.22f));
            a.Box(Trim, new Vector3(0, 1.1f, 0.6f), new Vector3(0.5f, 0.04f, 0.04f)); // handlebar

            wheels.Add(new Vector3(0, wheelD * 0.5f, L * 0.5f - 0.35f));
            wheels.Add(new Vector3(-W * 0.5f + 0.05f, wheelD * 0.5f, -L * 0.5f + 0.5f));
            wheels.Add(new Vector3(W * 0.5f - 0.05f, wheelD * 0.5f, -L * 0.5f + 0.5f));
            return new Vector3(W, 1.8f, L);
        }

        /// <summary>Non-mesh decoration (3D text) that is cheap to recreate per instance.</summary>
        static void AddExtras(VehicleShape shape, Transform meshRoot, int seed)
        {
            if (shape == VehicleShape.Truck)
            {
                var t = Util.Text3D(meshRoot, "HORN OK\nPLEASE", new Vector3(0, 1.9f, -4.33f), Vector3.zero, 0.32f, new Color(1f, 0.95f, 0.2f));
                t.lineSpacing = 0.9f;
            }
            else if (shape == VehicleShape.Bus)
            {
                Util.Text3D(meshRoot, "EXPRESS", new Vector3(0, 2.88f, 5.53f), new Vector3(0, 180, 0), 0.18f, new Color(0.15f, 0.05f, 0f));
            }
            else if (shape == VehicleShape.Taxi)
            {
                Util.Text3D(meshRoot, "TAXI", new Vector3(0, 1.66f, -0.37f), Vector3.zero, 0.11f, Color.black);
            }
        }

        static void AddIndicators(VehicleVisual vis, Vector3 size)
        {
            var amber = Amber;
            vis.indicatorLeft = MakeLamp(vis.body, "IndL", amber, new Vector3(-size.x * 0.48f, Mathf.Min(0.8f, size.y * 0.45f), -size.z * 0.5f - 0.03f), new Vector3(0.14f, 0.1f, 0.05f));
            vis.indicatorRight = MakeLamp(vis.body, "IndR", amber, new Vector3(size.x * 0.48f, Mathf.Min(0.8f, size.y * 0.45f), -size.z * 0.5f - 0.03f), new Vector3(0.14f, 0.1f, 0.05f));
            vis.SetIndicators(false, false);
        }

        static void AddLightRenderers(VehicleShape shape, VehicleVisual vis, Vector3 size)
        {
            var s = IsCar(shape) ? Spec(shape) : new CarSpec { clearance = 0.3f, bodyH = 0.6f, W = size.x, L = size.z };
            float y = s.clearance + s.bodyH * 0.5f + 0.12f;
            // brake lights: one renderer covering both lamps
            var asm = new MeshAssembler();
            foreach (float sx in new[] { -1f, 1f })
                asm.Box(BrakeLamp, new Vector3(sx * s.W * 0.36f, y, -s.L * 0.5f - 0.035f), new Vector3(s.W * 0.23f, 0.13f, 0.03f));
            asm.Box(BrakeLamp, new Vector3(0, s.clearance + s.bodyH + s.cabinH - 0.05f, s.cabinZ - s.cabinL * 0.5f - 0.02f), new Vector3(0.4f, 0.04f, 0.03f));
            var brake = asm.Build("BrakeLights", vis.body, false);
            vis.brakeLights = brake.GetComponent<MeshRenderer>();
            vis.brakeLights.enabled = false;

            // headlight beams: soft additive quads on the road in front
            var beamMat = GameAssets.I.particleAdditive;
            var beams = new GameObject("HeadlightBeams");
            beams.transform.SetParent(vis.body, false);
            var ba = new MeshAssembler();
            foreach (float sx in new[] { -1f, 1f })
                ba.Quad(beamMat, new Vector3(sx * s.W * 0.3f, 0.03f, s.L * 0.5f + 4.5f), new Vector2(2.2f, 9f), new Vector3(90, 0, 0));
            var beamMesh = ba.BakeMesh("Beams", out var bm);
            MeshAssembler.Apply(beams, beamMesh, bm, false);
            vis.headlightBeams = beams.GetComponent<MeshRenderer>();
            vis.headlightBeams.enabled = false;

            vis.indicatorLeft = MakeLamp(vis.body, "IndL", Amber, new Vector3(-s.W * 0.47f, y, -s.L * 0.5f - 0.04f), new Vector3(0.12f, 0.08f, 0.03f));
            vis.indicatorRight = MakeLamp(vis.body, "IndR", Amber, new Vector3(s.W * 0.47f, y, -s.L * 0.5f - 0.04f), new Vector3(0.12f, 0.08f, 0.03f));
            vis.SetIndicators(false, false);

            if (shape == VehicleShape.Police)
            {
                float top = s.clearance + s.bodyH + s.cabinH + 0.16f;
                vis.sirenRed = MakeLamp(vis.body, "SirenR", MaterialFactory.Unlit(new Color(1f, 0.1f, 0.1f)), new Vector3(-s.W * 0.18f, top, s.cabinZ), new Vector3(s.W * 0.33f, 0.11f, 0.23f));
                vis.sirenBlue = MakeLamp(vis.body, "SirenB", MaterialFactory.Unlit(new Color(0.15f, 0.35f, 1f)), new Vector3(s.W * 0.18f, top, s.cabinZ), new Vector3(s.W * 0.33f, 0.11f, 0.23f));
            }
        }

        static Renderer MakeLamp(Transform parent, string name, Material mat, Vector3 pos, Vector3 size)
        {
            var asm = new MeshAssembler();
            asm.Box(mat, pos, size);
            return asm.Build(name, parent, false).GetComponent<MeshRenderer>();
        }
    }
}
