using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Препятствие. Реагирует на удар коротким покачиванием.</summary>
    public class Obstacle : LaneObject
    {
        public ObstacleSpec Spec;
        public bool WasHit;
        public bool CountedAvoided;

        /// <summary>Визуальная часть, которую можно трясти (задаёт фабрика).</summary>
        public Transform Body;

        private float _punch;

        public void ResetState()
        {
            WasHit = false;
            CountedAvoided = false;
            _punch = 0f;
            if (Body != null)
            {
                Body.localRotation = Quaternion.identity;
                Body.localScale = Vector3.one;
            }
        }

        public void ReactToHit()
        {
            _punch = 1f;
        }

        private void Update()
        {
            if (_punch <= 0f || Body == null) return;
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 2.2f);
            float wobble = Mathf.Sin(Time.time * 42f) * 9f * _punch;
            Body.localRotation = Quaternion.Euler(0f, 0f, wobble);
            float squash = 1f - 0.12f * _punch;
            Body.localScale = new Vector3(1f + 0.1f * _punch, squash, 1f);
        }
    }
}
