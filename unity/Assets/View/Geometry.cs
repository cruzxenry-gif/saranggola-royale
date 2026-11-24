using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Saranggola.Sim;

namespace Saranggola.View
{
    public enum Shape { Box, Sphere, Capsule, Post, Cone, Diamond, Ring, Plane }

    public sealed class MeshDraft
    {
        private readonly List<Vector3> vertices = new List<Vector3>(128);
        private readonly List<Vector3> normals = new List<Vector3>(128);
        private readonly List<int> triangles = new List<int>(192);
        private readonly List<Color32> colors = new List<Color32>(128);

        public void Add(Mesh basis, Vector3 position, Vector3 size, Color color, Vector3 rotation = default)
        {
            var template = PropFactory.PrimitiveData.Get(basis);
            var transform = Matrix4x4.TRS(position, Quaternion.Euler(rotation), size);
            var normal = transform.inverse.transpose;
            var start = vertices.Count;
            for (int i = 0; i < template.Points.Length; i++)
            {
                vertices.Add(transform.MultiplyPoint3x4(template.Points[i]));
                normals.Add(normal.MultiplyVector(template.Normals[i]).normalized);
                colors.Add(template.Paint.Length != 0 ? (Color)template.Paint[i] * color : color);
            }
            foreach (int index in template.Indices) triangles.Add(start + index);
        }

