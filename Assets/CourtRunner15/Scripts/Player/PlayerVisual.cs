using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Процедурная анимация юриста: бег, прыжок, подкат, мигание
    /// при неуязвимости и мягкая тень под ногами.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        // Заполняется фабрикой ProceduralModelFactory.CreateLawyer.
        public Transform BodyRoot;
        public Transform Head;
        public Transform ArmLPivot;
        public Transform ArmRPivot;
        public Transform LegLPivot;
        public Transform LegRPivot;
        // Вторичные суставы (есть только у LawyerV3, допускают null).
        public Transform KneeLPivot;
        public Transform KneeRPivot;
        public Transform ElbowLPivot;
        public Transform ElbowRPivot;
        public Transform Briefcase;
        public Transform Shadow;

        private Renderer[] _renderers;
        private float _speed01;
        private bool _grounded = true;
        private bool _sliding;
        private float _vy;
        private float _boost;
        private bool _flicker;

        private float _legSwing;
        private float _armSwing;
        private float _bodyTilt;
        private float _bodyDrop;
        private float _kneeL;
        private float _kneeR;
        private float _elbowL;
        private float _elbowR;

        public void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void SetMotion(float speed01, bool grounded, bool sliding, float verticalVelocity, float boost)
        {
            _speed01 = speed01;
            _grounded = grounded;
            _sliding = sliding;
            _vy = verticalVelocity;
            _boost = boost;
        }

        public void SetFlicker(bool on)
        {
            _flicker = on;
            if (!on) SetRenderersEnabled(true);
        }

        public void ResetPose()
        {
            _legSwing = 0f;
            _armSwing = 0f;
            _bodyTilt = 0f;
            _bodyDrop = 0f;
            _kneeL = 0f;
            _kneeR = 0f;
            _elbowL = 0f;
            _elbowR = 0f;
            _flicker = false;
            SetRenderersEnabled(true);
            ApplyPose(0f, 0f, 0f, 0f, 0f, 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float phase = Time.time * (9f + 5f * _speed01 + 3f * _boost);

            float targetLegSwing;
            float targetArmSwing;
            float targetTilt;
            float targetDrop;
            float legPose = 0f; // статичная поза ног (прыжок/подкат)
            float armPose = 0f;

            if (_sliding)
            {
                targetLegSwing = 0f;
                targetArmSwing = 0f;
                targetTilt = 58f;
                targetDrop = 0.12f;
                legPose = -42f;
                armPose = -35f;
            }
            else if (!_grounded)
            {
                targetLegSwing = 0f;
                targetArmSwing = 0f;
                targetTilt = _vy > 0f ? 10f : 4f;
                targetDrop = 0f;
                legPose = _vy > 0f ? 48f : 20f;
                armPose = -55f;
            }
            else
            {
                targetLegSwing = 38f + 10f * _boost;
                targetArmSwing = 32f + 10f * _boost;
                targetTilt = 7f + 4f * _boost;
                targetDrop = 0f;
            }

            float lerp = 1f - Mathf.Exp(-12f * dt);
            _legSwing = Mathf.Lerp(_legSwing, targetLegSwing, lerp);
            _armSwing = Mathf.Lerp(_armSwing, targetArmSwing, lerp);
            _bodyTilt = Mathf.Lerp(_bodyTilt, targetTilt, lerp);
            _bodyDrop = Mathf.Lerp(_bodyDrop, targetDrop, lerp);

            float s = Mathf.Sin(phase);
            float bob = _grounded && !_sliding ? Mathf.Abs(Mathf.Cos(phase)) * 0.065f * (0.4f + 0.6f * _speed01) : 0f;

            UpdateSecondaryJoints(s, dt);
            ApplyPose(s, bob, legPose, armPose, _legSwing, _armSwing);

            UpdateShadow();
            UpdateFlicker();
        }

        /// <summary>Колени и локти: сгиб в такт бегу, фиксированные позы в прыжке/подкате.</summary>
        private void UpdateSecondaryJoints(float s, float dt)
        {
            float kneeL, kneeR, elbowL, elbowR;
            if (_sliding)
            {
                kneeL = 74f; kneeR = 66f;
                elbowL = -26f; elbowR = -22f;
            }
            else if (!_grounded)
            {
                if (_vy > 0f) { kneeL = 18f; kneeR = 64f; }
                else { kneeL = 30f; kneeR = 44f; }
                elbowL = -66f; elbowR = -58f;
            }
            else
            {
                // Голень отстаёт от бедра: сгиб при выносе ноги вперёд.
                float amp = (44f + 16f * _boost) * Mathf.Clamp01(_legSwing / 38f);
                kneeL = 10f + amp * Mathf.Max(0f, -s);
                kneeR = 10f + amp * Mathf.Max(0f, s);
                // Локти согнуты как у бегуна; рука с портфелем жёстче.
                elbowL = -(46f + 26f * Mathf.Max(0f, s));
                elbowR = -(38f + 12f * Mathf.Max(0f, -s));
            }

            float k = 1f - Mathf.Exp(-14f * dt);
            _kneeL = Mathf.Lerp(_kneeL, kneeL, k);
            _kneeR = Mathf.Lerp(_kneeR, kneeR, k);
            _elbowL = Mathf.Lerp(_elbowL, elbowL, k);
            _elbowR = Mathf.Lerp(_elbowR, elbowR, k);
        }

        private void ApplyPose(float s, float bob, float legPose, float armPose, float legSwing, float armSwing)
        {
            if (BodyRoot != null)
            {
                BodyRoot.localPosition = new Vector3(0f, bob - _bodyDrop, 0f);
                // Лёгкий встречный разворот корпуса добавляет живости бегу.
                BodyRoot.localRotation = Quaternion.Euler(_bodyTilt, -s * 4f, s * 2.5f);
            }
            if (LegLPivot != null) LegLPivot.localRotation = Quaternion.Euler(legPose + s * legSwing, 0f, 0f);
            if (LegRPivot != null) LegRPivot.localRotation = Quaternion.Euler(legPose * 0.4f - s * legSwing, 0f, 0f);
            if (ArmLPivot != null) ArmLPivot.localRotation = Quaternion.Euler(armPose - s * armSwing, 0f, 6f);
            if (ArmRPivot != null) ArmRPivot.localRotation = Quaternion.Euler(armPose + s * armSwing * 0.55f, 0f, -6f);
            if (KneeLPivot != null) KneeLPivot.localRotation = Quaternion.Euler(_kneeL, 0f, 0f);
            if (KneeRPivot != null) KneeRPivot.localRotation = Quaternion.Euler(_kneeR, 0f, 0f);
            if (ElbowLPivot != null) ElbowLPivot.localRotation = Quaternion.Euler(_elbowL, 0f, 0f);
            if (ElbowRPivot != null) ElbowRPivot.localRotation = Quaternion.Euler(_elbowR, 0f, 0f);
            if (Head != null) Head.localRotation = Quaternion.Euler(-_bodyTilt * 0.55f, s * 3f, 0f);
        }

        private void UpdateShadow()
        {
            if (Shadow == null) return;
            // Тень остаётся на полу, сжимается с высотой прыжка.
            float worldY = transform.position.y;
            Shadow.position = new Vector3(transform.position.x, 0.02f, transform.position.z);
            float k = Mathf.Clamp01(1f - worldY / 2.2f);
            float size = Mathf.Lerp(0.35f, 1f, k);
            Shadow.localScale = new Vector3(size, size, size);
        }

        private void UpdateFlicker()
        {
            if (!_flicker) return;
            bool visible = Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
            SetRenderersEnabled(visible);
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].enabled = enabled;
            }
        }
    }
}
