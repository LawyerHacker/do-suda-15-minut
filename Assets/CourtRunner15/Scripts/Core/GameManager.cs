using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Центральная точка игры: машина состояний, счёт, уверенность,
    /// создание и связывание всех систем. Работает в любой сцене.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Состояние")]
        public GameState State = GameState.MainMenu;
        public int CurrentLevelIndex;
        public int UnlockedLevels = 1; // 1..3
        public string DefeatReason = "";

        [Header("Босс")]
        public BossData CurrentBoss;
        public BossOption[] CurrentBossOptions; // порядок показа (перемешан)
        public int BossAttemptsLeft;

        [Header("Показатели")]
        public float Confidence = 60f;
        public int Combo;
        public int Multiplier = 1;
        public int MaxMultiplierReached = 1;
        public bool ShieldActive;
        public float WorldSpeed;
        public int SessionTotalScore;
        public int PickupsCollectedOnLevel;
        public int HitsOnLevel;

        [Header("Системы")]
        public LevelManager Levels;
        public PlayerController Player;
        public SpawnManager Spawner;
        public WorldSegmentManager Segments;
        public LevelVisualBuilder Visuals;
        public UIManager UI;
        public EffectsManager Effects;
        public AudioManager Audio;
        public CameraController CameraCtrl;
        public Light KeyLight;

        private float _scoreF;
        private float _boostBlend;
        private float _shieldCooldownUntil;
        // Таймеры суперпризов и дебаффов.
        private float _slowFactor = 1f;
        private float _slowUntil;
        private float _superSpeedUntil;
        private float _ramUntil;
        private float _weakJumpUntil;
        private float _magnetUntil;
        private float _aiUntil;
        private float _aiBanUntil;
        private float _slowmoRestoreAt = -1f;
        // Босс: сцена в мире, отложенное разрешение исхода (после реплики).
        private GameObject _bossStage;
        private float _bossResolveAt = -1f;
        private bool _bossWon;
        private bool[] _bossOptionUsed; // защита от повторного ответа той же карточкой (хоткеи)
        private const float MenuScrollSpeed = 4f;
        private const string PrefUnlocked = "CR15_Unlocked";
        private const string PrefBest = "CR15_Best_";
        private const int BaselinePixelLightCount = 1;
        private const float BaselineShadowDistance = 45f;
        private const int BaselineShadowCascades = 2;
        private const int BaselineMsaa = 2;
        private const int BaselineVSync = 1;
        private const int BaselineFrameRate = 60;

        public int Score
        {
            get { return Mathf.RoundToInt(_scoreF); }
        }

        public float BoostBlend
        {
            get { return _boostBlend; }
        }

        public bool BoostActive
        {
            get { return _boostBlend > 0.5f; }
        }

        /// <summary>«Исполнительный лист»: игрок сносит препятствия без штрафа.</summary>
        public bool RamActive
        {
            get { return Time.time < _ramUntil; }
        }

        /// <summary>«700 страниц PDF»: прыжок временно слабее.</summary>
        public bool WeakJumpActive
        {
            get { return Time.time < _weakJumpUntil; }
        }

        /// <summary>«Разъяснения Пленума»: бусты притягиваются к игроку.</summary>
        public bool MagnetActive
        {
            get { return Time.time < _magnetUntil; }
        }

        /// <summary>Суперускорение от «Ностальгии по ВАС РФ».</summary>
        public bool SuperSpeedActive
        {
            get { return Time.time < _superSpeedUntil; }
        }

        /// <summary>«Юрист освоил ИИ»: очки за бусты удваиваются.</summary>
        public bool AiActive
        {
            get { return Time.time < _aiUntil; }
        }

        /// <summary>«Подписка на ИИ забанена»: очки за бусты вдвое меньше.</summary>
        public bool AiBanActive
        {
            get { return Time.time < _aiBanUntil; }
        }

        /// <summary>Гарантирует существование игры в сцене (вызывается bootstrap-ом).</summary>
        public static GameManager EnsureExists()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("CourtRunner15");
            GameManager gm = go.AddComponent<GameManager>();
            return gm;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            UnlockedLevels = Mathf.Clamp(PlayerPrefs.GetInt(PrefUnlocked, 1), 1, LevelCatalog.LevelCount);
            InitializeSystems();
            ShowMainMenu();
            TryApplyStartupArgs();
        }

        private void InitializeSystems()
        {
            ApplyGtx1050TiGraphicsBaseline();

            Audio = gameObject.AddComponent<AudioManager>();
            Audio.Init();

            Levels = gameObject.AddComponent<LevelManager>();

            // Камера: используем существующую или создаём.
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            ApplyCameraBaseline(cam);
            CameraCtrl = cam.GetComponent<CameraController>();
            if (CameraCtrl == null) CameraCtrl = cam.gameObject.AddComponent<CameraController>();
            PostFxController.Attach(cam);
            gameObject.AddComponent<FpsMeter>();

            // Ключевой свет: используем существующий направленный или создаём.
            ConfigureKeyLight();
            LogGraphicsBaseline(cam);

            // Мир.
            GameObject world = new GameObject("World");
            Visuals = world.AddComponent<LevelVisualBuilder>();
            Visuals.Init(this);
            Segments = world.AddComponent<WorldSegmentManager>();
            Segments.Init(this, Visuals);
            Spawner = world.AddComponent<SpawnManager>();
            Spawner.Init(this);

            // Игрок.
            GameObject playerGo = new GameObject("Player");
            Player = playerGo.AddComponent<PlayerController>();
            Player.Init(this);
            Player.SetVisible(false);

            CameraCtrl.Init(this, Player.transform);

            // Эффекты (нужна камера).
            Effects = gameObject.AddComponent<EffectsManager>();
            Effects.Init(this, cam);

            // Интерфейс.
            GameObject uiGo = new GameObject("UI");
            UI = uiGo.AddComponent<UIManager>();
            UI.Init(this);
        }

        private void ApplyGtx1050TiGraphicsBaseline()
        {
            if (QualitySettings.pixelLightCount > BaselinePixelLightCount)
            {
                QualitySettings.pixelLightCount = BaselinePixelLightCount;
            }

            if (QualitySettings.shadowDistance <= 0f ||
                QualitySettings.shadowDistance > BaselineShadowDistance)
            {
                QualitySettings.shadowDistance = BaselineShadowDistance;
            }

            QualitySettings.shadowCascades = Mathf.Clamp(
                QualitySettings.shadowCascades, 1, BaselineShadowCascades);
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = BaselineMsaa;
            QualitySettings.vSyncCount = BaselineVSync;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            Application.targetFrameRate = BaselineFrameRate;

            // Мобильная ветка (телефон/планшет, в т.ч. мобильный браузер WebGL):
            // MSAA дорог на тайловых GPU. Windows/GTX1050Ti baseline не трогаем.
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
                Debug.Log("[CourtRunner15] Mobile perf branch: MSAA=0, vSync=0, targetFrameRate=60.");
            }
        }

        private static void ApplyCameraBaseline(Camera cam)
        {
            if (cam == null) return;

            // HDR нужен bloom'у PPSv2: светится только то, что ярче 1.0
            // (эмиссив/неон/золото), а не всё подряд, как было бы в LDR.
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.allowDynamicResolution = false;
            cam.renderingPath = RenderingPath.Forward;
        }

        private void ConfigureKeyLight()
        {
            Light[] lights = Object.FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light.type == LightType.Directional)
                {
                    if (KeyLight == null) KeyLight = light;
                    else if (light != KeyLight) light.enabled = false;
                }
                else
                {
                    light.enabled = false;
                }
            }

            if (KeyLight == null)
            {
                GameObject lightGo = new GameObject("Key Light");
                KeyLight = lightGo.AddComponent<Light>();
                KeyLight.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            KeyLight.enabled = true;
            KeyLight.type = LightType.Directional;
            KeyLight.shadows = LightShadows.Soft;
            KeyLight.shadowStrength = 0.5f;
            KeyLight.intensity = 1.1f;
            KeyLight.renderMode = LightRenderMode.Auto;
        }

        private static void LogGraphicsBaseline(Camera cam)
        {
            string quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            bool cameraHdr = cam != null && cam.allowHDR;
            Debug.LogFormat(
                "[CourtRunner15] GTX1050Ti graphics baseline active: quality={0}, pixelLights={1}, shadowDistance={2:0.#}, shadowCascades={3}, shadowResolution={4}, msaa={5}, vSync={6}, cameraHDR={7}, renderPath={8}.",
                quality,
                QualitySettings.pixelLightCount,
                QualitySettings.shadowDistance,
                QualitySettings.shadowCascades,
                QualitySettings.shadowResolution,
                QualitySettings.antiAliasing,
                QualitySettings.vSyncCount,
                cameraHdr,
                cam != null ? cam.renderingPath.ToString() : "none");
        }

        // ------------------------------------------------------------------
        // Переходы состояний
        // ------------------------------------------------------------------

        public void ShowMainMenu()
        {
            State = GameState.MainMenu;
            Time.timeScale = 1f;
            SessionTotalScore = 0;
            PrepareBackdropLevel(0);
            if (UI != null) UI.ShowScreen(GameState.MainMenu);
        }

        public void ShowLevelSelect()
        {
            State = GameState.LevelSelect;
            Time.timeScale = 1f;
            DestroyBossStage();
            if (Spawner != null) Spawner.ClearAll();
            if (Player != null) Player.SetVisible(false);
            if (CameraCtrl != null) CameraCtrl.SetMenuMode(true);
            if (UI != null)
            {
                UI.RefreshLevelSelect();
                UI.ShowScreen(GameState.LevelSelect);
            }
        }

        /// <summary>Мир уровня как живой фон меню.</summary>
        private void PrepareBackdropLevel(int levelIndex)
        {
            DestroyBossStage();
            LevelData data = LevelCatalog.Get(levelIndex);
            Visuals.ApplyGlobalLook(data, KeyLight, CameraCtrl.Cam);
            Segments.BuildForLevel(data);
            Spawner.ConfigureForLevel(data);
            Spawner.ClearAll();
            Effects.ConfigurePalette(data.Palette);
            Player.SetVisible(false);
            CameraCtrl.SetMenuMode(true);
        }

        public void StartLevel(int index)
        {
            index = Mathf.Clamp(index, 0, LevelCatalog.LevelCount - 1);
            CurrentLevelIndex = index;
            LevelData data = LevelCatalog.Get(index);

            State = GameState.Playing;
            Time.timeScale = 1f;
            Confidence = 60f;
            Combo = 0;
            Multiplier = 1;
            MaxMultiplierReached = 1;
            ShieldActive = false;
            _shieldCooldownUntil = 0f;
            _scoreF = 0f;
            _boostBlend = 0f;
            _slowFactor = 1f;
            _slowUntil = 0f;
            _superSpeedUntil = 0f;
            _ramUntil = 0f;
            _weakJumpUntil = 0f;
            _magnetUntil = 0f;
            _aiUntil = 0f;
            _aiBanUntil = 0f;
            _slowmoRestoreAt = -1f;
            PickupsCollectedOnLevel = 0;
            HitsOnLevel = 0;
            DefeatReason = "";
            DestroyBossStage();

            Levels.StartLevel(data);
            Visuals.ApplyGlobalLook(data, KeyLight, CameraCtrl.Cam);
            Segments.BuildForLevel(data);
            Spawner.ConfigureForLevel(data);
            Spawner.ClearAll();
            Effects.ConfigurePalette(data.Palette);

            Player.ResetForLevel();
            Player.SetVisible(true);
            CameraCtrl.SetMenuMode(false);

            UI.ShowScreen(GameState.Playing);
            Audio.Play("whoosh", 0.7f, 1f);
        }

        private void TryApplyStartupArgs()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int startLevel = -1;
            bool startBoss = false;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--cr15-boss")
                {
                    startBoss = true;
                }
                else if (arg.StartsWith("--cr15-start-level="))
                {
                    string value = arg.Substring("--cr15-start-level=".Length);
                    int levelNumber;
                    if (int.TryParse(value, out levelNumber))
                    {
                        startLevel = Mathf.Clamp(levelNumber - 1, 0, LevelCatalog.LevelCount - 1);
                    }
                }
            }

            if (startLevel < 0) return;
            StartLevel(startLevel);
            if (startBoss) StartBossEncounter();
        }

        public void RestartLevel()
        {
            StartLevel(CurrentLevelIndex);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
                UI.ShowScreen(GameState.Paused);
                Audio.Play("click", 0.8f, 1f);
            }
            else if (State == GameState.Paused)
            {
                ResumeGame();
            }
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            UI.ShowScreen(GameState.Playing);
            Audio.Play("click", 0.8f, 1f);
        }

        // ------------------------------------------------------------------
        // Босс: сатирическая мини-сцена после дистанции уровня
        // ------------------------------------------------------------------

        /// <summary>Дистанция пройдена — но проход перегородил босс.</summary>
        private void StartBossEncounter()
        {
            State = GameState.Boss;
            Time.timeScale = 1f;
            WorldSpeed = 0f;

            CurrentBoss = BossCatalog.Get(CurrentLevelIndex);
            BossAttemptsLeft = CurrentBoss.Attempts;
            _bossWon = false;
            _bossResolveAt = -1f;

            Spawner.ClearAll();
            Effects.SetBoost(0f);

            // Сцена: баррикада и босс перед игроком, поза игрока — нейтральная.
            DestroyBossStage();
            _bossStage = ProceduralModelFactory.CreateBossStage(CurrentBoss, Levels.Current);
            if (Player != null && Player.Visual != null) Player.Visual.ResetPose();

            // Перемешанная копия вариантов (номера карточек = хоткеи 1..9, 0).
            CurrentBossOptions = (BossOption[])CurrentBoss.Options.Clone();
            if (CurrentBoss.ShuffleOptions)
            {
                for (int i = CurrentBossOptions.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    BossOption tmp = CurrentBossOptions[i];
                    CurrentBossOptions[i] = CurrentBossOptions[j];
                    CurrentBossOptions[j] = tmp;
                }
            }
            _bossOptionUsed = new bool[CurrentBossOptions.Length];

            UI.ShowBoss(CurrentBoss, BossAttemptsLeft);
            UI.ShowScreen(GameState.Boss);
            Audio.Play("debuff", 0.9f, 0.7f);
            CameraCtrl.Shake(0.10f, 0.25f);
        }

        /// <summary>Ответ боссу (клик по карточке или клавиши 1..9/0).</summary>
        public void AnswerBoss(int optionIndex)
        {
            if (State != GameState.Boss || CurrentBoss == null) return;
            if (_bossResolveAt > 0f) return; // исход уже решён, ждём реплику
            if (CurrentBossOptions == null ||
                optionIndex < 0 || optionIndex >= CurrentBossOptions.Length) return;
            if (_bossOptionUsed != null && _bossOptionUsed[optionIndex]) return;

            BossOption opt = CurrentBossOptions[optionIndex];

            if (opt.Correct)
            {
                _bossWon = true;
                _bossResolveAt = Time.unscaledTime + 2.2f;
                UI.ShowBossReaction(CurrentBoss.WinLine, true);
                ProceduralModelFactory.OpenBossBarrier(_bossStage);
                Effects.PlaySuperBurst(new Vector3(0f, 1.6f, 11f));
                Audio.Play("fanfare", 1f, 1f);
                CameraCtrl.Shake(0.12f, 0.2f);
                return;
            }

            BossAttemptsLeft--;
            string reaction = !string.IsNullOrEmpty(opt.Reaction)
                ? opt.Reaction
                : CurrentBoss.WrongReactions[Random.Range(0, CurrentBoss.WrongReactions.Length)];

            if (BossAttemptsLeft <= 0)
            {
                _bossWon = false;
                _bossResolveAt = Time.unscaledTime + 2.4f;
                UI.ShowBossReaction(CurrentBoss.DefeatLine, false);
                UI.UpdateBossAttempts(0);
                Audio.Play("lose", 1f, 0.9f);
                CameraCtrl.Shake(0.3f, 0.4f);
                return;
            }

            if (_bossOptionUsed != null) _bossOptionUsed[optionIndex] = true;
            UI.ShowBossReaction(reaction, false);
            UI.UpdateBossAttempts(BossAttemptsLeft);
            UI.MarkBossOptionWrong(optionIndex);
            Audio.Play("debuff", 1f, 1.05f);
            CameraCtrl.Shake(0.16f, 0.25f);
        }

        /// <summary>Развязка боя после реплики: победа уровня или рестарт.</summary>
        private void ResolveBossOutcome()
        {
            _bossResolveAt = -1f;
            if (_bossWon)
            {
                DestroyBossStage();
                OnLevelCompleted();
            }
            else
            {
                string name = CurrentBoss != null ? CurrentBoss.Name : "Босс";
                DestroyBossStage();
                OnPlayerDefeated(string.Format(
                    "{0} не пропустил вас: три ответа мимо. Придётся пройти уровень заново.", name));
            }
        }

        private void DestroyBossStage()
        {
            if (_bossStage != null)
            {
                Destroy(_bossStage);
                _bossStage = null;
            }
        }

        private void OnLevelCompleted()
        {
            State = GameState.Victory;
            Time.timeScale = 1f;

            // Бонус за остаток уверенности.
            _scoreF += Confidence * 40f;
            int finalScore = Score;
            SessionTotalScore += finalScore;

            if (CurrentLevelIndex + 1 < LevelCatalog.LevelCount &&
                UnlockedLevels < CurrentLevelIndex + 2)
            {
                UnlockedLevels = CurrentLevelIndex + 2;
                PlayerPrefs.SetInt(PrefUnlocked, UnlockedLevels);
            }
            int best = BestScore(CurrentLevelIndex);
            if (finalScore > best) PlayerPrefs.SetInt(PrefBest + CurrentLevelIndex, finalScore);
            PlayerPrefs.Save();

            Player.SetVisible(false);
            Spawner.ClearAll();
            Effects.PlayVictory();
            Audio.Play("win", 1f, 1f);
            UI.ShowVictory(finalScore);
            UI.ShowScreen(GameState.Victory);
        }

        private void OnPlayerDefeated(string reason)
        {
            State = GameState.Defeat;
            Time.timeScale = 1f;
            DefeatReason = reason;
            Effects.SetBoost(0f); // остановить пыль у ног и линии скорости
            Audio.Play("lose", 1f, 1f);
            UI.ShowDefeat(reason);
            UI.ShowScreen(GameState.Defeat);
        }

        public void ShowFinale()
        {
            State = GameState.Finale;
            Time.timeScale = 1f;
            PrepareBackdropLevel(2);
            UI.ShowFinale(SessionTotalScore);
            UI.ShowScreen(GameState.Finale);
            Effects.PlayVictory();
        }

        public void UnlockAllLevels()
        {
            UnlockedLevels = LevelCatalog.LevelCount;
            PlayerPrefs.SetInt(PrefUnlocked, UnlockedLevels);
            PlayerPrefs.Save();
            if (UI != null) UI.RefreshLevelSelect();
            if (Audio != null) Audio.Play("click", 0.8f, 1.1f);
        }

        public int BestScore(int levelIndex)
        {
            return PlayerPrefs.GetInt(PrefBest + levelIndex, 0);
        }

        // ------------------------------------------------------------------
        // Игровые события
        // ------------------------------------------------------------------

        public void CollectPickup(Pickup pickup)
        {
            if (pickup == null || pickup.Spec == null) return;
            PickupSpec spec = pickup.Spec;

            Confidence = Mathf.Min(100f, Confidence + spec.ConfidenceGain);
            Combo++;
            RecalcMultiplier();
            // «Юрист освоил ИИ» удваивает очки, бан подписки — режет вдвое.
            float aiFactor = AiActive ? 2f : (AiBanActive ? 0.5f : 1f);
            _scoreF += spec.ScoreValue * Multiplier * aiFactor;
            PickupsCollectedOnLevel++;
            Levels.RegisterPickup();

            if (spec.Super)
            {
                ApplySuperPickup(pickup, spec);
                return;
            }

            LevelPalette pal = Levels.Current != null ? Levels.Current.Palette : null;
            Color c = spec.Secondary && pal != null ? pal.BoostSecondary :
                      (pal != null ? pal.BoostPrimary : Color.yellow);
            Effects.PlayPickupBurst(pickup.transform.position + Vector3.up * 1.1f, c);
            Audio.Play("pickup", 0.9f, 1f + Mathf.Min(0.25f, Combo * 0.02f));
            UI.PulseConfidence();
        }

        /// <summary>Суперприз — событие: слоу-мо, крупный тост, фанфары и эффект.</summary>
        private void ApplySuperPickup(Pickup pickup, PickupSpec spec)
        {
            switch (spec.SuperId)
            {
                case "cover":
                    ShieldActive = true;
                    break;
                case "glossa":
                    if (Combo < 10) Combo = 10;
                    RecalcMultiplier();
                    break;
                case "vas":
                    _superSpeedUntil = Time.time + 6f;
                    break;
                case "writ":
                    _ramUntil = Time.time + 8f;
                    break;
                case "plenum":
                    _magnetUntil = Time.time + 8f;
                    break;
                case "ai":
                    _aiUntil = Time.time + 10f;
                    _aiBanUntil = 0f; // освоенный ИИ снимает бан подписки
                    break;
            }

            Vector3 pos = pickup.transform.position + Vector3.up * 1.2f;
            Effects.PlaySuperBurst(pos);
            Audio.Play("fanfare", 1f, 1f);
            UI.PulseConfidence();
            UI.ShowEventToast(spec.ToastTitle, spec.ToastBody, true);
            CameraCtrl.Shake(0.12f, 0.2f);

            // Драматичная пауза: доля секунды слоу-мо.
            Time.timeScale = 0.35f;
            _slowmoRestoreAt = Time.unscaledTime + 0.5f;
        }

        /// <summary>Игрок с «Исполнительным листом» сносит препятствие без штрафа.</summary>
        public void RamSmash(Obstacle obstacle)
        {
            if (obstacle == null) return;
            _scoreF += 60f * Multiplier;
            if (!obstacle.CountedAvoided)
            {
                obstacle.CountedAvoided = true;
                Levels.RegisterAvoided();
            }
            Effects.PlaySmashBurst(obstacle.transform.position + Vector3.up * 1.0f);
            Audio.Play("hit", 0.6f, 1.4f);
            CameraCtrl.Shake(0.14f, 0.16f);
        }

        public void HitObstacle(Obstacle obstacle)
        {
            if (obstacle == null || obstacle.Spec == null) return;
            ObstacleSpec spec = obstacle.Spec;
            HitsOnLevel++;

            Vector3 pos = obstacle.transform.position + Vector3.up * 1.0f;

            if (ShieldActive)
            {
                ShieldActive = false;
                _shieldCooldownUntil = Time.time + 4f;
                Combo = 0;
                RecalcMultiplier();
                Effects.PlayShieldBreak(pos);
                Audio.Play("shield", 0.9f, 0.9f);
                CameraCtrl.Shake(0.18f, 0.25f);
                UI.FlashHit(0.25f);
                return;
            }

            float penalty = Random.Range(spec.PenaltyMin, spec.PenaltyMax);
            Confidence = Mathf.Max(0f, Confidence - penalty);
            Combo = 0;
            RecalcMultiplier();

            if (spec.Toxic)
            {
                // Ядовитое препятствие: дебафф + крупный тост.
                if (spec.SlowDuration > 0f)
                {
                    _slowFactor = spec.SlowFactor;
                    _slowUntil = Time.time + spec.SlowDuration;
                }
                if (spec.WeakJumpDuration > 0f)
                {
                    _weakJumpUntil = Time.time + spec.WeakJumpDuration;
                }
                if (spec.ScoreDebuffDuration > 0f)
                {
                    _aiBanUntil = Time.time + spec.ScoreDebuffDuration;
                    _aiUntil = 0f; // бан подписки гасит бонус ИИ
                }
                Effects.PlayToxicBurst(pos);
                Audio.Play("debuff", 1f, 1f);
                UI.ShowEventToast(spec.ToastTitle, spec.ToastBody, false);
                CameraCtrl.Shake(0.4f, 0.45f);
                UI.FlashHit(0.6f);
            }
            else
            {
                Effects.PlayHitBurst(pos);
                Audio.Play("hit", 1f, 1f);
                CameraCtrl.Shake(0.35f, 0.4f);
                UI.FlashHit(0.55f);
            }

            if (Confidence <= 0f)
            {
                OnPlayerDefeated(string.Format(
                    "Уверенность упала до 0% — виновато препятствие «{0}».", spec.Label));
            }
        }

        private void RecalcMultiplier()
        {
            int m = 1;
            if (Combo >= 10) m = 3;
            else if (Combo >= 5) m = 2;
            Multiplier = m;
            if (m > MaxMultiplierReached) MaxMultiplierReached = m;
            Levels.NotifyMultiplier(m);
        }

        // ------------------------------------------------------------------
        // Главный цикл
        // ------------------------------------------------------------------

        private void Update()
        {
            HandleHotkeys();

            // Возврат из слоу-мо суперприза (по нескейленному времени).
            if (_slowmoRestoreAt > 0f && Time.unscaledTime >= _slowmoRestoreAt)
            {
                _slowmoRestoreAt = -1f;
                if (State == GameState.Playing) Time.timeScale = 1f;
            }

            float dt = Time.deltaTime;

            if (State == GameState.Playing)
            {
                UpdateBoostAndShield(dt);
                LevelData data = Levels.Current;
                float baseSpeed = data != null ? data.BaseSpeed : 15f;
                float speedMul = 1f + 0.25f * _boostBlend;
                if (SuperSpeedActive) speedMul *= 1.35f;         // «ВАС РФ»
                if (Time.time < _slowUntil) speedMul *= _slowFactor; // дебафф
                WorldSpeed = baseSpeed * speedMul;

                Levels.Tick(dt, WorldSpeed);
                _scoreF += WorldSpeed * dt * 2f * Multiplier;

                Segments.Tick(dt, WorldSpeed);
                Spawner.Tick(dt, WorldSpeed);
                Player.Tick(dt);

                Effects.SetBoost(_boostBlend);
                UI.UpdateHUD(dt);

                if (Levels.IsComplete)
                {
                    StartBossEncounter();
                }
            }
            else if (State == GameState.Boss)
            {
                // Мир замер, босс держит проход; ждём развязку после реплики.
                WorldSpeed = 0f;
                if (_bossResolveAt > 0f && Time.unscaledTime >= _bossResolveAt)
                {
                    ResolveBossOutcome();
                }
            }
            else if (State == GameState.MainMenu || State == GameState.LevelSelect ||
                     State == GameState.Finale || State == GameState.Victory)
            {
                // Живой фон: мир медленно плывёт.
                WorldSpeed = MenuScrollSpeed;
                Segments.Tick(dt, MenuScrollSpeed);
                Effects.SetBoost(0f);
            }
            else
            {
                WorldSpeed = 0f;
            }
        }

        private void UpdateBoostAndShield(float dt)
        {
            float target = Confidence >= 75f ? 1f : 0f;
            _boostBlend = Mathf.MoveTowards(_boostBlend, target, dt * 2.5f);

            // Защита взводится при уверенности ≥90% (после траты — пауза 4 с)
            // и держится до первой ошибки, даже если уверенность просела.
            if (!ShieldActive && Confidence >= 90f && Time.time >= _shieldCooldownUntil)
            {
                ShieldActive = true;
                Audio.Play("shield", 0.7f, 1.2f);
            }
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing || State == GameState.Paused) TogglePause();
                else if (State == GameState.LevelSelect) ShowMainMenu();
                else if (State == GameState.Victory || State == GameState.Defeat) ShowLevelSelect();
                else if (State == GameState.Finale) ShowMainMenu();
                else if (State == GameState.Boss) ShowLevelSelect();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (State == GameState.Playing || State == GameState.Paused ||
                    State == GameState.Defeat || State == GameState.Victory)
                {
                    RestartLevel();
                }
            }

            if (State == GameState.Boss)
            {
                // Карточки ответов: 1..9 и 0 (= десятый вариант).
                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    {
                        AnswerBoss(i);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                {
                    AnswerBoss(9);
                }
            }

            // Отладочный прыжок к боссу: F6 в забеге завершает дистанцию уровня
            // (F6 не зависит от раскладки клавиатуры, в отличие от букв).
            if (State == GameState.Playing && Input.GetKeyDown(KeyCode.F6))
            {
                Levels.DebugCompleteDistance();
            }

            if (State == GameState.LevelSelect)
            {
                // Тестовые горячие клавиши: мгновенный запуск любого уровня.
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) StartLevel(0);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) StartLevel(1);
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) StartLevel(2);
            }
            else if (State == GameState.MainMenu)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ShowLevelSelect();
                }
            }
        }
    }
}
