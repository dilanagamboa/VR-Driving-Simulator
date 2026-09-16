using UnityEngine;

public class WeatherStageDetector : MonoBehaviour
{
    [Header("Configuración de Velocidad y Parada")]
    [Tooltip("Límite máximo permitido en niebla (km/h)")]
    public float maxAllowedSpeedKmh = 35f;
    [Tooltip("Tiempo requerido inmóvil en la parada final")]
    public float requiredStopTime = 2.0f;
    public float stopSpeedThreshold = 0.15f;

    [Header("Ajustes de Niebla")]
    public Color fogColor = new Color(0.65f, 0.7f, 0.75f, 1f);
    public float fogDensity = 0.045f;

    [Header("Referencias del Vehículo")]
    public GameObject vehicle;
    private Rigidbody vehicleRb;
    private WheelCollider[] vehicleWheels;
    private WheelFrictionCurve defaultForwardFriction;
    private WheelFrictionCurve defaultSidewaysFriction;

    // Estados de evaluación
    private bool speedExceeded = false;
    private bool vehicleInStopZone = false;
    private float stoppedTimer = 0f;
    private bool stopCompleted = false;
    private bool testFinished = false;

    private void Awake()
    {
        if (vehicle != null)
        {
            vehicleRb = vehicle.GetComponent<Rigidbody>();
            vehicleWheels = vehicle.GetComponentsInChildren<WheelCollider>();
            if (vehicleWheels.Length > 0)
            {
                defaultForwardFriction = vehicleWheels[0].forwardFriction;
                defaultSidewaysFriction = vehicleWheels[0].sidewaysFriction;
            }
        }
    }

    public void EnableWeatherConditions()
    {
        // Activar niebla ambiental
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        // Reducir agarre de ruedas (fricción resbaladiza)
        if (vehicleWheels != null)
        {
            foreach (var wheel in vehicleWheels)
            {
                WheelFrictionCurve forward = wheel.forwardFriction;
                forward.stiffness = 0.5f;
                wheel.forwardFriction = forward;

                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                sideways.stiffness = 0.45f;
                wheel.sidewaysFriction = sideways;
            }
        }
    }

    public void DisableWeatherConditions()
    {
        RenderSettings.fog = false;

        // Restaurar fricción original
        if (vehicleWheels != null && vehicleWheels.Length > 0)
        {
            foreach (var wheel in vehicleWheels)
            {
                wheel.forwardFriction = defaultForwardFriction;
                wheel.sidewaysFriction = defaultSidewaysFriction;
            }
        }
    }

    public void OnVehicleEnterStopArea(Rigidbody rb)
    {
        vehicleInStopZone = true;
        if (vehicleRb == null) vehicleRb = rb;
    }

    public void OnVehicleExitStopArea()
    {
        if (stopCompleted) return;
        vehicleInStopZone = false;
        stoppedTimer = 0f;
    }

    private void Update()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.AdverseWeather)
            return;

        if (testFinished || vehicleRb == null) return;

        float currentSpeedKmh = vehicleRb.linearVelocity.magnitude * 3.6f;

        // 1. Control de velocidad máxima en niebla
        if (currentSpeedKmh > maxAllowedSpeedKmh)
        {
            speedExceeded = true;
            testFinished = true;
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnTestFailed($"¡Infracción por exceso de velocidad! Límite en niebla: {maxAllowedSpeedKmh} km/h (Ibas a {currentSpeedKmh:F0} km/h).");
            }
            return;
        }

        // 2. Control de detención en zona de parada
        if (vehicleInStopZone && !stopCompleted)
        {
            if (vehicleRb.linearVelocity.magnitude <= stopSpeedThreshold)
            {
                stoppedTimer += Time.deltaTime;

                if (stoppedTimer >= requiredStopTime)
                {
                    stoppedTimer = requiredStopTime;
                    stopCompleted = true;
                    testFinished = true;

                    if (GameFlowManager.Instance != null)
                    {
                        GameFlowManager.Instance.OnTestCompleted("¡Excelente! Completaste el trayecto en niebla a velocidad segura y con frenado controlado.");
                    }
                }
            }
            else
            {
                stoppedTimer = 0f;
            }
        }
    }

    public void ResetState()
    {
        vehicleInStopZone = false;
        stoppedTimer = 0f;
        speedExceeded = false;
        stopCompleted = false;
        testFinished = false;
    }

    private void OnGUI()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.AdverseWeather)
            return;

        if (testFinished) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 17;
        style.fontStyle = FontStyle.Bold;

        // Tarjeta de checklist estilo pruebas 1 y 2
        GUI.Box(new Rect(20, 20, 330, 165), "Prueba 3: Clima Adverso (Niebla)");

        // 1. Límite de velocidad
        float currentSpeedKmh = vehicleRb != null ? (vehicleRb.linearVelocity.magnitude * 3.6f) : 0f;
        style.normal.textColor = (currentSpeedKmh <= maxAllowedSpeedKmh) ? Color.green : Color.red;
        GUI.Label(new Rect(30, 50, 310, 25), $"Velocidad actual: {currentSpeedKmh:F0} / {maxAllowedSpeedKmh} km/h", style);

        // 2. Estado de zona final
        style.normal.textColor = vehicleInStopZone ? Color.green : Color.white;
        string zoneText = vehicleInStopZone ? "✓ En zona de frenado final" : "○ Dirígete a la parada de niebla";
        GUI.Label(new Rect(30, 80, 310, 25), zoneText, style);

        // 3. Progreso de frenado
        if (stopCompleted)
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(30, 110, 310, 25), "✓ Parada segura: 100%", style);
        }
        else if (vehicleInStopZone)
        {
            style.normal.textColor = Color.yellow;
            float progreso = Mathf.Clamp01(stoppedTimer / requiredStopTime) * 100f;
            GUI.Label(new Rect(30, 110, 310, 25), $"Detención obligatoria: {progreso:F0}%", style);
        }
        else
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(30, 110, 310, 25), "○ Detención obligatoria: 0%", style);
        }

        // 4. Advertencia de adherencia
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(30, 140, 310, 25), "⚠ Asfalto resbaladizo (Frena con anticipación)", style);
    }
}