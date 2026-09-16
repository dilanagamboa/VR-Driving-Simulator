using UnityEngine;

public class ZoneSensor : MonoBehaviour
{
    public enum ZoneType { StopLine, IntersectionCrossing }
    public ZoneType zoneType;
    public IntersectionDetector detector;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        if (zoneType == ZoneType.StopLine)
        {
            detector.OnVehicleEnterStopZone(rb);
        }
        else if (zoneType == ZoneType.IntersectionCrossing)
        {
            detector.OnVehicleEnterIntersection();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        if (zoneType == ZoneType.StopLine)
        {
            detector.OnVehicleExitStopZone();
        }
    }
}