using UnityEngine;


namespace VRFishing.Data
{

    [CreateAssetMenu(fileName = "FishingRodConfig", menuName = "Scriptable Objects/FishingRodConfig")]
    public class FishingRodConfig : ScriptableObject
    {
        [Header("Rod Properties")]
        [Tooltip("D�ugo�� w�dki w metrach")]
        [SerializeField] private float rodLength = 2.5f;

        [Tooltip("Maksymalna d�ugo�� �y�ki w metrach")]
        [SerializeField] private float maxLineLength = 20f;

        [Header("Line Strength")]
        [Tooltip("Wytrzyma�o�� �y�ki (0-100). Przy przekroczeniu �y�ka si� zrywa")]
        [SerializeField] private float lineTensionMax = 100f;

        [Tooltip("Jak szybko napi�cie �y�ki ro�nie podczas walki z ryb�")]
        [SerializeField] private float tensionIncreaseRate = 15f;

        [Header("Cast Properties")]
        [Tooltip("Minimalna si�a zarzutu (przy powolnym ge�cie)")]
        [SerializeField] private float minCastForce = 5f;

        [Tooltip("Maksymalna si�a zarzutu (przy szybkim ge�cie)")]
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