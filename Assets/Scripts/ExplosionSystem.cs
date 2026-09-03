using UnityEngine;
using System.Collections.Generic;

namespace BallGame.Obstacles
{
    public static class ExplosionSystem
    {
        public static LayerMask ObstacleLayerMask = ~0;

        private const float CLUSTER_CHAIN_DISTANCE = 0.75f;
        private const float CASCADE_STEP_DELAY = 0.035f;
        private const float MIN_BLAST_RADIUS_FOR_CHAIN = 0.25f;
        private const float CHAIN_DEPTH_MULTIPLIER = 2.5f;
        private const float SPREAD_RADIUS_MULTIPLIER = 1.35f;
        private const float SPREAD_RADIUS_OFFSET = 0.1f;

        public static List<Obstacle> Explode(Obstacle directHit, float primaryBlastRadius)
        {
            var infectedList = new List<Obstacle>();
            var visited = new HashSet<Obstacle>();
            var queue = new Queue<(Obstacle obstacle, int depth)>();

            if (directHit == null || directHit.IsDestroyed || directHit.IsInfected)
                return infectedList;

            Vector3 epicentre = directHit.transform.position;

            int maxChainDepth = primaryBlastRadius < MIN_BLAST_RADIUS_FOR_CHAIN 
                ? 0 
                : Mathf.FloorToInt(primaryBlastRadius * CHAIN_DEPTH_MULTIPLIER);

            float maxSpreadDistance = (primaryBlastRadius * SPREAD_RADIUS_MULTIPLIER) + SPREAD_RADIUS_OFFSET;

            visited.Add(directHit);
            queue.Enqueue((directHit, 0));
            infectedList.Add(directHit);

            // Первинне радіальне ураження
            Collider[] initialHits = Physics.OverlapSphere(
                epicentre,
                primaryBlastRadius,
                ObstacleLayerMask,
                QueryTriggerInteraction.Ignore
            );

            foreach (var col in initialHits)
            {
                Obstacle obs = col.GetComponentInParent<Obstacle>();
                if (obs != null && !obs.IsDestroyed && !obs.IsInfected && !visited.Contains(obs))
                {
                    visited.Add(obs);
                    infectedList.Add(obs);

                    if (maxChainDepth > 0)
                    {
                        queue.Enqueue((obs, 1));
                    }
                }
            }

            // Ланцюгова детонація сусідів
            while (queue.Count > 0)
            {
                var (current, depth) = queue.Dequeue();

                if (depth >= maxChainDepth) continue;

                Collider[] neighbors = Physics.OverlapSphere(
                    current.transform.position,
                    CLUSTER_CHAIN_DISTANCE,
                    ObstacleLayerMask,
                    QueryTriggerInteraction.Ignore
                );

                foreach (var col in neighbors)
                {
                    Obstacle neighbor = col.GetComponentInParent<Obstacle>();
                    if (neighbor != null && !neighbor.IsDestroyed && !neighbor.IsInfected && !visited.Contains(neighbor))
                    {
                        float distFromEpicentre = Vector3.Distance(epicentre, neighbor.transform.position);
                        if (distFromEpicentre <= maxSpreadDistance)
                        {
                            visited.Add(neighbor);
                            infectedList.Add(neighbor);
                            queue.Enqueue((neighbor, depth + 1));
                        }
                    }
                }
            }

            // Запуск затримки детонації
            for (int i = 0; i < infectedList.Count; i++)
            {
                Obstacle obs = infectedList[i];
                float distance = Vector3.Distance(epicentre, obs.transform.position);
                float delay = distance * CASCADE_STEP_DELAY;
                obs.Infect(delay);
            }

            return infectedList;
        }
    }
}