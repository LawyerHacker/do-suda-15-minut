using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Вариант ответа боссу.</summary>
    public class BossOption
    {
        public string Text;
        public bool Correct;
        public string Reaction; // персональная ехидная реплика на этот неверный ответ
        public string Icon;     // id спрайта-картинки (MaterialFactory.PassportCoverSprite);
                                // если у всех вариантов есть Icon — карточки рисуются
                                // картинками вместо текста

        public BossOption(string text, bool correct)
        {
            Text = text; Correct = correct;
        }

        public BossOption(string text, bool correct, string reaction)
            : this(text, correct)
        {
            Reaction = reaction;
        }

        public BossOption(string text, bool correct, string reaction, string icon)
            : this(text, correct, reaction)
        {
            Icon = icon;
        }
    }

    /// <summary>
    /// Босс — сатирическая мини-сцена после уровня: перегороженный проход,
    /// реплика, выбор ответа. 3 попытки; провал — уровень заново, успех —
    /// проход открыт (после финального босса — победный экран).
    /// </summary>
    public class BossData
    {
        public string Id;               // judge | deputy | chairman — выбирает 3D-образ
        public string Name;             // крупный заголовок
        public string SketchLine;       // ремарка-сценка (мелким над репликой)
        public string IntroLine;        // главная реплика босса
        public string TaskHint;         // что нужно выбрать
        public string SubHint;          // доп. строка под TaskHint, над карточками (опционально)
        public BossOption[] Options;
        public string[] WrongReactions; // общий пул ехидных реакций (если у варианта нет своей)
        public string WinLine;          // реакция на правильный ответ
        public string DefeatLine;       // реплика, когда попытки кончились
        public int Attempts = 3;
        public bool ShuffleOptions = true;
    }

    /// <summary>Каталог боссов: после уровня N появляется босс N. Чтобы добавить
    /// нового босса — дописать BossData здесь и (по желанию) новый образ
    /// в ProceduralModelFactory.CreateBossFigure.</summary>
    public static class BossCatalog
    {
        private static BossData[] _bosses;

        public static BossData Get(int levelIndex)
        {
            if (_bosses == null) Build();
            levelIndex = Mathf.Clamp(levelIndex, 0, _bosses.Length - 1);
            return _bosses[levelIndex];
        }

        public static int Count
        {
            get { if (_bosses == null) Build(); return _bosses.Length; }
        }

        private static void Build()
        {
            _bosses = new BossData[3];

            // ---------- БОСС 1: СУДЬЯ ВЕРХОВНОГО СУДА ----------
            BossData judge = new BossData();
            judge.Id = "judge";
            judge.Name = "СУДЬЯ ВЕРХОВНОГО СУДА";
            judge.SketchLine = "Вы показываете паспорт. Судья медленно поднимает глаза на обложку…";
            judge.IntroLine = "«С такой обложкой паспорта в правосудие нельзя»";
            judge.TaskHint = "Выберите правильную обложку паспорта:";
            judge.Options = new BossOption[]
            {
                new BossOption("Обложка с жёлтой мульт-губкой", false,
                    "«Мульт-губка? Здесь вам не пятый арбитражный по делам подводных пекарен».", "sponge"),
                new BossOption("Обложка с аниме", false,
                    "«Аниме-тян в деле не заявлена. Отвод».", "anime"),
                new BossOption("Обложка со скелетом", false,
                    "«Скелет уже был. Мы это проходили. ВСЯ СТРАНА это проходила».", "skeleton"),
                new BossOption("Обложка с рэперами", false,
                    "«Читайте кодекс, а не рэп. Следующий».", "rappers"),
                new BossOption("Обложка с розовыми губами", false,
                    "«Это валентинка, а не документ. Следующий».", "lips"),
                new BossOption("Розовая обложка", false,
                    "«Слишком легкомысленно для кассации».", "pink"),
                new BossOption("Голубая обложка", false,
                    "«Почти. Но нет. Оттенок не процессуальный».", "lightblue"),
                new BossOption("Синяя обложка", false,
                    "«Тепло. Но синий бывает разный, а доверие — одно».", "blue"),
                new BossOption("Чёрная обложка", false,
                    "«Чёрная? Это к приставам, кабинет напротив».", "black"),
                new BossOption("Обложка с флагом России", true, null, "flag")
            };
            judge.WrongReactions = new string[]
            {
                "«Не позорьте документ»",
                "«Хо-хо. Нет»."
            };
            judge.WinLine = "«Вот. Совсем другое дело. Проходите — и не задерживайте правосудие»";
            judge.DefeatLine = "«Три обложки подряд?! Идите и подумайте о своём документообороте. Заново!»";
            _bosses[0] = judge;

            // ---------- БОСС 2: ДЕПУТАТ ГОСДУМЫ ----------
            BossData deputy = new BossData();
            deputy.Id = "deputy";
            deputy.Name = "ДЕПУТАТ ГОСДУМЫ";
            deputy.SketchLine = "";
            deputy.IntroLine = "«Принимаем адвокатскую монополию. Без статуса адвоката дальше нельзя»";
            deputy.TaskHint = "Как прорваться в суд без статуса адвоката?";
            deputy.Options = new BossOption[]
            {
                new BossOption("Пойти на дело слушателем", false,
                    "«Слушателем? Слушайте отсюда. С галёрки ходатайства не заявляют»."),
                new BossOption("Подделать удостоверение адвоката", false,
                    "«Смело! Статья, кстати, тоже смелая. Следующая попытка — из СИЗО?»"),
                new BossOption("Оспорить закон в Конституционном суде", true),
                new BossOption("Собрать митинг юристов и вынудить власти приостановить закон", true),
                new BossOption("Выйти из профессии юриста", false,
                    "«Отличная идея! Кто-то же должен освободить рынок. Но нет, вы нужны сюжету»."),
                new BossOption("Найти в законопроекте внутреннее противоречие и отправить его на доработку", true)
            };
            deputy.WrongReactions = new string[]
            {
                "«Поправка отклонена. Комитет смеётся»",
                "«Это не пройдёт даже первое чтение»."
            };
            deputy.WinLine = "«Э-э… регламент! Ладно, проект на доработку. Проходите, пока лобби не видит»";
            deputy.DefeatLine = "«Третье чтение прошло, вы — нет. Закон вступил в силу, бегите уровень заново!»";
            _bosses[1] = deputy;

            // ---------- БОСС 3: ПРЕДСЕДАТЕЛЬ ВС КРАСНОВ (ФИНАЛ) ----------
            BossData chairman = new BossData();
            chairman.Id = "chairman";
            chairman.Name = "ПРЕДСЕДАТЕЛЬ ВЕРХОВНОГО СУДА КРАСНОВ";
            chairman.SketchLine = "";
            chairman.IntroLine = "«Независимость судов? Слишком много процессуальной романтики»";
            chairman.TaskHint = "Верните правосудие на уровень ВАС!";
            chairman.SubHint = "Побудите председателя ВС Краснова вспомнить о лучших практиках правосудия";
            chairman.Options = new BossOption[]
            {
                new BossOption("Заставить его прочитать сборник «Глосса»", true),
                new BossOption("Показать ему круглые столы М-Логос", true),
                new BossOption("Пригласить его пройти курсы в НИУ ВШЭ", true),
                new BossOption("Отправить его посмотреть заседания Высокого суда Англии", true),
                new BossOption("Включить архив лучших постановлений ВАС РФ", true),
                new BossOption("Всем судьям необходимо заучить Конституцию и ссылаться на её прямое действие", true)
            };
            chairman.WrongReactions = new string[]
            {
                "«Отказать»." // недостижимо: все варианты верные
            };
            chairman.WinLine = "«…А ведь когда-то я тоже писал мотивировки. Ладно. Правосудие — восстановить. Проход — открыть»";
            chairman.DefeatLine = "«Жалоба оставлена без движения. Навсегда»"; // недостижимо
            _bosses[2] = chairman;
        }
    }
}
