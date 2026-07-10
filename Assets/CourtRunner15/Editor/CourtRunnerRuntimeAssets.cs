using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CourtRunner15.EditorTools
{
    public static class CourtRunnerRuntimeAssets
    {
        private const string ResourcesDir = "Assets/CourtRunner15/Resources";
        private const string LitPath = ResourcesDir + "/CourtRunner15RuntimeLit.mat";
        private const string SpritePath = ResourcesDir + "/CourtRunner15RuntimeSprite.mat";
        private const string AdditivePath = ResourcesDir + "/CourtRunner15RuntimeAdditive.mat";
        private const string UnlitTexturePath = ResourcesDir + "/CourtRunner15RuntimeUnlitTexture.mat";
        private const string PostFxSrcPath = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";
        private const string PostFxDstPath = ResourcesDir + "/PostProcessResources.asset";
        private const string IllustrationsDir = "Assets/CourtRunner15/Art/StaticIllustrations";
        private const string StaticIllustrationLibraryPath = ResourcesDir + "/StaticIllustrationLibrary.asset";
        private static readonly string[] RequiredIllustrations =
        {
            "MoscowCity_Panorama",
            "ASGM_Facade",
            "Rosreestr_Facade",
            "Rosreestr_Sign",
            "CourtQueue_Hall",
            "BossJudge_CutIn",
            "BossDeputy_CutIn",
            "BossChairman_CutIn"
        };

        [MenuItem("До суда 15 минут/Создать runtime-материалы")]
        public static void EnsureRuntimeAssets()
        {
            EnsureResourcesFolder();
            CreateOrUpdateMaterial(LitPath, FindShader("Universal Render Pipeline/Lit", "Standard", "Diffuse", "Unlit/Color"), Color.white);
            CreateOrUpdateMaterial(SpritePath, FindShader("Sprites/Default", "Unlit/Transparent", "Unlit/Texture", "Unlit/Color"), Color.white);
            CreateOrUpdateMaterial(AdditivePath, FindShader("Legacy Shaders/Particles/Additive", "Particles/Additive", "Sprites/Default"), Color.white);
            CreateOrUpdateMaterial(UnlitTexturePath, FindShader("Unlit/Texture", "Legacy Shaders/Unlit/Texture", "Sprites/Default", "Standard"), Color.white);
            CopyPostProcessResources();
            EnsureStaticIllustrations();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[До суда 15 минут] Runtime-материалы созданы или обновлены.");
        }

        /// <summary>
        /// Кладёт PostProcessResources из пакета в Resources, чтобы шейдеры
        /// PPSv2 попали в билд и слой можно было поднять кодом в рантайме.
        /// </summary>
        private static void CopyPostProcessResources()
        {
            if (AssetDatabase.LoadMainAssetAtPath(PostFxDstPath) != null) return;
            if (AssetDatabase.LoadMainAssetAtPath(PostFxSrcPath) == null)
            {
                Debug.LogWarning("[До суда 15 минут] Пакет com.unity.postprocessing не найден — PostProcessResources не скопирован.");
                return;
            }
            if (!AssetDatabase.CopyAsset(PostFxSrcPath, PostFxDstPath))
            {
                Debug.LogError("[До суда 15 минут] Не удалось скопировать PostProcessResources в Resources.");
            }
        }

        public static void EnsureRuntimeAssetsBatch()
        {
            EnsureRuntimeAssets();
        }

        private static void EnsureResourcesFolder()
        {
            if (AssetDatabase.IsValidFolder(ResourcesDir)) return;
            string absolute = Path.Combine(Application.dataPath, "CourtRunner15/Resources");
            Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();
        }

        private static void EnsureStaticIllustrations()
        {
            string absolute = Path.Combine(Application.dataPath, "CourtRunner15/Art/StaticIllustrations");
            Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();

            List<StaticIllustrationLibrary.Entry> entries = new List<StaticIllustrationLibrary.Entry>();
            for (int i = 0; i < RequiredIllustrations.Length; i++)
            {
                string name = RequiredIllustrations[i];
                string path = FindIllustrationPath(name);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning("[До суда 15 минут] Нет статичной иллюстрации: " + name);
                    continue;
                }

                ConfigureIllustrationImporter(path);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                {
                    Debug.LogWarning("[До суда 15 минут] Иллюстрация не импортировалась как Texture2D: " + path);
                    continue;
                }

                StaticIllustrationLibrary.Entry entry = new StaticIllustrationLibrary.Entry();
                entry.Name = name;
                entry.Texture = tex;
                entries.Add(entry);
            }

            StaticIllustrationLibrary lib =
                AssetDatabase.LoadAssetAtPath<StaticIllustrationLibrary>(StaticIllustrationLibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<StaticIllustrationLibrary>();
                AssetDatabase.CreateAsset(lib, StaticIllustrationLibraryPath);
            }
            lib.Entries = entries.ToArray();
            EditorUtility.SetDirty(lib);
        }

        private static string FindIllustrationPath(string name)
        {
            string jpg = IllustrationsDir + "/" + name + ".jpg";
            if (File.Exists(jpg)) return jpg;
            string png = IllustrationsDir + "/" + name + ".png";
            if (File.Exists(png)) return png;
            return null;
        }

        private static void ConfigureIllustrationImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool cutIn = Path.GetFileNameWithoutExtension(path).EndsWith("_CutIn");
            int maxSize = cutIn ? 1024 : 2048;

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = !cutIn;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = maxSize;
            standalone.textureCompression = TextureImporterCompression.CompressedHQ;
            standalone.format = TextureImporterFormat.Automatic;
            standalone.compressionQuality = 80;
            importer.SetPlatformTextureSettings(standalone);
            importer.maxTextureSize = maxSize;

            importer.SaveAndReimport();
        }

        private static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null) return shader;
            }
            return Shader.Find("Hidden/InternalErrorShader");
        }

        private static void CreateOrUpdateMaterial(string path, Shader shader, Color color)
        {
            if (shader == null)
            {
                Debug.LogError("[До суда 15 минут] Не найден ни один подходящий шейдер для " + path);
                return;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
        }
    }
}
