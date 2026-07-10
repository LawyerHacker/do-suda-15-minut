using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Базовый объект на дорожке (препятствие или буст).
    /// Хитбокс задаётся вручную (HalfExtents/CenterOffset) — столкновения
    /// считаются по AABB без физического движка.
    /// </summary>
    public class LaneObject : MonoBehaviour
    {
        public Vector3 HalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 CenterOffset = new Vector3(0f, 0.5f, 0f);
        public int Lane = 1;
        public string PoolKey = "";

        public Bounds WorldBounds
        {
            get { return new Bounds(transform.position + CenterOffset, HalfExtents * 2f); }
        }
    }
}
