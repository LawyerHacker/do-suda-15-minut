using UnityEngine;
using UnityEngine.EventSystems;

namespace CourtRunner15
{
    /// <summary>
    /// Управление юристом: три дорожки, прыжок, подкат, столкновения.
    /// Мир движется навстречу, игрок остаётся на z=0.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public PlayerVisual Visual;

        public bool IsSliding;
        public bool IsGrounded = true;

        private GameManager _gm;
        private SpeedTrail _trail;

        private int _lane = 1;        // 0 левая, 1 центр, 2 правая
        private float _xFrom;
        private float _xTo;
        private float _laneT = 1f;    // 0..1 прогресс смены полосы
        private const float LaneChangeTime = 0.15f;

        private float _y;
        private float _vy;
        private const float JumpVelocity = 8.2f;
        private const float Gravity = 26f;
        private const float SlamVelocity = -16f;

        private float _slideTimer;
        private const float SlideDuration = 0.55f;

        private float _invulnUntil;

        // Тач-управление (WebGL на телефоне/планшете): свайпы дублируют
        // клавиатуру, вызывая те же ветки в ReadInput. Один жест — один палец.
        private const float SwipeGestureWindow = 0.4f; // окно жеста, сек
        private const float SwipeAxisDominance = 1.3f; // ось должна явно доминировать
        private int _swipeFingerId = -1;
        private Vector2 _swipeStart;
        private float _swipeStartTime;
        private bool _swipeConsumed;
        private bool _touchLeft;
        private bool _touchRight;
        private bool _touchUp;
        private bool _touchDown;

        public bool Invulnerable
        {
            get { return Time.time < _invulnUntil; }
        }

        public float CurrentHeight
        {
            get { return (IsSliding && IsGrounded) ? 0.7f : 1.8f; }
        }

        public Bounds CurrentBounds
        {
            get
            {
                float h = CurrentHeight;
                Vector3 center = new Vector3(transform.position.x, _y + h * 0.5f, 0f);
                return new Bounds(center, new Vector3(0.9f, h, 0.7f));
            }
        }

        public static float LaneToX(int lane)
        {
            return (lane - 1) * LevelCatalog.LaneWidth;
        }

        public void Init(GameManager gm)
        {
            _gm = gm;
            Visual = ProceduralModelFactory.CreateLawyerV3(transform);
            _trail = gameObject.AddComponent<SpeedTrail>();
            _trail.Init(Visual.transform);
            ResetForLevel();
        }

        public void SetVisible(bool visible)
        {
            if (Visual != null) Visual.gameObject.SetActive(visible);
            if (_trail != null) _trail.SetEnabled(visible);
        }

        public void ResetForLevel()
        {
            _lane = 1;
            _xFrom = 0f;
            _xTo = 0f;
            _laneT = 1f;
            _y = 0f;
            _vy = 0f;
            _slideTimer = 0f;
            _invulnUntil = 0f;
            IsSliding = false;
            IsGrounded = true;
            transform.position = Vector3.zero;
            if (Visual != null) Visual.ResetPose();
            if (_trail != null) _trail.Clear();
        }

        /// <summary>Вызывается GameManager-ом только в состоянии Playing.</summary>
        public void Tick(float dt)
        {
            ReadInput();
            UpdateLane(dt);
            UpdateJump(dt);
            UpdateSlide(dt);

            float x = Mathf.SmoothStep(_xFrom, _xTo, _laneT);
            transform.position = new Vector3(x, _y, 0f);

            CheckCollisions();

            if (Visual != null)
            {
                float speed01 = Mathf.Clamp01(_gm.WorldSpeed / 24f);
                Visual.SetMotion(speed01, IsGrounded, IsSliding, _vy, _gm.BoostBlend);
                Visual.SetFlicker(Invulnerable);
            }
            if (_trail != null)
            {
                float trailBoost = Mathf.Max(_gm.BoostBlend, _gm.RamActive ? 1f : 0f);
                _trail.SetIntensity(trailBoost, Mathf.Clamp01(_gm.WorldSpeed / 25f));
            }
        }

