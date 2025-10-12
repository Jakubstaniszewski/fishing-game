// Assets/Scripts/Gameplay/Fishing/FishingLine.cs
// AKTUALIZACJA - dodanie kontroli d³ugoœci
using UnityEngine;

namespace VRFishing.Gameplay
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLine : MonoBehaviour
    {
        [Header("Line Points")]
        [SerializeField] private Transform lineStart;
        [SerializeField] private Transform lineEnd;

        [Header("Line Properties")]
        [SerializeField] private int lineSegments = 35;
        [SerializeField] private float lineWidth = 0.001f;
        [SerializeField] private float lineSag = 0.1f;

        [Header("Line Length Control")]
        [SerializeField] private float maxLineLength = 20f;
        [SerializeField] private bool enforceMaxLength = true;

        private LineRenderer lineRenderer;
        private Vector3[] linePositions;
        private float currentLength = 0f;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            linePositions = new Vector3[lineSegments];
            ConfigureLineRenderer();
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.positionCount = lineSegments;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;

            if (lineRenderer.material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    lineRenderer.material = new Material(shader);
                }
            }

            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }

        private void LateUpdate()
        {
            if (lineStart == null || lineEnd == null)
            {
                return;
            }

            DrawLine();
            currentLength = GetCurrentLineLength();
        }

        private void DrawLine()
        {
            Vector3 startPos = lineStart.position;
            Vector3 endPos = lineEnd.position;

            for (int i = 0; i < lineSegments; i++)
            {
                float t = i / (float)(lineSegments - 1);
                Vector3 position = Vector3.Lerp(startPos, endPos, t);
                float sag = lineSag * (1f - Mathf.Pow(2f * t - 1f, 2f));
                position.y -= sag;
                linePositions[i] = position;
            }

            lineRenderer.SetPositions(linePositions);
        }

        public void SetLineVisibility(bool visible)
        {
            lineRenderer.enabled = visible;
        }

        public void SetLineEnd(Transform newEnd)
        {
            lineEnd = newEnd;
        }

        public void SetMaxLength(float length)
        {
            maxLineLength = length;
        }

        public float GetCurrentLineLength()
        {
            if (lineStart == null || lineEnd == null) return 0f;
            return Vector3.Distance(lineStart.position, lineEnd.position);
        }

        public float GetMaxLineLength() => maxLineLength;

        public bool IsAtMaxLength() => currentLength >= maxLineLength;

        public float GetLengthPercentage() => Mathf.Clamp01(currentLength / maxLineLength);

        // Zwraca ile metrów ¿y³ki jest jeszcze dostêpne
        public float GetRemainingLength() => Mathf.Max(0, maxLineLength - currentLength);
    }
}