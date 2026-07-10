using System.Collections.Generic;
using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Звук без внешних файлов: все клипы синтезируются кодом.
    /// Идентификаторы: click, pickup, hit, win, lose, whoosh, shield.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private AudioSource _source;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private const float MasterVolume = 0.55f;
        private const int SampleRate = 44100;

        public void Init()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            _clips["click"] = MakeClip("sfx_click", 0.06f, ClickSample);
            _clips["pickup"] = MakeClip("sfx_pickup", 0.16f, PickupSample);
            _clips["hit"] = MakeClip("sfx_hit", 0.24f, HitSample);
            _clips["win"] = MakeClip("sfx_win", 1.05f, WinSample);
            _clips["lose"] = MakeClip("sfx_lose", 0.9f, LoseSample);
            _clips["whoosh"] = MakeClip("sfx_whoosh", 0.2f, WhooshSample);
            _clips["shield"] = MakeClip("sfx_shield", 0.4f, ShieldSample);
            _clips["fanfare"] = MakeClip("sfx_fanfare", 0.85f, FanfareSample);
            _clips["debuff"] = MakeClip("sfx_debuff", 0.65f, DebuffSample);
        }

        public void Play(string id, float volume, float pitch)
        {
            if (_source == null) return;
            AudioClip clip;
            if (!_clips.TryGetValue(id, out clip) || clip == null) return;
            _source.pitch = pitch;
            _source.PlayOneShot(clip, Mathf.Clamp01(volume) * MasterVolume);
        }

        // ------------------------------------------------------------------
        // Синтез
        // ------------------------------------------------------------------

        private AudioClip MakeClip(string name, float duration, System.Func<float, float> generator)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                data[i] = Mathf.Clamp(generator(t), -1f, 1f);
            }
            // Плавное окончание, чтобы не щёлкало.
            int fade = Mathf.Min(300, n);
            for (int i = 0; i < fade; i++)
            {
                data[n - 1 - i] *= (float)i / fade;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float freq, float t)
        {
            return Mathf.Sin(2f * Mathf.PI * freq * t);
        }

        private static float Square(float freq, float t)
        {
            return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
        }

        private float ClickSample(float t)
        {
            return Square(1650f, t) * Mathf.Exp(-t * 90f) * 0.4f;
        }

        private float PickupSample(float t)
        {
            // Восходящий свип с обертоном.
            float phase = 760f * t + 2600f * t * t;
            float s = Mathf.Sin(2f * Mathf.PI * phase) * 0.7f
                    + Mathf.Sin(4f * Mathf.PI * phase) * 0.25f;
            return s * Mathf.Exp(-t * 16f) * 0.8f;
        }

        private float HitSample(float t)
        {
            // Шум + низкий «бум».
            float noise = Mathf.Sin(t * 12345.6f) * Mathf.Sin(t * 7891.2f + 1.3f)
                        * Mathf.Sin(t * 5432.1f + 2.7f);
            float thump = Sine(92f, t) * Mathf.Exp(-t * 11f);
            return noise * Mathf.Exp(-t * 20f) * 0.55f + thump * 0.75f;
        }

        private float WinSample(float t)
        {
            // Мажорное арпеджио C5-E5-G5-C6.
            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float[] starts = { 0f, 0.16f, 0.32f, 0.48f };
            float s = 0f;
            for (int i = 0; i < 4; i++)
            {
                float lt = t - starts[i];
                if (lt < 0f) continue;
                float env = Mathf.Exp(-lt * 5f) * Mathf.Min(1f, lt * 60f);
                s += (Sine(freqs[i], lt) * 0.8f + Sine(freqs[i] * 2f, lt) * 0.15f) * env;
            }
            return s * 0.42f;
        }

        private float LoseSample(float t)
        {
            // Нисходящие грустные ноты.
            float[] freqs = { 392f, 311.1f, 233.1f };
            float[] starts = { 0f, 0.2f, 0.4f };
            float s = 0f;
            for (int i = 0; i < 3; i++)
            {
                float lt = t - starts[i];
                if (lt < 0f) continue;
                float env = Mathf.Exp(-lt * 4.5f) * Mathf.Min(1f, lt * 50f);
                s += (Sine(freqs[i], lt) + 0.3f * Sine(freqs[i] * 3f, lt)) * env;
            }
            return s * 0.35f;
        }

        private float WhooshSample(float t)
        {
            float noise = Mathf.Sin(t * 9871.3f) * Mathf.Sin(t * 6543.7f + 0.8f);
            float bell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.2f));
            return noise * bell * bell * 0.4f;
        }

        private float ShieldSample(float t)
        {
            float shimmer = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 9f * t);
            float s = Sine(880f, t) * 0.5f + Sine(1318.5f, t) * 0.35f;
            return s * shimmer * Mathf.Exp(-t * 7f) * 0.7f;
        }

        private float FanfareSample(float t)
        {
            // Быстрое мажорное арпеджио D5-F#5-A5-D6 + «блеск» сверху.
            float[] freqs = { 587.33f, 739.99f, 880f, 1174.66f };
            float[] starts = { 0f, 0.09f, 0.18f, 0.27f };
            float s = 0f;
            for (int i = 0; i < 4; i++)
            {
                float lt = t - starts[i];
                if (lt < 0f) continue;
                float env = Mathf.Exp(-lt * 6f) * Mathf.Min(1f, lt * 80f);
                s += (Sine(freqs[i], lt) * 0.7f + Sine(freqs[i] * 2f, lt) * 0.2f) * env;
            }
            s += Sine(2349.3f, t) * Mathf.Exp(-t * 9f) * 0.12f;
            return s * 0.5f;
        }

        private float DebuffSample(float t)
        {
            // Нисходящая пара нот с «пьяным» вибрато и низким жужжанием.
            float wob = 1f + 0.025f * Mathf.Sin(2f * Mathf.PI * 11f * t);
            float[] freqs = { 466.16f, 349.23f };
            float[] starts = { 0f, 0.17f };
            float s = 0f;
            for (int i = 0; i < 2; i++)
            {
                float lt = t - starts[i];
                if (lt < 0f) continue;
                float env = Mathf.Exp(-lt * 5f) * Mathf.Min(1f, lt * 60f);
                s += Sine(freqs[i] * wob, lt) * env;
            }
            float buzz = Square(92f, t) * Mathf.Exp(-t * 8f) * 0.08f;
            return s * 0.42f + buzz;
        }
    }
}
