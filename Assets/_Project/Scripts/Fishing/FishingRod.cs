using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

namespace VRFishing.Fishing
{
    /// <summary>
    /// Główny kontroler wędki z detekcją gestów
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class FishingRod : MonoBehaviour
    {
        [Header("Rod Components")]
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform hookObject;

        [Header("Hook Physics")]
        private GameObject activeHook;
        private Rigidbody hookRb;

        [Header("Line Settings")]
        [SerializeField] private float maxLineLength = 30f;
        [SerializeField] private float minLineLength = 2f;
        [SerializeField] private float reelSpeed = 2f;
        [SerializeField] private float reelForceMultiplier = 50f; // NOWY - możesz tunować w Inspectorze
        private float currentLineLength = 2f;

        [Header("Cast Settings")]
        [SerializeField] private float castForceMultiplier = 15f;
        [SerializeField] private float castDetectionThreshold = 0.8f; // NISKI próg!

        [Header("Debug")]
        [SerializeField] private bool showDebugGUI = true;

        [Header("State")]
        public bool isHeld = false;
        public FishingState currentState = FishingState.Idle;

        private float grabTime = 0f; // NOWE - kiedy złapano wędkę
        private const float CAST_COOLDOWN = 0.5f; // Pół sekundy cooldownu

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private LineRenderer lineRenderer;

        // Gesture detection
        private Vector3 lastPosition;
        private Vector3 velocity;
        private Transform controllerTransform;
        private float castTime = 0f;

        public enum FishingState
        {
            Idle,
            Casting,
            LineCast,
            Reeling,
            FishHooked
        }

        private void Awake()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            SetupLineRenderer();

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);

            if (rodTip == null)
            {
                Debug.LogError("❌ RodTip not assigned!");
            }

