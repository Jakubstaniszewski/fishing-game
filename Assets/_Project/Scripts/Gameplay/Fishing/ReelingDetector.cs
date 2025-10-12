// Assets/Scripts/Gameplay/Fishing/ReelingDetector.cs
using UnityEngine;

namespace VRFishing.Gameplay
{
    public class ReelingDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform reelingHand; // Prawa r�ka (kontroler)
        [SerializeField] private Transform anchorPoint; // W�dka (lewa r�ka)

        [Header("Detection Settings")]
        [SerializeField] private float minRadiusForDetection = 0.1f; // Minimalny promie� ruchu
        [SerializeField] private float detectionSensitivity = 30f;   // K�t w stopniach
        [SerializeField] private float cooldownBetweenDetections = 0.1f;

        [Header("Reeling Speed")]
        [SerializeField] private float reelingSpeedMultiplier = 1f;

        private Vector3 previousLocalPosition;
        private float previousAngle = 0f;
        private float totalRotation = 0f;
        private float lastDetectionTime = 0f;
        private bool isReeling = false;
        private float currentReelingSpeed = 0f; // m/s

        private void Start()
        {
            if (reelingHand != null && anchorPoint != null)
            {
                previousLocalPosition = GetLocalPosition();
                previousAngle = GetCurrentAngle();
            }
        }

        private void Update()
        {
            if (reelingHand == null || anchorPoint == null)
            {
                return;
            }

            DetectReelingMotion();
        }

        private void DetectReelingMotion()
        {
            Vector3 currentLocalPos = GetLocalPosition();
            float distanceFromAnchor = currentLocalPos.magnitude;

            // Sprawd� czy r�ka jest w odpowiedniej odleg�o�ci (nie za blisko w�dki)
            if (distanceFromAnchor < minRadiusForDetection)
            {
                isReeling = false;
                currentReelingSpeed = 0f;
                return;
            }

            // Oblicz k�t w p�aszczy�nie XZ (poziomej) wzgl�dem anchor
            float currentAngle = GetCurrentAngle();
            float angleDelta = Mathf.DeltaAngle(previousAngle, currentAngle);

            // Sprawd� czy delta przekracza pr�g (ruch obrotowy)
            if (Mathf.Abs(angleDelta) > detectionSensitivity * Time.deltaTime)
            {
                // Wykryto obr�t
                totalRotation += angleDelta;

                // Oblicz pr�dko�� korbowania (stopnie/s -> m/s)
                float rotationSpeed = Mathf.Abs(angleDelta) / Time.deltaTime;
                currentReelingSpeed = (rotationSpeed / 360f) * reelingSpeedMultiplier;

                isReeling = true;
                lastDetectionTime = Time.time;

                // Debug
                if (totalRotation > 360f || totalRotation < -360f)
                {
                    Debug.Log($"[Reeling] Pe�ny obr�t! Total: {totalRotation:F0}�");
                    totalRotation = 0f; // Reset po pe�nym obrocie
                }
            }
            else
            {
                // Brak ruchu obrotowego
                if (Time.time - lastDetectionTime > cooldownBetweenDetections)
                {
                    isReeling = false;
                    currentReelingSpeed = 0f;
                }
            }

            previousAngle = currentAngle;
            previousLocalPosition = currentLocalPos;
        }

        private Vector3 GetLocalPosition()
        {
            // Pozycja prawej r�ki wzgl�dem w�dki (anchor)
            return anchorPoint.InverseTransformPoint(reelingHand.position);
        }

        private float GetCurrentAngle()
        {
            Vector3 localPos = GetLocalPosition();
            // K�t w p�aszczy�nie XZ (poziomej)
            return Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg;
        }

        public bool IsReeling => isReeling;
        public float ReelingSpeed => currentReelingSpeed;
        public float TotalRotation => totalRotation;
    }
}