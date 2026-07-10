using UnityEngine;
using UnityEngine.UI;

namespace CourtRunner15
{
    /// <summary>Полноэкранная красная вспышка при столкновении.</summary>
    public class HitFlashEffect : MonoBehaviour
    {
        private Image _image;
        private float _alpha;
        private Color _color = new Color(0.9f, 0.12f, 0.08f, 0f);

        public void Bind(Image image)
        {
            _image = image;
            if (_image != null)
            {
                _image.color = new Color(_color.r, _color.g, _color.b, 0f);
                _image.raycastTarget = false;
            }
        }

        /// <summary>Смена цвета вспышки (например, золото суперприза).</summary>
        public void SetColor(Color c)
        {
            _color = c;
            if (_image != null && _alpha <= 0f)
            {
                _image.color = new Color(c.r, c.g, c.b, 0f);
            }
        }

        public void Flash(float strength)
        {
            _alpha = Mathf.Max(_alpha, Mathf.Clamp01(strength));
        }

        private void Update()
        {
            if (_image == null) return;
            if (_alpha > 0f)
            {
                _alpha = Mathf.Max(0f, _alpha - Time.unscaledDeltaTime * 2.4f);
                _image.color = new Color(_color.r, _color.g, _color.b, _alpha * 0.6f);
            }
        }
    }
}
