using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Вращение и вертикальное покачивание предмета буста.</summary>
    public class FloatingPickupEffect : MonoBehaviour
    {
        public Transform Target;
        public float SpinSpeed = 95f;
        public float BobAmplitude = 0.14f;
        public float BobSpeed = 2.2f;

        private float _baseY;
        private bool _captured;
        private float _phase;

        private void OnEnable()
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (Target == null) return;
            if (!_captured)
            {
                _baseY = Target.localPosition.y;
                _captured = true;
            }
            Target.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.Self);
            Vector3 p = Target.localPosition;
            p.y = _baseY + Mathf.Sin(Time.time * BobSpeed + _phase) * BobAmplitude;
            Target.localPosition = p;
        }
    }
}
