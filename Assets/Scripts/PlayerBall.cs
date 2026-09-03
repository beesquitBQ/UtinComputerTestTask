using System;
using System.Collections;
using UnityEngine;

namespace BallGame.Player
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerBall : MonoBehaviour
    {
        [Header("Size Settings")]
        [SerializeField] private float startRadius = 1.0f;
        [SerializeField] private float criticalRadius = 0.25f;
        [SerializeField] private float minVisualRadius = 0.05f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 1.8f;
        [SerializeField] private float jumpDuration = 0.55f;

        private const float SPHERE_VOLUME_FACTOR = 4f / 3f * Mathf.PI;
        private const float PARABOLA_ARC_FACTOR = 4f;
        private const float CRITICAL_TOLERANCE = 0.0005f;
        private const float MIN_DURATION_LIMIT = 0.01f;

        public event Action<float, float> OnRadiusChanged;
        public event Action OnCriticalReached;
        public event Action OnJumpFinished;

        public float StartRadius => startRadius;
        public float CriticalRadius => criticalRadius;
        public float CurrentRadius { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsAtCritical => CurrentRadius <= criticalRadius + CRITICAL_TOLERANCE;

        private float groundY;
        private float CurrentVolume => RadiusToVolume(CurrentRadius);
        private float CriticalVolume => RadiusToVolume(criticalRadius);

        private void Awake()
        {
            CurrentRadius = startRadius;
            groundY = transform.position.y - startRadius;
            ApplyVisualScaleAndPosition();
        }

        public static float RadiusToVolume(float r) => SPHERE_VOLUME_FACTOR * r * r * r;
        public static float VolumeToRadius(float v) => Mathf.Pow(Mathf.Max(v, 0f) / SPHERE_VOLUME_FACTOR, 1f / 3f);

        public float TryConsumeVolume(float requestedVolume)
        {
            float available = CurrentVolume - CriticalVolume;
            float toTake = Mathf.Clamp(requestedVolume, 0f, Mathf.Max(available, 0f));
            if (toTake <= 0f) return 0f;

            SetRadius(VolumeToRadius(CurrentVolume - toTake));
            return toTake;
        }

        public void ReturnVolume(float volume)
        {
            if (volume <= 0f) return;
            SetRadius(VolumeToRadius(CurrentVolume + volume));
        }

        private void SetRadius(float newRadius)
        {
            CurrentRadius = Mathf.Max(newRadius, 0f);
            ApplyVisualScaleAndPosition();
            OnRadiusChanged?.Invoke(CurrentRadius, startRadius);

            if (IsAtCritical)
                OnCriticalReached?.Invoke();
        }

        private void ApplyVisualScaleAndPosition()
        {
            float visualRadius = Mathf.Max(CurrentRadius, minVisualRadius);
            transform.localScale = Vector3.one * (visualRadius * 2f);

            if (!IsJumping)
            {
                Vector3 p = transform.position;
                transform.position = new Vector3(p.x, groundY + CurrentRadius, p.z);
            }
        }

        public void JumpTo(Vector3 targetPosition)
        {
            if (IsJumping) return;
            StartCoroutine(JumpRoutine(targetPosition));
        }

        private IEnumerator JumpRoutine(Vector3 targetPosition)
        {
            IsJumping = true;
            Vector3 start = transform.position;
            Vector3 target = new Vector3(targetPosition.x, groundY + CurrentRadius, targetPosition.z);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(jumpDuration, MIN_DURATION_LIMIT);
                float tt = Mathf.Clamp01(t);

                Vector3 flat = Vector3.Lerp(start, target, tt);
                float arc = PARABOLA_ARC_FACTOR * jumpHeight * tt * (1f - tt);

                transform.position = new Vector3(flat.x, flat.y + arc, flat.z);
                yield return null;
            }

            transform.position = target;
            IsJumping = false;
            OnJumpFinished?.Invoke();
        }
    }
}