#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BallGame.Level;
using BallGame.Player;

namespace BallGame.EditorTools
{
    public class LevelBudgetValidator : EditorWindow
    {
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private PlayerBall playerBall;
        [SerializeField] private float requiredBufferPercent = 20f;
        [SerializeField] private float blastRadiusMultiplier = 2.2f;

        // Константи балансу відповідно до поточної фізики гри
        private const float Z_SLICE_THRESHOLD = 1.8f;
        private const float CLUSTER_CONNECT_DISTANCE = 0.85f;
        private const float MIN_CHAIN_BLAST_RADIUS = 0.25f;
        private const float MIN_TAP_SHOT_RADIUS = 0.05f;
        private const float SPLIT_SAFETY_MARGIN = 0.15f;

        [MenuItem("Ball Game/Level Budget Validator")]
        private static void Open()
        {
            GetWindow<LevelBudgetValidator>("Level Budget Validator");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Level Balance Validator Tool:\n" +
                "• Accurately calculates real volume cost based on physical shot radius + blast multiplier\n" +
                "• Evaluates single targets, cascade clusters, and wide split obstacles\n" +
                "• Verifies player margin buffer (Requirement: >= 20%)",
                MessageType.Info);

            EditorGUILayout.Space(5);
            levelManager = (LevelManager)EditorGUILayout.ObjectField("Level Manager", levelManager, typeof(LevelManager), true);
            playerBall = (PlayerBall)EditorGUILayout.ObjectField("Player Ball", playerBall, typeof(PlayerBall), true);
            requiredBufferPercent = EditorGUILayout.FloatField("Min Buffer (%)", requiredBufferPercent);
            blastRadiusMultiplier = EditorGUILayout.FloatField("Blast Multiplier", blastRadiusMultiplier);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Validate Level Budget", GUILayout.Height(35)))
            {
                ValidateLevel();
            }
        }

        private void ValidateLevel()
        {
            if (levelManager == null || playerBall == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both LevelManager and PlayerBall.", "OK");
                return;
            }

            var zones = levelManager.GetComponentsInChildren<Zone>();
            if (zones.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No Zone components found inside LevelManager.", "OK");
                return;
            }

            float totalNeededVolume = 0f;
            string breakdownLog = "--- ZONE COST BREAKDOWN ---\n";

            for (int z = 0; z < zones.Length; z++)
            {
                Zone zone = zones[z];
                var obstacles = zone.Obstacles.Where(o => o != null).ToList();
                if (obstacles.Count == 0) continue;

                float zoneCost = 0f;
                var positions = obstacles.Select(o => o.transform.position).ToList();

                var zSlices = GroupByZSlice(positions, Z_SLICE_THRESHOLD);
                int singles = 0, splits = 0, clusters = 0;

                foreach (var slice in zSlices)
                {
                    if (slice.Count == 1)
                    {
                        // 1. Поодинокий блок (мінімальний клік)
                        singles++;
                        zoneCost += PlayerBall.RadiusToVolume(MIN_TAP_SHOT_RADIUS);
                    }
                    else
                    {
                        float minX = slice.Min(p => p.x);
                        float maxX = slice.Max(p => p.x);
                        float widthSpan = maxX - minX;

                        // Якщо блоки далеко один від одного — це спліт
                        if (widthSpan > CLUSTER_CONNECT_DISTANCE)
                        {
                            splits++;
                            
                            // Куля летить по центру X=0.
                            // Досяжність = фізичний радіус кулі R + радіус вибуху від її краю (R * blastMultiplier)
                            float maxDistanceFromCenter = slice.Max(p => Mathf.Abs(p.x));
                            float effectiveReachMultiplier = 1f + blastRadiusMultiplier;
                            float requiredShotRadius = (maxDistanceFromCenter + SPLIT_SAFETY_MARGIN) / effectiveReachMultiplier;

                            // Радіус повинен бути не меншим за мінімальний для вибуху
                            requiredShotRadius = Mathf.Max(requiredShotRadius, MIN_TAP_SHOT_RADIUS);
                            zoneCost += PlayerBall.RadiusToVolume(requiredShotRadius);
                        }
                        else
                        {
                            // 2. Кластер впритул (ланцюгова реакція)
                            clusters++;
                            float clusterTriggerRadius = MIN_CHAIN_BLAST_RADIUS / blastRadiusMultiplier;
                            zoneCost += PlayerBall.RadiusToVolume(clusterTriggerRadius);
                        }
                    }
                }

                totalNeededVolume += zoneCost;
                breakdownLog += $"Zone {z + 1} ({zone.name}): Singles: {singles}, Splits: {splits}, Clusters: {clusters} -> Cost: {zoneCost:F3} m³\n";
            }

            float startVolume = PlayerBall.RadiusToVolume(playerBall.StartRadius);
            float criticalVolume = PlayerBall.RadiusToVolume(playerBall.CriticalRadius);
            float spendableVolume = startVolume - criticalVolume;

            float remainingSpendable = spendableVolume - totalNeededVolume;
            float actualBufferPercent = spendableVolume > 0f
                ? (remainingSpendable / spendableVolume) * 100f
                : -100f;

            string summary = $"{breakdownLog}\n" +
                             $"• Available Spendable Volume: {spendableVolume:F3} m³\n" +
                             $"• Required Level Volume: {totalNeededVolume:F3} m³\n" +
                             $"• Remaining Margin: {actualBufferPercent:F1}% (Required: >={requiredBufferPercent}%)\n";

            if (actualBufferPercent >= requiredBufferPercent)
            {
                EditorUtility.DisplayDialog("Balance OK! ✅", summary + "\nLevel is passable with enough safety margin!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Low Margin or Impassable! ⚠️", summary + "\nConsider increasing Player StartRadius or moving side obstacles closer.", "OK");
            }

            Debug.Log($"[LevelBudgetValidator]\n{summary}");
        }

        private static List<List<Vector3>> GroupByZSlice(List<Vector3> points, float zThreshold)
        {
            var sorted = points.OrderBy(p => p.z).ToList();
            var slices = new List<List<Vector3>>();

            List<Vector3> currentSlice = new List<Vector3>();

            for (int i = 0; i < sorted.Count; i++)
            {
                if (currentSlice.Count == 0)
                {
                    currentSlice.Add(sorted[i]);
                }
                else
                {
                    if (Mathf.Abs(sorted[i].z - currentSlice[0].z) <= zThreshold)
                    {
                        currentSlice.Add(sorted[i]);
                    }
                    else
                    {
                        slices.Add(currentSlice);
                        currentSlice = new List<Vector3> { sorted[i] };
                    }
                }
            }

            if (currentSlice.Count > 0)
            {
                slices.Add(currentSlice);
            }

            return slices;
        }
    }
}
#endif