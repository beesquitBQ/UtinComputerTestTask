using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using BallGame.Shot;
using BallGame.Core;
using BallGame.Level;

namespace BallGame.Player
{
    [RequireComponent(typeof(PlayerBall))]
    public class ChargeShotController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBall playerBall;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private ShotProjectile shotPrefab;
        [SerializeField] private AimPathVisualizer aimVisualizer;
        [SerializeField] private Transform fallbackTarget;

        [Header("Placement")]
        [SerializeField] private float surfaceGap = 0.03f;

        [Header("Charge Settings")]
        [SerializeField] private float chargeVolumePerSecond = 0.6f;
        [SerializeField] private float minShotRadius = 0.03f;

        [Header("Overcharge")]
        [SerializeField] private float overchargeGracePeriod = 0.8f;
        [SerializeField] private bool autoReleaseOnMaxCharge = false;

        [Header("Shot Settings")]
        [SerializeField] private float shotSpeed = 14f;
        [SerializeField] private float blastRadiusMultiplier = 2.2f;

        private ShotProjectile currentShot;
        private bool isCharging;
        private float overchargeTimer;

        private const float MIN_AIM_DIR_MAGNITUDE_SQR = 0.001f;

        public bool IsCharging => isCharging;

        private void Reset()
        {
            playerBall = GetComponent<PlayerBall>();
            aimVisualizer = GetComponentInChildren<AimPathVisualizer>();
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (Pointer.current == null) return;

            bool isDown = Pointer.current.press.wasPressedThisFrame;
            bool isHeld = Pointer.current.press.isPressed;
            bool isUp = Pointer.current.press.wasReleasedThisFrame;

            if (isDown)
            {
                if (IsPointerOverUI()) return;
                BeginCharge();
            }

            if (isHeld && isCharging)
            {
                ContinueCharge();
            }

            if (isUp && isCharging)
            {
                ReleaseShot();
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }

            if (Pointer.current != null)
            {
                return EventSystem.current.IsPointerOverGameObject(Pointer.current.deviceId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private void BeginCharge()
        {
            if (playerBall.IsJumping || playerBall.IsAtCritical) return;

            isCharging = true;
            overchargeTimer = 0f;

            Vector3 aimDir = GetAimDirection(transform.position);
            Vector3 initialPos = transform.position + aimDir * (playerBall.CurrentRadius + surfaceGap);

            if (ShotPool.Instance != null)
            {
                currentShot = ShotPool.Instance.Get(initialPos, Quaternion.identity);
            }
            else
            {
                currentShot = Instantiate(shotPrefab, initialPos, Quaternion.identity);
            }

            currentShot.Initialize(blastRadiusMultiplier);
            currentShot.SetRadius(minShotRadius);

            if (aimVisualizer != null) 
                aimVisualizer.Show();
        }

        private void ContinueCharge()
        {
            if (currentShot == null) return;

            float requested = chargeVolumePerSecond * Time.deltaTime;
            float taken = playerBall.TryConsumeVolume(requested);

            if (taken > 0f)
            {
                float newShotVolume = PlayerBall.RadiusToVolume(currentShot.CurrentRadius) + taken;
                currentShot.SetRadius(PlayerBall.VolumeToRadius(newShotVolume));
            }

            Vector3 aimDir = GetAimDirection(transform.position);
            float totalDistance = playerBall.CurrentRadius + currentShot.CurrentRadius + surfaceGap;
            Vector3 shotCenterPos = transform.position + aimDir * totalDistance;

            currentShot.transform.position = shotCenterPos;

            if (aimVisualizer != null)
            {
                Vector3 shotFrontEdge = shotCenterPos + aimDir * currentShot.CurrentRadius;
                aimVisualizer.UpdateTrail(shotFrontEdge, aimDir, currentShot.CurrentRadius);
            }

            if (playerBall.IsAtCritical)
            {
                if (autoReleaseOnMaxCharge)
                {
                    ReleaseShot();
                    return;
                }

                overchargeTimer += Time.deltaTime;
                if (overchargeTimer >= overchargeGracePeriod)
                {
                    CancelChargeWithoutRefund();
                    GameManager.Instance?.LoseGame(LoseReason.OverCharged);
                }
            }
        }

        private void ReleaseShot()
        {
            isCharging = false;
            overchargeTimer = 0f;

            if (aimVisualizer != null) 
                aimVisualizer.Hide();

            if (currentShot == null) return;

            Vector3 origin = currentShot.transform.position;
            Vector3 aimDir = GetAimDirection(origin);

            currentShot.Launch(aimDir, shotSpeed);
            currentShot = null;
        }

        private Vector3 GetAimDirection(Vector3 fromPosition)
        {
            Transform dynamicTarget = null;
            if (levelManager != null)
            {
                dynamicTarget = levelManager.GetCurrentAimTarget();
            }

            if (dynamicTarget == null) 
                dynamicTarget = fallbackTarget;

            if (dynamicTarget != null)
            {
                Vector3 targetPos = dynamicTarget.position;
                targetPos.y = fromPosition.y;
                Vector3 dir = (targetPos - fromPosition).normalized;
                return dir.sqrMagnitude > MIN_AIM_DIR_MAGNITUDE_SQR ? dir : transform.forward;
            }

            return transform.forward;
        }

        private void CancelChargeWithoutRefund()
        {
            isCharging = false;
            overchargeTimer = 0f;

            if (aimVisualizer != null) 
                aimVisualizer.Hide();

            if (currentShot != null)
            {
                currentShot.Despawn();
                currentShot = null;
            }
        }
    }
}