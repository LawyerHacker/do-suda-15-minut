using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Текущее состояние цели уровня.</summary>
    public class GoalRuntime
    {
        public GoalSpec Spec;
        public int Current;

        public bool Done
        {
            get { return Current >= Spec.Target; }
        }
    }

    /// <summary>Прогресс текущего уровня: дистанция, таймер, цели.</summary>
    public class LevelManager : MonoBehaviour
    {
        public LevelData Current;
        public float DistanceTravelled;
        public bool IsComplete;
        public GoalRuntime[] Goals;

        private int _bestMultiplier;

        public float Progress01
        {
            get
            {
                if (Current == null || Current.Distance <= 0f) return 0f;
                return Mathf.Clamp01(DistanceTravelled / Current.Distance);
            }
        }

        /// <summary>Оставшееся «время до суда» пропорционально остатку дистанции.</summary>
        public string TimerText
        {
            get
            {
                if (Current == null) return "--:--";
                float left = Current.TimerSeconds * (1f - Progress01);
                int total = Mathf.Max(0, Mathf.CeilToInt(left));
                int m = total / 60;
                int s = total % 60;
                return string.Format("{0:00}:{1:00}", m, s);
            }
        }

        public void StartLevel(LevelData data)
        {
            Current = data;
            DistanceTravelled = 0f;
            IsComplete = false;
            _bestMultiplier = 1;
            Goals = new GoalRuntime[data.Goals.Length];
            for (int i = 0; i < data.Goals.Length; i++)
            {
                GoalRuntime g = new GoalRuntime();
                g.Spec = data.Goals[i];
                g.Current = 0;
                if (data.Goals[i].Kind == GoalKind.ReachMultiplier) g.Current = 1;
                Goals[i] = g;
            }
        }

        /// <summary>Продвинуть уровень на dt при скорости speed (м/с).</summary>
        public void Tick(float dt, float speed)
        {
            if (Current == null || IsComplete) return;
            DistanceTravelled += speed * dt;
            if (DistanceTravelled >= Current.Distance)
            {
                DistanceTravelled = Current.Distance;
                IsComplete = true;
            }
        }

        /// <summary>Отладка: мгновенно завершить дистанцию (выход на босса).</summary>
        public void DebugCompleteDistance()
        {
            if (Current == null || IsComplete) return;
            DistanceTravelled = Current.Distance;
            IsComplete = true;
        }

        public void RegisterPickup()
        {
            Bump(GoalKind.CollectPickups, 1);
        }

        public void RegisterAvoided()
        {
            Bump(GoalKind.AvoidObstacles, 1);
        }

        public void NotifyMultiplier(int multiplier)
        {
            if (multiplier <= _bestMultiplier) return;
            _bestMultiplier = multiplier;
            if (Goals == null) return;
            for (int i = 0; i < Goals.Length; i++)
            {
                if (Goals[i].Spec.Kind == GoalKind.ReachMultiplier)
                {
                    Goals[i].Current = Mathf.Min(multiplier, Goals[i].Spec.Target);
                }
            }
        }

        private void Bump(GoalKind kind, int amount)
        {
            if (Goals == null) return;
            for (int i = 0; i < Goals.Length; i++)
            {
                if (Goals[i].Spec.Kind == kind)
                {
                    Goals[i].Current = Mathf.Min(Goals[i].Current + amount, Goals[i].Spec.Target);
                }
            }
        }
    }
}
