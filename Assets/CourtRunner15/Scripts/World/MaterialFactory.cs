using System.Collections.Generic;
using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Материалы, шрифт, процедурные текстуры и спрайты.
    /// Автоматически использует URP/Lit, если URP установлен, иначе Standard.
    /// Прозрачные лучи/частицы — Sprites/Default (работает в обоих пайплайнах).
    /// </summary>
    public static class MaterialFactory
    {
        private static Shader _litShader;
        private static Shader _spriteShader;
        private static Shader _additiveShader;
        private static bool _shadersReady;
        private static bool _isUrp;
        private static Font _font;
        private static string _checkMark;
        private const string LitTemplateResource = "CourtRunner15RuntimeLit";
        private const string SpriteTemplateResource = "CourtRunner15RuntimeSprite";
        private const string AdditiveTemplateResource = "CourtRunner15RuntimeAdditive";

        private static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> TexCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static bool IsUrp
        {
            get { EnsureShaders(); return _isUrp; }
        }

        private static void EnsureShaders()
        {
            if (_shadersReady) return;
            _shadersReady = true;

            Material litTemplate = Resources.Load<Material>(LitTemplateResource);
            Material spriteTemplate = Resources.Load<Material>(SpriteTemplateResource);

            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null)
            {
                _litShader = urp;
                _isUrp = true;
            }
            else
            {
                _litShader = Shader.Find("Standard");
                _isUrp = false;
            }
            if (_litShader == null && litTemplate != null)
            {
                _litShader = litTemplate.shader;
                _isUrp = _litShader != null && _litShader.name.IndexOf("Universal Render Pipeline", System.StringComparison.Ordinal) >= 0;
            }

            _spriteShader = Shader.Find("Sprites/Default");
            if (_spriteShader == null && spriteTemplate != null) _spriteShader = spriteTemplate.shader;
            if (_spriteShader == null) _spriteShader = Shader.Find("Unlit/Texture");
            if (_spriteShader == null) _spriteShader = Shader.Find("Unlit/Transparent");
            if (_spriteShader == null) _spriteShader = Shader.Find("Unlit/Color");
            if (_spriteShader == null) _spriteShader = _litShader;
            if (_litShader == null) _litShader = _spriteShader;

            Material additiveTemplate = Resources.Load<Material>(AdditiveTemplateResource);
            _additiveShader = additiveTemplate != null ? additiveTemplate.shader : null;
            if (_additiveShader == null) _additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (_additiveShader == null) _additiveShader = _spriteShader;

            if (_litShader == null || _spriteShader == null)
            {
                Debug.LogError("[CourtRunner15] Runtime shaders are missing. Create runtime material templates before building.");
            }
        }

        // ------------------------------------------------------------------
        // Материалы
        // ------------------------------------------------------------------

        public static Material Solid(Color c, float smoothness, float metallic)
        {
            string key = string.Format("solid_{0}_{1:0.00}_{2:0.00}", ColorKey(c), smoothness, metallic);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_litShader);
            mat.color = c;
            SetSmoothness(mat, smoothness, metallic);
            MatCache[key] = mat;
            return mat;
        }

        public static Material Solid(Color c)
        {
            return Solid(c, 0.45f, 0f);
        }

        public static Material Glossy(Color c)
        {
            return Solid(c, 0.85f, 0.12f);
        }

        public static Material Emissive(Color baseColor, Color emission, float intensity)
        {
            string key = string.Format("emis_{0}_{1}_{2:0.0}", ColorKey(baseColor), ColorKey(emission), intensity);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_litShader);
            mat.color = baseColor;
            SetSmoothness(mat, 0.6f, 0f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emission * intensity);
            }
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Непрозрачный «неон»: сам себе источник вида света.</summary>
        public static Material Neon(Color c)
        {
            return Emissive(c * 0.6f, c, 2.2f);
        }

        public static Material StylizedTile(Color baseColor, Color lineColor, Color accentColor,
            int cells, float smoothness, float metallic)
        {
            cells = Mathf.Clamp(cells, 4, 16);
            string key = string.Format("tile_{0}_{1}_{2}_{3}_{4:0.00}_{5:0.00}",
                ColorKey(baseColor), ColorKey(lineColor), ColorKey(accentColor),
                cells, smoothness, metallic);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_litShader);
            mat.mainTexture = TileTexture(baseColor, lineColor, accentColor, cells);
            mat.color = Color.white;
            SetSmoothness(mat, smoothness, metallic);
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Полупрозрачный незатенённый материал (стекло, дымка).</summary>
        public static Material UnlitTransparent(Color c)
        {
            string key = "unlitT_" + ColorKey(c);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_spriteShader);
            mat.mainTexture = WhiteTexture();
            mat.color = c;
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Вертикальный световой луч (градиент, прозрачный).</summary>
        public static Material BeamMaterial(Color c)
        {
            string key = "beam_" + ColorKey(c);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_spriteShader);
            mat.mainTexture = BeamTexture();
            mat.color = c;
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>
        /// Аддитивное мягкое пятно света (для «отражений» на полу под пикапами
        /// и лампами). Складывается с фоном и цветёт в bloom. Альфа = яркость.
        /// </summary>
        public static Material AdditiveGlow(Color c)
        {
            string key = "addglow_" + ColorKey(c);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_additiveShader);
            mat.mainTexture = SoftDotTexture();
            ApplyTint(mat, c);
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Аддитивный вертикальный луч — светящийся столб под bloom.</summary>
        public static Material AdditiveBeam(Color c)
        {
            string key = "addbeam_" + ColorKey(c);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_additiveShader);
            mat.mainTexture = BeamTexture();
            ApplyTint(mat, c);
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Particles/Additive красится через _TintColor, запасные шейдеры — через color.</summary>
        private static void ApplyTint(Material mat, Color c)
        {
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
            else mat.color = c;
        }

        /// <summary>
        /// «Панорама Москва-Сити на закате»: эмиссивный материал-постер для
        /// панорамных окон офиса. Градиент неба, силуэты башен, тёплые окна.
        /// </summary>
        public static Material SkylineMaterial(int seed)
        {
            string key = "skyline_" + seed;
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_litShader);
            Texture2D tex = SkylineTexture(seed);
            mat.mainTexture = tex;
            mat.color = Color.white;
            SetSmoothness(mat, 0.75f, 0.1f);
            mat.EnableKeyword("_EMISSION");
            // Умеренная эмиссия: при ACES+bloom белое небо иначе выгорает в молоко.
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white * 0.55f);
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            MatCache[key] = mat;
            return mat;
        }

        public static Texture2D SkylineTexture(int seed)
        {
            string key = "skylinetex_" + seed;
            Texture2D cached;
            if (TexCache.TryGetValue(key, out cached)) return cached;

            int w = 256, h = 128;
            Texture2D t = NewTex(w, h);
            System.Random rng = new System.Random(seed);

            // Небо golden hour: сверху дымчато-синее, к горизонту — золото.
            Color top = new Color(0.42f, 0.46f, 0.60f);
            Color mid = new Color(0.95f, 0.68f, 0.40f);
            Color low = new Color(1.00f, 0.58f, 0.30f);
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                Color sky = v > 0.55f
                    ? Color.Lerp(mid, top, (v - 0.55f) / 0.45f)
                    : Color.Lerp(low, mid, v / 0.55f);
                for (int x = 0; x < w; x++) t.SetPixel(x, y, sky);
            }
            // Низкое солнце с ореолом.
            int sunX = 40 + rng.Next(w - 80);
            int sunY = 34 + rng.Next(18);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - sunX) * (x - sunX) + (y - sunY) * (y - sunY));
                    if (d < 26f)
                    {
                        float a = Mathf.Clamp01(1f - d / 26f);
                        Color c = t.GetPixel(x, y);
                        t.SetPixel(x, y, Color.Lerp(c, new Color(1f, 0.92f, 0.7f), a * a));
                    }
                }
            }

            // Дальний ряд башен (светлее), потом ближний (темнее, с окнами).
            for (int layer = 0; layer < 2; layer++)
            {
                bool near = layer == 1;
                Color silh = near ? new Color(0.10f, 0.11f, 0.19f) : new Color(0.24f, 0.24f, 0.34f);
                int towers = near ? 7 : 9;
                for (int i = 0; i < towers; i++)
                {
                    int tw = 14 + rng.Next(20);
                    int tx = rng.Next(w - tw);
                    int th = (near ? 34 : 22) + rng.Next(near ? 58 : 38);
                    bool golden = near && rng.Next(5) == 0; // один «Меркурий»
                    Color body = golden ? new Color(0.55f, 0.36f, 0.16f) : silh;
                    for (int y = 0; y < th; y++)
                    {
                        for (int x = tx; x < tx + tw; x++) t.SetPixel(x, y, body);
                    }
                    if (near)
                    {
                        for (int y = 2; y < th - 2; y += 3)
                        {
                            for (int x = tx + 2; x < tx + tw - 2; x += 3)
                            {
                                if (rng.NextDouble() < 0.30)
                                {
                                    t.SetPixel(x, y, new Color(1f, 0.84f, 0.5f));
                                }
                            }
                        }
                    }
                }
            }
            t.Apply();
            TexCache[key] = t;
            return t;
        }

        /// <summary>Материал частиц: мягкая точка, вершинный цвет, оба пайплайна.</summary>
        public static Material ParticleMaterial()
        {
            string key = "particle";
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_spriteShader);
            mat.mainTexture = SoftDotTexture();
            mat.color = Color.white;
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Материал следа за игроком.</summary>
        public static Material TrailMaterial(Color c)
        {
            string key = "trail_" + ColorKey(c);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_spriteShader);
            mat.mainTexture = WhiteTexture();
            mat.color = c;
            MatCache[key] = mat;
            return mat;
        }

        /// <summary>Фасад здания с «окнами» (текстура генерируется).</summary>
        public static Material WindowsMaterial(Color wall, Color glow, int seed)
        {
            string key = string.Format("win_{0}_{1}_{2}", ColorKey(wall), ColorKey(glow), seed);
            Material cached;
            if (MatCache.TryGetValue(key, out cached)) return cached;

            EnsureShaders();
            Material mat = CreateMaterial(_litShader);
            Texture2D tex = WindowsTexture(wall, glow, seed);
            mat.mainTexture = tex;
            mat.color = Color.white;
            SetSmoothness(mat, 0.35f, 0f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white * 0.25f);
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            MatCache[key] = mat;
            return mat;
        }

        private static void SetSmoothness(Material mat, float smoothness, float metallic)
        {
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness); // Standard
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness); // URP Lit
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        }

        private static Material CreateMaterial(Shader shader)
        {
            if (shader != null) return new Material(shader);

            Material fallback = Resources.GetBuiltinResource<Material>("Default-Material.mat");
            if (fallback != null) return new Material(fallback);

            fallback = Resources.GetBuiltinResource<Material>("Default-Diffuse.mat");
            if (fallback != null) return new Material(fallback);

            throw new System.InvalidOperationException("[CourtRunner15] No runtime material shader is available.");
        }

        private static string ColorKey(Color c)
        {
            return string.Format("{0:0.00}{1:0.00}{2:0.00}{3:0.00}", c.r, c.g, c.b, c.a);
        }

        // ------------------------------------------------------------------
        // Шрифт
        // ------------------------------------------------------------------

        public static Font UiFont
        {
            get
            {
                if (_font == null) _font = LoadFont();
                return _font;
            }
        }

        private static Font LoadFont()
        {
            // Свой TTF (Liberation Sans, OFL) — обязателен для WebGL: встроенный
            // LegacyRuntime там содержит только ASCII-атлас, а кириллицу берёт
            // из шрифтов ОС, которых в браузере нет. На Windows тот же typeface.
            Font f = Resources.Load<Font>("Fonts/LiberationSans-Regular");
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch (System.Exception) { f = null; }
            }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (System.Exception) { f = null; }
            }
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return f;
        }

        /// <summary>Галочка для целей: «✓», если глиф есть в шрифте, иначе «+».</summary>
        public static string CheckMark()
        {
            if (_checkMark == null)
            {
                Font f = UiFont;
                _checkMark = (f != null && f.HasCharacter('✓')) ? "✓" : "+";
            }
            return _checkMark;
        }

        // ------------------------------------------------------------------
        // Текстуры
        // ------------------------------------------------------------------

        private static Texture2D NewTex(int w, int h)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        public static Texture2D WhiteTexture()
        {
            Texture2D cached;
            if (TexCache.TryGetValue("white", out cached)) return cached;
            Texture2D t = NewTex(4, 4);
            Color32[] px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px);
            t.Apply();
            TexCache["white"] = t;
            return t;
        }

        /// <summary>Луч: горизонтальное сужение + затухание вверх.</summary>
        public static Texture2D BeamTexture()
        {
            Texture2D cached;
            if (TexCache.TryGetValue("beam", out cached)) return cached;
            int w = 32, h = 64;
            Texture2D t = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                float fadeY = Mathf.Pow(1f - v, 1.35f);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    float fadeX = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.6f);
                    float a = fadeX * fadeY;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            TexCache["beam"] = t;
            return t;
        }

        public static Texture2D SoftDotTexture()
        {
            Texture2D cached;
            if (TexCache.TryGetValue("softdot", out cached)) return cached;
            int s = 64;
            Texture2D t = NewTex(s, s);
            float half = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            TexCache["softdot"] = t;
            return t;
        }

        public static Texture2D WindowsTexture(Color wall, Color glow, int seed)
        {
            string key = string.Format("wintex_{0}_{1}_{2}", ColorKey(wall), ColorKey(glow), seed);
            Texture2D cached;
            if (TexCache.TryGetValue(key, out cached)) return cached;

            int w = 64, h = 64;
            Texture2D t = NewTex(w, h);
            t.filterMode = FilterMode.Point;
            System.Random rng = new System.Random(seed);
            Color dark = new Color(0.10f, 0.13f, 0.18f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    t.SetPixel(x, y, wall);
                }
            }
            int cols = 6, rows = 8;
            int cw = w / cols, ch = h / rows;
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    bool lit = rng.NextDouble() < 0.45;
                    Color wc = lit ? glow * (0.75f + (float)rng.NextDouble() * 0.35f) : dark;
                    wc.a = 1f;
                    for (int y = cy * ch + 2; y < cy * ch + ch - 2; y++)
                    {
                        for (int x = cx * cw + 2; x < cx * cw + cw - 2; x++)
                        {
                            t.SetPixel(x, y, wc);
                        }
                    }
                }
            }
            t.Apply();
            TexCache[key] = t;
            return t;
        }

        public static Texture2D TileTexture(Color baseColor, Color lineColor, Color accentColor, int cells)
        {
            cells = Mathf.Clamp(cells, 4, 16);
            string key = string.Format("tiletex_{0}_{1}_{2}_{3}",
                ColorKey(baseColor), ColorKey(lineColor), ColorKey(accentColor), cells);
            Texture2D cached;
            if (TexCache.TryGetValue(key, out cached)) return cached;

            int s = 128;
            Texture2D t = NewTex(s, s);
            t.filterMode = FilterMode.Point;
            int cell = Mathf.Max(6, s / cells);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int cx = x % cell;
                    int cy = y % cell;
                    float grain = Mathf.Sin((x + 13) * 12.9898f + (y + 7) * 78.233f) * 43758.5453f;
                    grain -= Mathf.Floor(grain);
                    Color c = baseColor * (0.92f + grain * 0.08f);
                    c.a = 1f;

                    bool seam = cx <= 1 || cy <= 1;
                    bool accent = (x / cell + y / cell) % 5 == 0 && (cx <= 2 || cy <= 2);
                    if (seam) c = Color.Lerp(c, lineColor, accent ? 0.62f : 0.38f);
                    if (accent) c = Color.Lerp(c, accentColor, 0.28f);
                    c.a = 1f;
                    t.SetPixel(x, y, c);
                }
            }
            t.Apply();
            TexCache[key] = t;
            return t;
        }

        // ------------------------------------------------------------------
        // UI-спрайты
        // ------------------------------------------------------------------

        public static Sprite WhiteSprite()
        {
            return GetOrCreateSprite("white", delegate
            {
                Texture2D t = WhiteTexture();
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                    new Vector2(0.5f, 0.5f), 100f);
            });
        }

        /// <summary>Скруглённый прямоугольник (9-slice) для панелей и кнопок.</summary>
        public static Sprite RoundedSprite()
        {
            return GetOrCreateSprite("rounded", delegate
            {
                int s = 64;
                float radius = 18f;
                Texture2D t = NewTex(s, s);
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (s - 1 - radius)));
                        float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (s - 1 - radius)));
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(radius - d + 0.5f);
                        if (d <= 0f) a = 1f;
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Min(1f, a)));
                    }
                }
                t.Apply();
                float b = radius + 4f;
                return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            });
        }

        public static Sprite CircleSprite()
        {
            return GetOrCreateSprite("circle", delegate
            {
                int s = 128;
                Texture2D t = NewTex(s, s);
                float half = (s - 1) * 0.5f;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                        float a = Mathf.Clamp01(half - d + 0.5f);
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                t.Apply();
                return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        /// <summary>Кольцо для круглого индикатора ускорения.</summary>
        public static Sprite RingSprite()
        {
            return GetOrCreateSprite("ring", delegate
            {
                int s = 256;
                Texture2D t = NewTex(s, s);
                float half = (s - 1) * 0.5f;
                float outer = half - 2f;
                float inner = outer - 30f;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                        float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                t.Apply();
                return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        public static Sprite SoftDotSprite()
        {
            return GetOrCreateSprite("softdot", delegate
            {
                Texture2D t = SoftDotTexture();
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        public static Sprite ConfidenceGradientSprite()
        {
            return GetOrCreateSprite("confidenceGradient", delegate
            {
                int w = 256, h = 8;
                Texture2D t = NewTex(w, h);
                Color red = new Color(0.95f, 0.22f, 0.18f, 1f);
                Color gold = new Color(1f, 0.82f, 0.18f, 1f);
                Color green = new Color(0.28f, 0.95f, 0.46f, 1f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float u = (float)x / (w - 1);
                        Color c = u < 0.5f
                            ? Color.Lerp(red, gold, u * 2f)
                            : Color.Lerp(gold, green, (u - 0.5f) * 2f);
                        t.SetPixel(x, y, c);
                    }
                }
                t.Apply();
                return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        public static Sprite VignetteSprite()
        {
            return GetOrCreateSprite("vignette", delegate
            {
                int s = 256;
                Texture2D t = NewTex(s, s);
                float half = (s - 1) * 0.5f;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float nx = (x - half) / half;
                        float ny = (y - half) / half;
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float edge = Mathf.SmoothStep(0.38f, 1.08f, d);
                        t.SetPixel(x, y, new Color(0f, 0f, 0f, edge));
                    }
                }
                t.Apply();
                return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        private static Sprite GetOrCreateSprite(string key, System.Func<Sprite> creator)
        {
            Sprite cached;
            if (SpriteCache.TryGetValue(key, out cached) && cached != null) return cached;
            Sprite s = creator();
            SpriteCache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------
        // Обложки паспорта (карточки босса-судьи): пиксель-арт 64x88
        // ------------------------------------------------------------------

        public static Sprite PassportCoverSprite(string id)
        {
            return GetOrCreateSprite("cover_" + id, delegate
            {
                Texture2D t = PassportCoverTexture(id);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                    new Vector2(0.5f, 0.5f), 100f);
            });
        }

        private static Texture2D PassportCoverTexture(string id)
        {
            int w = 64, h = 88;
            Texture2D t = NewTex(w, h);
            t.filterMode = FilterMode.Point;
            Color dark = new Color(0.12f, 0.11f, 0.12f, 1f);
            Color gold = new Color(0.95f, 0.78f, 0.30f, 1f);
            Color white = new Color(0.96f, 0.96f, 0.95f, 1f);

            switch (id)
            {
                case "flag":
                {
                    // Триколор + золотой герб — та самая правильная обложка.
                    CoverFill(t, Color.white, new Color(0.55f, 0.55f, 0.58f));
                    FillRect(t, 3, 31, 60, 58, new Color(0.16f, 0.32f, 0.72f));
                    FillRect(t, 3, 3, 60, 30, new Color(0.82f, 0.12f, 0.14f));
                    FillCircle(t, 32, 44, 8, gold);
                    FillCircle(t, 32, 44, 4, new Color(0.75f, 0.58f, 0.16f));
                    break;
                }
                case "blue":
                {
                    CoverFill(t, new Color(0.15f, 0.25f, 0.58f), new Color(0.08f, 0.13f, 0.32f));
                    FillCircle(t, 32, 56, 7, gold);
                    FillRect(t, 18, 30, 46, 33, gold);
                    FillRect(t, 22, 22, 42, 25, gold);
                    break;
                }
                case "lightblue":
                {
                    CoverFill(t, new Color(0.55f, 0.78f, 0.95f), new Color(0.3f, 0.5f, 0.7f));
                    FillCircle(t, 32, 54, 7, white);
                    FillRect(t, 20, 28, 44, 31, white);
                    break;
                }
                case "pink":
                {
                    CoverFill(t, new Color(0.96f, 0.62f, 0.78f), new Color(0.72f, 0.36f, 0.52f));
                    // Сердечко.
                    FillCircle(t, 26, 52, 6, white);
                    FillCircle(t, 38, 52, 6, white);
                    for (int y = 36; y <= 52; y++)
                    {
                        int half = (int)((y - 36) * 0.85f);
                        FillRect(t, 32 - half, y, 32 + half, y, white);
                    }
                    break;
                }
                case "black":
                {
                    CoverFill(t, new Color(0.10f, 0.10f, 0.12f), new Color(0.32f, 0.32f, 0.36f));
                    FillCircle(t, 32, 52, 7, new Color(0.72f, 0.72f, 0.76f));
                    FillRect(t, 20, 26, 44, 29, new Color(0.55f, 0.55f, 0.6f));
                    break;
                }
                case "skeleton":
                {
                    // Зелёная со скелетом — привет референсу из зала ВС.
                    CoverFill(t, new Color(0.16f, 0.48f, 0.22f), new Color(0.07f, 0.26f, 0.10f));
                    FillCircle(t, 32, 58, 10, white);
                    FillRect(t, 27, 42, 37, 49, white);
                    FillCircle(t, 28, 59, 3, dark);
                    FillCircle(t, 37, 59, 3, dark);
                    FillRect(t, 31, 51, 33, 54, dark);
                    // Скрещённые кости.
                    for (int i = -11; i <= 11; i++)
                    {
                        FillRect(t, 32 + i - 1, 30 + i / 2, 32 + i + 1, 32 + i / 2, white);
                        FillRect(t, 32 + i - 1, 30 - i / 2, 32 + i + 1, 32 - i / 2, white);
                    }
                    break;
                }
                case "sponge":
                {
                    // Жёлтая мульт-губка: глазастая и дырчатая.
                    CoverFill(t, new Color(0.97f, 0.85f, 0.22f), new Color(0.72f, 0.58f, 0.10f));
                    Color pore = new Color(0.82f, 0.66f, 0.12f);
                    FillCircle(t, 14, 70, 3, pore);
                    FillCircle(t, 50, 66, 4, pore);
                    FillCircle(t, 12, 34, 4, pore);
                    FillCircle(t, 52, 24, 3, pore);
                    FillCircle(t, 20, 14, 3, pore);
                    FillCircle(t, 26, 58, 7, white);
                    FillCircle(t, 40, 58, 7, white);
                    FillCircle(t, 26, 58, 3, new Color(0.25f, 0.5f, 0.9f));
                    FillCircle(t, 40, 58, 3, new Color(0.25f, 0.5f, 0.9f));
                    // Улыбка с зубами.
                    for (int x = 20; x <= 44; x++)
                    {
                        int dx = x - 32;
                        int y = 40 - (int)Mathf.Sqrt(Mathf.Max(0f, 100f - dx * dx)) / 2;
                        FillRect(t, x, y, x, y + 1, dark);
                    }
                    FillRect(t, 28, 36, 31, 40, white);
                    FillRect(t, 33, 36, 36, 40, white);
                    break;
                }
                case "anime":
                {
                    // Аниме: чёлка и огромные блестящие глаза.
                    CoverFill(t, new Color(0.93f, 0.90f, 0.97f), new Color(0.6f, 0.55f, 0.7f));
                    Color hairC = new Color(0.94f, 0.52f, 0.70f);
                    FillRect(t, 3, 70, 60, 84, hairC);
                    for (int i = 0; i < 4; i++)
                    {
                        int cx = 11 + i * 14;
                        for (int y = 58; y <= 70; y++)
                        {
                            int half = (y - 58) / 2 + 1;
                            FillRect(t, cx - half, y, cx + half, y, hairC);
                        }
                    }
                    FillEllipse(t, 24, 44, 8, 11, white);
                    FillEllipse(t, 42, 44, 8, 11, white);
                    FillEllipse(t, 24, 43, 5, 8, new Color(0.20f, 0.7f, 0.65f));
                    FillEllipse(t, 42, 43, 5, 8, new Color(0.20f, 0.7f, 0.65f));
                    FillCircle(t, 24, 42, 2, dark);
                    FillCircle(t, 42, 42, 2, dark);
                    FillCircle(t, 26, 48, 2, white);
                    FillCircle(t, 44, 48, 2, white);
                    FillRect(t, 30, 26, 36, 28, new Color(0.75f, 0.35f, 0.4f));
                    break;
                }
                case "rappers":
                {
                    // Рэперы: цепь, кулон-доллар, кепка.
                    CoverFill(t, new Color(0.13f, 0.10f, 0.18f), new Color(0.05f, 0.04f, 0.08f));
                    FillRect(t, 18, 70, 46, 78, new Color(0.85f, 0.15f, 0.18f));
                    FillRect(t, 12, 68, 32, 71, new Color(0.7f, 0.10f, 0.13f));
                    for (int x = 18; x <= 46; x += 4)
                    {
                        int dx = x - 32;
                        int y = 56 + (dx * dx) / 20;
                        FillCircle(t, x, y, 2, gold);
                    }
                    FillCircle(t, 32, 42, 8, gold);
                    FillRect(t, 31, 34, 33, 50, new Color(0.4f, 0.3f, 0.08f));
                    FillRect(t, 27, 45, 37, 47, new Color(0.4f, 0.3f, 0.08f));
                    FillRect(t, 27, 38, 37, 40, new Color(0.4f, 0.3f, 0.08f));
                    break;
                }
                case "lips":
                {
                    // Розовые губы-поцелуй на тёмной обложке.
                    CoverFill(t, new Color(0.11f, 0.10f, 0.13f), new Color(0.30f, 0.28f, 0.33f));
                    Color lip = new Color(0.95f, 0.42f, 0.60f);
                    Color lipDeep = new Color(0.80f, 0.26f, 0.46f);
                    Color mouth = new Color(0.42f, 0.10f, 0.20f);
                    // Верхняя губа — «лук Купидона» из двух половинок.
                    FillEllipse(t, 24, 51, 9, 6, lip);
                    FillEllipse(t, 40, 51, 9, 6, lip);
                    // Нижняя губа полнее и темнее.
                    FillEllipse(t, 32, 42, 16, 8, lipDeep);
                    // Линия рта и блик.
                    FillRect(t, 16, 47, 48, 48, mouth);
                    FillCircle(t, 38, 39, 2, new Color(1f, 0.75f, 0.85f));
                    break;
                }
                default:
                {
                    CoverFill(t, new Color(0.5f, 0.5f, 0.5f), dark);
                    break;
                }
            }
            t.Apply();
            return t;
        }

        /// <summary>Заливка обложки: скруглённые углы + рамка.</summary>
        private static void CoverFill(Texture2D t, Color face, Color border)
        {
            int w = t.width, h = t.height;
            const int r = 7;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = x < r ? r - x : (x >= w - r ? x - (w - 1 - r) : 0);
                    int dy = y < r ? r - y : (y >= h - r ? y - (h - 1 - r) : 0);
                    if (dx * dx + dy * dy > r * r)
                    {
                        t.SetPixel(x, y, clear);
                        continue;
                    }
                    bool edge = x < 3 || x >= w - 3 || y < 3 || y >= h - 3 ||
                                dx * dx + dy * dy > (r - 3) * (r - 3) && (dx > 0 && dy > 0);
                    t.SetPixel(x, y, edge ? border : face);
                }
            }
        }

        private static void FillRect(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(t.height - 1, y1); y++)
            {
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(t.width - 1, x1); x++)
                {
                    t.SetPixel(x, y, c);
                }
            }
        }

        private static void FillCircle(Texture2D t, int cx, int cy, int r, Color c)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= t.width || y >= t.height) continue;
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r * r) t.SetPixel(x, y, c);
                }
            }
        }

        private static void FillEllipse(Texture2D t, int cx, int cy, int rx, int ry, Color c)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || y < 0 || x >= t.width || y >= t.height) continue;
                    float nx = (x - cx) / (float)rx;
                    float ny = (y - cy) / (float)ry;
                    if (nx * nx + ny * ny <= 1f) t.SetPixel(x, y, c);
                }
            }
        }

        /// <summary>Толстый отрезок «молнии» между двумя точками.</summary>
        private static void DrawBoltSegment(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float k = steps == 0 ? 0f : (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, k));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, k));
                FillRect(t, x - 2, y - 1, x + 2, y + 1, c);
            }
        }
    }
}
