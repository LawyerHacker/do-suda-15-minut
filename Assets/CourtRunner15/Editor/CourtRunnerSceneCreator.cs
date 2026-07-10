using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CourtRunner15.EditorTools
{
    /// <summary>
    /// Меню «До суда 15 минут → Создать или обновить сцену»:
    /// создаёт Main.unity с камерой, светом и bootstrap-объектом.
    /// </summary>
    public static class CourtRunnerSceneCreator
    {
        public const string ScenePath = "Assets/CourtRunner15/Scenes/Main.unity";

        [MenuItem("До суда 15 минут/Создать или обновить сцену")]
        public static void CreateOrUpdateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[До суда 15 минут] Создание сцены отменено пользователем.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Камера.
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 3.4f, -5.6f);
            camGo.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.86f, 0.74f);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 260f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.allowDynamicResolution = false;
            cam.renderingPath = RenderingPath.Forward;

            // Ключевой свет.
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.95f, 0.84f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.5f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Точка входа игры.
            GameObject boot = new GameObject("CourtRunnerBootstrap");
            boot.AddComponent<CourtRunnerBootstrap>();

            System.IO.Directory.CreateDirectory(
                System.IO.Path.Combine(Application.dataPath, "CourtRunner15/Scenes"));

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (saved)
            {
                AddSceneToBuildSettings();
                AssetDatabase.Refresh();
                Debug.Log("[До суда 15 минут] Сцена создана и сохранена: " + ScenePath +
                          ". Нажмите Play для запуска игры.");
            }
            else
            {
                Debug.LogError("[До суда 15 минут] Не удалось сохранить сцену: " + ScenePath);
            }
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath) return;
            }
            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++) updated[i] = scenes[i];
            updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
