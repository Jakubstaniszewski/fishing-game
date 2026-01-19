using System.Collections.Generic;
using UnityEngine;

namespace VRFishing.Fishing
{
    public class FishHook : MonoBehaviour
    {
        [Header("State")]
        public bool hasFish = false;
        public bool inWater = false;
        public List<ParticleSystem> catchParticles;
        public AudioClip catchSound;
        public AudioSource audioSource;

        [Header("Caught Fish")]
        public string caughtFishName;
        public float caughtFishWeight;
        public float escapeChance = 0f;

        private GameObject caughtFishVisual;
        private Rigidbody rb;
        private FishingRod fishingRod;
        private bool isReeling = false;
        private float waterSurfaceY;
        private float reelTime = 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void SetRod(FishingRod rod)
        {
            fishingRod = rod;
        }

        private void FixedUpdate()
        {
            if (isReeling && inWater)
            {
                Vector3 pos = transform.position;
                if (pos.y < waterSurfaceY)
                {
                    pos.y = waterSurfaceY;
                    transform.position = pos;

                    if (rb.linearVelocity.y < 0)
                    {
                        Vector3 vel = rb.linearVelocity;
                        vel.y = 0;
                        rb.linearVelocity = vel;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("hookStopper"))
            {
                inWater = true;
                waterSurfaceY = transform.position.y;

                if (!isReeling)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false;
                    rb.isKinematic = true;
                    Debug.Log("Hook in water");
                }
                return;
            }

            var fish = other.GetComponent<AI.Fish>();
            if (fish != null && !hasFish)
            {
                Debug.Log($"Hook collided with {fish.name}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("hookStopper"))
            {
                inWater = false;
                rb.useGravity = true;
                Debug.Log("Hook left water");
            }
        }

        public void OnFishBite(AI.Fish fish)
        {
            hasFish = true;
            caughtFishName = fish.fishName;
            caughtFishWeight = fish.fishWeight;

            reelTime = 0f;
            escapeChance = 0f;

            if (fish.caughtVisualPrefab != null)
            {
                caughtFishVisual = Instantiate(fish.caughtVisualPrefab, transform);
                caughtFishVisual.transform.localPosition = Vector3.zero;
                caughtFishVisual.transform.localRotation = Quaternion.identity;
            }

            if (fishingRod != null)
            {
                fishingRod.SendHapticFeedback(0.8f, 0.5f);
            }

            Destroy(fish.gameObject);

            Debug.Log($"Fish hooked: {caughtFishName}");
        }

        public void ReleaseFish(bool escaped)
        {
            if (escaped)
            {
                Debug.Log($"{caughtFishName} escaped!");

                if (fishingRod != null)
                {
                    fishingRod.SendHapticFeedback(0.3f, 0.2f);
                }
            }

            if (caughtFishVisual != null)
            {
                Destroy(caughtFishVisual);
                caughtFishVisual = null;
            }

            hasFish = false;
            caughtFishName = "";
            caughtFishWeight = 0f;
            reelTime = 0f;
            escapeChance = 0f;
        }

        public void ApplyReelForce(Vector3 direction, float force)
        {
            isReeling = true;

            if (rb.isKinematic)
            {
                rb.isKinematic = false;
            }

            rb.useGravity = !inWater;

            Vector3 currentVel = rb.linearVelocity;
            Vector3 targetVel = direction * force * 0.1f;

            if (inWater)
            {
                targetVel.y = Mathf.Max(targetVel.y, 0);
            }

            rb.linearVelocity = Vector3.Lerp(currentVel, targetVel, 0.3f);
            rb.AddForce(direction * force, ForceMode.Force);

            if (hasFish)
            {
                reelTime += Time.deltaTime;
                escapeChance = Mathf.Clamp01(reelTime * 0.1f);

                if (Random.value < escapeChance * Time.deltaTime)
                {
                    ReleaseFish(true);
                }
            }
        }

        public void StopReeling()
        {
            isReeling = false;

            if (hasFish)
            {
                reelTime = Mathf.Max(0f, reelTime - Time.deltaTime * 0.66f);
                escapeChance = Mathf.Clamp01(reelTime * 0.1f);
            }

            if (inWater && !hasFish)
            {
                rb.linearVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }

        public void OnCaught()
        {
            if (!hasFish) return;

            Debug.Log($"{caughtFishName} CAUGHT! Weight: {caughtFishWeight}kg");

            if (catchParticles != null)
            {
                foreach (var particle in catchParticles)
                {
                    if (particle != null)
                    {
                        particle.Play();
                    }
                }
            }

            fishingRod.AddPoints(caughtFishWeight);

            if (catchSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(catchSound);
            }

            if (fishingRod != null)
            {
                fishingRod.SendHapticFeedback(1f, 0.8f);
            }

            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.RegisterFishCaught();
            }

            ReleaseFish(false);
        }
    }
}