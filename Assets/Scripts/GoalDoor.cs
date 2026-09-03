using UnityEngine;

namespace BallGame.Level
{
    public class GoalDoor : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float openDistance = 5f;
        [SerializeField] private Transform doorLeaf;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 3.5f, 0f);
        [SerializeField] private float openSpeed = 3f;

        private const float OPEN_THRESHOLD = 0.1f;
        private bool isOpenTriggered;
        private Vector3 closedLocalPos;

        public bool IsOpenTriggered => isOpenTriggered;

        public bool IsFullyOpen
        {
            get
            {
                if (doorLeaf == null) return true;
                Vector3 target = closedLocalPos + openLocalOffset;
                return Vector3.Distance(doorLeaf.localPosition, target) < OPEN_THRESHOLD;
            }
        }

        private void Awake()
        {
            if (doorLeaf != null) 
                closedLocalPos = doorLeaf.localPosition;
        }

        private void Update()
        {
            if (doorLeaf == null) return;

            if (player != null && !isOpenTriggered)
            {
                float dist = Vector3.Distance(player.position, transform.position);
                if (dist <= openDistance)
                {
                    OpenDoor();
                }
            }

            if (isOpenTriggered)
            {
                Vector3 targetLocalPos = closedLocalPos + openLocalOffset;
                doorLeaf.localPosition = Vector3.Lerp(doorLeaf.localPosition, targetLocalPos, Time.deltaTime * openSpeed);
            }
        }

        public void OpenDoor()
        {
            isOpenTriggered = true;
        }
    }
}