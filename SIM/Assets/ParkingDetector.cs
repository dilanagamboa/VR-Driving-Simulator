using UnityEngine;

public class ParkingDetector : MonoBehaviour
{
    [Header("Configuración de Parada")]
    [Tooltip("Velocidad máxima para considerar el carro detenido")]
    public float stopSpeedThreshold = 0.15f;
    [Tooltip("Segundos requeridos completamente quieto para validar")]
    public float requiredStopTime = 2.0f;

    [Header("Indicador Visual")]
    [Tooltip("Arrastra aquí el objeto del letrero flotante para ocultarlo al estacionar")]
    public GameObject parkingSign;

    private bool vehicleInside = false;
    private Rigidbody vehicleRb;
    private float stoppedTimer = 0f;
    private bool parkingCompleted = false;
    private bool parkingFailed = false;
    private string failureReason = "";

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            vehicleInside = true;
            vehicleRb = rb;
            stoppedTimer = 0f;

            // Asegurar que el vehículo tenga el detector de colisiones con conos
            VehicleCollisionDetector colDetector = rb.GetComponent<VehicleCollisionDetector>();
            if (colDetector == null)
            {
                colDetector = rb.gameObject.AddComponent<VehicleCollisionDetector>();
            }
            colDetector.OnConeHit += RegisterFailure;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null && rb == vehicleRb)
        {
            VehicleCollisionDetector colDetector = rb.GetComponent<VehicleCollisionDetector>();
            if (colDetector != null)
            {
                colDetector.OnConeHit -= RegisterFailure;
            }

            vehicleInside = false;
            vehicleRb = null;
            stoppedTimer = 0f;
            parkingCompleted = false;
            parkingFailed = false;

            // Volver a encender el letrero si el vehículo se retira del cajón
            if (parkingSign != null)
            {
                parkingSign.SetActive(true);
            }
        }
    }

    private void RegisterFailure(string reason)
    {
        if (!parkingCompleted)
        {
            parkingFailed = true;
            failureReason = reason;
            Debug.LogWarning("Maniobra reprobada: " + reason);
        }
    }

    private void Update()
    {
        if (!vehicleInside || vehicleRb == null || parkingCompleted || parkingFailed)
            return;

        float currentSpeed = vehicleRb.linearVelocity.magnitude;

        if (currentSpeed <= stopSpeedThreshold)
        {
            stoppedTimer += Time.deltaTime;

            if (stoppedTimer >= requiredStopTime)
            {
                parkingCompleted = true;
                Debug.Log("¡ESTACIONAMIENTO EXITOSO!");

                // Desactivar el letrero flotante al completar la maniobra
                if (parkingSign != null)
                {
                    parkingSign.SetActive(false);
                }
            }
        }
        else
        {
            // Si el carro se sigue moviendo dentro del cajón, reiniciar temporizador
            stoppedTimer = 0f;
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;

        if (parkingFailed)
        {
            style.fontSize = 26;
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 250, 60, 500, 50), "✗ PARQUEO ERRÓNEO: ¡Golpeaste un cono!", style);
        }
        else if (parkingCompleted)
        {
            style.fontSize = 26;
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(Screen.width / 2 - 200, 60, 400, 50), "✓ ¡ESTACIONAMIENTO EXITOSO!", style);
        }
        else if (vehicleInside)
        {
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            float progreso = Mathf.Clamp01(stoppedTimer / requiredStopTime) * 100f;
            GUI.Label(new Rect(Screen.width / 2 - 200, 60, 400, 50), $"Frenando en cajón: {progreso:F0}%", style);
        }
    }
}