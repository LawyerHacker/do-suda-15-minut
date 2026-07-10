using UnityEngine;

namespace CourtRunner15
{
    /// <summary>Экран UI с плавным появлением/исчезновением (не зависит от timeScale).</summary>
    public class ScreenController : MonoBehaviour
    {
        private CanvasGroup _group;
        private bool _visible;
        private const float FadeSpeed = 7f;

        private void Awake()
        {
            EnsureGroup();
        }

        private void EnsureGroup()
        {
            if (_group == null)
            {
                _group = gameObject.GetComponent<CanvasGroup>();
                if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Show(bool instant)
        {
            EnsureGroup();
            _visible = true;
            gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            _group.interactable = true;
            if (instant) _group.alpha = 1f;
        }

        public void Hide(bool instant)
        {
            EnsureGroup();
            _visible = false;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            if (instant)
            {
                _group.alpha = 0f;
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_group == null) return;
            float target = _visible ? 1f : 0f;
            if (Mathf.Approximately(_group.alpha, target))
            {
                if (!_visible && gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime * FadeSpeed);
            if (!_visible && _group.alpha <= 0f) gameObject.SetActive(false);
        }
    }
}
