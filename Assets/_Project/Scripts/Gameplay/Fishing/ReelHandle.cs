// Assets/Scripts/Gameplay/Fishing/ReelHandle.cs
// NOWY SKRYPT - interaktywna korbka
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFishing.Gameplay
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class ReelHandle : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private Transform rotationPivot; // Punkt obrotu (je�li inny ni� self)
        [SerializeField] private Vector3 rotationAxis = Vector3.forward; // O� obrotu (lokalnie)
        [SerializeField] private float rotationSpeed = 1f;

        [Header("Reeling Output")]
        [SerializeField] private float reelingPerDegree = 0.01f; // Ile metr�w �y�ki na stopie� obrotu

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private bool isGrabbed = false;

        private Quaternion previousRotation;
        private float totalRotationDegrees = 0f;
        private float currentReelingAmount = 0f; // Metr�w do zwini�cia w tej klatce

        private Rigidbody rb;
        private ConfigurableJoint joint;

        private void Awake()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            if (rotationPivot == null)
            {
                rotationPivot = transform;
            }

            previousRotation = rotationPivot.localRotation;

            // Subskrypcje
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);

            SetupPhysics();
        }

        private void SetupPhysics()
        {
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.mass = 0.1f;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Dodaj ConfigurableJoint �eby ograniczy� ruch tylko do obrotu
            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = transform.parent.GetComponent<Rigidbody>(); // Pod��cz do w�dki

            // Zablokuj wszystkie osie ruchu pozycyjnego
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // Pozw�l na obr�t tylko wok� jednej osi (Z - forward)
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Free; // Wolny obr�t wok� Z
        }

        private void Update()
        {
            if (isGrabbed)
            {
                CalculateRotation();
            }
        }

        private void CalculateRotation()
        {
            Quaternion currentRotation = rotationPivot.localRotation;

            // Oblicz delta obrotu wok� rotation axis
            Quaternion deltaRotation = Quaternion.Inverse(previousRotation) * currentRotation;

            // Wyci�gnij k�t obrotu wok� osi Z (lokalnie)
            float angle;
            Vector3 axis;
            deltaRotation.ToAngleAxis(out angle, out axis);

            // Normalizuj k�t do -180..180
            if (angle > 180f) angle -= 360f;

            // Sprawd� czy obr�t jest wok� w�a�ciwej osi
            if (Vector3.Dot(axis, rotationAxis) > 0.5f || Vector3.Dot(axis, -rotationAxis) > 0.5f)
            {
                // Akumuluj obr�t
                totalRotationDegrees += angle;

                // Oblicz ilo�� reelingu
                currentReelingAmount = Mathf.Abs(angle) * reelingPerDegree;

                // Debug co 360 stopni
                if (Mathf.Abs(totalRotationDegrees) >= 360f)
                {
                    Debug.Log($"[ReelHandle] Pe�ny obr�t! Total: {totalRotationDegrees:F0}�");
                    totalRotationDegrees = 0f; // Reset
                }
            }

            previousRotation = currentRotation;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            previousRotation = rotationPivot.localRotation;
            Debug.Log("[ReelHandle] Korbka z�apana");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            currentReelingAmount = 0f;
            Debug.Log("[ReelHandle] Korbka puszczona");
        }

        private void OnDestroy()
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        // Publiczne gettery dla FishingRodController
        public bool IsGrabbed => isGrabbed;
        public float CurrentReelingAmount => currentReelingAmount;
        public float TotalRotation => totalRotationDegrees;

        // Reset reeling amount po przeczytaniu (wywo�aj z FishingRodController po u�yciu)
        public void ConsumeReelingAmount()
        {
            currentReelingAmount = 0f;
        }
    }
}