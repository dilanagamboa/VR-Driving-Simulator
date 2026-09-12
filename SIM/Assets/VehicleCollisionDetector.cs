using System;
using UnityEngine;

public class VehicleCollisionDetector : MonoBehaviour
{
    public event Action<string> OnConeHit;

    private void OnCollisionEnter(Collision collision)
    {
        // Comprueba si el objeto golpeado o su padre tiene el Tag de cono
        if (collision.gameObject.CompareTag("TrafficCone") ||
            (collision.transform.parent != null && collision.transform.parent.CompareTag("TrafficCone")))
        {
            OnConeHit?.Invoke("Colisión con cono de tránsito");
        }
    }
}