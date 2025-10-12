// Assets/Scripts/Gameplay/Fishing/FishingRodController.cs
// AKTUALIZACJA - integracja z ReelHandle zamiast ReelingDetector
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRFishing.Data;

namespace VRFishing.Gameplay
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class FishingRodController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FishingRodConfig config;

        [Header("Rod Parts")]
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform handleTransform;

        [Header("Line & Hook")]
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private Transform hook;
        [SerializeField] private HookPhysics hookPhysics;

        [Header("Cast Detection")]
        [SerializeField] private float castVelocityThreshold = 2f;
        [SerializeField] private float castAngleThreshold = 45f;
        [SerializeField] private float maxReasonableVelocity = 10f;

        [Header("Reeling")]
        [SerializeField] private ReelHandle reelHandle; // ZMIENIONE z ReelingDetector
        [SerializeField] private float reelingForce = 5f;

        [Header("Hook Offset")]
        [SerializeField] private Vector3 hookRestOffset = new Vector3(0, -0.1f, 0);

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor currentInteractor;
        private bool isLineOut = false;
        private bool canCast = true;
        private float grabTime = -999f;

        private Vector3 previousPosition;
        private Vector3 currentVelocity;
        private float lastVelocityUpdateTime;

        private void Awake()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            if (config == null)
            {
                Debug.LogError($"[FishingRodController] Brak konfiguracji!");
            }

            if (fishingLine == null)
            {
                Debug.LogError($"[FishingRodController] Brak FishingLine!");
            }

            if (hookPhysics == null && hook != null)
            {
                hookPhysics = hook.GetComponent<HookPhysics>();
            }

            // Auto-znajdź ReelHandle jeśli nie przypisany
            if (reelHandle == null)
            {
                reelHandle = GetComponentInChildren<ReelHandle>();
                if (reelHandle != null)
                {
                    Debug.Log("[FishingRodController] Auto-znaleziono ReelHandle");
                }
            }

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void Start()
        {
            previousPosition = transform.position;
            lastVelocityUpdateTime = Time.time;

            if (hook != null && rodTip != null)
            {
                hook.position = rodTip.position + hookRestOffset;
            }

            if (fishingLine != null && config != null)
            {
                fishingLine.SetMaxLength(config.MaxLineLength);
            }
        }

        private void Update()
        {
            if (!isLineOut && hook != null && rodTip != null)
            {
                hook.position = rodTip.position + hookRestOffset;
            }

            float timeSinceGrab = Time.time - grabTime;

            if (grabInteractable.isSelected &&
                canCast &&
                !isLineOut &&
                timeSinceGrab > 0.5f)
            {
                CalculateVelocity();
                DetectCastGesture();
            }

            // Korbowanie - ZMIENIONE na ReelHandle
            if (isLineOut && reelHandle != null)
            {
                HandleReeling();
            }

            if (isLineOut && fishingLine != null)
            {
                EnforceMaxLineLength();
            }
        }

        private void HandleReeling()
        {
            // Sprawdź czy korbka jest złapana i czy jest obrót
            if (reelHandle.IsGrabbed && reelHandle.CurrentReelingAmount > 0.001f)
            {
                float reelingAmount = reelHandle.CurrentReelingAmount * reelingForce;
                ReelInHook(reelingAmount);

                // Zużyj amount (reset do następnej klatki)
                reelHandle.ConsumeReelingAmount();
            }
        }

        private void ReelInHook(float amount)
        {
            if (hook == null || rodTip == null || hookPhysics == null) return;

            Vector3 directionToRod = (rodTip.position - hook.position).normalized;
            float currentDistance = Vector3.Distance(hook.position, rodTip.position);

            if (currentDistance < 0.5f)
            {
                CompleteReeling();
                return;
            }

            Vector3 newPosition = hook.position + directionToRod * amount;

            Rigidbody rb = hook.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(newPosition);
                rb.linearVelocity *= 0.95f;
            }
            else
            {
                hook.position = newPosition;
            }
        }

        private void CompleteReeling()
        {
            Debug.Log("[FishingRodController] Żyłka całkowicie zwinięta");

            isLineOut = false;

            if (hookPhysics != null)
            {
                hookPhysics.ResetPosition(rodTip.position + hookRestOffset);
            }
        }

        private void EnforceMaxLineLength()
        {
            if (fishingLine.IsAtMaxLength() && hookPhysics != null)
            {
                Rigidbody rb = hook.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 directionFromRod = (hook.position - rodTip.position).normalized;
                    Vector3 velocityAwayFromRod = Vector3.Dot(rb.linearVelocity, directionFromRod) * directionFromRod;

                    if (Vector3.Dot(rb.linearVelocity, directionFromRod) > 0)
                    {
                        rb.linearVelocity -= velocityAwayFromRod;
                    }
                }
            }
        }

        // ... reszta metod bez zmian (CalculateVelocity, DetectCastGesture, PerformCast, etc.)
        // Skopiuj z poprzedniej wersji

        private void CalculateVelocity()
        {
            Vector3 currentPosition = transform.position;
            float deltaTime = Time.time - lastVelocityUpdateTime;

            if (deltaTime < 0.001f) return;

            Vector3 displacement = currentPosition - previousPosition;
            Vector3 calculatedVelocity = displacement / deltaTime;

            if (calculatedVelocity.magnitude > maxReasonableVelocity)
            {
                Debug.LogWarning($"[FishingRod] Odrzucono prędkość: {calculatedVelocity.magnitude:F1} m/s");
            }
            else
            {
                currentVelocity = calculatedVelocity;
            }

            previousPosition = currentPosition;
            lastVelocityUpdateTime = Time.time;
        }

        private void DetectCastGesture()
        {
            float speed = currentVelocity.magnitude;
            if (speed < 4f) return;

            Vector3 localVel = transform.InverseTransformDirection(currentVelocity);
            bool isForwardDominant = localVel.z > Mathf.Abs(localVel.y) && localVel.z > Mathf.Abs(localVel.x);
            bool isForwardFastEnough = localVel.z > 3.5f;

            if (isForwardDominant && isForwardFastEnough)
            {
                PerformCast(currentVelocity);
            }
        }

        private void PerformCast(Vector3 velocity)
        {
            if (!canCast || isLineOut) return;

            float speed = velocity.magnitude;
            float normalizedSpeed = Mathf.InverseLerp(3f, 8f, speed);
            float castForce = Mathf.Lerp(config.MinCastForce, config.MaxCastForce, normalizedSpeed);
            castForce = Mathf.Min(castForce, 10f);

            Vector3 castDirection = (velocity.normalized + rodTip.forward).normalized;

            if (hookPhysics != null)
            {
                hookPhysics.LaunchHook(castDirection, castForce);
            }

            isLineOut = true;
            canCast = false;

            Debug.Log($"[FishingRodController] Zarzut! Siła: {castForce:F1}");
            Invoke(nameof(ResetCastCooldown), 2f);
        }

        private void ResetCastCooldown()
        {
            canCast = true;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            currentInteractor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
            grabTime = Time.time;
            currentVelocity = Vector3.zero;
            previousPosition = transform.position;
            lastVelocityUpdateTime = Time.time;
            Debug.Log("[FishingRodController] Wędka złapana");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            currentInteractor = null;
            Debug.Log("[FishingRodController] Wędka puszczona");
        }

        private void OnDestroy()
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        public bool IsLineOut => isLineOut;
        public float GetCurrentLineLength()
        {
            return fishingLine != null ? fishingLine.GetCurrentLineLength() : 0f;
        }
    }
}