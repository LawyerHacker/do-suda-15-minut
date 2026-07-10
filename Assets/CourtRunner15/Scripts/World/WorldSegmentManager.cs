using System.Collections.Generic;
using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Зацикленные сегменты мира: движутся навстречу игроку и
    /// перекидываются вперёд без видимых дыр.
    /// </summary>
    public class WorldSegmentManager : MonoBehaviour
    {
        private GameManager _gm;
        private LevelVisualBuilder _builder;
        private GameObject _root;
        private readonly List<Transform> _segments = new List<Transform>();

        private const float SegmentLength = 30f;
        private const int SegmentCount = 7;   // покрытие: от -30 до +180 м
        private const float RecycleZ = -45f;

        public void Init(GameManager gm, LevelVisualBuilder builder)
        {
            _gm = gm;
            _builder = builder;
        }

        public void BuildForLevel(LevelData data)
        {
            if (_root != null) Destroy(_root);
            _segments.Clear();

            _root = new GameObject("Segments");
            _root.transform.SetParent(transform, false);

            for (int i = 0; i < SegmentCount; i++)
            {
                GameObject seg = _builder.BuildSegment(data, i, SegmentLength);
                seg.transform.SetParent(_root.transform, false);
                seg.transform.position = new Vector3(0f, 0f, -SegmentLength + i * SegmentLength);
                _segments.Add(seg.transform);
            }
        }

        public void Tick(float dt, float speed)
        {
            float move = speed * dt;
            for (int i = 0; i < _segments.Count; i++)
            {
                Transform t = _segments[i];
                if (t == null) continue;
                Vector3 p = t.position;
                p.z -= move;
                if (p.z < RecycleZ)
                {
                    p.z += SegmentCount * SegmentLength;
                }
                t.position = p;
            }
        }
    }
}
