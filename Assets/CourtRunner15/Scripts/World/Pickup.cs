using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Буст уверенности. При сборе прячет «сердцевину» и луч.</summary>
    public class Pickup : LaneObject
    {
        public PickupSpec Spec;
        public bool Collected;

        /// <summary>Видимая часть буста (карта/предмет + луч), задаёт фабрика.</summary>
        public GameObject Core;

        public void ResetState()
        {
            Collected = false;
            if (Core != null) Core.SetActive(true);
        }

        public void Collect()
        {
            Collected = true;
            if (Core != null) Core.SetActive(false);
        }
    }
}
