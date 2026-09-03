using System.Collections.Generic;
using UnityEngine;
using BallGame.Shot;

namespace BallGame.Core
{
    public class ShotPool : MonoBehaviour
    {
        public static ShotPool Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private ShotProjectile shotPrefab;
        [SerializeField] private int initialPoolSize = 6;

        private readonly Queue<ShotProjectile> pool = new Queue<ShotProjectile>();

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
            if (shotPrefab == null) return;

            for (int i = 0; i < initialPoolSize; i++)
            {
                ShotProjectile shot = Instantiate(shotPrefab, transform);
                shot.gameObject.SetActive(false);
                pool.Enqueue(shot);
            }
        }

        public ShotProjectile Get(Vector3 position, Quaternion rotation)
        {
            ShotProjectile shot;

            if (pool.Count > 0)
            {
                shot = pool.Dequeue();
            }
            else
            {
                shot = Instantiate(shotPrefab, transform);
            }

            shot.transform.position = position;
            shot.transform.rotation = rotation;
            shot.gameObject.SetActive(true);
            return shot;
        }

        public void ReturnToPool(ShotProjectile shot)
        {
            if (shot == null) return;

            shot.gameObject.SetActive(false);
            shot.transform.SetParent(transform);
            pool.Enqueue(shot);
        }
    }
}