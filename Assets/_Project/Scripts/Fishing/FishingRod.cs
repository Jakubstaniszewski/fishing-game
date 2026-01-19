using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFishing.Fishing
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class FishingRod : MonoBehaviour
    {
        [Header("Rod Components")]
        [SerializeField] private Transform rodTip;

        [Header("Hook")]
        private GameObject activeHook;
        private Rigidbody hookRb;
        private FishHook fishHook;
        [SerializeField] private List<ParticleSystem> catchParticles;
        [SerializeField] private AudioClip catchSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Line Settings")]
        [SerializeField] private float maxLineLength = 30f;
        [SerializeField] private float minLineLength = 1f;
        [SerializeField] private float reelSpeed = 3f;
        [SerializeField] private float reelForce = 50f;

        [Header("Cast Settings")]
        [SerializeField] private float castForceMultiplier = 20f;
        [SerializeField] private float castDetectionThreshold = 0.8f;
        [SerializeField] private float minCastForce = 8f;

        [Header("Input")]
        [SerializeField] private InputActionReference reelInput;

        [Header("Debug")]
        [SerializeField] private bool showDebugGUI = true;
        public bool reel=false;

        public bool isHeld = false;
        public FishingState currentState = FishingState.Idle;

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private LineRenderer lineRenderer;
        public float score = 0.0f;
        private Vector3 lastPosition;
        private Vector3 velocity;
        private Transform controllerTransform;

        private float grabTime = 0f;
        private const float CAST_COOLDOWN = 0.5f;

        public enum FishingState
        {
            Idle,
            Casting,
            LineCast,
            FishHooked
        }

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            SetupLineRenderer();

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnEnable()
        {
            if (reelInput != null && reelInput.action != null)
            {
                reelInput.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (reelInput != null && reelInput.action != null)
            {
                reelInput.action.Disable();
            }
        }

        public void AddPoints(float points)
        {
            score += points;
            Debug.Log($"Added {points} points. Total score: {score}");
        }


        private void SetupLineRenderer()
        {
            GameObject lineObject = new GameObject("FishingLine");
            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.015f;

            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = Color.white;
            lineRenderer.material = lineMat;

            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (isHeld && controllerTransform != null)
            {
                velocity = (controllerTransform.position - lastPosition) / Time.deltaTime;
                lastPosition = controllerTransform.position;

                DetectCastGesture();
                HandleReelInput();
            }

            // Update state based on fish
            if (fishHook != null && fishHook.hasFish && currentState == FishingState.LineCast)
            {
                currentState = FishingState.FishHooked;
            }
            else if (fishHook != null && !fishHook.hasFish && currentState == FishingState.FishHooked)
            {
                currentState = FishingState.LineCast;
            }

            UpdateLine();
            ClampHookDistance();
        }

        private void HandleReelInput()
        {
            if (currentState != FishingState.LineCast && currentState != FishingState.FishHooked) return;
            if (activeHook == null || fishHook == null) return;

            float inputValue = 0f;

            if (reelInput != null && reelInput.action != null)
            {
                inputValue = reelInput.action.ReadValue<float>();
            }

            if (Input.GetKey(KeyCode.E))
            {
                inputValue = 1f;
            }

            if (inputValue > 0.1f)
            {
                ReelInByAmount(reelSpeed * inputValue * Time.deltaTime);

                
            }
            else if(reel)
            {
                ReelInByAmount(reelSpeed * Time.deltaTime);
            }
            else
            {
                // Not reeling - let hook rest if in water
                fishHook.StopReeling();
            }
        }

        private void DetectCastGesture()
        {
            if (currentState != FishingState.Idle) return;
            if (rodTip == null) return;
            if (Time.time - grabTime < CAST_COOLDOWN) return;

            float speed = velocity.magnitude;

            if (Camera.main == null) return;

            Vector3 playerForward = Camera.main.transform.forward;
            playerForward.y = 0;
            playerForward.Normalize();

            float forwardSpeed = Vector3.Dot(velocity, playerForward);

            if (speed > castDetectionThreshold && forwardSpeed > castDetectionThreshold * 0.5f)
            {
                Cast();
            }
        }
       
        private void Cast()
        {

            currentState = FishingState.Casting;

            if (activeHook != null)
            {
                Destroy(activeHook);
            }

            activeHook = new GameObject("Hook");
            activeHook.transform.position = rodTip.position;
            activeHook.tag = "Hook";

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(activeHook.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.08f;
            sphere.GetComponent<Renderer>().material.color = Color.yellow;

            hookRb = activeHook.AddComponent<Rigidbody>();
            hookRb.mass = 0.03f;
            hookRb.linearDamping = 0.3f;

            var col = activeHook.AddComponent<SphereCollider>();
            col.radius = 0.04f;
            col.isTrigger = true;

            fishHook = activeHook.AddComponent<FishHook>();
            fishHook.SetRod(this);
            fishHook.catchSound = catchSound; 
            fishHook.catchParticles = catchParticles;
            fishHook.audioSource=audioSource;

            Vector3 castDir = rodTip.forward;
            float force = Mathf.Max(minCastForce, velocity.magnitude) * castForceMultiplier;
            hookRb.AddForce(castDir * force, ForceMode.Impulse);

            lineRenderer.enabled = true;

            SendHapticFeedback(0.5f, 0.2f);

            StartCoroutine(FinishCast());
        }

        private IEnumerator FinishCast()
        {
            yield return new WaitForSeconds(0.3f);
            currentState = FishingState.LineCast;
        }

        public void ReelInByAmount(float amount)
        {
            if (activeHook == null || fishHook == null) return;
            if (currentState != FishingState.LineCast && currentState != FishingState.FishHooked) return;

            Vector3 toRod = (rodTip.position - activeHook.transform.position).normalized;
            float pullForce = amount * reelForce;

            fishHook.ApplyReelForce(toRod, pullForce);

            float dist = Vector3.Distance(rodTip.position, activeHook.transform.position);
            if (Time.frameCount % 5 == 0)
            {
                SendHapticFeedback(0.15f, 0.05f);
            }
            if (dist < minLineLength)
            {
                if (fishHook.hasFish)
                {
                    fishHook.OnCaught();
                }

                ResetRod();
            }
        }

        private void UpdateLine()
        {
            if (activeHook != null && lineRenderer != null && rodTip != null)
            {
                lineRenderer.SetPosition(0, rodTip.position);
                lineRenderer.SetPosition(1, activeHook.transform.position);

                float dist = Vector3.Distance(rodTip.position, activeHook.transform.position);
                float tension = Mathf.Clamp01(dist / maxLineLength);

                Color lineColor = Color.Lerp(Color.white, Color.red, tension);

                if (fishHook != null && fishHook.hasFish)
                {
                    lineColor = Color.Lerp(Color.yellow, Color.red, tension);
                }

                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
            }
        }

        private void ClampHookDistance()
        {
            if (activeHook == null || hookRb == null || rodTip == null) return;

            float dist = Vector3.Distance(rodTip.position, activeHook.transform.position);

            if (dist > maxLineLength)
            {
                Vector3 dir = (rodTip.position - activeHook.transform.position).normalized;
                activeHook.transform.position = rodTip.position - dir * maxLineLength;

                float outwardSpeed = Vector3.Dot(hookRb.linearVelocity, -dir);
                if (outwardSpeed > 0)
                {
                    hookRb.linearVelocity -= (-dir) * outwardSpeed;
                }
            }
        }

        private void ResetRod()
        {
            if (activeHook != null)
            {
                Destroy(activeHook);
                activeHook = null;
                hookRb = null;
                fishHook = null;
            }

            lineRenderer.enabled = false;
            currentState = FishingState.Idle;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isHeld = true;
            controllerTransform = args.interactorObject.transform;
            lastPosition = controllerTransform.position;
            grabTime = Time.time;

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

        public void SendHapticFeedback(float amplitude, float duration)
        {
            if (grabInteractable.isSelected)
            {
                var interactor = grabInteractable.firstInteractorSelecting;
                if (interactor is XRBaseInputInteractor inputInteractor)
                {
                    inputInteractor.SendHapticImpulse(amplitude, duration);
                }
            }
        }
        public FishHook GetFishHook()
        {
            return fishHook;
        }
        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }

            if (activeHook != null) Destroy(activeHook);
            if (lineRenderer != null) Destroy(lineRenderer.gameObject);
        }

        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(10, 10, 400, 30), $"Score: {score:F1}", style);
            GUI.Label(new Rect(10, 40, 400, 30), $"State: {currentState}", style);
            GUI.Label(new Rect(10, 70, 400, 30), $"Velocity: {velocity.magnitude:F2} m/s", style);

            if (activeHook != null && rodTip != null)
            {
                float dist = Vector3.Distance(rodTip.position, activeHook.transform.position);
                GUI.Label(new Rect(10, 100, 400, 30), $"Line: {dist:F1}m / {maxLineLength}m", style);

                if (fishHook != null)
                {
                    GUI.Label(new Rect(10, 130, 400, 30), $"In water: {fishHook.inWater} | Fish: {fishHook.hasFish}", style);

                    if (fishHook.hasFish)
                    {
                        GUI.Label(new Rect(10, 160, 400, 30), $"{fishHook.caughtFishName} - {fishHook.caughtFishWeight}kg", style);

                        float escape = fishHook.escapeChance * 100f;
                        style.normal.textColor = Color.Lerp(Color.green, Color.red, fishHook.escapeChance);
                        GUI.Label(new Rect(10, 190, 400, 30), $"Escape Risk: {escape:F0}%", style);
                        style.normal.textColor = Color.white;
                    }
                }
            }
        }


        public bool HasActiveHook()
        {
            return activeHook != null;
        }

        public float GetLineDistance()
        {
            if (activeHook != null && rodTip != null)
            {
                return Vector3.Distance(rodTip.position, activeHook.transform.position);
            }
            return 0f;
        }

        public float GetMaxLineLength()
        {
            return maxLineLength;
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