        public Mesh Commit(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    public sealed partial class PropFactory : System.IDisposable
    {
        internal sealed class PrimitiveData
        {
            private static readonly Dictionary<Mesh, PrimitiveData> Cache = new Dictionary<Mesh, PrimitiveData>();
            public readonly Vector3[] Points, Normals;
            public readonly int[] Indices;
            public readonly Color32[] Paint;
            private PrimitiveData(Mesh mesh) { Points = mesh.vertices; Normals = mesh.normals; Indices = mesh.triangles; Paint = mesh.colors32; }
            public static PrimitiveData Get(Mesh m)
            {
                if (!Cache.TryGetValue(m, out var data)) Cache[m] = data = new PrimitiveData(m);
                return data;
            }
        }

        public static readonly Color Ink = Html("#2c3040");
        public static readonly Color Cream = Html("#f4d89f");
        public static readonly Color Wood = Html("#82604d");
        public static readonly Color Reed = Html("#c4a16e");
        public static readonly Color Leaf = Html("#649065");
        public static readonly Color Rock = Html("#7b7478");
        public static readonly Color Terracotta = Html("#c17861");
        public static readonly Color[] TeamColors =
        {
            Html("#60ddbb"), Html("#fd8e7b"), Html("#f2c66c"), Html("#aaa5ff"),
            Html("#77b8ed"), Html("#ed9abf")
        };
        private static readonly Dictionary<Shape, Mesh> Shapes = new Dictionary<Shape, Mesh>();
        private readonly Dictionary<Color32, Material> flats = new Dictionary<Color32, Material>();
        private readonly Dictionary<Color32, Material> unlit = new Dictionary<Color32, Material>();
        private readonly HashSet<UnityEngine.Object> owned = new HashSet<UnityEngine.Object>();
        public readonly Material Toon, Outline, Blob, Flash;
        public Material WorldMaterial => Toon != null ? Toon : Flat(Html("#a7a183"));

        public PropFactory()
        {
            var shader = Shader.Find("Saranggola/Toon");
            if (shader != null && shader.isSupported)
            {
                Toon = Make(shader, Color.white, "Toon vertex paint");
                Flash = Make(shader, Color.white, "Hit flash");
                Flash.SetFloat("_Flash", 1);
                Own(Toon);Own(Flash);
                var hull = Shader.Find("Saranggola/Outline");
                if (hull != null && hull.isSupported) { Outline = Make(hull, Ink, "Outline"); Own(Outline); }
            }
            else Debug.LogWarning("Saranggola toon shader unavailable. Using flat URP/Lit colours.");
            Blob = Unlit(Cream * new Color(.24f,.23f,.27f,1));
        }

        public static Color Html(string hex) => ColorUtility.TryParseHtmlString(hex, out var value) ? value : Color.white;
        public static Vector3 Xz(Vec2 p, float height = 0) => new Vector3(p.X, height, p.Z);
        public static Vec2 FlatPosition(Vector3 p) => new Vec2(p.x, p.z);
        public static Color Team(int id) => TeamColors[(id < 0 ? 0 : id) % TeamColors.Length];

        private static Material Make(Shader shader, Color tint, string name)
        {
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            var material = new Material(shader) { name = name, enableInstancing = true };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            return material;
        }

        public void Own(UnityEngine.Object item)
        {
            if(item!=null)owned.Add(item);
        }

        public void Dispose()
        {
            foreach(var item in owned)UnityEngine.Object.Destroy(item);
            owned.Clear();
        }

        public Material Flat(Color color)
        {
            var key = (Color32)color;
            if (!flats.TryGetValue(key, out var material))
            {
                flats[key] = material = Make(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), color, "Flat paint");
                Own(material);
            }
            return material;
        }

        public Material Unlit(Color color)
        {
            var key = (Color32)color;
            if (!unlit.TryGetValue(key, out var material))
            {
                unlit[key] = material = Make(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"), color, "Unlit paint");
                Own(material);
            }
            return material;
        }

        public MeshRenderer Place(Transform parent, string name, Mesh mesh, Material material, Vector3 at,
                                  Vector3 scale, bool shadows = false)
        {
            if(!Shapes.ContainsValue(mesh))Own(mesh);
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = at;
            piece.transform.localScale = scale;
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        public static Mesh Primitive(Shape type)
        {
            if (Shapes.TryGetValue(type, out var cached)) return cached;
            var v = new List<Vector3>(86);
            var t = new List<int>(250);
            switch (type)
            {
                case Shape.Box:
                    Wall(v,t,new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f));
                    Wall(v,t,new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f));
                    Wall(v,t,new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f));
                    Wall(v,t,new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f));
                    Wall(v,t,new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f));
                    Wall(v,t,new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f));
                    break;
                case Shape.Sphere:
                case Shape.Capsule:
                    const int around = 10, rows = 6;
                    for (int y = 0; y <= rows; y++)
                    {
                        float a = y * Mathf.PI / rows;
                        for (int x = 0; x <= around; x++)
                        {
                            float b = x * 2 * Mathf.PI / around;
                            float lift = type == Shape.Capsule ? Mathf.Cos(a) * .40f + (y < rows / 2 ? .10f : -.10f) : Mathf.Cos(a) * .50f;
                            v.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b)*.5f,lift,Mathf.Sin(a)*Mathf.Sin(b)*.5f));
                        }
                    }
                    for (int y = 0; y < rows; y++)
                        for (int x = 0; x < around; x++)
                        {
                            int a = y * (around+1) + x, b = a + around + 1;
                            t.Add(a); t.Add(a+1); t.Add(b);
                            t.Add(a+1); t.Add(b+1); t.Add(b);
                        }
                    break;
                case Shape.Post:
                case Shape.Cone:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4, b = (i+1) * Mathf.PI / 4;
                        var from = new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f);
                        var to = new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f);
                        if (type == Shape.Cone) Triangle(v,t,to,from,Vector3.up*.5f);
                        else
                        {
                            Wall(v,t,to,from,from+Vector3.up,to+Vector3.up);
                            Triangle(v,t,Vector3.up*.5f,to+Vector3.up,from+Vector3.up);
                        }
                        Triangle(v,t,Vector3.down*.5f,from,to);
                    }
                    break;
                case Shape.Diamond:
                    var top = Vector3.back * .6f;
                    var tail = Vector3.forward * .80f;
                    var left = Vector3.left * .57f;
                    var right = Vector3.right * .57f;
                    Triangle(v,t,top,left,tail); Triangle(v,t,top,tail,right);
                    Triangle(v,t,top,tail,left); Triangle(v,t,top,right,tail);
                    break;
                case Shape.Ring:
                    for (int i = 0; i < 48; i++)
                    {
                        float a = i * Mathf.PI / 24, b = (i+1) * Mathf.PI / 24;
                        var start = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                        var next = new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                        Wall(v,t,start*.96f,next*.96f,next,start);
                    }
                    break;
                case Shape.Plane:
                    Wall(v,t,new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f));
                    break;
            }
            var mesh = new Mesh { name = "Procedural " + type };
            mesh.SetVertices(v);
            mesh.SetTriangles(t,0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return Shapes[type] = mesh;
        }

        private static void Triangle(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            int index = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.Add(index); t.Add(index+1); t.Add(index+2);
        }

        private static void Wall(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int index = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.Add(index); t.Add(index+1); t.Add(index+2); t.Add(index); t.Add(index+2); t.Add(index+3);
        }
    }
}
