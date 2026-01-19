using UnityEngine;

namespace VRFishing.Fishing
{

    public class BoatFishingStatus : MonoBehaviour
    {

        public string currentZone = "";


        private void OnTriggerEnter(Collider other)
        {
            FishingZone zone = other.GetComponent<FishingZone>();
            if (zone != null)
            {
                currentZone = zone.zoneName;
                other.GetComponent<MeshRenderer>().enabled = false; 

            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<FishingZone>() != null)
            {
                currentZone = "";
                other.GetComponent<MeshRenderer>().enabled = true;
            }
        }
    }
}
