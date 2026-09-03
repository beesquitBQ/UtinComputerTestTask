using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BallGame.Player;
using BallGame.Obstacles;
using BallGame.Core;

namespace BallGame.Level
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Player & Zones")]
        [SerializeField] private PlayerBall playerBall;
        [SerializeField] private List<Zone> zones = new List<Zone>();

        [Header("Finish Section")]
        [SerializeField] private Transform doorApproachPoint;
        [SerializeField] private GoalDoor goalDoor;
        [SerializeField] private Transform finishPoint;

        private const float DOOR_OPEN_TIMEOUT = 1.5f;
        private const float DOOR_FALLBACK_DELAY = 0.3f;

        private int currentZoneIndex;
        private bool isFinalSequenceRunning;

        public event Action<int, int> OnZoneCleared;
        public event Action OnAllZonesCleared;

        public int CurrentZoneIndex => currentZoneIndex;
        public int TotalZones => zones.Count;

        private void OnEnable()
        {
            Obstacle.OnObstacleDestroyed += HandleObstacleDestroyed;
            if (playerBall != null) playerBall.OnJumpFinished += HandleJumpFinished;
        }

        private void OnDisable()
        {
            Obstacle.OnObstacleDestroyed -= HandleObstacleDestroyed;
            if (playerBall != null) playerBall.OnJumpFinished -= HandleJumpFinished;
        }

        public Transform GetCurrentAimTarget()
        {
            if (currentZoneIndex < zones.Count && zones[currentZoneIndex] != null)
            {
                return zones[currentZoneIndex].AimTarget;
            }

            if (doorApproachPoint != null) return doorApproachPoint;
            if (finishPoint != null) return finishPoint;

            return null;
        }

        private void HandleObstacleDestroyed(Obstacle obstacle)
        {
            if (currentZoneIndex >= zones.Count || isFinalSequenceRunning) return;

            Zone currentZone = zones[currentZoneIndex];
            if (currentZone == null) return;

            if (!currentZone.ContainsObstacle(obstacle)) return;

            if (currentZone.IsCleared)
            {
                AdvanceToNextZone();
            }
        }

        private void HandleJumpFinished()
        {
            if (currentZoneIndex < zones.Count && zones[currentZoneIndex].IsCleared && !isFinalSequenceRunning)
            {
                AdvanceToNextZone();
            }
        }

        private void AdvanceToNextZone()
        {
            currentZoneIndex++;
            OnZoneCleared?.Invoke(currentZoneIndex, zones.Count);

            if (currentZoneIndex < zones.Count)
            {
                Vector3 targetPos = zones[currentZoneIndex].Waypoint.position;
                playerBall.JumpTo(targetPos);
            }
            else
            {
                OnAllZonesCleared?.Invoke();
                StartCoroutine(FinalDoorSequenceRoutine());
            }
        }

        private IEnumerator FinalDoorSequenceRoutine()
        {
            isFinalSequenceRunning = true;

            // Стрибок перед двері
            Vector3 approachPos = doorApproachPoint != null ? doorApproachPoint.position : transform.position;
            playerBall.JumpTo(approachPos);

            while (playerBall.IsJumping)
            {
                yield return null;
            }

            // Відкриття дверей
            if (goalDoor != null)
            {
                goalDoor.OpenDoor();

                float timeout = DOOR_OPEN_TIMEOUT;
                while (!goalDoor.IsFullyOpen && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(DOOR_FALLBACK_DELAY);
            }

            // Фінальний стрибок
            if (finishPoint != null)
            {
                playerBall.JumpTo(finishPoint.position);

                while (playerBall.IsJumping)
                {
                    yield return null;
                }
            }

            GameManager.Instance?.WinGame();
        }

        public int EstimateRemainingObstacleCount()
        {
            int count = 0;
            foreach (var z in zones)
            {
                if (z == null) continue;
                foreach (var o in z.Obstacles)
                {
                    if (o != null && !o.IsDestroyed) count++;
                }
            }
            return count;
        }
    }
}