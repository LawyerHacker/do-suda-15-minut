using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Все частицы игры: сбор бустов, удары, линии скорости, пыль у ног,
    /// конфетти победы. Создаются кодом, без внешних ресурсов.
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        private GameManager _gm;
        private Camera _cam;

        private ParticleSystem _pickupPs;
        private ParticleSystem _hitPs;
        private ParticleSystem _shieldPs;
        private ParticleSystem _confettiPs;
        private ParticleSystem _speedLines;
        private ParticleSystem _footDust;
        private ParticleSystem _superPs;
        private ParticleSystem _toxicPs;

        private Color _confettiA = new Color(1f, 0.8f, 0.25f);
        private Color _confettiB = new Color(0.35f, 0.95f, 0.45f);

        public void Init(GameManager gm, Camera cam)
        {
            _gm = gm;
            _cam = cam;

            _pickupPs = MakeBurstSystem("FX_Pickup", 0.45f, 0.85f, 2.5f, 5.5f, 0.07f, 0.17f, 0.1f);
            _hitPs = MakeBurstSystem("FX_Hit", 0.35f, 0.7f, 3f, 7f, 0.06f, 0.16f, 0.5f);
            SetStartColor(_hitPs, new Color(0.95f, 0.25f, 0.15f), new Color(0.98f, 0.95f, 0.9f));
            _shieldPs = MakeBurstSystem("FX_Shield", 0.4f, 0.8f, 3f, 6f, 0.07f, 0.15f, 0.05f);
            SetStartColor(_shieldPs, new Color(0.4f, 0.85f, 1f), Color.white);
            _superPs = MakeBurstSystem("FX_Super", 0.6f, 1.1f, 3.5f, 8f, 0.09f, 0.22f, 0.05f);
            SetStartColor(_superPs, new Color(1f, 0.85f, 0.30f), Color.white);
            _toxicPs = MakeBurstSystem("FX_Toxic", 0.5f, 0.9f, 2.5f, 6f, 0.08f, 0.18f, 0.15f);
            SetStartColor(_toxicPs, new Color(0.55f, 0.95f, 0.25f), new Color(0.65f, 0.4f, 0.85f));
            _confettiPs = MakeConfetti();
            _speedLines = MakeSpeedLines();
            _footDust = MakeFootDust();
        }

        // ------------------------------------------------------------------
        // Публичные вызовы
        // ------------------------------------------------------------------

        public void ConfigurePalette(LevelPalette pal)
        {
            _confettiA = pal.BoostPrimary;
            _confettiB = pal.BoostSecondary;
            if (_footDust != null)
            {
                Color dust = new Color(pal.FloorAccent.r, pal.FloorAccent.g, pal.FloorAccent.b, 0.32f);
                SetStartColor(_footDust, dust, dust);
            }
            if (_confettiPs != null)
            {
                SetStartColor(_confettiPs, _confettiA, _confettiB);
            }
        }

        public void PlayPickupBurst(Vector3 pos, Color color)
        {
            if (_pickupPs == null) return;
            _pickupPs.transform.position = pos;
            SetStartColor(_pickupPs, color, Color.white);
            _pickupPs.Emit(18);
        }

        public void PlayHitBurst(Vector3 pos)
        {
            if (_hitPs == null) return;
            _hitPs.transform.position = pos;
            _hitPs.Emit(22);
        }

        public void PlayShieldBreak(Vector3 pos)
        {
            if (_shieldPs == null) return;
            _shieldPs.transform.position = pos;
            _shieldPs.Emit(26);
        }

        /// <summary>Суперприз: золотой залп + немного конфетти.</summary>
        public void PlaySuperBurst(Vector3 pos)
        {
            if (_superPs != null)
            {
                _superPs.transform.position = pos;
                _superPs.Emit(42);
            }
            if (_confettiPs != null)
            {
                _confettiPs.transform.position = pos + Vector3.up * 1.5f;
                _confettiPs.Emit(30);
            }
        }

        /// <summary>Ядовитое препятствие: кислотный залп.</summary>
        public void PlayToxicBurst(Vector3 pos)
        {
            if (_toxicPs == null) return;
            _toxicPs.transform.position = pos;
            _toxicPs.Emit(28);
        }

        /// <summary>Снос преграды «Исполнительным листом».</summary>
        public void PlaySmashBurst(Vector3 pos)
        {
            if (_superPs == null) return;
            _superPs.transform.position = pos;
            _superPs.Emit(14);
        }

        public void PlayVictory()
        {
            if (_confettiPs == null) return;
            _confettiPs.transform.position = new Vector3(0f, 7f, 7f);
            _confettiPs.Emit(150);
        }

        public void SetBoost(float boost01)
        {
            if (_speedLines != null)
            {
                var em = _speedLines.emission;
                em.rateOverTime = boost01 * 85f;
            }
            if (_footDust != null)
            {
                var em = _footDust.emission;
                bool playing = _gm != null && _gm.State == GameState.Playing;
                em.rateOverTime = playing ? 6f + 30f * boost01 : 0f;
            }
        }

        // ------------------------------------------------------------------
        // Создание систем
        // ------------------------------------------------------------------

        private ParticleSystem MakeBase(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 600;
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material = MaterialFactory.ParticleMaterial();
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            return ps;
        }

        private void SetStartColor(ParticleSystem ps, Color a, Color b)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        }

        private void AddFadeOut(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        private ParticleSystem MakeBurstSystem(string name, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float gravity)
        {
            ParticleSystem ps = MakeBase(name, transform);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            AddFadeOut(ps);
            return ps;
        }

        private ParticleSystem MakeConfetti()
        {
            ParticleSystem ps = MakeBase("FX_Confetti", transform);
            ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 0.55f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.6f;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            AddFadeOut(ps);
            SetStartColor(ps, _confettiA, _confettiB);
            return ps;
        }

        private ParticleSystem MakeSpeedLines()
        {
            Transform parent = _cam != null ? _cam.transform : transform;
            ParticleSystem ps = MakeBase("FX_SpeedLines", parent);
            ps.transform.localPosition = new Vector3(0f, 0.4f, 9f);
            ps.transform.localRotation = Quaternion.identity;
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.38f));
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 5.5f, 12f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            // Все три кривые в одном режиме (TwoConstants), иначе Unity
            // спамит предупреждение в лог каждый кадр.
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-48f, -68f);
            ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.velocityScale = 0.055f;
            psr.lengthScale = 1f;
            AddFadeOut(ps);
            ps.Play();
            return ps;
        }

        private ParticleSystem MakeFootDust()
        {
            Transform parent = _gm != null && _gm.Player != null ? _gm.Player.transform : transform;
            ParticleSystem ps = MakeBase("FX_FootDust", parent);
            ps.transform.localPosition = new Vector3(0f, 0.06f, -0.25f);
            ps.transform.localRotation = Quaternion.Euler(-100f, 0f, 0f);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.15f;
            AddFadeOut(ps);
            ps.Play();
            return ps;
        }
    }
}
