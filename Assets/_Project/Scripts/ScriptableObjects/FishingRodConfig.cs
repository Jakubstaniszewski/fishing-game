using UnityEngine;


namespace VRFishing.Data
{

    [CreateAssetMenu(fileName = "FishingRodConfig", menuName = "Scriptable Objects/FishingRodConfig")]
    public class FishingRodConfig : ScriptableObject
    {
        [Header("Rod Properties")]
        [Tooltip("D³ugoœæ wêdki w metrach")]
        [SerializeField] private float rodLength = 2.5f;

        [Tooltip("Maksymalna d³ugoœæ ¿y³ki w metrach")]
        [SerializeField] private float maxLineLength = 20f;

        [Header("Line Strength")]
        [Tooltip("Wytrzyma³oœæ ¿y³ki (0-100). Przy przekroczeniu ¿y³ka siê zrywa")]
        [SerializeField] private float lineTensionMax = 100f;

        [Tooltip("Jak szybko napiêcie ¿y³ki roœnie podczas walki z ryb¹")]
        [SerializeField] private float tensionIncreaseRate = 15f;

        [Header("Cast Properties")]
        [Tooltip("Minimalna si³a zarzutu (przy powolnym geœcie)")]
        [SerializeField] private float minCastForce = 5f;

        [Tooltip("Maksymalna si³a zarzutu (przy szybkim geœcie)")]
        [SerializeField] private float maxCastForce = 25f;

        [Tooltip("Minimalny dystans zarzutu w metrach")]
        [SerializeField] private float minCastDistance = 3f;

        [Tooltip("Maksymalny dystans zarzutu w metrach")]
        [SerializeField] private float maxCastDistance = 30f;

        
        public float RodLength => rodLength;
        public float MaxLineLength => maxLineLength;
        public float LineTensionMax => lineTensionMax;
        public float TensionIncreaseRate => tensionIncreaseRate;
        public float MinCastForce => minCastForce;
        public float MaxCastForce => maxCastForce;
        public float MinCastDistance => minCastDistance;
        public float MaxCastDistance => maxCastDistance;
    }
}