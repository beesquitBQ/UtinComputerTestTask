using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using BallGame.Core;
using BallGame.Player;
using BallGame.Level;

namespace BallGame.UI
{
    public class GameUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlayerBall playerBall;
        [SerializeField] private LevelManager levelManager;

        [Header("HUD")]
        [SerializeField] private Slider sizeSlider;
        [SerializeField] private TextMeshProUGUI zoneProgressText;

        [Header("Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private TextMeshProUGUI loseReasonText;

        [Header("Buttons")]
        [SerializeField] private Button winRestartButton;
        [SerializeField] private Button loseRestartButton;

        private const float MIN_THRESHOLD = 0.001f;
        private const float WIN_DELAY = 0.3f;
        private const float LOSE_DELAY = 0.5f;

        private bool canTapToRestart;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (playerBall == null) playerBall = FindFirstObjectByType<PlayerBall>();
            if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();

            if (winRestartButton != null) winRestartButton.onClick.AddListener(RestartLevel);
            if (loseRestartButton != null) loseRestartButton.onClick.AddListener(RestartLevel);
        }

        private void OnEnable()
        {
            if (playerBall != null) playerBall.OnRadiusChanged += HandleRadiusChanged;
            if (levelManager != null) levelManager.OnZoneCleared += HandleZoneCleared;

            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm != null)
            {
                gm.OnWin += HandleWin;
                gm.OnLose += HandleLose;
            }
        }

        private void OnDisable()
        {
            if (playerBall != null) playerBall.OnRadiusChanged -= HandleRadiusChanged;
            if (levelManager != null) levelManager.OnZoneCleared -= HandleZoneCleared;

            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm != null)
            {
                gm.OnWin += HandleWin;
                gm.OnLose += HandleLose;
            }
        }

        private void Start()
        {
            UpdateZoneText(0, levelManager != null ? levelManager.TotalZones : 1);
            if (playerBall != null)
            {
                HandleRadiusChanged(playerBall.CurrentRadius, playerBall.StartRadius);
            }
        }

        private void Update()
        {
            if (!canTapToRestart) return;

            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                RestartLevel();
            }
        }

        private void HandleRadiusChanged(float currentRadius, float startRadius)
        {
            if (sizeSlider != null && playerBall != null)
            {
                float maxSpendableRange = playerBall.StartRadius - playerBall.CriticalRadius;
                float currentAvailable = currentRadius - playerBall.CriticalRadius;

                float linearValue = maxSpendableRange > MIN_THRESHOLD
                    ? Mathf.Clamp01(currentAvailable / maxSpendableRange)
                    : 0f;

                sizeSlider.value = linearValue;
            }
        }

        private void HandleZoneCleared(int currentZone, int totalZones)
        {
            UpdateZoneText(currentZone, totalZones);
        }

        private void UpdateZoneText(int current, int total)
        {
            if (zoneProgressText != null)
            {
                zoneProgressText.text = $"Zone: {Mathf.Min(current + 1, total)} / {total}";
            }
        }

        private void HandleWin()
        {
            if (winPanel != null) winPanel.SetActive(true);
            StartCoroutine(EnableTapToRestartWithDelay(WIN_DELAY));
        }

        private void HandleLose(LoseReason reason)
        {
            if (losePanel != null) losePanel.SetActive(true);
            if (loseReasonText != null)
            {
                loseReasonText.text = reason == LoseReason.OverCharged
                    ? "The ball was overcharged into the shot!"
                    : "Not enough ball size left to clear the path!";
            }
            StartCoroutine(EnableTapToRestartWithDelay(LOSE_DELAY));
        }

        private IEnumerator EnableTapToRestartWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            canTapToRestart = true;
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}