using UnityEngine;


namespace VRFishing.Fishing
{
    /// <summary>
    /// Haczyk - wykrywa kolizje z rybami
    /// </summary>
    public class FishHook : MonoBehaviour
    {
        [Header("State")]
        public bool hasFish = false;
        public AI.Fish caughtFish = null;

        private void OnTriggerEnter(Collider other)
        {
            // Sprawdź czy to ryba
            var fish = other.GetComponent<AI.Fish>();

            if (fish != null && !hasFish)
            {
                Debug.Log($"🎣 Hook collided with {fish.name}");
                // Ryba sama zdecyduje czy ugryzie (w swoim AI)
            }
        }

        public void OnFishBite(AI.Fish fish)
        {
            hasFish = true;
            caughtFish = fish;

            Debug.Log($"🎉 Fish hooked: {fish.fishName}");

            // Powiadom wędkę o złapaniu
            // (rozszerzymy później)
        }

        private void Update()
        {
            // Jeśli mamy rybę, trzymaj ją przy haczyk (opcjonalnie)
            if (hasFish && caughtFish != null)
            {
                // Ryba porusza się sama (walczy), ale jest "przywiązana"
            }
        }
    }
}