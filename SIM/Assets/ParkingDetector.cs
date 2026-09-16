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
        // Si ya completamos la prueba con éxito, ignorar salidas para no reiniciar las variables
        if (parkingCompleted) return;

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

            if (parkingSign != null)
            {
                parkingSign.SetActive(true);
            }
        }
    }

    private void RegisterFailure(string reason)
    {
        if (!parkingCompleted && !parkingFailed)
        {
            parkingFailed = true;
            failureReason = reason;
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnTestFailed("¡Has chocado un cono! Maniobra fallida.");
            }
        }
    }

    private void Update()
    {
        // Solo evaluar si estamos en la fase de parqueo
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.Parking)
            return;

        // Si ya completó o falló, frenar cualquier cálculo
        if (!vehicleInside || vehicleRb == null || parkingCompleted || parkingFailed)
            return;

        float currentSpeed = vehicleRb.linearVelocity.magnitude;

        if (currentSpeed <= stopSpeedThreshold)
        {
            stoppedTimer += Time.deltaTime;

            if (stoppedTimer >= requiredStopTime)
            {
                stoppedTimer = requiredStopTime; // Clavar en el tiempo máximo exacto
                parkingCompleted = true;

                if (parkingSign != null)
                    parkingSign.SetActive(false);

                if (GameFlowManager.Instance != null)
                {
                    GameFlowManager.Instance.OnTestCompleted("¡Estacionamiento Correcto!");
                }
            }
        }
        else
        {
            stoppedTimer = 0f;
        }
    }

    public void ResetState()
    {
        vehicleInside = false;
        vehicleRb = null;
        stoppedTimer = 0f;
        parkingCompleted = false;
        parkingFailed = false;
        failureReason = "";

        if (parkingSign != null)
        {
            parkingSign.SetActive(true);
        }
    }

    private void OnGUI()
    {
        // Solo mostrar cuando estemos jugando la Prueba 1
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.Parking)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 17;
        style.fontStyle = FontStyle.Bold;

        // Misma caja de checklist que la Prueba 2 (mismo tamaño, posición y estilo)
        GUI.Box(new Rect(20, 20, 320, 165), "Prueba 1: Maniobra de Estacionamiento");

        // 1. Estado de posición dentro del cajón
        if (vehicleInside)
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(30, 50, 300, 25), "✓ Posición: Dentro del cajón", style);
        }
        else
        {
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(30, 50, 300, 25), "○ Posición: Entra al cajón de conos", style);
        }

        // 2. Progreso de frenado total
        if (parkingCompleted)
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(30, 80, 300, 25), "✓ Detención completa: 100%", style);
        }
        else if (vehicleInside)
        {
            style.normal.textColor = Color.yellow;
            float progreso = Mathf.Clamp01(stoppedTimer / requiredStopTime) * 100f;
            GUI.Label(new Rect(30, 80, 300, 25), $"Detención obligatoria: {progreso:F0}%", style);
        }
        else
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(30, 80, 300, 25), "○ Detención obligatoria: 0%", style);
        }

        // 3. Estado de colisión con conos
        if (parkingFailed)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(30, 110, 300, 25), "✗ Conos: ¡COLISIÓN DETECTADA!", style);
        }
        else
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(30, 110, 300, 25), "✓ Conos intactos: Sin toques", style);
        }

        // 4. Instrucción guía
        style.normal.textColor = Color.white;
        string guia = parkingCompleted ? "¡Maniobra aprobada!" : "Mantén el vehículo quieto 2s";
        GUI.Label(new Rect(30, 140, 300, 25), $"► {guia}", style);
    }
}