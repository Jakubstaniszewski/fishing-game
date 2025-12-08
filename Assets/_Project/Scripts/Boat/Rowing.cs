using UnityEngine;

public class Rowing : MonoBehaviour
{
    [Header("Przypisz te pola")]
    public Rigidbody lodkaRB;      // Rigidbody ��dki (BoatRoot)
    public Transform koniecWiosla; // Obiekt na ko�cu p�etwy wios�a

    [Header("Ustawienia")]
    public float silaNapedu = 400f; // Moc
    public float poziomWody = 1.0f; // TWOJA WODA JEST NA Y=1

    private Rigidbody wiosloRB;

    void Start()
    {
        wiosloRB = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (koniecWiosla.position.y < poziomWody)
        {
            Vector3 predkosc = wiosloRB.GetPointVelocity(koniecWiosla.position);

            // --- NOWOŚĆ: Blokada Wsteczna ---
            // Zamieniamy prędkość świata na prędkość lokalną względem łódki
            Vector3 lokalnaPredkosc = lodkaRB.transform.InverseTransformDirection(predkosc);

            // Jeśli wiosło porusza się w stronę dziobu (Z > 0), to znaczy że robimy zamach.
            // Wtedy NIE chcemy pchać łódki do tyłu. Przerywamy funkcję.
            if (lokalnaPredkosc.z > 0) return;
            // --------------------------------

            // Reszta bez zmian...
            Vector3 sila = -predkosc * silaNapedu;
            sila.y = 0;

            if (sila.magnitude > 0.1f)
            {
                lodkaRB.AddForceAtPosition(sila * Time.fixedDeltaTime, transform.position, ForceMode.Impulse);
            }
        }
    }
}
