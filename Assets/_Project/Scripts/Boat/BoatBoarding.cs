using UnityEngine;


public class BoatBoarding : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea teleportArea;
    public Transform playerXROrigin;

    public void OnEnable()
    {
        if (teleportArea != null)
            teleportArea.teleporting.AddListener(OnTeleportToBoat);
    }

    public void OnDisable()
    {
        if (teleportArea != null)
            teleportArea.teleporting.RemoveListener(OnTeleportToBoat);
    }

    public void OnTeleportToBoat(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportingEventArgs args)
    {
        if (playerXROrigin != null)
        {
            playerXROrigin.SetParent(transform);

          
            var cc = playerXROrigin.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
        }
    }
}