// Assets/Scripts/Gameplay/Fishing/HookPhysics.cs
// AKTUALIZACJA - dodanie trybu kinematic
using UnityEngine;

namespace VRFishing.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class HookPhysics : MonoBehaviour
    {
        [Header("Physics Properties")]
        [SerializeField] private float mass = 0.05f;
        [SerializeField] private float drag = 0.5f;
        [SerializeField] private float waterDrag = 2f;

        [Header("Water Detection")]
        [SerializeField] private float waterSurfaceY = 0f;

        private Rigidbody rb;
        private bool isInWater = false;
        private bool isLaunched = false; // Nowe

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ConfigureRigidbody();

            // Na starcie kinematic (bez fizyki)
            SetKinematic(true);
        }

        private void ConfigureRigidbody()
        {
            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = 0.5f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void FixedUpdate()
        {
            if (isLaunched)
            {
                CheckWaterSubmersion();
            }
        }

        private void CheckWaterSubmersion()
        {
            bool wasInWater = isInWater;
            isInWater = transform.position.y < waterSurfaceY;

            if (isInWater != wasInWater)
            {
                rb.linearDamping = isInWater ? waterDrag : drag;

                if (isInWater)
                {
                    Debug.Log("[HookPhysics] Haczyk wszedł do wody!");
                }
            }
        }

        public void LaunchHook(Vector3 direction, float force)
        {
            // Aktywuj fizykę
            SetKinematic(false);
            isLaunched = true;

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            Debug.Log($"[HookPhysics] Zarzut! Kierunek: {direction}, Siła: {force}");
        }

        public void ResetPosition(Vector3 position)
        {
            // Wyłącz fizykę i zresetuj pozycję
            SetKinematic(true);
            isLaunched = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = position;
        }

        private void SetKinematic(bool kinematic)
        {
            rb.isKinematic = kinematic;
            rb.useGravity = !kinematic;
        }

        public bool IsInWater => isInWater;
        public bool IsLaunched => isLaunched;
        public Vector3 Velocity => rb.linearVelocity;
    }
}