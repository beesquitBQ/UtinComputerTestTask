using System;
using System.Collections;
using UnityEngine;
using BallGame.Core;

namespace BallGame.Obstacles
{
    public class Obstacle : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private GameObject fallbackVfxPrefab;
        [SerializeField] private Color infectedColor = new Color(1f, 0.3f, 0.2f, 1f);

        private const float WOBBLE_AMPLITUDE = 0.15f;

        public static event Action<Obstacle> OnObstacleDestroyed;

        public bool IsInfected { get; private set; }
        public bool IsDestroyed { get; private set; }

        private Renderer meshRenderer;
        private Vector3 originalScale;

        private void Awake()
        {
            meshRenderer = GetComponentInChildren<Renderer>();
            originalScale = transform.localScale;
        }

        public void Infect(float delaySeconds = 0f)
        {
            if (IsInfected || IsDestroyed) return;

            IsInfected = true;

            if (delaySeconds <= 0f)
            {
                DestroyNow();
            }
            else
            {
                StartCoroutine(DelayedExplosionRoutine(delaySeconds));
            }
        }

        private IEnumerator DelayedExplosionRoutine(float delay)
        {
            float elapsed = 0f;
            if (meshRenderer != null)
            {
                meshRenderer.material.color = infectedColor;
            }

            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / delay;
                transform.localScale = originalScale * (1f + WOBBLE_AMPLITUDE * Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }

            DestroyNow();
        }

        private void DestroyNow()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;

            if (VFXPool.Instance != null)
            {
                VFXPool.Instance.PlayExplosion(transform.position);
            }
            else if (fallbackVfxPrefab != null)
            {
                Instantiate(fallbackVfxPrefab, transform.position, Quaternion.identity);
            }

            OnObstacleDestroyed?.Invoke(this);

            Destroy(gameObject);
        }
    }
}