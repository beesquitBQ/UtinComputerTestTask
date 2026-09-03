using UnityEngine;
using BallGame.Core;
using BallGame.Player;

namespace BallGame.Level
{
    [RequireComponent(typeof(Collider))]
    public class FinishTarget : MonoBehaviour
    {
        private bool isTriggered;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;

            PlayerBall player = other.GetComponentInParent<PlayerBall>();
            if (player != null)
            {
                isTriggered = true;
                GameManager.Instance?.WinGame();
            }
        }
    }
}