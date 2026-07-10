using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CourtRunner15
{
    /// <summary>
    /// Лёгкие плоские панели с фотореалистичными иллюстрациями:
    /// билборды, окна, задники и UI-портреты боссов.
    /// </summary>
    public static class StaticIllustrationFactory
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static StaticIllustrationLibrary _library;
        private const string UnlitTextureTemplate = "CourtRunner15RuntimeUnlitTexture";

        public static GameObject CreateWorldPoster(string textureName, Vector2 size,
            Vector3 position, Vector3 rotation)
        {
            return CreateWorldPoster(null, textureName, size, position, rotation, 0.95f, true);
        }

        public static GameObject CreateWorldPoster(Transform parent, string textureName, Vector2 size,
            Vector3 position, Vector3 rotation)
        {
            return CreateWorldPoster(parent, textureName, size, position, rotation, 0.95f, true);
        }

        public static GameObject CreateBackdrop(string textureName, Vector2 size, Vector3 position)
        {
            return CreateWorldPoster(null, textureName, size, position, Vector3.zero, 1.08f, true);
        }

        public static GameObject CreateBackdrop(Transform parent, string textureName, Vector2 size,
            Vector3 position)
        {
            return CreateWorldPoster(parent, textureName, size, position, Vector3.zero, 1.08f, true);
        }

        public static GameObject CreateWindowView(string textureName, Vector2 size, Vector3 position)
        {
            return CreateWorldPoster(null, textureName, size, position, Vector3.zero, 0.82f, true);
        }

        public static GameObject CreateWindowView(Transform parent, string textureName, Vector2 size,
            Vector3 position, float rotY)
        {
            return CreateWorldPoster(parent, textureName, size, position, new Vector3(0f, rotY, 0f),
                0.82f, true);
        }

        public static GameObject CreateUnframedBackdrop(Transform parent, string textureName, Vector2 size,
            Vector3 position)
        {
            GameObject go = CreateWorldPoster(parent, textureName, size, position, Vector3.zero,
                0.92f, false);
            go.name = "embeddedBackdrop_" + textureName;
            return go;
        }

        public static GameObject CreateMaskedWindowView(Transform parent, string textureName, Vector2 size,
            Vector3 position, float rotY, int seed)
        {
            GameObject go = CreateWorldPoster(parent, textureName, size, position,
                new Vector3(0f, rotY, 0f), 0.66f, false);
            go.name = "windowBank_" + textureName;
            ApplyWindowUvVariant(go.transform, textureName, seed);
            AddWindowArchitecture(go.transform, size, seed);
            return go;
        }

        /// <summary>
        /// Разные окна показывают разные фрагменты панорамы (или её зеркало),
        /// чтобы банки окон читались как непрерывный вид за фасадом,
        /// а не как повторяющаяся картинка. Материалов максимум 4 на текстуру.
        /// </summary>
        private static void ApplyWindowUvVariant(Transform root, string textureName, int seed)
        {
            int variant = ((seed % 4) + 4) % 4;
            if (variant == 0) return;

            Vector2 scale;
            Vector2 offset;
            if (variant == 1) { scale = new Vector2(0.70f, 0.90f); offset = new Vector2(0.02f, 0.06f); }
            else if (variant == 2) { scale = new Vector2(0.70f, 0.90f); offset = new Vector2(0.28f, 0.08f); }
            else { scale = new Vector2(-1f, 1f); offset = new Vector2(1f, 0f); }

            Transform pane = root.Find("pane");
            if (pane == null) return;

            string key = textureName + "_uvwin" + variant;
            Material mat;
            if (!Materials.TryGetValue(key, out mat) || mat == null)
            {
                Material baseMat = pane.GetComponent<Renderer>().sharedMaterial;
                mat = new Material(baseMat);
                mat.name = baseMat.name + "_uv" + variant;
                mat.mainTextureScale = scale;
                mat.mainTextureOffset = offset;
                Materials[key] = mat;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name != "pane" && child.name != "paneBack") continue;
                Renderer r = child.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
        }

        public static GameObject CreateBossCutIn(string textureName)
        {
            GameObject go = new GameObject("bossCutIn_" + textureName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            Image img = go.GetComponent<Image>();
            img.sprite = GetSprite(textureName);
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            return go;
        }

        public static Image CreateBossCutIn(Transform parent, string textureName, Vector2 anchor,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateBossCutIn(textureName);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go.GetComponent<Image>();
        }

        public static Sprite GetSprite(string textureName)
        {
            Sprite cached;
            if (Sprites.TryGetValue(textureName, out cached) && cached != null) return cached;

            Texture2D tex = GetTexture(textureName);
            if (tex == null) return null;

            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            Sprites[textureName] = sprite;
            return sprite;
        }

        public static Texture2D GetTexture(string textureName)
        {
            if (_library == null) _library = StaticIllustrationLibrary.Load();
            if (_library == null)
            {
                Debug.LogWarning("[CourtRunner15] StaticIllustrationLibrary not found in Resources.");
                return null;
            }
            return _library.GetTexture(textureName);
        }

        private static GameObject CreateWorldPoster(Transform parent, string textureName, Vector2 size,
            Vector3 position, Vector3 rotation, float emission, bool framed)
        {
            Texture2D tex = GetTexture(textureName);
            if (tex == null)
            {
                Debug.LogWarning("[CourtRunner15] Missing static illustration: " + textureName);
                return new GameObject("missingIllustration_" + textureName);
            }

            GameObject root = new GameObject("staticIllustration_" + textureName);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(rotation);

            Material mat = WorldMaterial(textureName, tex, emission);
            AddPane(root.transform, "pane", Vector3.zero, Quaternion.identity, size, mat);
            // Back face keeps side/backdrop panels readable if a segment happens to be viewed off-axis.
            AddPane(root.transform, "paneBack", Vector3.zero, Quaternion.Euler(0f, 180f, 0f), size, mat);

            if (framed) AddFrame(root.transform, size);
            return root;
        }

        private static void AddPane(Transform parent, string name, Vector3 pos, Quaternion rot,
            Vector2 size, Material mat)
        {
            GameObject pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pane.name = name;
            Collider col = pane.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            pane.transform.SetParent(parent, false);
            pane.transform.localPosition = pos;
            pane.transform.localRotation = rot;
            pane.transform.localScale = new Vector3(size.x, size.y, 1f);
            Renderer r = pane.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static void AddFrame(Transform parent, Vector2 size)
        {
            Material frameMat = MaterialFactory.Solid(new Color(0.92f, 0.74f, 0.34f), 0.75f, 0.35f);
            float t = Mathf.Max(0.045f, Mathf.Min(size.x, size.y) * 0.018f);
            float z = -0.035f;
            AddFramePart(parent, new Vector3(0f, size.y * 0.5f + t * 0.5f, z),
                new Vector3(size.x + t * 2f, t, 0.06f), frameMat, "frameT");
            AddFramePart(parent, new Vector3(0f, -size.y * 0.5f - t * 0.5f, z),
                new Vector3(size.x + t * 2f, t, 0.06f), frameMat, "frameB");
            AddFramePart(parent, new Vector3(-size.x * 0.5f - t * 0.5f, 0f, z),
                new Vector3(t, size.y + t * 2f, 0.06f), frameMat, "frameL");
            AddFramePart(parent, new Vector3(size.x * 0.5f + t * 0.5f, 0f, z),
                new Vector3(t, size.y + t * 2f, 0.06f), frameMat, "frameR");
        }

        private static void AddWindowArchitecture(Transform parent, Vector2 size, int seed)
        {
            Material frameMat = MaterialFactory.Solid(new Color(0.055f, 0.075f, 0.085f), 0.82f, 0.18f);
            Material revealMat = MaterialFactory.Solid(new Color(0.025f, 0.030f, 0.034f), 0.6f, 0.05f);
            Material glassMat = MaterialFactory.UnlitTransparent(new Color(0.45f, 0.66f, 0.86f, 0.16f));
            Material blindMat = MaterialFactory.Solid(new Color(0.82f, 0.78f, 0.64f), 0.55f, 0.02f);

            float t = Mathf.Max(0.075f, Mathf.Min(size.x, size.y) * 0.038f);
            float z = -0.055f;
            AddFramePart(parent, new Vector3(0f, 0f, 0.045f),
                new Vector3(size.x + 0.42f, size.y + 0.38f, 0.05f), revealMat, "windowReveal");

            AddPane(parent, "glassSheen", new Vector3(0f, 0f, z - 0.012f), Quaternion.identity,
                new Vector2(size.x * 0.96f, size.y * 0.90f), glassMat);

            AddFramePart(parent, new Vector3(0f, size.y * 0.5f + t * 0.5f, z),
                new Vector3(size.x + t * 2f, t, 0.14f), frameMat, "windowHead");
            AddFramePart(parent, new Vector3(0f, -size.y * 0.5f - t * 0.5f, z),
                new Vector3(size.x + t * 2f, t, 0.14f), frameMat, "windowBase");
            AddFramePart(parent, new Vector3(-size.x * 0.5f - t * 0.5f, 0f, z),
                new Vector3(t, size.y + t * 2f, 0.14f), frameMat, "windowLeft");
            AddFramePart(parent, new Vector3(size.x * 0.5f + t * 0.5f, 0f, z),
                new Vector3(t, size.y + t * 2f, 0.14f), frameMat, "windowRight");

            int bays = Mathf.Clamp(Mathf.RoundToInt(size.x / 1.35f), 2, 5);
            for (int i = 1; i < bays; i++)
            {
                float x = -size.x * 0.5f + size.x * i / bays;
                AddFramePart(parent, new Vector3(x, 0f, z - 0.01f),
                    new Vector3(t * 0.72f, size.y * 0.96f, 0.12f), frameMat, "mullion");
            }

            AddFramePart(parent, new Vector3(0f, 0f, z - 0.012f),
                new Vector3(size.x * 0.94f, t * 0.62f, 0.10f), frameMat, "crossbar");
            AddFramePart(parent, new Vector3(0f, -size.y * 0.5f - 0.16f, z - 0.03f),
                new Vector3(size.x + 0.55f, 0.18f, 0.34f), frameMat, "sill");

            int sideBias = seed % 3;
            int slats = 3 + (seed % 2);
            float slatWidth = size.x * (sideBias == 0 ? 0.42f : 0.55f);
            float slatX = sideBias == 1 ? -size.x * 0.18f : size.x * 0.18f;
            for (int i = 0; i < slats; i++)
            {
                float y = size.y * 0.34f - i * 0.22f;
                AddFramePart(parent, new Vector3(slatX, y, z - 0.045f),
                    new Vector3(slatWidth, 0.035f, 0.05f), blindMat, "blindSlat");
            }
        }

        private static void AddFramePart(Transform parent, Vector3 pos, Vector3 scale,
            Material mat, string name)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static Material WorldMaterial(string textureName, Texture2D tex, float emission)
        {
            string key = textureName + "_" + emission.ToString("0.00");
            Material cached;
            if (Materials.TryGetValue(key, out cached) && cached != null) return cached;

            Material template = Resources.Load<Material>(UnlitTextureTemplate);
            Shader shader = template != null ? template.shader : Shader.Find("Unlit/Texture");
            Material mat;
            if (shader != null)
            {
                mat = template != null ? new Material(template) : new Material(shader);
                mat.mainTexture = tex;
                mat.color = Color.white;
            }
            else
            {
                mat = new Material(MaterialFactory.Emissive(Color.white, Color.white, emission));
                mat.mainTexture = tex;
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white * emission);
            }

            mat.name = "StaticIllustration_" + textureName;
            Materials[key] = mat;
            return mat;
        }
    }
}
