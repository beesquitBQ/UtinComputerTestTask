using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
    public class VFXPool : MonoBehaviour
    {
        public static VFXPool Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private ParticleSystem explosionVfxPrefab;
        [SerializeField] private int initialPoolSize = 12;

        private const float MIN_VFX_LIFETIME = 0.4f;
        private readonly Queue<ParticleSystem> pool = new Queue<ParticleSystem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Prewarm();
        }

        private void Prewarm()
        {
            if (explosionVfxPrefab == null) return;

            for (int i = 0; i < initialPoolSize; i++)
            {
                ParticleSystem ps = Instantiate(explosionVfxPrefab, transform);
                ps.gameObject.SetActive(false);
                pool.Enqueue(ps);
            }
        }

        public void PlayExplosion(Vector3 position)
        {
            ParticleSystem ps;

            if (pool.Count > 0)
            {
                ps = pool.Dequeue();
            }
            else if (explosionVfxPrefab != null)
            {
                ps = Instantiate(explosionVfxPrefab, transform);
            }
            else
            {
                return;
            }

            ps.transform.position = position;
            ps.gameObject.SetActive(true);
            ps.Clear();
            ps.Play();

            StartCoroutine(ReturnRoutine(ps));
        }

        private IEnumerator ReturnRoutine(ParticleSystem ps)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            yield return new WaitForSeconds(Mathf.Max(duration, MIN_VFX_LIFETIME));

            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                ps.transform.SetParent(transform);
                pool.Enqueue(ps);
            }
        }
    }
}