using UnityEngine;
using TMPro;

namespace VRFishing.Fishing
{
    public class RodDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FishingRod fishingRod;

        [Header("Text Elements")]
        [SerializeField] private TextMeshPro stateText;
        [SerializeField] private TextMeshPro lineText;
        [SerializeField] private TextMeshPro scoreText;
        [SerializeField] private TextMeshPro escapeText;

        private void Update()
        {
            if (fishingRod == null) return;

            if (stateText != null)
            {
                stateText.text = $"State: {fishingRod.currentState}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {fishingRod.score:F1}";
            }

            if (lineText != null)
            {
                if (fishingRod.HasActiveHook())
                {
                    float dist = fishingRod.GetLineDistance();
                    float max = fishingRod.GetMaxLineLength();
                    lineText.text = $"Line: {dist:F1}m / {max:F0}m";
                    lineText.gameObject.SetActive(true);
                }
                else
                {
                    lineText.gameObject.SetActive(false);
                }
            }

            if (escapeText != null)
            {
                FishHook hook = fishingRod.GetFishHook();

                if (hook != null && hook.hasFish)
                {
                    float escape = hook.escapeChance * 100f;
                    escapeText.text = $"Escape: {escape:F0}%";
                    escapeText.color = Color.Lerp(Color.green, Color.red, hook.escapeChance);
                    escapeText.gameObject.SetActive(true);
                }
                else
                {
                    escapeText.gameObject.SetActive(false);
                }
            }
        }
    }
}