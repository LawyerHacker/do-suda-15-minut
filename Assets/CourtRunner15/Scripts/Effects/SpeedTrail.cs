using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// След скорости за юристом: два шлейфа. Игрок в мире почти неподвижен
    /// (движется мир), поэтому шлейфы включаются только при реальном
    /// манёвре — смене полосы или прыжке, — иначе копятся в «каплю».
    /// </summary>
    public class SpeedTrail : MonoBehaviour
    {
        private TrailRenderer _left;
        private TrailRenderer _right;
        private Material _matL;
        private Material _matR;
        private Color _baseColor = new Color(1f, 0.85f, 0.4f, 0.5f);
        private bool _visible = true;
        private Vector3 _lastPos;

        public void Init(Transform visualRoot)
        {
            _left = MakeTrail(visualRoot, new Vector3(-0.3f, 0.95f, -0.12f), "TrailL");
            _right = MakeTrail(visualRoot, new Vector3(0.3f, 0.95f, -0.12f), "TrailR");
            _matL = _left.material;
            _matR = _right.material;
            _lastPos = transform.position;
        }

        private void LateUpdate()
        {
            if (_left == null || _right == null) return;
            Vector3 p = transform.position;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float lateral = new Vector2(p.x - _lastPos.x, p.y - _lastPos.y).magnitude / dt;
            _lastPos = p;
            bool moving = lateral > 1.4f;
            _left.emitting = _visible && moving;
            _right.emitting = _visible && moving;
        }

        private TrailRenderer MakeTrail(Transform parent, Vector3 localPos, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TrailRenderer tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.2f;
            tr.startWidth = 0.14f;
            tr.endWidth = 0.01f;
            tr.numCapVertices = 4;
            tr.minVertexDistance = 0.06f;
            tr.material = MaterialFactory.TrailMaterial(Color.white);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.emitting = true;
            return tr;
        }

        public void SetIntensity(float boost, float speed01)
        {
            if (_left == null || _right == null) return;
            float alpha = 0.18f + 0.5f * boost;
            Color c = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
            if (_matL != null) _matL.color = c;
            if (_matR != null) _matR.color = c;
            float time = 0.14f + 0.28f * boost;
            float width = 0.09f + 0.22f * boost;
            _left.time = time;
            _right.time = time;
            _left.startWidth = width;
            _right.startWidth = width;
        }

        public void SetEnabled(bool enabled)
        {
            _visible = enabled;
            if (_left != null)
            {
                _left.emitting = false;
                if (!enabled) _left.Clear();
            }
            if (_right != null)
            {
                _right.emitting = false;
                if (!enabled) _right.Clear();
            }
        }

        public void Clear()
        {
            if (_left != null) _left.Clear();
            if (_right != null) _right.Clear();
        }
    }
}