            if (hookObject != null)
            {
                hookObject.gameObject.SetActive(false);
            }
        }

        private void SetupLineRenderer()
        {
            GameObject lineObject = new GameObject("FishingLine");
            lineObject.transform.SetParent(null);

            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.03f;
            lineRenderer.endWidth = 0.025f;

            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = Color.white;
            lineRenderer.material = lineMat;

            lineRenderer.sortingOrder = 100;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 5;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        private void Update()
        {
            if (isHeld && controllerTransform != null)
            {
                // Oblicz velocity
                velocity = (controllerTransform.position - lastPosition) / Time.deltaTime;
                lastPosition = controllerTransform.position;

                HandleInput();
                DetectCastGesture();
            }

            UpdateLineVisuals();
            UpdateHookPhysics();
        }

        private void HandleInput()
        {
            if (currentState == FishingState.LineCast)
            {
                // Klawisz E = nawijanie (fallback)
                if (Input.GetKey(KeyCode.E))
                {
                    ReelIn();
                    return;
                }

                // Próba odczytu Trigger z VR kontrolera
                var devices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                    UnityEngine.XR.InputDeviceCharacteristics.Controller |
                    UnityEngine.XR.InputDeviceCharacteristics.Right,
                    devices
                );

                if (devices.Count > 0)
                {
                    if (devices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue))
                    {
                        if (triggerValue > 0.5f)
                        {
                            ReelIn();
                            return;
                        }
                    }
                }

                // Auto-reel
                if (Time.time - castTime > 5f)
                {
                    ReelIn();
                }
            }
        }

        private void DetectCastGesture()
        {
            if (currentState != FishingState.Idle) return;
            if (rodTip == null) return;

            if (Time.time - grabTime < CAST_COOLDOWN)
            {
                return; // Za wcześnie po złapaniu - ignoruj
            }

            // Prosta detekcja - dowolny szybki ruch
            float speed = velocity.magnitude;

            if (speed > castDetectionThreshold)
            {
                Debug.Log($"🎣 CAST! Velocity: {speed:F2} m/s");
                CastLine();
            }
        }

        private void CastLine()
        {
            if (currentState != FishingState.Idle) return;

            Debug.Log("========== CAST START ==========");
            currentState = FishingState.Casting;
            castTime = Time.time;

            CreateHook();

            Vector3 castDirection = rodTip.forward;
            float castForce = Mathf.Clamp(velocity.magnitude, 3f, 15f) * castForceMultiplier;

            hookRb.AddForce(castDirection * castForce, ForceMode.Impulse);

            lineRenderer.enabled = true;
            Debug.Log($"Line enabled: {lineRenderer.enabled}");

            SendHapticFeedback(0.5f, 0.2f);

            StartCoroutine(CastRoutine());
        }

        private IEnumerator CastRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            currentState = FishingState.LineCast;
            Debug.Log("✅ Line in water - hold TRIGGER or E to reel");
        }

        private void CreateHook()
        {
            if (activeHook != null)
            {
                Destroy(activeHook);
            }

            activeHook = new GameObject("Hook_Active");
            activeHook.transform.position = rodTip.position;
            activeHook.tag = "Hook"; // DODAJ TAG!

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(activeHook.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.1f;

            var renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;

            hookRb = activeHook.AddComponent<Rigidbody>();
            hookRb.mass = 0.05f;
            hookRb.linearDamping = 0.5f;
            hookRb.angularDamping = 0.5f;

            var collider = activeHook.AddComponent<SphereCollider>();
            collider.radius = 0.05f;
            collider.isTrigger = true;

            // DODAJ SKRYPT HACZYKA!
            activeHook.AddComponent<FishHook>();

            currentLineLength = minLineLength;

            Debug.Log($"Hook created at: {activeHook.transform.position}");
        }

        /// <summary>
        /// Automatyczne nawijanie (fallback dla Trigger/E lub auto-reel)
        /// </summary>
        private void ReelIn()
        {
            if (activeHook == null) return;

            // Użyj nowej metody z automatyczną prędkością
            ReelInByAmount(reelSpeed * Time.deltaTime);

            // Haptic feedback co 10 klatek (tylko dla automatycznego nawijania)
            if (Time.frameCount % 10 == 0)
            {
                SendHapticFeedback(0.1f, 0.05f);
            }
        }

        private void UpdateLineVisuals()
        {
            if (activeHook != null && lineRenderer != null && rodTip != null)
            {
                if (!lineRenderer.enabled)
                {
                    lineRenderer.enabled = true;
                }

                lineRenderer.SetPosition(0, rodTip.position);
                lineRenderer.SetPosition(1, activeHook.transform.position);

                float distance = Vector3.Distance(rodTip.position, activeHook.transform.position);
                float tension = Mathf.Clamp01(distance / maxLineLength);

                float baseWidth = 0.03f;
                float tensionWidth = baseWidth * (1f + tension * 0.5f);

                lineRenderer.startWidth = tensionWidth;
                lineRenderer.endWidth = tensionWidth * 0.8f;

                Gradient gradient = new Gradient();
                if (tension < 0.5f)
                {
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(Color.white, 0.0f),
                            new GradientColorKey(Color.Lerp(Color.white, Color.yellow, tension * 2f), 1.0f)
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(1.0f, 0.0f),
                            new GradientAlphaKey(1.0f, 1.0f)
                        }
                    );
                }
                else
                {
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(Color.yellow, 0.0f),
                            new GradientColorKey(Color.Lerp(Color.yellow, Color.red, (tension - 0.5f) * 2f), 1.0f)
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(1.0f, 0.0f),
                            new GradientAlphaKey(1.0f, 1.0f)
                        }
                    );
                }

                lineRenderer.colorGradient = gradient;
            }
            else if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
        }

        private void UpdateHookPhysics()
        {
            if (activeHook != null && hookRb != null && rodTip != null)
            {
                float distance = Vector3.Distance(rodTip.position, activeHook.transform.position);

                if (distance > maxLineLength)
                {
                    Vector3 direction = (activeHook.transform.position - rodTip.position).normalized;
                    activeHook.transform.position = rodTip.position + direction * maxLineLength;
                    hookRb.linearVelocity = Vector3.zero;
                }
            }
        }

        private void ResetRod()
        {
            Debug.Log("🔄 Rod reset");

            if (activeHook != null)
            {
                Destroy(activeHook);
            }

            lineRenderer.enabled = false;
            currentState = FishingState.Idle;
            currentLineLength = minLineLength;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isHeld = true;
            controllerTransform = args.interactorObject.transform;
            lastPosition = controllerTransform.position;
            grabTime = Time.time;

            Debug.Log($"🎣 Fishing rod grabbed by {controllerTransform.name}");
            SendHapticFeedback(0.3f, 0.1f);
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isHeld = false;
            controllerTransform = null;

            if (currentState != FishingState.Idle)
            {
                ResetRod();
            }
        }

        private void SendHapticFeedback(float amplitude, float duration)
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

            if (activeHook != null)
            {
                Destroy(activeHook);
            }

            if (lineRenderer != null)
            {
                Destroy(lineRenderer.gameObject);
            }
        }

        // DEBUG GUI
        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;

            GUI.backgroundColor = new Color(0, 0, 0, 0.7f);

            GUI.Label(new Rect(10, 10, 400, 30), $"State: {currentState}", style);
            GUI.Label(new Rect(10, 40, 400, 30), $"Velocity: {velocity.magnitude:F2} m/s", style);

            // NOWE: Pokaż długość żyłki
            if (activeHook != null && rodTip != null)
            {
                float distance = Vector3.Distance(rodTip.position, activeHook.transform.position);
                GUI.Label(new Rect(10, 70, 500, 30), $"Line out: {distance:F1}m / {maxLineLength:F0}m", style);

                // Progress bar
                float percent = distance / maxLineLength;
                GUI.Box(new Rect(10, 105, 300, 20), "");
                GUI.Box(new Rect(10, 105, 300 * percent, 20), "");
            }
        }

        /// <summary>
        /// Nawija żyłkę o określoną ilość (wywoływane przez ReelHandle)
        /// </summary>
        public void ReelInByAmount(float amount)
        {
            if (activeHook == null) return;
            if (currentState != FishingState.LineCast && currentState != FishingState.Reeling) return;

            currentState = FishingState.Reeling;

            currentLineLength -= amount;
            currentLineLength = Mathf.Max(currentLineLength, minLineLength);

            Vector3 directionToTip = (rodTip.position - activeHook.transform.position).normalized;
            float distance = Vector3.Distance(rodTip.position, activeHook.transform.position);

            if (distance > currentLineLength)
            {
                hookRb.AddForce(directionToTip * amount * reelForceMultiplier, ForceMode.Impulse);
            }

            // DEBUG - pokaż ile nawinąłeś
            if (amount > 0.01f)
            {
                Debug.Log($"🎣 Reeling: {amount:F3}m | Distance left: {distance:F1}m");
            }

            if (distance < 1f)
            {
                ResetRod();
                Debug.Log("✅ Fish caught! Haczyk nawinięty!");
            }
        }
        private void OnDrawGizmos()
        {
            if (rodTip != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(rodTip.position, 0.1f);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(rodTip.position, rodTip.forward * 2f);
            }
        }
    }
}