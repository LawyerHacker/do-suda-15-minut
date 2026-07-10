using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Описание препятствия.</summary>
    public class ObstacleSpec
    {
        public string Id;
        public string Label;
        public ObstacleKind Kind;
        public float PenaltyMin;
        public float PenaltyMax;

        // «Ядовитые» препятствия: редкие, с дебаффом и крупным тостом.
        public bool Toxic;
        public string ToastTitle;
        public string ToastBody;
        public float SlowFactor = 1f;    // множитель скорости мира (0..1)
        public float SlowDuration;       // секунды замедления
        public float WeakJumpDuration;   // секунды ослабленного прыжка
        public float ScoreDebuffDuration; // секунды «очки за бусты вдвое меньше» (бан подписки ИИ)

        public ObstacleSpec(string id, string label, ObstacleKind kind, float penMin, float penMax)
        {
            Id = id; Label = label; Kind = kind; PenaltyMin = penMin; PenaltyMax = penMax;
        }

        public static ObstacleSpec MakeToxic(string id, string label, ObstacleKind kind,
            float penMin, float penMax, string toastTitle, string toastBody,
            float slowFactor, float slowDuration, float weakJumpDuration,
            float scoreDebuffDuration = 0f)
        {
            ObstacleSpec s = new ObstacleSpec(id, label, kind, penMin, penMax);
            s.Toxic = true;
            s.ToastTitle = toastTitle;
            s.ToastBody = toastBody;
            s.SlowFactor = slowFactor;
            s.SlowDuration = slowDuration;
            s.WeakJumpDuration = weakJumpDuration;
            s.ScoreDebuffDuration = scoreDebuffDuration;
            return s;
        }
    }

    /// <summary>Описание буста (артефакта уверенности).</summary>
    public class PickupSpec
    {
        public string Id;
        public string Label;
        public float ConfidenceGain; // проценты уверенности
        public int ScoreValue;
        public bool Secondary;       // true -> зелёная подсветка, иначе золотая

        // Суперпризы: редкие события с эффектом, слоу-мо и крупным тостом.
        public bool Super;
        public string SuperId;       // cover | glossa | vas | writ | plenum
        public string ToastTitle;
        public string ToastBody;

        public PickupSpec(string id, string label, float conf, int score, bool secondary)
        {
            Id = id; Label = label; ConfidenceGain = conf; ScoreValue = score; Secondary = secondary;
        }

        public static PickupSpec MakeSuper(string id, string superId, string label,
            float conf, int score, string toastTitle, string toastBody)
        {
            PickupSpec s = new PickupSpec(id, label, conf, score, false);
            s.Super = true;
            s.SuperId = superId;
            s.ToastTitle = toastTitle;
            s.ToastBody = toastBody;
            return s;
        }
    }

    /// <summary>Цель уровня (информационная, победа считается по дистанции).</summary>
    public class GoalSpec
    {
        public GoalKind Kind;
        public string Label;
        public int Target;

        public GoalSpec(GoalKind kind, string label, int target)
        {
            Kind = kind; Label = label; Target = target;
        }
    }

    /// <summary>Палитра уровня.</summary>
    public class LevelPalette
    {
        public Color Sky;
        public Color FogColor;
        public Color Ambient;
        public Color KeyLight;
        public Color Floor;
        public Color FloorAccent;
        public Color LaneLine;
        public Color Wall;
        public Color WallAccent;
        public Color Ceiling;
        public Color SignBg;
        public Color SignText;
        public Color Danger;
        public Color BoostPrimary;
        public Color BoostSecondary;
        public float FogStart;
        public float FogEnd;
        public bool Indoor;
    }

    /// <summary>Полные данные уровня.</summary>
    public class LevelData
    {
        public int Index;
        public string Title;
        public string Subtitle;
        public string Description;
        public int Theme;
        public float Distance;      // метры
        public float BaseSpeed;     // м/с
        public float WaveInterval;  // секунды между волнами
        public float TimerSeconds;  // «До суда: mm:ss» на старте
        public LevelPalette Palette;
        public ObstacleSpec[] Obstacles;
        public PickupSpec[] Pickups;
        public ObstacleSpec[] ToxicObstacles; // редкие «ядовитые» препятствия с дебаффами
        public PickupSpec[] SuperPickups;     // редкие суперпризы-события
        public GoalSpec[] Goals;
        public string[] WallSigns;  // русские вывески на стенах/фоне
    }

    /// <summary>Статические данные всех трёх уровней.</summary>
    public static class LevelCatalog
    {
        public const int LevelCount = 3;
        public const float LaneWidth = 2.4f;

        private static LevelData[] _levels;

        public static LevelData Get(int index)
        {
            if (_levels == null) Build();
            index = Mathf.Clamp(index, 0, LevelCount - 1);
            return _levels[index];
        }

        /// <summary>Суперпризы: одинаковый пул на всех уровнях, спавнятся редко.</summary>
        private static PickupSpec[] BuildSuperPickups()
        {
            return new PickupSpec[]
            {
                PickupSpec.MakeSuper("super_cover", "cover", "Паспорт для обложки", 10f, 500,
                    "ПАСПОРТ ДЛЯ ОБЛОЖКИ",
                    "Ты нашёл паспорт для обложки. Теперь судья Верховного суда не сможет тебя наругать."),
                PickupSpec.MakeSuper("super_glossa", "glossa", "Мудрость Glossa", 25f, 500,
                    "МУДРОСТЬ GLOSSA",
                    "Ты постиг мудрость авторов сборника Glossa. Теперь ты подкован теоретически больше любого судьи."),
                PickupSpec.MakeSuper("super_vas", "vas", "Ностальгия по ВАС РФ", 12f, 500,
                    "НОСТАЛЬГИЯ ПО ВАС РФ",
                    "Ты вспомнил лучшие судебные акты Высшего Арбитражного Суда. В тебе пробудились силы."),
                PickupSpec.MakeSuper("super_writ", "writ", "Исполнительный лист", 6f, 500,
                    "ИСПОЛНИТЕЛЬНЫЙ ЛИСТ",
                    "Исполнительный лист у тебя на руках. 8 секунд можно сносить любые преграды."),
                PickupSpec.MakeSuper("super_plenum", "plenum", "Разъяснения Пленума", 8f, 500,
                    "ПЛЕНУМ ВСЁ РАЗЪЯСНИЛ",
                    "Пленум ВС всё разъяснил. Доказательства сами тянутся к тебе."),
                PickupSpec.MakeSuper("super_ai", "ai", "Юрист освоил ИИ", 10f, 500,
                    "ЮРИСТ ОСВОИЛ ИИ",
                    "Нейросеть пишет процессуалку за секунды. 10 секунд все очки удвоены!")
            };
        }

        private static void Build()
        {
            _levels = new LevelData[LevelCount];
            PickupSpec[] supers = BuildSuperPickups();

            // Уровень 1: вместо «Паспорта для обложки» — «Попал в Право-300».
            // Тот же щит-эффект (SuperId="cover"): статус прикрывает от задержки.
            PickupSpec[] supersL1 = new PickupSpec[supers.Length];
            for (int i = 0; i < supers.Length; i++)
            {
                supersL1[i] = supers[i].Id == "super_cover"
                    ? PickupSpec.MakeSuper("super_pravo300", "cover", "Попал в Право-300", 10f, 500,
                        "ПОПАЛ В ПРАВО-300",
                        "Фирма ворвалась в рейтинг. Статус сияет: следующая задержка тебе не страшна.")
                    : supers[i];
            }

            // ---------- УРОВЕНЬ 1: ОФИС ----------
            // Референс: предзакатное солнце в окнах, тёплое дерево, золотые блики.
            LevelPalette office = new LevelPalette();
            office.Sky = new Color(0.86f, 0.72f, 0.55f);
            office.FogColor = new Color(0.78f, 0.64f, 0.48f);
            office.Ambient = new Color(0.44f, 0.41f, 0.38f);
            office.KeyLight = new Color(1.0f, 0.87f, 0.64f);
            office.Floor = new Color(0.36f, 0.31f, 0.26f);
            office.FloorAccent = new Color(0.96f, 0.88f, 0.70f);
            office.LaneLine = new Color(1.0f, 0.90f, 0.55f);
            office.Wall = new Color(0.82f, 0.75f, 0.63f);
            office.WallAccent = new Color(0.46f, 0.32f, 0.20f);
            office.Ceiling = new Color(0.85f, 0.81f, 0.73f);
            office.SignBg = new Color(0.08f, 0.12f, 0.26f);
            office.SignText = new Color(1.0f, 0.90f, 0.62f);
            office.Danger = new Color(0.92f, 0.20f, 0.14f);
            office.BoostPrimary = new Color(1.0f, 0.80f, 0.24f);
            office.BoostSecondary = new Color(0.35f, 0.95f, 0.45f);
            office.FogStart = 42f;
            office.FogEnd = 150f;
            office.Indoor = true;

            LevelData l1 = new LevelData();
            l1.Index = 0;
            l1.Title = "Москва-Сити, офис";
            l1.Subtitle = "Уровень 1 · Разминка";
            l1.Description = "Вы спешите в суд, покиньте быстрее офис в Москва-Сити.";
            l1.Theme = LevelTheme.Office;
            l1.Distance = 1250f;
            l1.BaseSpeed = 15f;
            l1.WaveInterval = 1.2f;
            l1.TimerSeconds = 900f; // 15:00
            l1.Palette = office;
            l1.Obstacles = new ObstacleSpec[]
            {
                new ObstacleSpec("folders",     "Стопка срочных папок",    ObstacleKind.Block, 10f, 14f),
                new ObstacleSpec("deadline",    "ДЕДЛАЙН!",                ObstacleKind.Block, 12f, 16f),
                new ObstacleSpec("coffee",      "Разлитый кофе",           ObstacleKind.Jump,  10f, 13f),
                new ObstacleSpec("voice",       "Голосовое 05:47",         ObstacleKind.Jump,  10f, 13f),
                new ObstacleSpec("edits",       "Правки перед заседанием", ObstacleKind.Slide, 11f, 15f),
                new ObstacleSpec("attachments", "Забытые приложения",      ObstacleKind.Jump,  10f, 13f)
            };
            l1.Pickups = new PickupSpec[]
            {
                new PickupSpec("power",     "Доверенность",        12f, 200, false),
                new PickupSpec("coffeecup", "Кофе перед судом",     8f, 100, true),
                new PickupSpec("casefile",  "Папка дела",          10f, 150, false),
                new PickupSpec("call",      "Спокойный звонок",     9f, 120, true),
                new PickupSpec("plan",      "План выступления",    15f, 300, false),
                new PickupSpec("claim",     "Исковое заявление",   12f, 250, false)
            };
            l1.SuperPickups = supersL1;
            l1.ToxicObstacles = new ObstacleSpec[]
            {
                ObstacleSpec.MakeToxic("pdf700", "700 страниц одним PDF", ObstacleKind.Jump, 10f, 14f,
                    "700 СТРАНИЦ ОДНИМ PDF",
                    "Оппонент приложил 700 страниц сканов одним PDF. Портфель резко потяжелел.",
                    1f, 0f, 6f),
                ObstacleSpec.MakeToxic("flip", "Позиция поменялась", ObstacleKind.Slide, 12f, 16f,
                    "ПОЗИЦИЯ ПОМЕНЯЛАСЬ",
                    "Позиция поменялась после вопроса суда. Главное — держать лицо.",
                    0.78f, 3f, 0f),
                ObstacleSpec.MakeToxic("aiban", "Подписка на ИИ забанена", ObstacleKind.Block, 10f, 14f,
                    "ПОДПИСКА НА ИИ ЗАБАНЕНА",
                    "Разработчик ИИ забанил твой тариф в самый нужный момент. Пиши руками: очки вдвое меньше.",
                    1f, 0f, 0f, 8f)
            };
            l1.Goals = new GoalSpec[]
            {
                new GoalSpec(GoalKind.CollectPickups,  "Собрать бусты",     20),
                new GoalSpec(GoalKind.AvoidObstacles,  "Избежать задержек", 40),
                new GoalSpec(GoalKind.ReachMultiplier, "Множитель",          2)
            };
            l1.WallSigns = new string[]
            {
                "Дело не ждёт",
                "Закон на нашей стороне",
                "Зал суда — 15 минут",
                "Тише! Идёт подготовка",
                "Кофе — топливо юриста",
                "Дедлайн уже здесь",
                "КАД: проверь зал заседания",
                "Сроки — это святое",
                "КонсультантПлюс: базы обновлены",
                "Практика свежая — КонсультантПлюс",
                "БЦ «Москва-Сити» · 57 этаж",
                "Переговорка «Федерация» — занято"
            };
            _levels[0] = l1;

            // ---------- УРОВЕНЬ 2: РЕЕСТР И КВАРТИРА ----------
            // Референс: golden hour у фасада АС Москвы — персиковое небо,
            // мокрый глянцевый асфальт, тёплый камень, синие таблички с золотом.
            LevelPalette street = new LevelPalette();
            street.Sky = new Color(0.95f, 0.68f, 0.42f);
            street.FogColor = new Color(0.88f, 0.62f, 0.38f);
            street.Ambient = new Color(0.48f, 0.42f, 0.38f);
            street.KeyLight = new Color(1.0f, 0.76f, 0.46f);
            street.Floor = new Color(0.12f, 0.12f, 0.14f);
            street.FloorAccent = new Color(0.96f, 0.90f, 0.78f);
            street.LaneLine = new Color(1.0f, 0.84f, 0.32f);
            street.Wall = new Color(0.66f, 0.55f, 0.44f);
            street.WallAccent = new Color(0.12f, 0.20f, 0.42f);
            street.Ceiling = new Color(0.95f, 0.74f, 0.50f);
            street.SignBg = new Color(0.06f, 0.12f, 0.30f);
            street.SignText = new Color(1.0f, 0.88f, 0.58f);
            street.Danger = new Color(0.95f, 0.23f, 0.13f);
            street.BoostPrimary = new Color(1.0f, 0.80f, 0.24f);
            street.BoostSecondary = new Color(0.30f, 0.95f, 0.55f);
            street.FogStart = 58f;
            street.FogEnd = 190f;
            street.Indoor = false;

            LevelData l2 = new LevelData();
            l2.Index = 1;
            l2.Title = "Большая Тульская";
            l2.Subtitle = "Уровень 2 · Городской спор";
            l2.Description = "Вам нужно добежать до здания АСГМ.";
            l2.Theme = LevelTheme.Street;
            l2.Distance = 1350f;
            l2.BaseSpeed = 17.5f;
            l2.WaveInterval = 1.0f;
            l2.TimerSeconds = 1000f; // 16:40
            l2.Palette = street;
            l2.Obstacles = new ObstacleSpec[]
            {
                new ObstacleSpec("fraud",     "Мошенничество обнаружено", ObstacleKind.Block, 13f, 18f),
                new ObstacleSpec("rumors",    "Лужа «Слухи»",             ObstacleKind.Jump,  12f, 16f),
                new ObstacleSpec("transfer",  "Подтвердить перевод?",     ObstacleKind.Block, 14f, 20f),
                new ObstacleSpec("wires",     "Провода мошенников",       ObstacleKind.Jump,  12f, 16f),
                new ObstacleSpec("contracts", "Стопки договоров",         ObstacleKind.Block, 13f, 18f),
                new ObstacleSpec("press",     "Журналисты",               ObstacleKind.Slide, 13f, 18f),
                new ObstacleSpec("atm",       "Банкомат: ОШИБКА",         ObstacleKind.Block, 14f, 20f)
            };
            l2.Pickups = new PickupSpec[]
            {
                new PickupSpec("egrn",     "Выписка ЕГРН",         12f, 200, false),
                new PickupSpec("keys",     "Ключи от квартиры",    10f, 150, false),
                new PickupSpec("notary",   "Нотариальный договор", 12f, 250, false),
                new PickupSpec("argument", "Правовой довод",       15f, 300, true),
                new PickupSpec("client",   "Спокойный клиент",      8f, 100, true),
                new PickupSpec("scales",   "Весы правосудия",      12f, 250, false)
            };
            l2.SuperPickups = supers;
            l2.ToxicObstacles = new ObstacleSpec[]
            {
                ObstacleSpec.MakeToxic("kad502", "КАД лёг", ObstacleKind.Block, 8f, 12f,
                    "КАД ЛЁГ",
                    "Картотека арбитражных дел легла за 10 минут до заседания. Дышите глубже.",
                    0.75f, 5f, 0f),
                ObstacleSpec.MakeToxic("monopoly", "Адвокатская монополия", ObstacleKind.Slide, 10f, 14f,
                    "МОНОПОЛИЯ. ПЕРВОЕ ЧТЕНИЕ",
                    "Госдума приняла в первом чтении закон об адвокатской монополии. Короткая паника.",
                    0.7f, 3f, 0f)
            };
            l2.Goals = new GoalSpec[]
            {
                new GoalSpec(GoalKind.CollectPickups,  "Собрать выписки и бусты", 25),
                new GoalSpec(GoalKind.AvoidObstacles,  "Избежать задержек",       55),
                new GoalSpec(GoalKind.ReachMultiplier, "Множитель",                3)
            };
            l2.WallSigns = new string[]
            {
                "РОСРЕЕСТР",
                "МФЦ · Мои документы",
                "Получить выписку онлайн",
                "Квартира с двойной продажей?",
                "БАНК «НАДЁЖНЫЙ»",
                "Не переводите деньги незнакомцам",
                "Новости: спорная сделка века",
                "КонсультантПлюс · региональный центр",
                "Москва-Сити: башни делят этажами",
                "Б. Тульская, 17 — 300 м",
                "РОСРЕЕСТР · приём до 20:00"
            };
            _levels[1] = l2;

            // ---------- УРОВЕНЬ 3: КОРИДОРЫ СУДА ----------
            // Референс: мраморный зал в тёмном золоте — навью-синие таблички
            // с золотым текстом, светлый мрамор, тёмное дерево, красная дорожка.
            LevelPalette court = new LevelPalette();
            court.Sky = new Color(0.05f, 0.06f, 0.11f);
            court.FogColor = new Color(0.24f, 0.16f, 0.09f);
            court.Ambient = new Color(0.42f, 0.40f, 0.44f);
            court.KeyLight = new Color(1.0f, 0.80f, 0.52f);
            court.Floor = new Color(0.70f, 0.68f, 0.64f);
            court.FloorAccent = new Color(0.58f, 0.05f, 0.05f);
            court.LaneLine = new Color(1.0f, 0.80f, 0.30f);
            court.Wall = new Color(0.36f, 0.24f, 0.15f);
            court.WallAccent = new Color(0.82f, 0.78f, 0.72f);
            court.Ceiling = new Color(0.19f, 0.17f, 0.22f);
            court.SignBg = new Color(0.07f, 0.10f, 0.26f);
            court.SignText = new Color(1.0f, 0.85f, 0.48f);
            court.Danger = new Color(1.0f, 0.16f, 0.10f);
            court.BoostPrimary = new Color(1.0f, 0.80f, 0.25f);
            court.BoostSecondary = new Color(0.35f, 0.95f, 0.50f);
            court.FogStart = 42f;
            court.FogEnd = 160f;
            court.Indoor = true;

            LevelData l3 = new LevelData();
            l3.Index = 2;
            l3.Title = "Зал №3";
            l3.Subtitle = "Уровень 3 · Финальный рывок";
            l3.Description = "Вам нужно добиться правосудия.";
            l3.Theme = LevelTheme.Court;
            l3.Distance = 1500f;
            l3.BaseSpeed = 20f;
            l3.WaveInterval = 0.85f;
            l3.TimerSeconds = 570f; // 09:30
            l3.Palette = court;
            l3.Obstacles = new ObstacleSpec[]
            {
                new ObstacleSpec("queue",    "Очередь на вход",        ObstacleKind.Block, 18f, 22f),
                new ObstacleSpec("frame",    "Рамка безопасности",     ObstacleKind.Slide, 18f, 23f),
                new ObstacleSpec("bailiff",  "Пристав",                ObstacleKind.Block, 20f, 25f),
                new ObstacleSpec("nopower",  "Нет доверенности!",      ObstacleKind.Block, 18f, 23f),
                new ObstacleSpec("skull",    "Паспорт с черепом",      ObstacleKind.Jump,  18f, 22f),
                new ObstacleSpec("ailink",   "Фейковая ссылка ИИ",     ObstacleKind.Jump,  19f, 24f),
                new ObstacleSpec("fine",     "ШТРАФ",                  ObstacleKind.Jump,  18f, 22f),
                new ObstacleSpec("oldlaw",   "Неактуальная практика",  ObstacleKind.Slide, 18f, 23f)
            };
            l3.Pickups = new PickupSpec[]
            {
                new PickupSpec("cover",       "Обложка паспорта",     10f, 150, false),
                new PickupSpec("pass",        "Пропуск в суд",        12f, 200, false),
                new PickupSpec("perfectcase", "Идеальное дело",       12f, 250, false),
                new PickupSpec("speech",      "Блестящая речь",       15f, 300, false),
                new PickupSpec("practice",    "Проверенная практика", 10f, 150, true),
                new PickupSpec("aicourse",    "Курс «ИИ для юриста»",  8f, 120, true),
                new PickupSpec("lawbook",     "Книга «ЗАКОН»",        12f, 250, false),
                new PickupSpec("scales",      "Весы правосудия",      12f, 250, false)
            };
            l3.SuperPickups = supers;
            l3.ToxicObstacles = new ObstacleSpec[]
            {
                ObstacleSpec.MakeToxic("bribe", "Судья внезапно против", ObstacleKind.Block, 22f, 28f,
                    "ОППОНЕНТ КОРРУМПИРОВАЛ СУДЬЮ",
                    "Ну, по ощущениям. Уверенность падает, факты не помогают.",
                    0.68f, 4f, 0f),
                ObstacleSpec.MakeToxic("chamber", "Совещательная", ObstacleKind.Block, 6f, 10f,
                    "СУДЬЯ УШЁЛ В СОВЕЩАТЕЛЬНУЮ",
                    "И забыл вернуться. Время течёт, зал молчит.",
                    0.72f, 6f, 0f),
                ObstacleSpec.MakeToxic("badpoa", "Доверенность без полномочий", ObstacleKind.Slide, 16f, 22f,
                    "ДОВЕРЕННОСТЬ БЕЗ ПОЛНОМОЧИЙ",
                    "Доверенность есть, полномочий нет. Вообще никаких.",
                    0.8f, 3f, 0f),
                ObstacleSpec.MakeToxic("aiban", "Подписка на ИИ забанена", ObstacleKind.Block, 14f, 18f,
                    "ПОДПИСКА НА ИИ ЗАБАНЕНА",
                    "Разработчик ИИ забанил аккаунт прямо у дверей зала. Позиция — по памяти, очки вдвое меньше.",
                    1f, 0f, 0f, 8f)
            };
            l3.Goals = new GoalSpec[]
            {
                new GoalSpec(GoalKind.CollectPickups,  "Собрать бусты",     30),
                new GoalSpec(GoalKind.AvoidObstacles,  "Избежать задержек", 70),
                new GoalSpec(GoalKind.ReachMultiplier, "Множитель",          3)
            };
            l3.WallSigns = new string[]
            {
                "АС города Москвы",
                "Картотека арбитражных дел",
                "Канцелярия · Перерыв 13:00–14:00",
                "Выдача исполнительных листов",
                "Ознакомление — строго по записи",
                "Судебные акты",
                "Пропуска · Предъявите паспорт",
                "Тишина! Идёт заседание",
                "Зал №3",
                "Ознакомление с материалами дел",
                "Приставы бдят",
                "Кассация не прощает",
                "Зал ознакомления: КонсультантПлюс",
                "Wi-Fi суда — пароль у пристава"
            };
            _levels[2] = l3;
        }
    }
}
