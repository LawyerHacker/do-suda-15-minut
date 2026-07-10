using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CourtRunner15
{
    /// <summary>
    /// Весь интерфейс игры: стартовый экран, выбор уровня, HUD, пауза,
    /// победа, поражение, финал. Крупный русский текст, тёмные панели.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private GameManager _gm;
        private Font _font;
        private Transform _canvasT;

        private ScreenController _menuScreen;
        private ScreenController _levelSelectScreen;
        private ScreenController _hudScreen;
        private ScreenController _pauseScreen;
        private ScreenController _victoryScreen;
        private ScreenController _defeatScreen;
        private ScreenController _finaleScreen;
        private ScreenController _bossScreen;

        // HUD
        private Text _levelLabel;
        private Text _timerLabel;
        private Text _scoreLabel;
        private Text _comboLabel;
        private Text _confidenceLabel;
        private Text _boostStatusLabel;
        private Image _confidenceFill;
        private RectTransform _confidenceBar;
        private GameObject _shieldChip;
        private readonly Text[] _goalLabels = new Text[3];
        private CircularBoostIndicator _boostRing;
        private HitFlashEffect _flash;

        // Выбор уровня
        private readonly Text[] _cardStatus = new Text[3];
        private readonly Text[] _cardBest = new Text[3];
        private readonly GameObject[] _cardLocks = new GameObject[3];
        private readonly Button[] _cardButtons = new Button[3];

        // Победа / поражение / финал
        private Text _victoryStats;
        private Text _nextButtonLabel;
        private Text _defeatReasonText;
        private Text _finaleScoreText;

        // Тост события (суперприз / ядовитое препятствие)
        private CanvasGroup _toastGroup;
        private RectTransform _toastRt;
        private Image _toastFrame;
        private Image _toastPanel;
        private Image _toastStripe;
        private Text _toastTitle;
        private Text _toastBody;
        private GameObject _toastMedal;
        private HitFlashEffect _superFlash;
        private float _toastUntil;
        private float _toastPop;

        // Экран босса
        private const int BossMaxCards = 10;
        private Text _bossTitle;
        private Text _bossSketch;
        private Text _bossLine;
        private Text _bossHint;
        private Text _bossSubHint;
        private Text _bossReaction;
        private GameObject _bossCutInFrame;
        private Image _bossCutIn;
        private readonly Button[] _bossCards = new Button[BossMaxCards];
        private readonly Text[] _bossCardTexts = new Text[BossMaxCards];
        // Картинки-карточки (режим «вместо слов»: у всех вариантов есть Icon).
        private readonly Button[] _bossIconCards = new Button[BossMaxCards];
        private readonly Image[] _bossIconImages = new Image[BossMaxCards];
        private readonly Text[] _bossIconNums = new Text[BossMaxCards];
        private readonly Image[] _bossAttemptIcons = new Image[3];

        private int _cachedScore = -1;
        private int _cachedDist = -1;
        private int _cachedLevelIndex = -1;
        private string _cachedTimer = "";
        private float _confidencePulse;

        private static readonly Color PanelDark = new Color(0.02f, 0.05f, 0.1f, 0.62f);
        private static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        private static readonly Color Green = new Color(0.38f, 0.95f, 0.47f);
        private static readonly Color Red = new Color(0.95f, 0.28f, 0.2f);
        private static readonly Color TextMain = new Color(0.96f, 0.97f, 1f);
        private static readonly Color TextDim = new Color(0.72f, 0.76f, 0.84f);

        public void Init(GameManager gm)
        {
            _gm = gm;
            _font = MaterialFactory.UiFont;
            EnsureEventSystem();
            BuildCanvas();
            _menuScreen = BuildMainMenu();
            _levelSelectScreen = BuildLevelSelect();
            _hudScreen = BuildHud();
            _pauseScreen = BuildPause();
            _victoryScreen = BuildVictory();
            _defeatScreen = BuildDefeat();
            _finaleScreen = BuildFinale();
            _bossScreen = BuildBoss();
            HideAll(true);
        }

        // ------------------------------------------------------------------
        // Инфраструктура
        // ------------------------------------------------------------------

        private void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasT = canvasGo.transform;
        }

        private RectTransform MakeRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private RectTransform MakeScreenRoot(string name)
        {
            RectTransform rt = MakeRect(_canvasT, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rt = MakeRect(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Image MakePanel(Transform parent, string name, Color color,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            Image img = MakeImage(parent, name, MaterialFactory.RoundedSprite(), color);
            img.type = Image.Type.Sliced;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        private Text MakeText(Transform parent, string name, string text, int size,
            Color color, TextAnchor anchor, bool bold)
        {
            RectTransform rt = MakeRect(parent, name);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Тёмная обводка — текст читается поверх светлого мира.</summary>
        private void AddTextOutline(Text t, Color color, float dist)
        {
            Outline ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = color;
            ol.effectDistance = new Vector2(dist, -dist);
        }

        private void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private Button MakeButton(Transform parent, string name, string label, int fontSize,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color bg, Color textColor,
            UnityAction onClick)
        {
            Image img = MakeImage(parent, name, MaterialFactory.RoundedSprite(), bg);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            Place(img.rectTransform, anchor, pivot, pos, size);

            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.18f, 1.25f, 1f);
            cb.pressedColor = new Color(0.75f, 0.78f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            Navigation nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(onClick);

            Text t = MakeText(img.transform, "label", label, fontSize, textColor,
                TextAnchor.MiddleCenter, true);
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            return b;
        }

        private ScreenController AttachScreen(RectTransform root)
        {
            ScreenController sc = root.gameObject.AddComponent<ScreenController>();
            return sc;
        }

        private void HideAll(bool instant)
        {
            _menuScreen.Hide(instant);
            _levelSelectScreen.Hide(instant);
            _hudScreen.Hide(instant);
            _pauseScreen.Hide(instant);
            _victoryScreen.Hide(instant);
            _defeatScreen.Hide(instant);
            _finaleScreen.Hide(instant);
            _bossScreen.Hide(instant);
        }

        // ------------------------------------------------------------------
        // Экраны
        // ------------------------------------------------------------------

        private ScreenController BuildMainMenu()
        {
            RectTransform root = MakeScreenRoot("Screen_MainMenu");

            MakePanel(root, "titleBack", new Color(0.02f, 0.04f, 0.09f, 0.5f),
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 360f));

            Text t1 = MakeText(root, "title1", "ДО СУДА", 128, TextMain, TextAnchor.MiddleCenter, true);
            Place(t1.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000f, 140f));
            Text t2 = MakeText(root, "title2", "15 МИНУТ", 128, Gold, TextAnchor.MiddleCenter, true);
            Place(t2.rectTransform, new Vector2(0.5f, 0.69f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000f, 140f));
            Text sub = MakeText(root, "subtitle", "юридический 3D-раннер", 34, TextDim,
                TextAnchor.MiddleCenter, false);
            Place(sub.rectTransform, new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 60f));

            MakePanel(root, "descBack", PanelDark, new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1100f, 130f));
            Text desc = MakeText(root, "desc",
                "Юрист опаздывает на главное заседание года.\nБегите через офис, город и коридоры суда: собирайте документы,\nуклоняйтесь от дедлайнов и держите уверенность выше нуля.",
                27, TextMain, TextAnchor.MiddleCenter, false);
            Place(desc.rectTransform, new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1080f, 120f));

            MakeButton(root, "startBtn", "НАЧАТЬ", 42, new Vector2(0.5f, 0.27f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(380f, 96f), new Color(0.85f, 0.62f, 0.12f, 0.97f),
                new Color(0.1f, 0.07f, 0.02f), delegate { _gm.ShowLevelSelect(); });

            // Тёмно-бордовый: на светлом бежевом фоне меню серый текст терялся.
            Text hint = MakeText(root, "hint",
                "A/D или ←/→ — дорожки  ·  Space — прыжок  ·  S / Shift — подкат  ·  Esc — пауза  ·  R — заново",
                23, new Color(0.40f, 0.08f, 0.06f), TextAnchor.MiddleCenter, false);
            Place(hint.rectTransform, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1600f, 40f));

            return AttachScreen(root);
        }

        private ScreenController BuildLevelSelect()
        {
            RectTransform root = MakeScreenRoot("Screen_LevelSelect");

            MakeImage(root, "dim", MaterialFactory.WhiteSprite(), new Color(0f, 0f, 0f, 0.35f))
                .rectTransform.SetAsFirstSibling();
            RectTransform dim = root.Find("dim").GetComponent<RectTransform>();
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = Vector2.zero;
            dim.offsetMax = Vector2.zero;

            Text title = MakeText(root, "title", "ВЫБОР УРОВНЯ", 66, TextMain, TextAnchor.MiddleCenter, true);
            Place(title.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 90f));

            Color[] headers =
            {
                new Color(0.24f, 0.42f, 0.3f, 0.95f),
                new Color(0.14f, 0.34f, 0.58f, 0.95f),
                new Color(0.45f, 0.3f, 0.12f, 0.95f)
            };

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                LevelData data = LevelCatalog.Get(i);
                float x = (i - 1) * 520f;

                Image card = MakePanel(root, "card" + i, new Color(0.04f, 0.07f, 0.13f, 0.92f),
                    new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f),
                    new Vector2(480f, 600f));
                Transform ct = card.transform;

                Image header = MakeImage(ct, "header", MaterialFactory.RoundedSprite(), headers[i]);
                header.type = Image.Type.Sliced;
                Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -14f), new Vector2(452f, 120f));
                Text num = MakeText(header.transform, "num", (i + 1).ToString(), 76, TextMain,
                    TextAnchor.MiddleLeft, true);
                Place(num.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(28f, 0f), new Vector2(90f, 110f));
                Text sub = MakeText(header.transform, "sub", data.Subtitle, 24, TextMain,
                    TextAnchor.MiddleLeft, false);
                Place(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(120f, 0f), new Vector2(320f, 110f));

                Text cardTitle = MakeText(ct, "title", data.Title, 34, TextMain, TextAnchor.MiddleCenter, true);
                Place(cardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -150f), new Vector2(440f, 90f));

                Text desc = MakeText(ct, "desc", data.Description, 24, TextDim, TextAnchor.UpperCenter, false);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -246f), new Vector2(430f, 150f));

                Text stats = MakeText(ct, "stats",
                    string.Format("Дистанция: {0} м\nТаймер: {1}", (int)data.Distance, FormatTime(data.TimerSeconds)),
                    24, TextMain, TextAnchor.MiddleCenter, false);
                Place(stats.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 218f), new Vector2(430f, 70f));

                _cardBest[i] = MakeText(ct, "best", "", 24, Gold, TextAnchor.MiddleCenter, false);
                Place(_cardBest[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 174f), new Vector2(430f, 40f));

                _cardStatus[i] = MakeText(ct, "status", "", 26, Green, TextAnchor.MiddleCenter, true);
                Place(_cardStatus[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 130f), new Vector2(430f, 40f));

                _cardButtons[i] = MakeButton(ct, "play", "ИГРАТЬ", 32, new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(260f, 76f),
                    new Color(0.85f, 0.62f, 0.12f, 0.97f), new Color(0.1f, 0.07f, 0.02f),
                    delegate { _gm.StartLevel(idx); });

                GameObject lockGo = MakeImage(ct, "lock", MaterialFactory.RoundedSprite(),
                    new Color(0.01f, 0.02f, 0.05f, 0.78f)).gameObject;
                Image lockImg = lockGo.GetComponent<Image>();
                lockImg.type = Image.Type.Sliced;
                lockImg.raycastTarget = true;
                RectTransform lockRt = lockGo.GetComponent<RectTransform>();
                lockRt.anchorMin = Vector2.zero;
                lockRt.anchorMax = Vector2.one;
                lockRt.offsetMin = Vector2.zero;
                lockRt.offsetMax = Vector2.zero;
                Text lockText = MakeText(lockGo.transform, "lockText",
                    string.Format("ЗАКРЫТО\n\nПройдите уровень {0}", i), 30, TextDim,
                    TextAnchor.MiddleCenter, true);
                RectTransform lockTextRt = lockText.rectTransform;
                lockTextRt.anchorMin = Vector2.zero;
                lockTextRt.anchorMax = Vector2.one;
                lockTextRt.offsetMin = Vector2.zero;
                lockTextRt.offsetMax = Vector2.zero;
                _cardLocks[i] = lockGo;
            }

            MakeButton(root, "unlockAll", "Открыть все уровни", 24, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-560f, 36f), new Vector2(320f, 58f),
                new Color(0.12f, 0.2f, 0.34f, 0.9f), TextMain, delegate { _gm.UnlockAllLevels(); });

            MakeButton(root, "back", "← Назад", 24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(560f, 36f), new Vector2(220f, 58f),
                new Color(0.12f, 0.2f, 0.34f, 0.9f), TextMain, delegate { _gm.ShowMainMenu(); });

            Text hint = MakeText(root, "hint", "Клавиши 1 / 2 / 3 — быстрый запуск уровня · Esc — назад",
                22, TextDim, TextAnchor.MiddleCenter, false);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 6f), new Vector2(1000f, 30f));

            return AttachScreen(root);
        }

        private ScreenController BuildHud()
        {
            RectTransform root = MakeScreenRoot("Screen_HUD");

            // Вспышка удара — под всеми панелями HUD.
            Image flashImg = MakeImage(root, "hitFlash", MaterialFactory.WhiteSprite(),
                new Color(0.9f, 0.12f, 0.08f, 0f));
            RectTransform flashRt = flashImg.rectTransform;
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
            _flash = root.gameObject.AddComponent<HitFlashEffect>();
            _flash.Bind(flashImg);

            // При активной пост-обработке виньетку рисует PPSv2 — спрайт почти гасим.
            Image vignette = MakeImage(root, "screenVignette", MaterialFactory.VignetteSprite(),
                new Color(0f, 0f, 0f, PostFxController.Active ? 0.08f : 0.32f));
            RectTransform vignetteRt = vignette.rectTransform;
            vignetteRt.anchorMin = Vector2.zero;
            vignetteRt.anchorMax = Vector2.one;
            vignetteRt.offsetMin = Vector2.zero;
            vignetteRt.offsetMax = Vector2.zero;
            vignette.transform.SetAsFirstSibling();

            // Левый верх: пауза, уровень, таймер. Кнопка паузы крупнее —
            // на телефоне палец должен попадать без снайперской точности.
            MakePanel(root, "topLeft", PanelDark, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(420f, 128f));
            MakeButton(root, "pauseBtn", "II", 40, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -36f), new Vector2(96f, 96f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.TogglePause(); });
            _levelLabel = MakeText(root, "levelLabel", "Уровень 1", 28, TextDim, TextAnchor.MiddleLeft, true);
            Place(_levelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(152f, -46f), new Vector2(280f, 36f));
            _timerLabel = MakeText(root, "timerLabel", "До суда: 15:00", 38, Gold, TextAnchor.MiddleLeft, true);
            Place(_timerLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(152f, -92f), new Vector2(285f, 46f));

            // Центр сверху: счёт, множитель, дистанция.
            MakePanel(root, "topCenter", PanelDark, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(560f, 138f));
            _scoreLabel = MakeText(root, "scoreLabel", "0", 64, TextMain, TextAnchor.MiddleCenter, true);
            Place(_scoreLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(540f, 72f));
            _comboLabel = MakeText(root, "comboLabel", "x1 · 0 м / 1250 м", 28, TextDim,
                TextAnchor.MiddleCenter, false);
            Place(_comboLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -104f), new Vector2(540f, 36f));

            // Правый верх: уверенность.
            MakePanel(root, "topRight", PanelDark, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(450f, 148f));
            Text confCaption = MakeText(root, "confCaption", "УВЕРЕННОСТЬ", 24, TextDim,
                TextAnchor.MiddleLeft, true);
            Place(confCaption.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-244f, -46f), new Vector2(220f, 32f));
            _confidenceLabel = MakeText(root, "confValue", "60%", 34, TextMain, TextAnchor.MiddleRight, true);
            Place(_confidenceLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-48f, -46f), new Vector2(140f, 40f));

            Image barBg = MakeImage(root, "confBarBg", MaterialFactory.WhiteSprite(),
                new Color(0.06f, 0.09f, 0.14f, 0.9f));
            Place(barBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-44f, -84f), new Vector2(400f, 26f));
            _confidenceBar = barBg.rectTransform;
            _confidenceFill = MakeImage(barBg.transform, "fill", MaterialFactory.ConfidenceGradientSprite(), Color.white);
            _confidenceFill.type = Image.Type.Filled;
            _confidenceFill.fillMethod = Image.FillMethod.Horizontal;
            _confidenceFill.fillAmount = 0.6f;
            RectTransform fillRt = _confidenceFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            // Отметки порогов 75% и 90%.
            Image mark75 = MakeImage(barBg.transform, "mark75", MaterialFactory.WhiteSprite(),
                new Color(1f, 1f, 1f, 0.75f));
            Place(mark75.rectTransform, new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(3f, 34f));
            Image mark90 = MakeImage(barBg.transform, "mark90", MaterialFactory.WhiteSprite(),
                new Color(0.4f, 0.9f, 1f, 0.85f));
            Place(mark90.rectTransform, new Vector2(0.9f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(3f, 34f));

            _boostStatusLabel = MakeText(root, "boostStatus", "", 24, Gold, TextAnchor.MiddleLeft, true);
            Place(_boostStatusLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-244f, -128f), new Vector2(240f, 32f));

            Image shieldChip = MakePanel(root, "shieldChip", new Color(0.1f, 0.4f, 0.2f, 0.92f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -118f), new Vector2(150f, 40f));
            Text shieldText = MakeText(shieldChip.transform, "txt", "ЗАЩИТА", 24, Green,
                TextAnchor.MiddleCenter, true);
            RectTransform strt = shieldText.rectTransform;
            strt.anchorMin = Vector2.zero;
            strt.anchorMax = Vector2.one;
            strt.offsetMin = Vector2.zero;
            strt.offsetMax = Vector2.zero;
            _shieldChip = shieldChip.gameObject;
            _shieldChip.SetActive(false);

            // Низ слева: цели.
            MakePanel(root, "goals", PanelDark, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 24f), new Vector2(470f, 190f));
            Text goalsCaption = MakeText(root, "goalsCaption", "ЦЕЛИ", 24, TextDim,
                TextAnchor.MiddleLeft, true);
            Place(goalsCaption.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(48f, 178f), new Vector2(200f, 30f));
            for (int i = 0; i < 3; i++)
            {
                _goalLabels[i] = MakeText(root, "goal" + i, "—", 26, TextMain, TextAnchor.MiddleLeft, false);
                Place(_goalLabels[i].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(48f, 136f - i * 44f), new Vector2(420f, 36f));
            }

            // Низ справа: круглый индикатор ускорения.
            RectTransform ringRt = MakeRect(root, "boostRing");
            Place(ringRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 56f),
                new Vector2(172f, 172f));
            _boostRing = ringRt.gameObject.AddComponent<CircularBoostIndicator>();
            _boostRing.Build();

            // Золотая вспышка суперприза (поверх вспышки удара).
            Image superFlashImg = MakeImage(root, "superFlash", MaterialFactory.WhiteSprite(),
                new Color(1f, 0.84f, 0.35f, 0f));
            StretchFull(superFlashImg.rectTransform);
            superFlashImg.transform.SetSiblingIndex(1);
            _superFlash = root.gameObject.AddComponent<HitFlashEffect>();
            _superFlash.SetColor(new Color(1f, 0.84f, 0.35f));
            _superFlash.Bind(superFlashImg);

            // Тост события: «судебная табличка» — синяя плашка в золотой рамке
            // с гербовым медальоном (как на референсе «НОСТАЛЬГИЯ ПО ВАС РФ»).
            _toastFrame = MakePanel(root, "eventToastFrame", Gold,
                new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(994f, 182f));
            _toastRt = _toastFrame.rectTransform;
            _toastGroup = _toastFrame.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
            _toastGroup.interactable = false;
            _toastGroup.blocksRaycasts = false;

            _toastPanel = MakePanel(_toastFrame.transform, "eventToast", new Color(0.05f, 0.08f, 0.21f, 0.97f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(978f, 166f));

            _toastStripe = MakeImage(_toastPanel.transform, "stripe", MaterialFactory.WhiteSprite(), Gold);
            Place(_toastStripe.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(918f, 4f));

            _toastTitle = MakeText(_toastPanel.transform, "title", "", 42, Gold, TextAnchor.MiddleCenter, true);
            Place(_toastTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -22f), new Vector2(940f, 56f));

            _toastBody = MakeText(_toastPanel.transform, "body", "", 27, TextMain, TextAnchor.UpperCenter, false);
            _toastBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_toastBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -84f), new Vector2(920f, 76f));

            Image medal = MakeImage(_toastFrame.transform, "medal", MaterialFactory.CircleSprite(), Gold);
            Place(medal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 4f), new Vector2(64f, 64f));
            Text medalTxt = MakeText(medal.transform, "txt", "РФ", 26,
                new Color(0.08f, 0.10f, 0.24f), TextAnchor.MiddleCenter, true);
            StretchFull(medalTxt.rectTransform);
            _toastMedal = medal.gameObject;

            return AttachScreen(root);
        }

        private ScreenController BuildPause()
        {
            RectTransform root = MakeScreenRoot("Screen_Pause");
            Image dim = MakeImage(root, "dim", MaterialFactory.WhiteSprite(), new Color(0f, 0f, 0f, 0.6f));
            StretchFull(dim.rectTransform);

            MakePanel(root, "panel", new Color(0.04f, 0.07f, 0.13f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 520f));
            Text title = MakeText(root, "title", "ПАУЗА", 68, TextMain, TextAnchor.MiddleCenter, true);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 170f), new Vector2(500f, 90f));

            MakeButton(root, "resume", "ПРОДОЛЖИТЬ", 32, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 50f), new Vector2(400f, 78f),
                new Color(0.85f, 0.62f, 0.12f, 0.97f), new Color(0.1f, 0.07f, 0.02f),
                delegate { _gm.ResumeGame(); });
            MakeButton(root, "restart", "ЗАНОВО (R)", 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -50f), new Vector2(400f, 74f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.RestartLevel(); });
            MakeButton(root, "toSelect", "ВЫБОР УРОВНЯ", 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -148f), new Vector2(400f, 74f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.ShowLevelSelect(); });

            return AttachScreen(root);
        }

        private ScreenController BuildVictory()
        {
            RectTransform root = MakeScreenRoot("Screen_Victory");
            Image dim = MakeImage(root, "dim", MaterialFactory.WhiteSprite(), new Color(0f, 0f, 0f, 0.55f));
            StretchFull(dim.rectTransform);

            MakePanel(root, "panel", new Color(0.04f, 0.08f, 0.13f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 640f));
            Text title = MakeText(root, "title", "УРОВЕНЬ ПРОЙДЕН!", 58, Gold, TextAnchor.MiddleCenter, true);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 236f), new Vector2(680f, 80f));
            Text sub = MakeText(root, "sub", "Вы успеваете. Пока что.", 28, TextDim, TextAnchor.MiddleCenter, false);
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 182f), new Vector2(680f, 40f));

            _victoryStats = MakeText(root, "stats", "", 32, TextMain, TextAnchor.MiddleCenter, false);
            Place(_victoryStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 46f), new Vector2(660f, 220f));

            Button next = MakeButton(root, "next", "СЛЕДУЮЩИЙ УРОВЕНЬ", 30, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -138f), new Vector2(480f, 80f),
                new Color(0.85f, 0.62f, 0.12f, 0.97f), new Color(0.1f, 0.07f, 0.02f),
                delegate
                {
                    if (_gm.CurrentLevelIndex >= LevelCatalog.LevelCount - 1) _gm.ShowFinale();
                    else _gm.StartLevel(_gm.CurrentLevelIndex + 1);
                });
            _nextButtonLabel = next.GetComponentInChildren<Text>();

            MakeButton(root, "again", "ЕЩЁ РАЗ (R)", 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-130f, -232f), new Vector2(240f, 64f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.RestartLevel(); });
            MakeButton(root, "select", "ВЫБОР УРОВНЯ", 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(130f, -232f), new Vector2(240f, 64f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.ShowLevelSelect(); });

            return AttachScreen(root);
        }

        private ScreenController BuildDefeat()
        {
            RectTransform root = MakeScreenRoot("Screen_Defeat");
            Image dim = MakeImage(root, "dim", MaterialFactory.WhiteSprite(), new Color(0.2f, 0f, 0f, 0.6f));
            StretchFull(dim.rectTransform);

            // Панель выше, причина компактнее: раньше 3-строчная причина с
            // подсказкой упиралась в кнопку «ПОВТОРИТЬ» (тесно на телефоне).
            MakePanel(root, "panel", new Color(0.09f, 0.04f, 0.05f, 0.96f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 560f));
            Text title = MakeText(root, "title", "ВЫ ОПАЗДЫВАЕТЕ…", 56, Red, TextAnchor.MiddleCenter, true);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 196f), new Vector2(660f, 80f));

            _defeatReasonText = MakeText(root, "reason", "", 27, TextMain, TextAnchor.MiddleCenter, false);
            _defeatReasonText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_defeatReasonText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 58f), new Vector2(640f, 200f));

            MakeButton(root, "retry", "ПОВТОРИТЬ (R)", 32, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -104f), new Vector2(420f, 80f),
                new Color(0.85f, 0.62f, 0.12f, 0.97f), new Color(0.1f, 0.07f, 0.02f),
                delegate { _gm.RestartLevel(); });
            MakeButton(root, "select", "ВЫБОР УРОВНЯ", 28, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -196f), new Vector2(420f, 70f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.ShowLevelSelect(); });

            return AttachScreen(root);
        }

        private ScreenController BuildFinale()
        {
            RectTransform root = MakeScreenRoot("Screen_Finale");
            Image dim = MakeImage(root, "dim", MaterialFactory.WhiteSprite(),
                new Color(0.12f, 0.08f, 0.01f, 0.62f));
            StretchFull(dim.rectTransform);

            MakePanel(root, "panel", new Color(0.05f, 0.05f, 0.1f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f));

            Text title = MakeText(root, "title", "ВЫ УСПЕЛИ В СУД!", 76, Gold, TextAnchor.MiddleCenter, true);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 200f), new Vector2(860f, 100f));

            Text sub = MakeText(root, "sub",
                "Правосудие восстановлено, проход открыт. Клиент спокоен, судья почти улыбнулся.\nВсе три забега и три босса позади — юрист года!",
                30, TextMain, TextAnchor.MiddleCenter, false);
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 84f), new Vector2(840f, 110f));

            _finaleScoreText = MakeText(root, "score", "Итоговый счёт: 0", 46, Gold,
                TextAnchor.MiddleCenter, true);
            Place(_finaleScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(840f, 70f));

            MakeButton(root, "again", "ИГРАТЬ СНОВА", 34, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -128f), new Vector2(420f, 84f),
                new Color(0.85f, 0.62f, 0.12f, 0.97f), new Color(0.1f, 0.07f, 0.02f),
                delegate { _gm.ShowMainMenu(); });
            MakeButton(root, "select", "ВЫБОР УРОВНЯ", 28, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -226f), new Vector2(420f, 70f),
                new Color(0.12f, 0.2f, 0.34f, 0.95f), TextMain, delegate { _gm.ShowLevelSelect(); });

            return AttachScreen(root);
        }

        /// <summary>Экран босса: табличка с именем, реплика, карточки ответов
        /// (2 колонки, до 10 штук, хоткеи 1..9/0), индикатор попыток.</summary>
        private ScreenController BuildBoss()
        {
            RectTransform root = MakeScreenRoot("Screen_Boss");

            // Затемнение нижней половины — карточки читаются поверх мира.
            Image dim = MakeImage(root, "dim", MaterialFactory.WhiteSprite(), new Color(0f, 0f, 0f, 0.40f));
            RectTransform dimRt = dim.rectTransform;
            dimRt.anchorMin = new Vector2(0f, 0f);
            dimRt.anchorMax = new Vector2(1f, 0.49f);
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;

            // Табличка имени: синяя плашка в золотой рамке с медальоном.
            Image frame = MakePanel(root, "titleFrame", Gold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(1180f, 130f));
            Image plate = MakePanel(frame.transform, "titlePlate", new Color(0.05f, 0.08f, 0.21f, 0.97f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1166f, 116f));
            _bossTitle = MakeText(plate.transform, "title", "БОСС", 42, Gold, TextAnchor.MiddleCenter, true);
            StretchFull(_bossTitle.rectTransform);
            Image medal = MakeImage(frame.transform, "medal", MaterialFactory.CircleSprite(), Gold);
            Place(medal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 2f), new Vector2(58f, 58f));
            Text medalTxt = MakeText(medal.transform, "txt", "РФ", 24,
                new Color(0.08f, 0.10f, 0.24f), TextAnchor.MiddleCenter, true);
            StretchFull(medalTxt.rectTransform);

            _bossSketch = MakeText(root, "sketch", "", 24, TextDim, TextAnchor.MiddleCenter, false);
            Place(_bossSketch.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -172f), new Vector2(1400f, 34f));

            // Реплика — тёплое золото, задание — светло-голубой; тёмно-синяя
            // обводка держит контраст и на светлой улице, и в тёмном суде.
            Color outlineNavy = new Color(0.03f, 0.05f, 0.14f, 0.95f);
            _bossLine = MakeText(root, "line", "", 34, new Color(1f, 0.88f, 0.45f),
                TextAnchor.MiddleCenter, true);
            _bossLine.horizontalOverflow = HorizontalWrapMode.Wrap;
            AddTextOutline(_bossLine, outlineNavy, 2f);
            Place(_bossLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -214f), new Vector2(1420f, 80f));

            _bossHint = MakeText(root, "hint", "", 30, new Color(0.78f, 0.90f, 1f),
                TextAnchor.MiddleCenter, true);
            AddTextOutline(_bossHint, outlineNavy, 2f);
            Place(_bossHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -298f), new Vector2(1200f, 38f));

            _bossSubHint = MakeText(root, "subHint", "", 24, TextDim, TextAnchor.MiddleCenter, false);
            Place(_bossSubHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -340f), new Vector2(1400f, 30f));

            // Реакция босса на выбранный ответ.
            _bossReaction = MakeText(root, "reaction", "", 28, TextMain, TextAnchor.MiddleCenter, true);
            _bossReaction.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_bossReaction.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 474f), new Vector2(1400f, 66f));

            // Попытки: три золотых кругляша.
            Text attemptsCaption = MakeText(root, "attemptsCaption", "ПОПЫТКИ", 22, TextDim,
                TextAnchor.MiddleRight, true);
            Place(attemptsCaption.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-196f, -40f), new Vector2(170f, 30f));
            for (int i = 0; i < _bossAttemptIcons.Length; i++)
            {
                Image icon = MakeImage(root, "attempt" + i, MaterialFactory.CircleSprite(), Gold);
                Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-140f + i * 46f, -36f), new Vector2(36f, 36f));
                _bossAttemptIcons[i] = icon;
            }

            // Боковой фотореалистичный cut-in: держится во время boss encounter,
            // не заменяя low-poly фигуру босса в 3D-сцене.
            Image cutFrame = MakePanel(root, "cutInFrame", Gold,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(30f, 34f), new Vector2(354f, 354f));
            _bossCutInFrame = cutFrame.gameObject;
            _bossCutIn = StaticIllustrationFactory.CreateBossCutIn(cutFrame.transform,
                "BossJudge_CutIn", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(332f, 332f));
            _bossCutInFrame.SetActive(false);

            // Карточки ответов: 2 колонки по 5, снизу вверх мира — сверху вниз списка.
            for (int i = 0; i < BossMaxCards; i++)
            {
                int idx = i;
                int col = i % 2;
                int row = i / 2;
                Button card = MakeButton(root, "card" + i, "", 22,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(col == 0 ? -272f : 272f, 384f - row * 78f), new Vector2(528f, 70f),
                    new Color(0.07f, 0.12f, 0.30f, 0.96f), TextMain,
                    delegate { _gm.AnswerBoss(idx); });
                _bossCards[i] = card;
                Text label = card.GetComponentInChildren<Text>();
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.fontSize = 21;
                RectTransform lrt = label.rectTransform;
                lrt.offsetMin = new Vector2(14f, 4f);
                lrt.offsetMax = new Vector2(-14f, -4f);
                _bossCardTexts[i] = label;
            }

            // Картинки-карточки: сетка 5x2, крупная обложка + номер-хоткей.
            for (int i = 0; i < BossMaxCards; i++)
            {
                int idx = i;
                int col = i % 5;
                int row = i / 5;
                Button card = MakeButton(root, "iconCard" + i, "", 20,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-384f + col * 192f, row == 0 ? 240f : 8f), new Vector2(180f, 224f),
                    new Color(0.07f, 0.12f, 0.30f, 0.96f), TextMain,
                    delegate { _gm.AnswerBoss(idx); });
                _bossIconCards[i] = card;

                Image icon = MakeImage(card.transform, "icon", null, Color.white);
                icon.preserveAspect = true;
                RectTransform irt = icon.rectTransform;
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(16f, 12f);
                irt.offsetMax = new Vector2(-16f, -34f);
                _bossIconImages[i] = icon;

                Text num = MakeText(card.transform, "num", "", 24, Gold, TextAnchor.UpperLeft, true);
                Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(12f, -6f), new Vector2(60f, 30f));
                _bossIconNums[i] = num;

                card.gameObject.SetActive(false);
            }

            return AttachScreen(root);
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------
        // Публичный API
        // ------------------------------------------------------------------

        public void ShowScreen(GameState state)
        {
            _menuScreen.Hide(false);
            _levelSelectScreen.Hide(false);
            _pauseScreen.Hide(false);
            _victoryScreen.Hide(false);
            _defeatScreen.Hide(false);
            _finaleScreen.Hide(false);
            _bossScreen.Hide(false);

            bool hudVisible = state == GameState.Playing || state == GameState.Paused;
            if (hudVisible) _hudScreen.Show(false);
            else _hudScreen.Hide(false);

            switch (state)
            {
                case GameState.MainMenu: _menuScreen.Show(false); break;
                case GameState.LevelSelect: _levelSelectScreen.Show(false); break;
                case GameState.Paused: _pauseScreen.Show(false); break;
                case GameState.Victory: _victoryScreen.Show(false); break;
                case GameState.Defeat: _defeatScreen.Show(false); break;
                case GameState.Finale: _finaleScreen.Show(false); break;
                case GameState.Boss: _bossScreen.Show(false); break;
            }

            if (state == GameState.LevelSelect) RefreshLevelSelect();
        }

        public void RefreshLevelSelect()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_cardStatus[i] == null) continue;
                bool unlocked = _gm.UnlockedLevels > i;
                int best = _gm.BestScore(i);
                bool done = best > 0;
                _cardLocks[i].SetActive(!unlocked);
                _cardButtons[i].interactable = unlocked;
                if (!unlocked)
                {
                    _cardStatus[i].text = "ЗАКРЫТО";
                    _cardStatus[i].color = TextDim;
                }
                else if (done)
                {
                    _cardStatus[i].text = "ПРОЙДЕН ★";
                    _cardStatus[i].color = Green;
                }
                else
                {
                    _cardStatus[i].text = "ОТКРЫТ";
                    _cardStatus[i].color = Gold;
                }
                _cardBest[i].text = done ? string.Format("Рекорд: {0}", best) : "Рекорд: —";
            }
        }

        public void UpdateHUD(float dt)
        {
            if (_gm.Levels == null || _gm.Levels.Current == null) return;
            LevelManager lm = _gm.Levels;

            int score = _gm.Score;
            if (score != _cachedScore)
            {
                _cachedScore = score;
                _scoreLabel.text = score.ToString("N0").Replace(",", " ");
            }

            int dist = (int)lm.DistanceTravelled;
            if (dist != _cachedDist)
            {
                _cachedDist = dist;
                _comboLabel.text = string.Format("x{0} · {1} м / {2} м",
                    _gm.Multiplier, dist, (int)lm.Current.Distance);
            }

            string timer = lm.TimerText;
            if (timer != _cachedTimer)
            {
                _cachedTimer = timer;
                _timerLabel.text = "До суда: " + timer;
            }

            if (lm.Current.Index != _cachedLevelIndex)
            {
                _cachedLevelIndex = lm.Current.Index;
                _levelLabel.text = string.Format("Уровень {0} · {1}", lm.Current.Index + 1, lm.Current.Title);
            }

            // Уверенность: цвет от красного к зелёному.
            float conf = _gm.Confidence;
            _confidenceLabel.text = Mathf.RoundToInt(conf) + "%";
            _confidenceFill.fillAmount = conf / 100f;
            Color barColor = conf < 50f
                ? Color.Lerp(Red, Gold, conf / 50f)
                : Color.Lerp(Gold, Green, (conf - 50f) / 50f);
            _confidenceLabel.color = barColor;

            if (_confidencePulse > 0f)
            {
                _confidencePulse = Mathf.Max(0f, _confidencePulse - Time.unscaledDeltaTime * 3f);
                float s = 1f + 0.1f * _confidencePulse;
                _confidenceBar.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                _confidenceBar.localScale = Vector3.one;
            }

            bool boost = _gm.BoostActive;
            _boostStatusLabel.text = boost ? "УСКОРЕНИЕ +25%" : "";
            _shieldChip.SetActive(_gm.ShieldActive);

            if (lm.Goals != null)
            {
                string check = MaterialFactory.CheckMark();
                for (int i = 0; i < _goalLabels.Length && i < lm.Goals.Length; i++)
                {
                    GoalRuntime g = lm.Goals[i];
                    string prefix = g.Done ? check + " " : "· ";
                    string body;
                    if (g.Spec.Kind == GoalKind.ReachMultiplier)
                    {
                        body = string.Format("{0} x{1} (сейчас x{2})", g.Spec.Label, g.Spec.Target, _gm.Multiplier);
                    }
                    else
                    {
                        body = string.Format("{0} {1}/{2}", g.Spec.Label, g.Current, g.Spec.Target);
                    }
                    _goalLabels[i].text = prefix + body;
                    _goalLabels[i].color = g.Done ? Green : TextMain;
                }
            }

            if (_boostRing != null)
            {
                _boostRing.SetState(Mathf.Clamp01(conf / 75f), boost);
            }
        }

        public void PulseConfidence()
        {
            _confidencePulse = 1f;
        }

        public void FlashHit(float strength)
        {
            if (_flash != null) _flash.Flash(strength);
        }

        /// <summary>Заполнить экран босса данными и перемешанными карточками.
        /// Если у всех вариантов есть Icon — показываются картинки вместо слов.</summary>
        public void ShowBoss(BossData boss, int attempts)
        {
            if (_bossTitle == null || boss == null) return;
            _bossTitle.text = boss.Name;
            _bossSketch.text = string.IsNullOrEmpty(boss.SketchLine) ? "" : boss.SketchLine;
            _bossLine.text = boss.IntroLine;
            _bossHint.text = boss.TaskHint;
            _bossSubHint.text = string.IsNullOrEmpty(boss.SubHint) ? "" : boss.SubHint;
            _bossReaction.text = "";

            Sprite cutIn = StaticIllustrationFactory.GetSprite(BossCutInName(boss));
            if (_bossCutInFrame != null)
            {
                _bossCutInFrame.SetActive(cutIn != null);
                if (_bossCutIn != null && cutIn != null) _bossCutIn.sprite = cutIn;
            }

            BossOption[] opts = _gm.CurrentBossOptions;
            bool iconMode = opts != null && opts.Length > 0;
            if (iconMode)
            {
                for (int i = 0; i < opts.Length; i++)
                {
                    if (string.IsNullOrEmpty(opts[i].Icon)) { iconMode = false; break; }
                }
            }

            for (int i = 0; i < BossMaxCards; i++)
            {
                bool used = opts != null && i < opts.Length;
                int hotkey = (i + 1) % 10; // карточка 10 вешается на клавишу 0

                _bossCards[i].gameObject.SetActive(used && !iconMode);
                if (used && !iconMode)
                {
                    _bossCardTexts[i].text = hotkey + ".  " + opts[i].Text;
                    _bossCardTexts[i].color = TextMain;
                    _bossCards[i].interactable = true;
                    _bossCards[i].image.color = new Color(0.07f, 0.12f, 0.30f, 0.96f);
                }

                _bossIconCards[i].gameObject.SetActive(used && iconMode);
                if (used && iconMode)
                {
                    _bossIconImages[i].sprite = MaterialFactory.PassportCoverSprite(opts[i].Icon);
                    _bossIconImages[i].color = Color.white;
                    _bossIconNums[i].text = hotkey.ToString();
                    _bossIconCards[i].interactable = true;
                    _bossIconCards[i].image.color = new Color(0.07f, 0.12f, 0.30f, 0.96f);
                }
            }
            UpdateBossAttempts(attempts);
        }

        private string BossCutInName(BossData boss)
        {
            if (boss == null) return "BossJudge_CutIn";
            switch (boss.Id)
            {
                case "deputy": return "BossDeputy_CutIn";
                case "chairman": return "BossChairman_CutIn";
                default: return "BossJudge_CutIn";
            }
        }

        /// <summary>Реплика босса после ответа: золото на успех, ехидство на промах.</summary>
        public void ShowBossReaction(string text, bool positive)
        {
            if (_bossReaction == null) return;
            _bossReaction.text = text;
            _bossReaction.color = positive ? Gold : new Color(0.98f, 0.55f, 0.42f);
        }

        /// <summary>Погасить кругляши израсходованных попыток.</summary>
        public void UpdateBossAttempts(int left)
        {
            for (int i = 0; i < _bossAttemptIcons.Length; i++)
            {
                if (_bossAttemptIcons[i] == null) continue;
                _bossAttemptIcons[i].color = i < left
                    ? Gold
                    : new Color(0.24f, 0.21f, 0.19f, 0.85f);
            }
        }

        /// <summary>Затемнить неверную карточку, чтобы не кликали повторно.</summary>
        public void MarkBossOptionWrong(int index)
        {
            if (index < 0 || index >= BossMaxCards) return;
            if (_bossCards[index] != null && _bossCards[index].gameObject.activeSelf)
            {
                _bossCards[index].interactable = false;
                _bossCards[index].image.color = new Color(0.15f, 0.08f, 0.10f, 0.92f);
                if (_bossCardTexts[index] != null)
                {
                    _bossCardTexts[index].color = new Color(0.55f, 0.46f, 0.46f);
                }
            }
            if (_bossIconCards[index] != null && _bossIconCards[index].gameObject.activeSelf)
            {
                _bossIconCards[index].interactable = false;
                _bossIconCards[index].image.color = new Color(0.15f, 0.08f, 0.10f, 0.92f);
                if (_bossIconImages[index] != null)
                {
                    _bossIconImages[index].color = new Color(0.45f, 0.40f, 0.40f, 0.9f);
                }
            }
        }

        /// <summary>Крупный тост события: золотая табличка с гербом для суперприза,
        /// кислотная рамка для дебаффа.</summary>
        public void ShowEventToast(string title, string body, bool super)
        {
            if (_toastTitle == null) return;
            Color accent = super ? Gold : new Color(0.66f, 0.95f, 0.30f);
            _toastTitle.text = title;
            _toastTitle.color = accent;
            _toastBody.text = body;
            _toastStripe.color = accent;
            if (_toastFrame != null)
            {
                _toastFrame.color = super ? Gold : new Color(0.45f, 0.72f, 0.16f);
            }
            if (_toastPanel != null)
            {
                _toastPanel.color = super ? new Color(0.05f, 0.08f, 0.21f, 0.97f)
                                          : new Color(0.06f, 0.10f, 0.04f, 0.97f);
            }
            if (_toastMedal != null) _toastMedal.SetActive(super);
            _toastUntil = Time.unscaledTime + 3.6f;
            _toastPop = 1f;
            _toastGroup.alpha = 1f;
            if (super && _superFlash != null) _superFlash.Flash(0.55f);
        }

        private void Update()
        {
            if (_toastGroup == null) return;
            float now = Time.unscaledTime;
            if (now < _toastUntil)
            {
                // Короткий «выстрел» масштаба при появлении, затем плавное затухание.
                _toastPop = Mathf.Max(0f, _toastPop - Time.unscaledDeltaTime * 5f);
                float pop = 1f + 0.14f * _toastPop * _toastPop;
                _toastRt.localScale = new Vector3(pop, pop, 1f);
                float remain = _toastUntil - now;
                _toastGroup.alpha = Mathf.Clamp01(remain / 0.45f);
            }
            else if (_toastGroup.alpha > 0f)
            {
                _toastGroup.alpha = 0f;
            }
        }

        public void ShowVictory(int finalScore)
        {
            LevelData data = _gm.Levels.Current;
            _victoryStats.text = string.Format(
                "Счёт: {0}\nУверенность на финише: {1}%\nСобрано бустов: {2}\nСтолкновений: {3}",
                finalScore, Mathf.RoundToInt(_gm.Confidence), _gm.PickupsCollectedOnLevel, _gm.HitsOnLevel);
            bool last = data != null && data.Index >= LevelCatalog.LevelCount - 1;
            if (_nextButtonLabel != null)
            {
                _nextButtonLabel.text = last ? "ФИНАЛ" : "СЛЕДУЮЩИЙ УРОВЕНЬ";
            }
        }

        public void ShowDefeat(string reason)
        {
            _defeatReasonText.text = reason + "\n\nСоберите бусты уверенности и попробуйте ещё раз!";
        }

        public void ShowFinale(int totalScore)
        {
            _finaleScoreText.text = string.Format("Итоговый счёт: {0}", totalScore);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
        }
    }
}
