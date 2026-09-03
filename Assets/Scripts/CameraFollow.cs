using UnityEngine;

namespace BallGame.Utility
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Positioning")]
        [SerializeField] private Vector3 offset = new Vector3(-4.5f, 9.5f, -7.0f);
        [SerializeField] private Vector3 lookAheadOffset = new Vector3(2.0f, 0.5f, 4.0f);

        [Header("Settings")]
        [SerializeField] private float smoothTime = 0.2f;

        private Vector3 currentVelocity;

        private void LateUpdate()
        {
            if (target == null) return;

            // Плавний рух за гравцем
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

            // Орієнтація погляду трохи вперед за траєкторією
            Vector3 targetFocusPoint = target.position + lookAheadOffset;
            transform.LookAt(targetFocusPoint);
        }
    }
}