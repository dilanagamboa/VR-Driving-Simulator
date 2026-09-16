using UnityEngine;

public class WeatherStopSensor : MonoBehaviour
{
    public WeatherStageDetector detector;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null && detector != null)
        {
            detector.OnVehicleEnterStopArea(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null && detector != null)
        {
            detector.OnVehicleExitStopArea();
        }
    }
}