using UnityEngine;

namespace VRFishing.Core
{
    /// <summary>
    /// Singleton zarz¹dzaj¹cy globalnym stanem gry
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public int fishCaught = 0;
        public float sessionTime = 0f;

        [Header("References")]
        public Transform playerTransform;
        public Transform boatTransform;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            sessionTime += Time.deltaTime;
        }

        public void RegisterFishCaught()
        {
            fishCaught++;
            Debug.Log($"Fish caught! Total: {fishCaught}");
        }
    }
}