        private void ReadInput()
        {
            ReadTouchGestures();

            bool left = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                        _touchLeft;
            bool right = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) ||
                         _touchRight;
            bool jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) ||
                        Input.GetKeyDown(KeyCode.UpArrow) || _touchUp;
            bool slide = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ||
                         Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) ||
                         _touchDown;

            if (left && _lane > 0)
            {
                _xFrom = transform.position.x;
                _lane--;
                _xTo = LaneToX(_lane);
                _laneT = 0f;
            }
            if (right && _lane < 2)
            {
                _xFrom = transform.position.x;
                _lane++;
                _xTo = LaneToX(_lane);
                _laneT = 0f;
            }

            if (jump && IsGrounded)
            {
                // «700 страниц PDF» делает портфель тяжелее — прыжок ниже.
                _vy = JumpVelocity * (_gm.WeakJumpActive ? 0.84f : 1f);
                IsGrounded = false;
                _slideTimer = 0f; // прыжок отменяет подкат
                _gm.Audio.Play("whoosh", 0.5f, 1.25f);
            }

            if (slide)
            {
                _slideTimer = SlideDuration;
                if (!IsGrounded)
                {
                    // Подкат в воздухе — резкий прижим к земле.
                    _vy = Mathf.Min(_vy, SlamVelocity);
                }
                _gm.Audio.Play("whoosh", 0.45f, 0.8f);
            }
        }

        /// <summary>Свайпы: влево/вправо — дорожки, вверх — прыжок, вниз — подкат.
        /// Тап (короткое касание без сдвига) свайпом не считается; жест,
        /// начатый на UI-кнопке, игрока не двигает.</summary>
        private void ReadTouchGestures()
        {
            _touchLeft = false;
            _touchRight = false;
            _touchUp = false;
            _touchDown = false;

            int touchCount = Input.touchCount;
            bool trackedSeen = false;

            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (_swipeFingerId == -1)
                {
                    if (touch.phase != TouchPhase.Began) continue;
                    if (IsTouchOverUi(touch.fingerId)) continue;
                    _swipeFingerId = touch.fingerId;
                    _swipeStart = touch.position;
                    _swipeStartTime = Time.unscaledTime;
                    _swipeConsumed = false;
                    trackedSeen = true;
                    continue;
                }

                if (touch.fingerId != _swipeFingerId) continue;
                trackedSeen = true;

                if (touch.phase == TouchPhase.Canceled)
                {
                    _swipeFingerId = -1;
                    continue;
                }
                if (touch.phase == TouchPhase.Ended)
                {
                    if (!_swipeConsumed &&
                        Time.unscaledTime - _swipeStartTime <= SwipeGestureWindow)
                    {
                        TryEmitSwipe(touch.position);
                    }
                    _swipeFingerId = -1;
                    continue;
                }

                if (_swipeConsumed) continue;
                if (Time.unscaledTime - _swipeStartTime > SwipeGestureWindow)
                {
                    // Палец держат слишком долго — это удержание, не свайп.
                    _swipeConsumed = true;
                    continue;
                }
                TryEmitSwipe(touch.position);
            }

            // Отслеживаемый палец пропал из списка (потеря фокуса) — сброс.
            if (_swipeFingerId != -1 && !trackedSeen) _swipeFingerId = -1;
        }

        private void TryEmitSwipe(Vector2 position)
        {
            Vector2 delta = position - _swipeStart;
            float threshold = SwipeThresholdPixels();
            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);
            if (ax < threshold && ay < threshold) return;

            if (ax > ay * SwipeAxisDominance)
            {
                if (delta.x > 0f) _touchRight = true;
                else _touchLeft = true;
                _swipeConsumed = true;
            }
            else if (ay > ax * SwipeAxisDominance)
            {
                if (delta.y > 0f) _touchUp = true;
                else _touchDown = true;
                _swipeConsumed = true;
            }
            // Диагональ без явной оси: ждём, пока жест определится.
        }

        /// <summary>Порог свайпа ~40-60 px, на плотных экранах — по DPI (~0.25").</summary>
        private static float SwipeThresholdPixels()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f) return 50f;
            return Mathf.Clamp(dpi * 0.25f, 40f, 110f);
        }

        private static bool IsTouchOverUi(int fingerId)
        {
            EventSystem es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject(fingerId);
        }

        private void UpdateLane(float dt)
        {
            if (_laneT < 1f)
            {
                _laneT = Mathf.Min(1f, _laneT + dt / LaneChangeTime);
            }
        }

        private void UpdateJump(float dt)
        {
            if (!IsGrounded)
            {
                _y += _vy * dt;
                _vy -= Gravity * dt;
                if (_y <= 0f)
                {
                    _y = 0f;
                    _vy = 0f;
                    IsGrounded = true;
                }
            }
        }

        private void UpdateSlide(float dt)
        {
            if (_slideTimer > 0f)
            {
                _slideTimer -= dt;
            }
            IsSliding = _slideTimer > 0f && IsGrounded;
        }

        private void CheckCollisions()
        {
            if (_gm.Spawner == null) return;
            var objects = _gm.Spawner.ActiveObjects;
            Bounds pb = CurrentBounds;

            for (int i = 0; i < objects.Count; i++)
            {
                LaneObject lo = objects[i];
                if (lo == null || !lo.gameObject.activeSelf) continue;

                Pickup pickup = lo as Pickup;
                if (pickup != null)
                {
                    if (pickup.Collected) continue;
                    if (pb.Intersects(lo.WorldBounds))
                    {
                        pickup.Collect();
                        _gm.CollectPickup(pickup);
                    }
                    continue;
                }

                Obstacle obstacle = lo as Obstacle;
                if (obstacle != null)
                {
                    if (obstacle.WasHit) continue;

                    // «Исполнительный лист»: сносим преграды без штрафа.
                    if (_gm.RamActive)
                    {
                        if (pb.Intersects(lo.WorldBounds))
                        {
                            obstacle.WasHit = true;
                            obstacle.ReactToHit();
                            _gm.RamSmash(obstacle);
                        }
                        continue;
                    }

                    if (Invulnerable) continue;
                    if (pb.Intersects(lo.WorldBounds))
                    {
                        obstacle.WasHit = true;
                        obstacle.ReactToHit();
                        _invulnUntil = Time.time + 0.9f;
                        _gm.HitObstacle(obstacle);
                        if (_gm.State != GameState.Playing) return;
                    }
                }
            }
        }
    }
}
