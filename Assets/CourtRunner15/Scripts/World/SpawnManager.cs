using System.Collections.Generic;
using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Волны препятствий и бустов. Гарантирует честный безопасный путь:
    /// блокирующих препятствий не больше двух на волну, на лёгкой фазе
    /// всегда остаётся полностью свободная дорожка.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        public List<LaneObject> ActiveObjects = new List<LaneObject>();

        private GameManager _gm;
        private LevelData _level;
        private readonly Dictionary<string, Stack<LaneObject>> _pools =
            new Dictionary<string, Stack<LaneObject>>();
        private GameObject _container;
        private float _waveTimer;
        private float _superTimer;
        private System.Random _rng = new System.Random();
        private readonly List<ObstacleSpec> _specBuffer = new List<ObstacleSpec>();
        private const double ToxicChance = 0.12; // доля волновых препятствий-«ядов»

        private const float SpawnZ = 150f;
        private const float DespawnZ = -28f;
        private const float AvoidedZ = -2f;

        public void Init(GameManager gm)
        {
            _gm = gm;
        }

        public void ConfigureForLevel(LevelData data)
        {
            // Другой уровень — другая палитра и набор объектов: пул пересоздаём.
            ActiveObjects.Clear();
            _pools.Clear();
            if (_container != null) Destroy(_container);
            _container = new GameObject("Spawned");
            _container.transform.SetParent(transform, false);

            _level = data;
            _waveTimer = 1.1f;
            _superTimer = 13f + (float)_rng.NextDouble() * 9f;
            _rng = new System.Random(data.Index * 999331 + 7);
        }

        public void ClearAll()
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i] != null) ReturnToPool(ActiveObjects[i]);
            }
            ActiveObjects.Clear();
        }

        public void Tick(float dt, float speed)
        {
            if (_level == null) return;
            float move = speed * dt;
            bool magnet = _gm.MagnetActive;
            float playerX = _gm.Player != null ? _gm.Player.transform.position.x : 0f;

            for (int i = ActiveObjects.Count - 1; i >= 0; i--)
            {
                LaneObject lo = ActiveObjects[i];
                if (lo == null)
                {
                    ActiveObjects.RemoveAt(i);
                    continue;
                }
                Vector3 p = lo.transform.position;
                p.z -= move;

                // «Разъяснения Пленума»: бусты сами тянутся к дорожке игрока.
                if (magnet)
                {
                    Pickup pk = lo as Pickup;
                    if (pk != null && !pk.Collected && p.z > 0f && p.z < 30f)
                    {
                        p.x = Mathf.MoveTowards(p.x, playerX, dt * 14f);
                    }
                }
                lo.transform.position = p;

                Obstacle obs = lo as Obstacle;
                if (obs != null && !obs.WasHit && !obs.CountedAvoided && p.z < AvoidedZ)
                {
                    obs.CountedAvoided = true;
                    _gm.Levels.RegisterAvoided();
                }

                if (p.z < DespawnZ)
                {
                    ReturnToPool(lo);
                    ActiveObjects.RemoveAt(i);
                }
            }

            _waveTimer -= dt;
            if (_waveTimer <= 0f)
            {
                float progress = _gm.Levels != null ? _gm.Levels.Progress01 : 0f;
                SpawnWave(progress, speed);
                float interval = _level.WaveInterval * Mathf.Lerp(1f, 0.84f, progress);
                _waveTimer += Mathf.Max(0.4f, interval);
            }

            // Редкие суперпризы: отдельный таймер, независимый от волн.
            if (_level.SuperPickups != null && _level.SuperPickups.Length > 0)
            {
                _superTimer -= dt;
                if (_superTimer <= 0f)
                {
                    PickupSpec spec = _level.SuperPickups[_rng.Next(_level.SuperPickups.Length)];
                    SpawnPickup(spec, _rng.Next(3), SpawnZ + 6f, 0f);
                    _superTimer = 24f + (float)_rng.NextDouble() * 14f;
                }
            }
        }

        // ------------------------------------------------------------------
        // Генерация волны
        // ------------------------------------------------------------------

        private void SpawnWave(float progress, float speed)
        {
            float difficulty = Mathf.Clamp01(0.25f + 0.75f * progress);
            int[] lanes = ShuffledLanes();

            // Сколько дорожек перекрыть «глухими» препятствиями (максимум 2).
            int fullCount;
            double r = _rng.NextDouble();
            if (difficulty < 0.3f) fullCount = r < 0.5 ? 1 : 0;
            else if (r < 0.2) fullCount = 0;
            else if (r < 0.62) fullCount = 1;
            else fullCount = 2;

            for (int i = 0; i < fullCount; i++)
            {
                ObstacleSpec spec = PickObstacle(ObstacleKind.Block);
                if (spec != null)
                {
                    SpawnObstacle(spec, lanes[i], SpawnZ + Jitter(2.5f));
                }
            }

            // Остальные дорожки: свободны или проходимы прыжком/подкатом.
            bool leftFree = false;
            for (int i = fullCount; i < 3; i++)
            {
                int lane = lanes[i];
                bool lastRemaining = i == 2;
                bool forceFree = lastRemaining && !leftFree && difficulty < 0.34f;
                float chance = 0.28f + 0.5f * difficulty;

                if (!forceFree && _rng.NextDouble() < chance)
                {
                    ObstacleKind kind = _rng.NextDouble() < 0.55 ? ObstacleKind.Jump : ObstacleKind.Slide;
                    ObstacleSpec spec = PickObstacle(kind);
                    if (spec == null) spec = PickObstacle(kind == ObstacleKind.Jump ? ObstacleKind.Slide : ObstacleKind.Jump);
                    if (spec != null)
                    {
                        Obstacle o = SpawnObstacle(spec, lane, SpawnZ + Jitter(2.5f));
                        // Награда за смелый прыжок: буст над низким препятствием.
                        if (spec.Kind == ObstacleKind.Jump && _rng.NextDouble() < 0.35)
                        {
                            SpawnPickup(PickRandomPickup(), lane, o.transform.position.z, 0.55f);
                        }
                    }
                    else
                    {
                        leftFree = true;
                    }
                }
                else
                {
                    leftFree = true;
                }
            }

            // Дорожка бустов между воротами.
            if (_rng.NextDouble() < 0.6)
            {
                int lane = _rng.Next(3);
                float gap = _level.WaveInterval * Mathf.Max(8f, speed);
                float startZ = SpawnZ + gap * 0.45f;
                int count = 2 + _rng.Next(2);
                for (int i = 0; i < count; i++)
                {
                    SpawnPickup(PickRandomPickup(), lane, startZ + i * 2.7f, 0f);
                }
            }
        }

        private int[] ShuffledLanes()
        {
            int[] lanes = { 0, 1, 2 };
            for (int i = 2; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                int tmp = lanes[i];
                lanes[i] = lanes[j];
                lanes[j] = tmp;
            }
            return lanes;
        }

        private float Jitter(float amount)
        {
            return (float)_rng.NextDouble() * amount;
        }

        private ObstacleSpec PickObstacle(ObstacleKind kind)
        {
            // Редкий шанс подсунуть «ядовитое» препятствие того же типа.
            if (_level.ToxicObstacles != null && _rng.NextDouble() < ToxicChance)
            {
                _specBuffer.Clear();
                for (int i = 0; i < _level.ToxicObstacles.Length; i++)
                {
                    if (_level.ToxicObstacles[i].Kind == kind) _specBuffer.Add(_level.ToxicObstacles[i]);
                }
                if (_specBuffer.Count > 0) return _specBuffer[_rng.Next(_specBuffer.Count)];
            }

            _specBuffer.Clear();
            for (int i = 0; i < _level.Obstacles.Length; i++)
            {
                if (_level.Obstacles[i].Kind == kind) _specBuffer.Add(_level.Obstacles[i]);
            }
            if (_specBuffer.Count == 0) return null;
            return _specBuffer[_rng.Next(_specBuffer.Count)];
        }

        private PickupSpec PickRandomPickup()
        {
            return _level.Pickups[_rng.Next(_level.Pickups.Length)];
        }

        // ------------------------------------------------------------------
        // Пул объектов
        // ------------------------------------------------------------------

        private Obstacle SpawnObstacle(ObstacleSpec spec, int lane, float z)
        {
            string key = "o_" + spec.Id;
            LaneObject pooled = TakeFromPool(key);
            Obstacle o;
            if (pooled == null)
            {
                o = ProceduralModelFactory.CreateObstacle(spec, _level.Palette);
                o.PoolKey = key;
                o.transform.SetParent(_container.transform, false);
            }
            else
            {
                o = (Obstacle)pooled;
            }
            o.Lane = lane;
            o.transform.position = new Vector3(PlayerController.LaneToX(lane), 0f, z);
            o.gameObject.SetActive(true);
            o.ResetState();
            ActiveObjects.Add(o);
            return o;
        }

        private Pickup SpawnPickup(PickupSpec spec, int lane, float z, float yOffset)
        {
            if (spec == null) return null;
            string key = "p_" + spec.Id;
            LaneObject pooled = TakeFromPool(key);
            Pickup p;
            if (pooled == null)
            {
                p = ProceduralModelFactory.CreatePickup(spec, _level.Palette);
                p.PoolKey = key;
                p.transform.SetParent(_container.transform, false);
            }
            else
            {
                p = (Pickup)pooled;
            }
            p.Lane = lane;
            p.transform.position = new Vector3(PlayerController.LaneToX(lane), yOffset, z);
            p.gameObject.SetActive(true);
            p.ResetState();
            ActiveObjects.Add(p);
            return p;
        }

        private LaneObject TakeFromPool(string key)
        {
            Stack<LaneObject> stack;
            if (!_pools.TryGetValue(key, out stack)) return null;
            while (stack.Count > 0)
            {
                LaneObject lo = stack.Pop();
                if (lo != null) return lo;
            }
            return null;
        }

        private void ReturnToPool(LaneObject lo)
        {
            lo.gameObject.SetActive(false);
            Stack<LaneObject> stack;
            if (!_pools.TryGetValue(lo.PoolKey, out stack))
            {
                stack = new Stack<LaneObject>();
                _pools[lo.PoolKey] = stack;
            }
            stack.Push(lo);
        }
    }
}
