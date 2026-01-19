using UnityEngine;
using UnityEngine.InputSystem;

namespace VRFishing.Fishing
{
    public class ReelHandle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FishingRod fishingRod;
        [SerializeField] private Transform reelVisual;
        [SerializeField] private Transform pivotPoint;
        [SerializeField] private Transform controllerTransform;

        [Header("Input")]
        [SerializeField] private InputActionReference gripAction;

        [Header("Reel Settings")]
        [SerializeField] private float reelPerRotation = 50f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private bool isGrabbing = false;
        private bool controllerInRange = false;
        private Vector3 lastControllerPos;
        private float totalRotation = 0f;

        private void OnEnable()
        {
            if (gripAction != null && gripAction.action != null)
            {
                gripAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (gripAction != null && gripAction.action != null)
            {
                gripAction.action.Disable();
            }
        }

        private void Update()
        {
            if (gripAction == null || controllerTransform == null || pivotPoint == null) return;

            float gripValue = gripAction.action.ReadValue<float>();
            // Start grabbing
            if (!isGrabbing && gripValue > 0.8f && controllerInRange)
            {
                isGrabbing = true;
                lastControllerPos = controllerTransform.position;
                totalRotation = 0f;
                Debug.Log("Reel grabbed");
            }

            // Stop grabbing
            if (isGrabbing && gripValue < 0.2f)
            {
                isGrabbing = false;
                Debug.Log($"Reel released - {totalRotation:F0} degrees total");
            }

            // Handle rotation
            if (isGrabbing)
            {
                CalculateRotation();
            }
            else
            {
                fishingRod.reel = false;
            }
        }

        private void CalculateRotation()
        {
            Vector3 pivotPos = pivotPoint.position;
            Vector3 pivotUp = pivotPoint.up;
            
            // Project controller positions onto the plane
            Vector3 lastFlat = lastControllerPos - pivotPos;
            lastFlat = lastFlat - Vector3.Dot(lastFlat, pivotUp) * pivotUp;

            Vector3 currentFlat = controllerTransform.position - pivotPos;
            currentFlat = currentFlat - Vector3.Dot(currentFlat, pivotUp) * pivotUp;

            // Skip if too close to center
            if (lastFlat.magnitude < 0.01f || currentFlat.magnitude < 0.01f)
            {
                lastControllerPos = controllerTransform.position;
                return;
            }

            // Calculate signed angle
            float angle = Vector3.SignedAngle(lastFlat, currentFlat, pivotUp);

            // Filter noise
            if (Mathf.Abs(angle) > 0.5f && Mathf.Abs(angle) < 45f)
            {
                totalRotation += angle;

                // Rotate the visual
                if (reelVisual != null)
                {
                    reelVisual.Rotate(pivotUp, angle, Space.World);
                }

                // Reel in
                if (fishingRod != null)
                {
                    float reelAmount = (Mathf.Abs(angle) / 360f) * reelPerRotation;
                    
                    if ( reelAmount > 0.001f && isGrabbing && controllerInRange)
                    {
                        fishingRod.reel = true;
                    }
                    else
                    {
                        fishingRod.reel = false;
                    }


                    if (showDebug && reelAmount > 0.001f)
                    {
                        Debug.Log($"Reel: {reelAmount:F4}m | Angle: {angle:F1} | Total: {totalRotation:F1}");
                    }
                }
            }

            lastControllerPos = controllerTransform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the controller
            if (other.transform == controllerTransform || other.transform.IsChildOf(controllerTransform))
            {
                controllerInRange = true;
                Debug.Log("Controller in reel range");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform == controllerTransform || other.transform.IsChildOf(controllerTransform))
            {
                controllerInRange = false;
                Debug.Log("Controller left reel range");
            }
        }

        private void OnDrawGizmos()
        {
            if (pivotPoint == null) return;

            // Draw pivot axis
            Gizmos.color = Color.green;
            Gizmos.DrawRay(pivotPoint.position, pivotPoint.up * 0.15f);

            // Draw rotation circle
            Gizmos.color = Color.yellow;
            int segments = 32;
            float radius = 0.08f;

            Vector3 right = pivotPoint.right * radius;
            Vector3 forward = pivotPoint.forward * radius;

            Vector3 prevPoint = pivotPoint.position + right;
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 nextPoint = pivotPoint.position + right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // Draw controller connection when grabbing
            if (Application.isPlaying && isGrabbing && controllerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pivotPoint.position, controllerTransform.position);
            }
        }
    }
}