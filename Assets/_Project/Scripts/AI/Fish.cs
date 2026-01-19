using UnityEngine;
using System.Collections;
using VRFishing.Fishing;

namespace VRFishing.AI
{
    [RequireComponent(typeof(Rigidbody))]
    public class Fish : MonoBehaviour
    {
        [Header("Fish Stats")]
        public string fishName = "Bass";
        public float fishWeight = 2.5f;

        [Header("Prefabs")]
        public GameObject caughtVisualPrefab;

        [Header("Swimming")]
        [SerializeField] private float swimSpeed = 2f;
        [SerializeField] private float turnSpeed = 45f;
        [SerializeField] private Vector2 swimAreaMin = new Vector2(-5, -5);
        [SerializeField] private Vector2 swimAreaMax = new Vector2(5, 5);
        [SerializeField] private float swimDepthMin = -3f;
        [SerializeField] private float swimDepthMax = -0.5f;

        [Header("Bite Behavior")]
        [SerializeField] private float biteDetectionRadius = 2f;
        [SerializeField] private float biteChance = 0.7f;
        [SerializeField] private float biteDelay = 1f;

        public FishState currentState = FishState.Swimming;

        private Rigidbody rb;
        private Vector3 targetPosition;
        private Transform hookTransform;
        private FishHook fishHook;

        public enum FishState
        {
            Swimming,
            Investigating,
            Biting
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 2f;
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
            }
        }

        private void SwimBehavior()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.AddForce(direction * swimSpeed, ForceMode.Force);

            if (rb.linearVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, targetPosition) < 1f)
            {
                ChooseNewTarget();
            }
        }

        private void ChooseNewTarget()
        {
            targetPosition = transform.position + new Vector3(
                Random.Range(swimAreaMin.x, swimAreaMax.x),
                Random.Range(swimDepthMin, swimDepthMax),
                Random.Range(swimAreaMin.y, swimAreaMax.y)
            );
        }

        private void LookForHook()
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, biteDetectionRadius);

            foreach (var col in nearby)
            {
                if (col.CompareTag("Hook"))
                {
                    FishHook hook = col.GetComponent<FishHook>();
                    if (hook != null && hook.hasFish) continue;

                    hookTransform = col.transform;
                    fishHook = hook;
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

            Vector3 direction = (hookTransform.position - transform.position).normalized;
            rb.AddForce(direction * swimSpeed * 1.5f, ForceMode.Force);

            if (Vector3.Distance(transform.position, hookTransform.position) < 0.3f)
            {
                StartCoroutine(TryBite());
            }
        }

        private IEnumerator TryBite()
        {
            currentState = FishState.Biting;
            yield return new WaitForSeconds(biteDelay);

            if (Random.value < biteChance && fishHook != null && !fishHook.hasFish)
            {
                Debug.Log($"🎣 {fishName} BIT THE HOOK!");
                fishHook.OnFishBite(this);
                // Fish will be destroyed in OnFishBite
            }
            else
            {
                Debug.Log($"🐟 {fishName} ignored the hook");
                currentState = FishState.Swimming;
                hookTransform = null;
                fishHook = null;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, biteDetectionRadius);

            if (Application.isPlaying && currentState == FishState.Swimming)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
}