using UnityEngine;

public class FishingZone : MonoBehaviour
{
    public string zoneName = "Zone 1";
    public Color highlightColor = Color.cyan;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}
