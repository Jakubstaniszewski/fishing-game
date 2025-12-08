using UnityEngine;


public class BoatBoarding : MonoBehaviour
{
    [Header("References")]
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
            // 1. Przyklej gracza (to już masz i działa)
            playerXROrigin.SetParent(transform);

            // 2. OPCJA NUKLEARNA: Wyłączamy CharacterController
            // To sprawi, że przestaniesz "pychać" łódkę, ale dalej będziesz na niej stał
            var cc = playerXROrigin.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                Debug.Log("Fizyka gracza wyłączona - łódka powinna być stabilna.");
            }
        }
    }
}