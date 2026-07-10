using System.Collections.Generic;
using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Лёгкие low-poly меши с плоской заливкой (flat shading): усечённые
    /// коробки, гранёные призмы и сферы. Каждый меш — десятки треугольников,
    /// все кэшируются. Используются для персонажа и мелкого декора.
    /// </summary>
    public static class LowPolyMeshFactory
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        // ------------------------------------------------------------------
        // Публичные меши
        // ------------------------------------------------------------------

        /// <summary>Усечённая коробка: своя ширина/глубина снизу и сверху. Пивот в центре высоты.</summary>
        public static Mesh Frustum(float wBottom, float dBottom, float wTop, float dTop, float height)
        {
            string key = string.Format("fr_{0:0.000}_{1:0.000}_{2:0.000}_{3:0.000}_{4:0.000}",
                wBottom, dBottom, wTop, dTop, height);
            Mesh cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            float hb = height * 0.5f;
            float xb = wBottom * 0.5f, zb = dBottom * 0.5f;
            float xt = wTop * 0.5f, zt = dTop * 0.5f;
            Vector3 b0 = new Vector3(-xb, -hb, -zb);
            Vector3 b1 = new Vector3(xb, -hb, -zb);
            Vector3 b2 = new Vector3(xb, -hb, zb);
            Vector3 b3 = new Vector3(-xb, -hb, zb);
            Vector3 t0 = new Vector3(-xt, hb, -zt);
            Vector3 t1 = new Vector3(xt, hb, -zt);
            Vector3 t2 = new Vector3(xt, hb, zt);
            Vector3 t3 = new Vector3(-xt, hb, zt);

            MeshDraft d = new MeshDraft();
            d.Quad(b3, b2, t2, t3); // перед (+z)
            d.Quad(b1, b0, t0, t1); // зад (-z)
            d.Quad(b0, b3, t3, t0); // левая (-x)
            d.Quad(b2, b1, t1, t2); // правая (+x)
            d.Quad(t3, t2, t1, t0); // верх (+y)
            d.Quad(b0, b1, b2, b3); // низ (-y)
            Mesh m = d.Bake("LP_Frustum");
            Cache[key] = m;
            return m;
        }

        /// <summary>Гранёная призма-«цилиндр» с крышками. Пивот в центре высоты.</summary>
        public static Mesh Prism(int sides, float rBottom, float rTop, float height)
        {
            sides = Mathf.Clamp(sides, 3, 12);
            string key = string.Format("pr_{0}_{1:0.000}_{2:0.000}_{3:0.000}",
                sides, rBottom, rTop, height);
            Mesh cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            float hb = height * 0.5f;
            Vector3[] bot = new Vector3[sides];
            Vector3[] top = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                bot[i] = new Vector3(c * rBottom, -hb, s * rBottom);
                top[i] = new Vector3(c * rTop, hb, s * rTop);
            }

            MeshDraft d = new MeshDraft();
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                d.Quad(bot[j], bot[i], top[i], top[j]); // боковая грань наружу
            }
            for (int i = 1; i < sides - 1; i++)
            {
                d.Tri(top[0], top[i + 1], top[i]); // крышка (+y)
                d.Tri(bot[0], bot[i], bot[i + 1]); // дно (-y)
            }
            Mesh m = d.Bake("LP_Prism");
            Cache[key] = m;
            return m;
        }

        /// <summary>Гранёная сфера (UV, flat shading). Пивот в центре.</summary>
        public static Mesh Sphere(int lonSegments, int latSegments, float radius)
        {
            lonSegments = Mathf.Clamp(lonSegments, 4, 12);
            latSegments = Mathf.Clamp(latSegments, 3, 10);
            string key = string.Format("sp_{0}_{1}_{2:0.000}", lonSegments, latSegments, radius);
            Mesh cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            Vector3[,] p = new Vector3[lonSegments + 1, latSegments + 1];
            for (int j = 0; j <= latSegments; j++)
            {
                float lat = j * Mathf.PI / latSegments; // 0 (верх) .. PI (низ)
                float y = radius * Mathf.Cos(lat);
                float r = radius * Mathf.Sin(lat);
                for (int i = 0; i <= lonSegments; i++)
                {
                    float lon = i * Mathf.PI * 2f / lonSegments;
                    p[i, j] = new Vector3(r * Mathf.Cos(lon), y, r * Mathf.Sin(lon));
                }
            }

            MeshDraft d = new MeshDraft();
            for (int j = 0; j < latSegments; j++)
            {
                for (int i = 0; i < lonSegments; i++)
                {
                    Vector3 a = p[i, j];
                    Vector3 b = p[i + 1, j];
                    Vector3 c = p[i + 1, j + 1];
                    Vector3 e = p[i, j + 1];
                    if (j == 0) d.Tri(a, b, c);                     // полюс сверху
                    else if (j == latSegments - 1) d.Tri(a, b, e);  // полюс снизу
                    else d.Quad(a, b, c, e);
                }
            }
            Mesh m = d.Bake("LP_Sphere");
            Cache[key] = m;
            return m;
        }

        // ------------------------------------------------------------------
        // Создание объектов
        // ------------------------------------------------------------------

        public static GameObject MeshPart(Transform parent, Mesh mesh, Vector3 localPos,
            Vector3 euler, Vector3 scale, Material mat, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        public static GameObject MeshPart(Transform parent, Mesh mesh, Vector3 localPos,
            Material mat, string name)
        {
            return MeshPart(parent, mesh, localPos, Vector3.zero, Vector3.one, mat, name);
        }

        // ------------------------------------------------------------------
        // Черновик меша: дублируем вершины на каждую грань (flat shading)
        // ------------------------------------------------------------------

        private class MeshDraft
        {
            private readonly List<Vector3> _verts = new List<Vector3>(96);
            private readonly List<int> _tris = new List<int>(144);

            public void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int n = _verts.Count;
                _verts.Add(a); _verts.Add(b); _verts.Add(c);
                _tris.Add(n); _tris.Add(n + 1); _tris.Add(n + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int n = _verts.Count;
                _verts.Add(a); _verts.Add(b); _verts.Add(c); _verts.Add(d);
                _tris.Add(n); _tris.Add(n + 1); _tris.Add(n + 2);
                _tris.Add(n); _tris.Add(n + 2); _tris.Add(n + 3);
            }

            public Mesh Bake(string name)
            {
                Mesh m = new Mesh();
                m.name = name;
                m.SetVertices(_verts);
                m.SetTriangles(_tris, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
