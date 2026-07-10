namespace CourtRunner15
{
    /// <summary>Глобальные состояния игры.</summary>
    public enum GameState
    {
        MainMenu,
        LevelSelect,
        Playing,
        Paused,
        Boss,    // мини-сцена с боссом после дистанции уровня
        Victory,
        Defeat,
        Finale
    }

    /// <summary>Тип препятствия: как его честно пройти.</summary>
    public enum ObstacleKind
    {
        Block, // занимает всю высоту — сменить полосу
        Jump,  // низкое — перепрыгнуть
        Slide  // верхнее — проскочить подкатом
    }

    /// <summary>Тип цели уровня.</summary>
    public enum GoalKind
    {
        CollectPickups,
        AvoidObstacles,
        ReachMultiplier
    }

    /// <summary>Тема окружения уровня.</summary>
    public static class LevelTheme
    {
        public const int Office = 0;
        public const int Street = 1;
        public const int Court = 2;
    }
}
