using UnityEngine;
using UnityEngine.UI;

namespace CourtRunner15
{
    /// <summary>
    /// Круглый индикатор ускорения: заполняющееся кольцо, бегущий силуэт
    /// и свечение при активном ускорении.
    /// </summary>
    public class CircularBoostIndicator : MonoBehaviour
    {
        private Image _ringFill;
        private Image _glow;
        private RectTransform _iconRoot;
        private Text _caption;
        private bool _active;

        private static readonly Color IdleColor = new Color(0.75f, 0.78f, 0.85f, 0.85f);
        private static readonly Color ActiveColor = new Color(1f, 0.82f, 0.25f, 1f);

        public void Build()
        {
            // Тёмная подложка.
            Image bg = MakeImage(transform, "bg", MaterialFactory.CircleSprite(),
                new Color(0.02f, 0.04f, 0.09f, 0.66f));
            Stretch(bg.rectTransform, 0f);

            // Свечение позади кольца.
            _glow = MakeImage(transform, "glow", MaterialFactory.SoftDotSprite(),
                new Color(1f, 0.82f, 0.25f, 0f));
            Stretch(_glow.rectTransform, -34f);

            Image ringBg = MakeImage(transform, "ringBg", MaterialFactory.RingSprite(),
                new Color(1f, 1f, 1f, 0.13f));
            Stretch(ringBg.rectTransform, 8f);

            _ringFill = MakeImage(transform, "ringFill", MaterialFactory.RingSprite(), IdleColor);
            Stretch(_ringFill.rectTransform, 8f);
            _ringFill.type = Image.Type.Filled;
            _ringFill.fillMethod = Image.FillMethod.Radial360;
            _ringFill.fillOrigin = (int)Image.Origin360.Top;
            _ringFill.fillClockwise = true;
            _ringFill.fillAmount = 0f;

            // Бегущий силуэт из простых форм.
            _iconRoot = new GameObject("icon").AddComponent<RectTransform>();
            _iconRoot.SetParent(transform, false);
            _iconRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _iconRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _iconRoot.sizeDelta = new Vector2(80f, 80f);
            _iconRoot.anchoredPosition = new Vector2(0f, 8f);

            MakeIconPart(new Vector2(2f, 26f), new Vector2(18f, 18f), 0f, MaterialFactory.CircleSprite());  // голова
            MakeIconPart(new Vector2(0f, 2f), new Vector2(14f, 32f), 14f, MaterialFactory.RoundedSprite()); // корпус
            MakeIconPart(new Vector2(10f, -24f), new Vector2(9f, 28f), -34f, MaterialFactory.RoundedSprite()); // нога вперёд
            MakeIconPart(new Vector2(-9f, -22f), new Vector2(9f, 28f), 38f, MaterialFactory.RoundedSprite());  // нога назад
            MakeIconPart(new Vector2(13f, 6f), new Vector2(8f, 24f), -46f, MaterialFactory.RoundedSprite());   // рука
            MakeIconPart(new Vector2(22f, -6f), new Vector2(15f, 11f), 8f, MaterialFactory.RoundedSprite());   // портфель

            _caption = new GameObject("caption").AddComponent<Text>();
            _caption.rectTransform.SetParent(transform, false);
            _caption.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _caption.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _caption.rectTransform.sizeDelta = new Vector2(200f, 30f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, 24f);
            _caption.font = MaterialFactory.UiFont;
            _caption.fontSize = 19;
            _caption.fontStyle = FontStyle.Bold;
            _caption.alignment = TextAnchor.MiddleCenter;
            _caption.color = IdleColor;
            _caption.text = "УСКОРЕНИЕ";
            _caption.raycastTarget = false;
        }

        private Image MakeIconPart(Vector2 pos, Vector2 size, float rotation, Sprite sprite)
        {
            Image img = MakeImage(_iconRoot, "part", sprite, Color.white);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return img;
        }

        private Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>fill01 — прогресс к порогу ускорения, active — ускорение включено.</summary>
        public void SetState(float fill01, bool active)
        {
            _active = active;
            if (_ringFill != null)
            {
                _ringFill.fillAmount = Mathf.Clamp01(fill01);
                _ringFill.color = active ? ActiveColor : Color.Lerp(IdleColor, ActiveColor, fill01 * 0.6f);
            }
            if (_caption != null)
            {
                _caption.color = active ? ActiveColor : IdleColor;
                _caption.text = active ? "УСКОРЕНИЕ +25%" : "УСКОРЕНИЕ";
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            if (_glow != null)
            {
                float target = _active ? 0.4f + 0.15f * Mathf.Sin(t * 7f) : 0f;
                Color c = _glow.color;
                c.a = Mathf.MoveTowards(c.a, target, Time.unscaledDeltaTime * 2f);
                _glow.color = c;
            }
            float scale = _active ? 1f + 0.035f * Mathf.Sin(t * 8f) : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
            if (_iconRoot != null)
            {
                _iconRoot.anchoredPosition = new Vector2(0f, 8f + (_active ? Mathf.Abs(Mathf.Sin(t * 9f)) * 4f : 0f));
            }
        }
    }
}
