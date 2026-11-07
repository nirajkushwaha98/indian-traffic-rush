using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ITR
{
    /// <summary>
    /// Collects transformed primitive meshes and bakes them into ONE mesh with one sub-mesh per material.
    /// Every procedural model (cars, buildings, road) goes through this so each object is a single renderer.
    /// </summary>
    public class MeshAssembler
    {
        readonly List<Material> order = new List<Material>();
        readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();

        public int PartCount { get; private set; }

        public void Add(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!groups.TryGetValue(mat, out var list))
            {
                list = new List<CombineInstance>();
                groups[mat] = list;
                order.Add(mat);
            }
            list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, rot, scale) });
            PartCount++;
        }

        public void Box(Material mat, Vector3 pos, Vector3 size) => Add(Prims.Cube, mat, pos, Quaternion.identity, size);
        public void Box(Material mat, Vector3 pos, Vector3 size, Vector3 euler) => Add(Prims.Cube, mat, pos, Quaternion.Euler(euler), size);
        public void Sphere(Material mat, Vector3 pos, Vector3 size) => Add(Prims.Sphere, mat, pos, Quaternion.identity, size);

        /// <summary>Cylinder with its axis along local Y (height = size.y).</summary>
        public void Cylinder(Material mat, Vector3 pos, float diameter, float height, Vector3 euler) =>
            Add(Prims.Cylinder, mat, pos, Quaternion.Euler(euler), new Vector3(diameter, height * 0.5f, diameter));

        /// <summary>Wheel lying on its side (axis along X).</summary>
        public void Wheel(Material tyre, Material hub, Vector3 pos, float diameter, float width)
        {
            Cylinder(tyre, pos, diameter, width, new Vector3(0, 0, 90));
            Cylinder(hub, pos, diameter * 0.55f, width * 1.06f, new Vector3(0, 0, 90));
        }

        public void Quad(Material mat, Vector3 pos, Vector2 size, Vector3 euler) =>
            Add(Prims.Quad, mat, pos, Quaternion.Euler(euler), new Vector3(size.x, size.y, 1f));

        public Mesh BakeMesh(string name, out Material[] materials)
        {
            var subs = new List<CombineInstance>(order.Count);
            int vertexCount = 0;
            foreach (var mat in order)
            {
                var sub = new Mesh();
                var parts = groups[mat];
                foreach (var p in parts) vertexCount += p.mesh.vertexCount;
                sub.indexFormat = IndexFormat.UInt32;
                sub.CombineMeshes(parts.ToArray(), true, true);
                subs.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = vertexCount > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.CombineMeshes(subs.ToArray(), false, false);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            foreach (var s in subs) Util.SafeDestroy(s.mesh);
            materials = order.ToArray();
            return mesh;
        }

        public GameObject Build(string name, Transform parent, bool castShadows = true)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            Apply(go, BakeMesh(name, out var mats), mats, castShadows);
            return go;
        }

        public static void Apply(GameObject go, Mesh mesh, Material[] mats, bool castShadows)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    /// <summary>Cached built-in primitive meshes (grabbed once from CreatePrimitive).</summary>
    public static class Prims
    {
        static Mesh cube, sphere, cylinder, quad;
        public static Mesh Cube => Get(ref cube, PrimitiveType.Cube);
        /// <summary>Low-poly UV sphere (~70 verts instead of Unity's 515) — used for foliage, heads, shrubs.</summary>
        public static Mesh Sphere
        {
            get
            {
                if (sphere == null) sphere = BuildLowSphere(10, 7);
                return sphere;
            }
        }

        static Mesh BuildLowSphere(int lon, int lat)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float v = (float)y / lat;
                float phi = v * Mathf.PI;
                for (int x = 0; x <= lon; x++)
                {
                    float u = (float)x / lon;
                    float th = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    verts.Add(n * 0.5f); normals.Add(n); uvs.Add(new Vector2(u, 1f - v));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int i0 = y * (lon + 1) + x, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            var m = new Mesh { name = "LowSphere" };
            m.SetVertices(verts); m.SetNormals(normals); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
        public static Mesh Cylinder => Get(ref cylinder, PrimitiveType.Cylinder);
        public static Mesh Quad => Get(ref quad, PrimitiveType.Quad);

        static Mesh Get(ref Mesh cache, PrimitiveType type)
        {
            if (cache != null) return cache;
            // Load the built-in mesh directly. CreatePrimitive would also add a collider component that
            // engine-code stripping removes from player builds (logs "class 'CapsuleCollider' doesn't exist").
            string res = type == PrimitiveType.Cube ? "Cube.fbx" : type == PrimitiveType.Cylinder ? "Cylinder.fbx"
                       : type == PrimitiveType.Quad ? "Quad.fbx" : "New-Sphere.fbx";
            cache = Resources.GetBuiltinResource<Mesh>(res);
            if (cache != null) return cache;
            var go = GameObject.CreatePrimitive(type);
            cache = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return cache;
        }
    }
}
