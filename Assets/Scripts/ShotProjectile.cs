using System.Collections;
using UnityEngine;
using BallGame.Obstacles;
using BallGame.Core;

namespace BallGame.Shot
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ShotProjectile : MonoBehaviour
    {
        public static int ActiveShotCount { get; private set; }

        [Header("Lifetime & Impact")]
        [SerializeField] private float maxLifetime = 3.0f;
        [SerializeField] private float impactDespawnDelay = 0.05f;

        private const float MIN_VISUAL_RADIUS = 0.02f;
        private const string BOUNDS_TAG = "LevelBounds";

        public float CurrentRadius { get; private set; }

        private float blastRadiusMultiplier = 2f;
        private Vector3 velocity;
        private bool launched;
        private bool isDetonating;
        private float lifeTimer;
        private Coroutine despawnCoroutine;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
        }

        public void Initialize(float blastRadiusMultiplier)
        {
            this.blastRadiusMultiplier = blastRadiusMultiplier;
            launched = false;
            isDetonating = false;
            lifeTimer = 0f;
        }

        public void SetRadius(float radius)
        {
            CurrentRadius = Mathf.Max(radius, 0f);
            float visual = Mathf.Max(CurrentRadius, MIN_VISUAL_RADIUS);
            transform.localScale = Vector3.one * (visual * 2f);
        }

        public void Launch(Vector3 direction, float speed)
        {
            if (launched) return;

            launched = true;
            isDetonating = false;
            lifeTimer = 0f;
            ActiveShotCount++;
            velocity = direction.normalized * speed;
        }

        private void Update()
        {
            if (!launched) return;

            transform.position += velocity * Time.deltaTime;

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifetime && !isDetonating)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!launched) return;

            Obstacle obstacle = other.GetComponentInParent<Obstacle>();
            if (obstacle != null && !obstacle.IsDestroyed && !obstacle.IsInfected)
            {
                float blastRadius = CurrentRadius * blastRadiusMultiplier;
                ExplosionSystem.Explode(obstacle, blastRadius);

                if (!isDetonating)
                {
                    isDetonating = true;
                    if (despawnCoroutine != null) StopCoroutine(despawnCoroutine);
                    despawnCoroutine = StartCoroutine(DelayedDespawnRoutine(impactDespawnDelay));
                }
                return;
            }

            if (other.CompareTag(BOUNDS_TAG))
            {
                Despawn();
            }
        }

        private IEnumerator DelayedDespawnRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            Despawn();
        }

        public void Despawn()
        {
            if (despawnCoroutine != null)
            {
                StopCoroutine(despawnCoroutine);
                despawnCoroutine = null;
            }

            if (launched)
            {
                ActiveShotCount = Mathf.Max(0, ActiveShotCount - 1);
                launched = false;
            }

            isDetonating = false;

            if (ShotPool.Instance != null)
            {
                ShotPool.Instance.ReturnToPool(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            if (despawnCoroutine != null)
            {
                StopCoroutine(despawnCoroutine);
                despawnCoroutine = null;
            }

            if (launched)
            {
                ActiveShotCount = Mathf.Max(0, ActiveShotCount - 1);
                launched = false;
            }

            isDetonating = false;
        }
    }
}