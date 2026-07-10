using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CourtRunner15.EditorTools
{
    /// <summary>
    /// Проверка проекта: сцена, данные уровней, шрифт, ключевые типы.
    /// Запуск из меню или в batchmode:
    /// -executeMethod CourtRunner15.EditorTools.CourtRunnerValidator.RunBatch
    /// </summary>
    public static class CourtRunnerValidator
    {
        [MenuItem("До суда 15 минут/Проверить проект")]
        public static void ValidateMenu()
        {
            Validate();
        }

        public static bool Validate()
        {
            List<string> passed = new List<string>();
            List<string> failed = new List<string>();

            // 1. Сцена.
            if (File.Exists(CourtRunnerSceneCreator.ScenePath))
            {
                passed.Add("Сцена Main.unity найдена: " + CourtRunnerSceneCreator.ScenePath);
                string sceneText = File.ReadAllText(CourtRunnerSceneCreator.ScenePath);
                string metaPath = "Assets/CourtRunner15/Scripts/Bootstrap/CourtRunnerBootstrap.cs.meta";
                if (File.Exists(metaPath))
                {
                    string metaText = File.ReadAllText(metaPath);
                    int gi = metaText.IndexOf("guid:");
                    string guid = gi >= 0 ? metaText.Substring(gi + 5).Trim().Split('\n')[0].Trim() : "";
                    if (guid.Length == 32 && sceneText.Contains(guid))
                    {
                        passed.Add("Сцена ссылается на CourtRunnerBootstrap (guid совпадает).");
                    }
                    else if (guid.Length == 32)
                    {
                        failed.Add("Сцена НЕ ссылается на bootstrap-скрипт: пересоздайте сцену через меню «До суда 15 минут».");
                    }
                }
            }
            else
            {
                failed.Add("Сцена не найдена. Меню: «До суда 15 минут → Создать или обновить сцену».");
            }

            // 2. Ключевые типы (компилируются и доступны).
            System.Type[] required =
            {
                typeof(CourtRunner15.GameManager),
                typeof(CourtRunner15.LevelManager),
                typeof(CourtRunner15.LevelCatalog),
                typeof(CourtRunner15.PlayerController),
                typeof(CourtRunner15.PlayerVisual),
                typeof(CourtRunner15.SpawnManager),
                typeof(CourtRunner15.WorldSegmentManager),
                typeof(CourtRunner15.LevelVisualBuilder),
                typeof(CourtRunner15.ProceduralModelFactory),
                typeof(CourtRunner15.StaticIllustrationFactory),
                typeof(CourtRunner15.StaticIllustrationLibrary),
                typeof(CourtRunner15.MaterialFactory),
                typeof(CourtRunner15.UIManager),
                typeof(CourtRunner15.ScreenController),
                typeof(CourtRunner15.CircularBoostIndicator),
                typeof(CourtRunner15.CameraController),
                typeof(CourtRunner15.EffectsManager),
                typeof(CourtRunner15.SpeedTrail),
                typeof(CourtRunner15.FloatingPickupEffect),
                typeof(CourtRunner15.HitFlashEffect),
                typeof(CourtRunner15.AudioManager),
                typeof(CourtRunner15.CourtRunnerBootstrap),
                typeof(CourtRunner15.LaneObject),
                typeof(CourtRunner15.Obstacle),
                typeof(CourtRunner15.Pickup)
            };
            passed.Add("Ключевые скрипты скомпилированы: " + required.Length + " типов доступно.");

            // 3. Данные уровней.
            for (int i = 0; i < CourtRunner15.LevelCatalog.LevelCount; i++)
            {
                CourtRunner15.LevelData d = CourtRunner15.LevelCatalog.Get(i);
                List<string> issues = new List<string>();
                if (d.Distance < 500f) issues.Add("дистанция подозрительно мала");
                if (d.BaseSpeed <= 0f) issues.Add("скорость не задана");
                if (d.WaveInterval <= 0.3f) issues.Add("интервал волн слишком мал");
                if (d.Palette == null) issues.Add("нет палитры");
                if (d.Pickups == null || d.Pickups.Length < 3) issues.Add("мало бустов");
                if (d.Goals == null || d.Goals.Length != 3) issues.Add("целей должно быть 3");
                if (d.WallSigns == null || d.WallSigns.Length < 3) issues.Add("мало вывесок");

                int blocks = 0, passable = 0;
                if (d.Obstacles != null)
                {
                    for (int o = 0; o < d.Obstacles.Length; o++)
                    {
                        if (d.Obstacles[o].Kind == CourtRunner15.ObstacleKind.Block) blocks++;
                        else passable++;
                        if (d.Obstacles[o].PenaltyMin > d.Obstacles[o].PenaltyMax)
                            issues.Add("штраф min>max у «" + d.Obstacles[o].Label + "»");
                    }
                }
                if (blocks == 0) issues.Add("нет блокирующих препятствий");
                if (passable == 0) issues.Add("нет препятствий для прыжка/подката — волны будут нечестными");

                if (issues.Count == 0)
                {
                    passed.Add(string.Format(
                        "Уровень {0} «{1}»: {2} м, скорость {3}, волны {4} с, препятствий {5}, бустов {6}.",
                        i + 1, d.Title, d.Distance, d.BaseSpeed, d.WaveInterval,
                        d.Obstacles.Length, d.Pickups.Length));
                }
                else
                {
                    failed.Add("Уровень " + (i + 1) + ": " + string.Join("; ", issues.ToArray()));
                }
            }

            // 4. Шрифт с кириллицей.
            Font font = CourtRunner15.MaterialFactory.UiFont;
            if (font != null) passed.Add("Шрифт UI загружен: " + font.name);
            else failed.Add("Не удалось получить шрифт UI.");

            // 5. Боссы: по одному на уровень, корректность вариантов по сценарию.
            if (CourtRunner15.BossCatalog.Count == CourtRunner15.LevelCatalog.LevelCount)
            {
                passed.Add("Боссов в каталоге: " + CourtRunner15.BossCatalog.Count + " (по одному на уровень).");
            }
            else
            {
                failed.Add("Боссов " + CourtRunner15.BossCatalog.Count + ", а уровней " + CourtRunner15.LevelCatalog.LevelCount + ".");
            }
            for (int i = 0; i < CourtRunner15.BossCatalog.Count; i++)
            {
                CourtRunner15.BossData b = CourtRunner15.BossCatalog.Get(i);
                List<string> bossIssues = new List<string>();
                if (string.IsNullOrEmpty(b.Name)) bossIssues.Add("нет имени");
                if (string.IsNullOrEmpty(b.IntroLine)) bossIssues.Add("нет реплики");
                if (string.IsNullOrEmpty(b.WinLine)) bossIssues.Add("нет реакции на победу");
                if (string.IsNullOrEmpty(b.DefeatLine)) bossIssues.Add("нет реплики поражения");
                if (b.Attempts < 1) bossIssues.Add("попыток меньше 1");
                if (b.Options == null || b.Options.Length < 2) bossIssues.Add("меньше 2 вариантов");
                int correct = 0;
                if (b.Options != null)
                {
                    for (int o = 0; o < b.Options.Length; o++)
                    {
                        if (b.Options[o].Correct) correct++;
                        if (string.IsNullOrEmpty(b.Options[o].Text)) bossIssues.Add("пустой вариант №" + (o + 1));
                    }
                }
                if (correct == 0) bossIssues.Add("нет правильного варианта");
                if (b.WrongReactions == null || b.WrongReactions.Length == 0) bossIssues.Add("нет пула реакций на ошибку");
                // Сценарные требования.
                if (i == 0 && correct != 1) bossIssues.Add("у судьи должен быть ровно 1 правильный ответ (флаг России), а их " + correct);
                if (i == 1 && correct < 2) bossIssues.Add("у депутата должно быть минимум 2 правильных ответа, а их " + correct);
                if (i == 2 && b.Options != null && correct != b.Options.Length) bossIssues.Add("у председателя все варианты должны быть правильными");

                if (bossIssues.Count == 0)
                {
                    passed.Add(string.Format("Босс {0} «{1}»: вариантов {2}, правильных {3}, попыток {4}.",
                        i + 1, b.Name, b.Options.Length, correct, b.Attempts));
                }
                else
                {
                    failed.Add("Босс " + (i + 1) + ": " + string.Join("; ", bossIssues.ToArray()));
                }
            }

            // 6. Ассеты пост-обработки (PPSv2).
            if (File.Exists("Assets/CourtRunner15/Resources/PostProcessResources.asset"))
            {
                passed.Add("PostProcessResources скопирован в Resources (bloom/grading доступны в билде).");
            }
            else
            {
                failed.Add("Нет Assets/CourtRunner15/Resources/PostProcessResources.asset — запустите «Создать runtime-материалы» (нужен пакет com.unity.postprocessing).");
            }
            if (File.Exists("Assets/CourtRunner15/Resources/CourtRunner15RuntimeAdditive.mat"))
            {
                passed.Add("Аддитивный runtime-материал найден (световые пятна/лучи).");
            }
            else
            {
                failed.Add("Нет CourtRunner15RuntimeAdditive.mat — запустите «Создать runtime-материалы».");
            }
            if (File.Exists("Assets/CourtRunner15/Resources/CourtRunner15RuntimeUnlitTexture.mat"))
            {
                passed.Add("Unlit/Texture runtime-материал найден (фото-панели без realtime lights).");
            }
            else
            {
                failed.Add("Нет CourtRunner15RuntimeUnlitTexture.mat — запустите «Создать runtime-материалы».");
            }

            // 7. Статичные фотореалистичные иллюстрации: импорт и runtime-каталог.
            string[] illustrations =
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
            int illustrationsOk = 0;
            for (int i = 0; i < illustrations.Length; i++)
            {
                string name = illustrations[i];
                bool cutIn = name.EndsWith("_CutIn");
                int expectedMax = cutIn ? 1024 : 2048;
                string path = File.Exists("Assets/CourtRunner15/Art/StaticIllustrations/" + name + ".jpg")
                    ? "Assets/CourtRunner15/Art/StaticIllustrations/" + name + ".jpg"
                    : "Assets/CourtRunner15/Art/StaticIllustrations/" + name + ".png";

                if (!File.Exists(path))
                {
                    failed.Add("Нет статичной иллюстрации: " + name);
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    failed.Add("Не удалось прочитать importer для " + path);
                    continue;
                }

                TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
                int maxSize = standalone.overridden ? standalone.maxTextureSize : importer.maxTextureSize;
                if (maxSize != expectedMax)
                {
                    failed.Add(name + ": Max Size должен быть " + expectedMax + ", сейчас " + maxSize);
                }
                else if (importer.mipmapEnabled == cutIn)
                {
                    failed.Add(name + ": mip maps должны быть " + (!cutIn ? "включены" : "выключены"));
                }
                else
                {
                    illustrationsOk++;
                }
            }

            CourtRunner15.StaticIllustrationLibrary lib =
                AssetDatabase.LoadAssetAtPath<CourtRunner15.StaticIllustrationLibrary>(
                    "Assets/CourtRunner15/Resources/StaticIllustrationLibrary.asset");
            if (lib != null && lib.Count == illustrations.Length)
            {
                passed.Add("StaticIllustrationLibrary собран: " + lib.Count + " иллюстраций.");
            }
            else
            {
                failed.Add("StaticIllustrationLibrary не собран или неполный.");
            }
            if (illustrationsOk == illustrations.Length)
            {
                passed.Add("Импорт статичных иллюстраций настроен: world=2048+mips, cut-in=1024 без mips.");
            }

            // Отчёт.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("========== ПРОВЕРКА «ДО СУДА 15 МИНУТ» ==========");
            for (int i = 0; i < passed.Count; i++) sb.AppendLine("  [OK] " + passed[i]);
            for (int i = 0; i < failed.Count; i++) sb.AppendLine("  [ОШИБКА] " + failed[i]);
            sb.AppendLine(failed.Count == 0
                ? "ИТОГ: все проверки пройдены."
                : "ИТОГ: есть проблемы — " + failed.Count + " шт.");
            sb.AppendLine("=================================================");

            if (failed.Count == 0) Debug.Log(sb.ToString());
            else Debug.LogError(sb.ToString());
            return failed.Count == 0;
        }

        /// <summary>Для Unity batchmode: код выхода 0 — успех, 2 — ошибки.</summary>
        public static void RunBatch()
        {
            // Runtime-ассеты (материалы, PostProcessResources) — идемпотентно.
            CourtRunnerRuntimeAssets.EnsureRuntimeAssetsBatch();
            bool ok = Validate();
            EditorApplication.Exit(ok ? 0 : 2);
        }
    }
}
