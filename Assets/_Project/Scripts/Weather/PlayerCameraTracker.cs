using UnityEngine;

public class PlayerCameraTracker : MonoBehaviour
{
    public Transform playerCamera;
    public float yoffset = 5f; 

    void Update()
    {
        if(playerCamera != null) 
        {
            Vector3 newPos = playerCamera.position; 
            newPos.y += yoffset;

            transform.position = newPos;
        }
    }
}
