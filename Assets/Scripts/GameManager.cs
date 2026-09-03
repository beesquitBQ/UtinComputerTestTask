using System;
using UnityEngine;
using BallGame.Player;
using BallGame.Level;
using BallGame.Shot;

namespace BallGame.Core
{
    public enum LoseReason
    {
        OverCharged,
        OutOfResource
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerBall playerBall;
        [SerializeField] private ChargeShotController chargeShotController;
        [SerializeField] private LevelManager levelManager;

        [Header("Timers")]
        [SerializeField] private float defeatCheckGraceTime = 0.4f;

        public bool IsGameOver { get; private set; }

        public event Action OnWin;
        public event Action<LoseReason> OnLose;

        private float outOfResourceTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Reset()
        {
            playerBall = FindFirstObjectByType<PlayerBall>();
            chargeShotController = FindFirstObjectByType<ChargeShotController>();
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        private void Update()
        {
            if (IsGameOver || playerBall == null || levelManager == null) return;

            bool hasActiveShots = ShotProjectile.ActiveShotCount > 0;
            bool isCharging = chargeShotController != null && chargeShotController.IsCharging;
            bool hasRemainingObstacles = levelManager.EstimateRemainingObstacleCount() > 0;
            bool isPlayerJumping = playerBall.IsJumping;

            if (playerBall.IsAtCritical && !hasActiveShots && !isCharging && !isPlayerJumping && hasRemainingObstacles)
            {
                outOfResourceTimer += Time.deltaTime;
                if (outOfResourceTimer >= defeatCheckGraceTime)
                {
                    LoseGame(LoseReason.OutOfResource);
                }
            }
            else
            {
                outOfResourceTimer = 0f;
            }
        }

        public void WinGame()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            OnWin?.Invoke();
            Debug.Log("[GameManager] Win!");
        }

        public void LoseGame(LoseReason reason)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            OnLose?.Invoke(reason);
            Debug.Log($"[GameManager] Lose: {reason}");
        }
    }
}