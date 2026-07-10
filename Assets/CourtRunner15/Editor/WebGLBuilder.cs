using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CourtRunner15.EditorTools
{
    /// <summary>
    /// Сборка WebGL-версии для https://speshivsud.ru.
    /// Запуск в batchmode:
    /// -executeMethod CourtRunner15.EditorTools.WebGLBuilder.Build
    /// Windows build settings не трогаем: меняются только PlayerSettings.WebGL.*
    /// (они не влияют на standalone) и общий runInBackground.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string ScenePath = "Assets/CourtRunner15/Scenes/Main.unity";
        private const string OutputRelative = "BuildWebGL/webgl";

        [MenuItem("До суда 15 минут/Собрать WebGL")]
        public static void BuildMenu()
        {
            Build();
        }

        public static void Build()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(projectRoot, OutputRelative).Replace('\\', '/');
            Directory.CreateDirectory(outDir);

            if (!File.Exists(Path.Combine(projectRoot, ScenePath)))
            {
                Debug.LogError("[WebGLBuilder] Сцена не найдена: " + ScenePath);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // WebGL-настройки: Brotli + фолбэк-декомпрессия (работает даже без
            // Content-Encoding: br на сервере), наш мобильный шаблон, фон в тон.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:CourtRunner";
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;
            // Тёмный сплэш в тон странице (#0b0b10).
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.043f, 0.043f, 0.063f, 1f);

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary summary = report.summary;
            Debug.LogFormat("[WebGLBuilder] Результат: {0}, размер {1:N0} байт, ошибок {2}.",
                summary.result, summary.totalSize, summary.totalErrors);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
