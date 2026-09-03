using UnityEngine;

namespace BallGame.Player
{
    [RequireComponent(typeof(LineRenderer))]
    public class AimPathVisualizer : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Trail Settings")]
        [SerializeField] private float trailLength = 3.5f;
        [SerializeField] private float widthMultiplier = 1.6f;
        [SerializeField] private float minWidth = 0.08f;

        [Header("Color & Alpha")]
        [SerializeField] private Color baseColor = new Color(0.2f, 0.8f, 1f, 1f);
        [Range(0f, 1f)]
        [SerializeField] private float startAlpha = 0.75f;

        private const float ALPHA_KEY_MID_HIGH = 0.7f;
        private const float ALPHA_KEY_MID_LOW = 0.2f;
        private const float TIME_KEY_1 = 0.3f;
        private const float TIME_KEY_2 = 0.7f;
        private const int LINE_POINTS_COUNT = 2;

        private void Reset()
        {
            lineRenderer = GetComponent<LineRenderer>();
            EnsureTransparentMaterial();
            SetupGradient();
        }

        private void Awake()
        {
            if (lineRenderer == null) 
                lineRenderer = GetComponent<LineRenderer>();

            EnsureTransparentMaterial();
            SetupGradient();
            Hide();
        }

        private void EnsureTransparentMaterial()
        {
            if (lineRenderer == null) return;

            if (lineRenderer.sharedMaterial == null || lineRenderer.sharedMaterial.name.Contains("Default"))
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                    shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

                if (shader != null)
                {
                    lineRenderer.material = new Material(shader);
                }
            }
        }

        private void SetupGradient()
        {
            if (lineRenderer == null) return;

            lineRenderer.positionCount = LINE_POINTS_COUNT;
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(baseColor, 0.0f),
                    new GradientColorKey(baseColor, 1.0f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(startAlpha, 0.0f),
                    new GradientAlphaKey(startAlpha * ALPHA_KEY_MID_HIGH, TIME_KEY_1),
                    new GradientAlphaKey(startAlpha * ALPHA_KEY_MID_LOW, TIME_KEY_2),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );

            lineRenderer.colorGradient = gradient;
        }

        public void Show()
        {
            if (lineRenderer != null) lineRenderer.enabled = true;
        }

        public void Hide()
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        public void UpdateTrail(Vector3 shotFrontPoint, Vector3 direction, float currentShotRadius)
        {
            if (lineRenderer == null || !lineRenderer.enabled) return;

            float calculatedWidth = Mathf.Max(currentShotRadius * widthMultiplier * 2f, minWidth);
            lineRenderer.startWidth = calculatedWidth;
            lineRenderer.endWidth = calculatedWidth;

            Vector3 start = shotFrontPoint;
            Vector3 end = start + direction.normalized * trailLength;

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }
    }
}