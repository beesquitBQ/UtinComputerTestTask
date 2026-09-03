using System.Collections.Generic;
using UnityEngine;
using BallGame.Player;
using BallGame.Level;

namespace BallGame.Utility
{
    public class TrackWidthVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBall playerBall;
        [SerializeField] private LevelManager levelManager;

        [Header("Track Segments")]
        [SerializeField] private List<Transform> trackSegments = new List<Transform>();

        [Header("Width Settings")]
        [SerializeField] private float widthMultiplier = 1.4f;
        [SerializeField] private float minWidth = 0.8f;
        [SerializeField] private float smoothSpeed = 6f;

        private const float WIDTH_CHANGE_THRESHOLD = 0.001f;
        private const string TRACK_TAG = "Track";
        private const string PLAYER_FILTER = "player";
        private const string OBSTACLE_FILTER = "obstacle";

        private float targetWidth;
        private float currentWidth;
        private readonly List<Vector3> originalScales = new List<Vector3>();

        private void Reset()
        {
            playerBall = FindFirstObjectByType<PlayerBall>();
            levelManager = FindFirstObjectByType<LevelManager>();
            AutoFindTrackSegments();
        }

        private void Awake()
        {
            if (playerBall == null)
            {
                playerBall = FindFirstObjectByType<PlayerBall>();
            }

            if (trackSegments == null || trackSegments.Count == 0)
            {
                AutoFindTrackSegments();
            }

            originalScales.Clear();
            foreach (var segment in trackSegments)
            {
                if (segment != null)
                {
                    originalScales.Add(segment.localScale);
                }
            }

            if (playerBall != null)
            {
                float initialWidth = CalculateTargetWidth(playerBall.StartRadius);
                currentWidth = initialWidth;
                targetWidth = initialWidth;
                ApplyWidthToAllSegments(currentWidth);
            }
        }

        private void OnEnable()
        {
            if (playerBall != null) playerBall.OnRadiusChanged += HandleRadiusChanged;
        }

        private void OnDisable()
        {
            if (playerBall != null) playerBall.OnRadiusChanged -= HandleRadiusChanged;
        }

        private void Update()
        {
            if (Mathf.Abs(currentWidth - targetWidth) > WIDTH_CHANGE_THRESHOLD)
            {
                currentWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * smoothSpeed);
                ApplyWidthToAllSegments(currentWidth);
            }
        }

        private void HandleRadiusChanged(float currentRadius, float startRadius)
        {
            targetWidth = CalculateTargetWidth(currentRadius);
        }

        private float CalculateTargetWidth(float radius)
        {
            float calculated = (radius * 2f) * widthMultiplier;
            return Mathf.Max(calculated, minWidth);
        }

        private void ApplyWidthToAllSegments(float width)
        {
            for (int i = 0; i < trackSegments.Count; i++)
            {
                Transform seg = trackSegments[i];
                if (seg == null) continue;

                Vector3 orig = (i < originalScales.Count) ? originalScales[i] : seg.localScale;
                seg.localScale = new Vector3(width, orig.y, orig.z);
            }
        }

        [ContextMenu("Find Track Segments")]
        public void AutoFindTrackSegments()
        {
            trackSegments.Clear();

            GameObject[] taggedTracks = GameObject.FindGameObjectsWithTag(TRACK_TAG);
            if (taggedTracks.Length > 0)
            {
                foreach (var obj in taggedTracks) trackSegments.Add(obj.transform);
                return;
            }

            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                string lowerName = r.name.ToLower();
                if (!lowerName.Contains(PLAYER_FILTER) && !lowerName.Contains(OBSTACLE_FILTER))
                {
                    trackSegments.Add(r.transform);
                }
            }
        }
    }
}