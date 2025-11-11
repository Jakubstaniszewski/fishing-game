using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFishing.Fishing
{
    /// <summary>
    /// Korbka do nawijania - obraca się wokół osi wędki (UP)
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class ReelHandle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FishingRod fishingRod;
        [SerializeField] private Transform rotationPivot;

        [Header("Reel Settings")]
        [SerializeField] private float reelPerRotation = 0.5f;
        [SerializeField] private float hapticIntensity = 0.2f;
        [SerializeField] private float minAngleDelta = 0.5f; // Minimalna detekcja

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Transform controllerTransform;
        private bool isGrabbed = false;

        // Tracking rotation
        private Vector3 lastControllerLocalPos;
        private float totalRotation = 0f;

        private void Awake()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            // POPRAWKA: Ustaw Rigidbody jako kinematic (nie usuwaj!)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Debug.Log("✅ ReelHandle Rigidbody configured as kinematic");
            }
            else
            {
                // Dodaj Rigidbody jeśli brakuje
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Debug.Log("✅ Added kinematic Rigidbody to ReelHandle");
            }

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);

            if (rotationPivot == null)
            {
                rotationPivot = transform;
            }
        }

        private void Update()
        {
            if (isGrabbed && controllerTransform != null)
            {
                DetectRotation();
            }
        }

        private void DetectRotation()
        {
            Vector3 controllerLocalPos = rotationPivot.InverseTransformPoint(controllerTransform.position);

            if (lastControllerLocalPos != Vector3.zero)
            {
                float lastAngle = Mathf.Atan2(lastControllerLocalPos.x, lastControllerLocalPos.z) * Mathf.Rad2Deg;
                float currentAngleCalc = Mathf.Atan2(controllerLocalPos.x, controllerLocalPos.z) * Mathf.Rad2Deg;

                float deltaAngle = Mathf.DeltaAngle(lastAngle, currentAngleCalc);

                // ZWIĘKSZONE FILTRY - większa czułość na błędy
                if (Mathf.Abs(deltaAngle) < 90f && Mathf.Abs(deltaAngle) > minAngleDelta)
                {
                    totalRotation += deltaAngle;

                    rotationPivot.Rotate(Vector3.up, deltaAngle, Space.Self);

                    if (fishingRod != null)
                    {
                        // DOKŁADNIEJSZE OBLICZENIE - mniejsze wartości
                        float rotationFraction = Mathf.Abs(deltaAngle) / 360f;
                        float reelAmount = rotationFraction * reelPerRotation;

                        // DEBUG - pokaż dokładnie co się dzieje
                        if (showDebug && reelAmount > 0.001f)
                        {
                            Debug.Log($"🔄 Angle: {deltaAngle:F2}° | Fraction: {rotationFraction:F4} | Reel: {reelAmount:F4}m | Total: {totalRotation:F1}°");
                        }

                        fishingRod.ReelInByAmount(reelAmount);

                        SendHaptic(hapticIntensity, 0.02f);
                    }
                }
            }

            lastControllerLocalPos = controllerLocalPos;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            controllerTransform = args.interactorObject.transform;
            lastControllerLocalPos = Vector3.zero;
            totalRotation = 0f;

            Debug.Log($"🎣 Reel handle grabbed!");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            controllerTransform = null;
            lastControllerLocalPos = Vector3.zero;

            Debug.Log($"🎣 Reel released - {totalRotation:F0}° total rotation");
        }

        private void SendHaptic(float amplitude, float duration)
        {
            if (grabInteractable.isSelected)
            {
                var interactor = grabInteractable.firstInteractorSelecting;
                if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
                {
                    controllerInteractor.SendHapticImpulse(amplitude, duration);
                }
            }
        }

        private void OnDestroy()
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        private void OnDrawGizmos()
        {
            if (rotationPivot != null)
            {
                // Pokaż oś obrotu (UP - wzdłuż wędki)
                Gizmos.color = Color.green;
                Gizmos.DrawRay(rotationPivot.position, rotationPivot.up * 0.3f);

                // Pokaż płaszczyznę obrotu (poziomo)
                Gizmos.color = Color.yellow;
                DrawCircleGizmo(rotationPivot.position, rotationPivot.up, 0.12f);

                if (isGrabbed && controllerTransform != null)
                {
                    // Linia do kontrolera
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(rotationPivot.position, controllerTransform.position);
                    Gizmos.DrawWireSphere(controllerTransform.position, 0.02f);
                }
            }
        }

        // Helper - rysuj okrąg
        private void DrawCircleGizmo(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 forward = Vector3.Slerp(normal, -normal, 0.5f);
            Vector3 right = Vector3.Cross(normal, forward).normalized * radius;
            forward = Vector3.Cross(right, normal).normalized * radius;

            Vector3 lastPoint = center + right;
            for (int i = 1; i <= 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                Vector3 nextPoint = center + right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Gizmos.DrawLine(lastPoint, nextPoint);
                lastPoint = nextPoint;
            }
        }
    }
}