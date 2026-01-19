using UnityEngine;

public class Rowing : MonoBehaviour
{
    [Header("Rigidbodies")]
    public Rigidbody boatRB;
    public Transform oarTip;

    [Header("Settings")]
    public float propulsionForce = 400f;
    public float waterLevel = 1.0f;

    private Rigidbody oarRB;

    void Start()
    {
        oarRB = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (oarTip.position.y < waterLevel)
        {
            Vector3 velocity = oarRB.GetPointVelocity(oarTip.position);
            Vector3 localVelocity = boatRB.transform.InverseTransformDirection(velocity);

            if (localVelocity.z > 0f)
                return;

            Vector3 force = -velocity * propulsionForce;
            force.y = 0f;

            if (force.magnitude > 0.1f)
            {
                boatRB.AddForceAtPosition(
                    force * Time.fixedDeltaTime,
                    transform.position,
                    ForceMode.Impulse
                );
            }
        }
    }
}
