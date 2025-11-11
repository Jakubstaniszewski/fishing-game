using UnityEngine;
using System.Collections;
using VRFishing.Fishing;

namespace VRFishing.AI
{
    /// <summary>
    /// Podstawowe AI ryby - pływanie, branie haczyka, walka
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Fish : MonoBehaviour
    {
        [Header("Fish Stats")]
        [SerializeField] public string fishName = "Bass";
        [SerializeField] public float fishWeight = 2.5f;
        [SerializeField] public float fightStrength = 1f;

        [Header("Swimming")]
        [SerializeField] private float swimSpeed = 2f;
        [SerializeField] private float turnSpeed = 45f;
        [SerializeField] private Vector2 swimAreaMin = new Vector2(-20, -20);
        [SerializeField] private Vector2 swimAreaMax = new Vector2(20, 20);
        [SerializeField] private float swimDepthMin = -3f;
        [SerializeField] private float swimDepthMax = -0.5f;

        [Header("Bite Behavior")]
        [SerializeField] private float biteDetectionRadius = 2f;
        [SerializeField] private float biteChance = 0.7f; // 70% szansy na ugryzienie
        [SerializeField] private float biteDelay = 1f; // Czeka przed ugryzieniem

        [Header("Fight Behavior")]
        [SerializeField] private float pullForce = 5f; // Siła szarpania
        [SerializeField] private float tireRate = 0.1f; // Jak szybko się męczy

        [Header("State")]
        public FishState currentState = FishState.Swimming;
        private float energy = 100f; // 0-100, spada podczas walki
    
        private Rigidbody rb;
        private Vector3 targetPosition;
        private Transform hookTransform;
        private bool isHooked = false;

        public enum FishState
        {
            Swimming,      // Swobodne pływanie
            Investigating, // Podpływa do haczyka
            Biting,        // Gryzie haczyk
            Hooked,        // Złapana - walka!
            Caught         // Wyciągnięta z wody
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false; // Ryby nie spadają :)
            rb.linearDamping = 2f; // Opór wody

            ChooseNewTarget();
        }

        private void Update()
        {
            switch (currentState)
            {
                case FishState.Swimming:
                    SwimBehavior();
                    LookForHook();
                    break;

                case FishState.Investigating:
                    InvestigateBehavior();
                    break;

                case FishState.Hooked:
                    FightBehavior();
                    break;
            }
        }

        private void SwimBehavior()
        {
            // Płyń do celu
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.AddForce(direction * swimSpeed, ForceMode.Force);

            // Obracaj się w kierunku ruchu
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }

            // Wybierz nowy cel gdy jesteś blisko
            if (Vector3.Distance(transform.position, targetPosition) < 1f)
            {
                ChooseNewTarget();
            }
        }

        private void ChooseNewTarget()
        {
            // Losowa pozycja w obszarze pływania
            targetPosition = new Vector3(
                Random.Range(swimAreaMin.x, swimAreaMax.x),
                Random.Range(swimDepthMin, swimDepthMax),
                Random.Range(swimAreaMin.y, swimAreaMax.y)
            );
        }

        private void LookForHook()
        {
            // Szukaj haczyka w pobliżu
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, biteDetectionRadius);

            foreach (var col in nearbyObjects)
            {
                if (col.CompareTag("Hook")) // Dodamy tag za chwilę
                {
                    hookTransform = col.transform;
                    currentState = FishState.Investigating;
                    Debug.Log($"🐟 {fishName} spotted the hook!");
                    break;
                }
            }
        }

        private void InvestigateBehavior()
        {
            if (hookTransform == null)
            {
                currentState = FishState.Swimming;
                return;
            }

            // Podpływaj do haczyka
            Vector3 direction = (hookTransform.position - transform.position).normalized;
            rb.AddForce(direction * swimSpeed * 1.5f, ForceMode.Force);

            // Jeśli bardzo blisko - spróbuj ugryźć
            float distance = Vector3.Distance(transform.position, hookTransform.position);
            if (distance < 0.3f)
            {
                StartCoroutine(TryBite());
            }
        }

        private IEnumerator TryBite()
        {
            currentState = FishState.Biting;

            yield return new WaitForSeconds(biteDelay);

            // Losowa szansa na ugryzienie
            if (Random.value < biteChance)
            {
                Debug.Log($"🎣 {fishName} BIT THE HOOK!");
                BiteHook();
            }
            else
            {
                Debug.Log($"🐟 {fishName} ignored the hook");
                currentState = FishState.Swimming;
                hookTransform = null;
            }
        }

        private void BiteHook()
        {
            isHooked = true;
            currentState = FishState.Hooked;

            // Powiadom haczyk że został złapany
            var hook = hookTransform.GetComponent<FishHook>();
            if (hook != null)
            {
                hook.OnFishBite(this);
            }
        }

        private void FightBehavior()
        {
            if (hookTransform == null)
            {
                isHooked = false;
                currentState = FishState.Swimming;
                return;
            }

            // Szarp w losowych kierunkach!
            if (Random.value < 0.02f) // 2% szansy co klatkę = ~raz na sekundę
            {
                Vector3 pullDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-1f, 1f)
                ).normalized;

                rb.AddForce(pullDirection * pullForce * fightStrength, ForceMode.Impulse);
                Debug.Log($"🐟 {fishName} PULLS!");
            }

            // Męczenie się
            energy -= tireRate * Time.deltaTime;
            energy = Mathf.Max(energy, 0f);

            // Jeśli zmęczona - słabsza walka
            if (energy < 50f)
            {
                fightStrength = Mathf.Lerp(fightStrength, 0.3f, Time.deltaTime);
            }

            // Jeśli wyciągnięta z wody - złapana!
            if (transform.position.y > 0.5f)
            {
                OnCaught();
            }
        }

        public void OnCaught()
        {
            currentState = FishState.Caught;
            Debug.Log($"🎉 {fishName} CAUGHT! Weight: {fishWeight}kg");

            // Wyłącz fizykę
            rb.isKinematic = true;

            // Powiadom GameManager
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.RegisterFishCaught();
            }

            // Destroy za 3 sekundy
            Destroy(gameObject, 3f);
        }

        private void OnDrawGizmos()
        {
            // Pokaż obszar detekcji haczyka
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, biteDetectionRadius);

            // Pokaż cel pływania
            if (Application.isPlaying && currentState == FishState.Swimming)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
            }

            // Pokaż połączenie z haczykiem
            if (isHooked && hookTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, hookTransform.position);
            }
        }
    }
}