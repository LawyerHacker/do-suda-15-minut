using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Камера за спиной юриста: плавное следование по X, лёгкий наклон вниз,
    /// расширение FOV при ускорении, тряска при ударе, режим меню с дрейфом.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public Camera Cam;

        private GameManager _gm;
        private Transform _target;
        private bool _menuMode = true;

        private float _followX;
        private float _shakeAmp;
        private float _shakeTime;
        private float _shakeDuration = 1f;

        private const float BaseFov = 60f;
        private const float BoostFov = 68.5f;
        private const float MenuFov = 57f;

        public void Init(GameManager gm, Transform target)
        {
            _gm = gm;
            _target = target;
            Cam = GetComponent<Camera>();
            if (Cam != null)
            {
                Cam.nearClipPlane = 0.1f;
                Cam.fieldOfView = BaseFov;
                Cam.allowHDR = true; // нужен bloom'у PPSv2 (селективное золотое свечение)
                Cam.allowMSAA = true;
                Cam.allowDynamicResolution = false;
                Cam.renderingPath = RenderingPath.Forward;
            }
            transform.position = new Vector3(0f, 3.35f, -5.6f);
            transform.rotation = Quaternion.Euler(14f, 0f, 0f);
        }

        public void SetMenuMode(bool menu)
        {
            _menuMode = menu;
        }

        public void Shake(float amplitude, float duration)
        {
            _shakeAmp = Mathf.Max(_shakeAmp, amplitude);
            _shakeTime = duration;
            _shakeDuration = Mathf.Max(0.01f, duration);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            if (_menuMode)
            {
                float t = Time.unscaledTime;
                Vector3 menuPos = new Vector3(
                    Mathf.Sin(t * 0.23f) * 0.8f,
                    4.55f + Mathf.Sin(t * 0.17f) * 0.3f,
                    -7.6f);
                transform.position = Vector3.Lerp(transform.position, menuPos, 1f - Mathf.Exp(-2.5f * dt));
                Quaternion look = Quaternion.LookRotation(
                    new Vector3(0f, 1.7f, 26f) - transform.position, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-2.5f * dt));
                if (Cam != null)
                {
                    Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, MenuFov, 1f - Mathf.Exp(-3f * dt));
                }
                return;
            }

            float boost = _gm != null ? _gm.BoostBlend : 0f;
            float targetX = _target != null ? _target.position.x * 0.55f : 0f;
            _followX = Mathf.Lerp(_followX, targetX, 1f - Mathf.Exp(-10f * dt));

            Vector3 basePos = new Vector3(_followX, 3.35f, -5.6f);

            // Тряска при ударе (затухающий шум Перлина).
            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                float k = Mathf.Clamp01(_shakeTime / _shakeDuration);
                float amp = _shakeAmp * k;
                float n1 = Mathf.PerlinNoise(Time.time * 28f, 0.3f) - 0.5f;
                float n2 = Mathf.PerlinNoise(0.7f, Time.time * 31f) - 0.5f;
                basePos += new Vector3(n1 * 2f * amp, n2 * 1.4f * amp, 0f);
                if (_shakeTime <= 0f) _shakeAmp = 0f;
            }

            transform.position = basePos;

            Vector3 lookPoint = new Vector3(_followX * 0.5f, 1.15f, 8.5f);
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);

            if (Cam != null)
            {
                float targetFov = Mathf.Lerp(BaseFov, BoostFov, boost);
                Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, targetFov, 1f - Mathf.Exp(-6f * dt));
            }
        }
    }
}
