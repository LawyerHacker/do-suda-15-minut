using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Лёгкий замер FPS для смоук-проверок: несколько раз за сессию пишет
    /// среднее и худший кадр в Player.log, потом замолкает навсегда.
    /// </summary>
    public class FpsMeter : MonoBehaviour
    {
        private const float WindowSeconds = 20f;
        private const int MaxReports = 6;

        private float _accum;
        private int _frames;
        private float _worstDt;
        private int _reports;

        private void Update()
        {
            if (_reports >= MaxReports)
            {
                enabled = false;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _accum += dt;
            _frames++;
            if (dt > _worstDt) _worstDt = dt;

            if (_accum >= WindowSeconds)
            {
                float avg = _frames / _accum;
                float worst = _worstDt > 0f ? 1f / _worstDt : 0f;
                Debug.LogFormat("[CourtRunner15] FPS: avg={0:0.0}, worst={1:0.0} (окно {2:0} с).",
                    avg, worst, _accum);
                _accum = 0f;
                _frames = 0;
                _worstDt = 0f;
                _reports++;
            }
        }
    }
}
