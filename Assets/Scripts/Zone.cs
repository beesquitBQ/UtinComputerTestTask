using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BallGame.Obstacles;

namespace BallGame.Level
{
    public class Zone : MonoBehaviour
    {
        [SerializeField] private Transform waypoint;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private List<Obstacle> obstacles = new List<Obstacle>();

        public Transform Waypoint => waypoint;
        public Transform AimTarget => aimTarget != null ? aimTarget : waypoint;
        public IReadOnlyList<Obstacle> Obstacles => obstacles;

        public bool IsCleared
        {
            get
            {
                if (obstacles == null || obstacles.Count == 0) return true;
                return obstacles.All(o => o == null || o.IsDestroyed);
            }
        }

        public bool ContainsObstacle(Obstacle obstacle)
        {
            return obstacles != null && obstacles.Contains(obstacle);
        }

        private void Reset()
        {
            obstacles = GetComponentsInChildren<Obstacle>().ToList();
        }
    }
}