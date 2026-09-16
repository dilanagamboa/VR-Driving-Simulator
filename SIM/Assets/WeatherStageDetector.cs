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
    public Color fogColor = new Color(0.6f, 0.65f, 0.7f, 1f);
    public float fogDensity = 0.12f;

    [Header("Control de Iluminación Ambiental (Atmósfera)")]
    [Tooltip("Arrastra aquí tu Directional Light (el Sol)")]
    public Light sunLight;
    public float stormySunIntensity = 0.2f;
    public Color stormySunColor = new Color(0.6f, 0.65f, 0.7f, 1f);
    public Color stormyAmbientColor = new Color(0.45f, 0.5f, 0.55f, 1f);

    // Valores originales para restaurar el clima soleado
    private float defaultSunIntensity;
    private Color defaultSunColor;
    private Color defaultAmbientColor;

    [Header("Control de Luces (L)")]
    [Tooltip("Componentes de luz delanteros del vehículo (opcional)")]
    public Light[] headlights;
    public bool headlightsOn = false;

    [Header("Referencias del Vehículo")]
    public GameObject vehicle;
    private Rigidbody vehicleRb;
    private WheelCollider[] vehicleWheels;
    private WheelFrictionCurve defaultForwardFriction;
    private WheelFrictionCurve defaultSidewaysFriction;

    // Estados de evaluación
    private bool vehicleInStopZone = false;
    private float stoppedTimer = 0f;
    private bool stopCompleted = false;
    private bool testFinished = false;

    // Temporizador para parpadeo de alerta
    private float warningBlinkTimer = 0f;
    private bool warningBlinkState = false;

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

        // Respaldar ajustes de luz originales
        if (sunLight != null)
        {
            defaultSunIntensity = sunLight.intensity;
            defaultSunColor = sunLight.color;
        }
        defaultAmbientColor = RenderSettings.ambientLight;
    }

    public void EnableWeatherConditions()
    {
        // 1. Activar Niebla
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        // 2. Oscurecer la atmósfera y teñir ambiente
        if (sunLight != null)
        {
            sunLight.intensity = stormySunIntensity;
            sunLight.color = stormySunColor;
        }
        RenderSettings.ambientLight = stormyAmbientColor;

        // 3. Fricción resbaladiza
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

        SetHeadlights(false);
    }

    public void DisableWeatherConditions()
    {
        // 1. Apagar niebla
        RenderSettings.fog = false;

        // 2. Restaurar iluminación solar y ambiental original
        if (sunLight != null)
        {
            sunLight.intensity = defaultSunIntensity;
            sunLight.color = defaultSunColor;
        }
        RenderSettings.ambientLight = defaultAmbientColor;

        // 3. Restaurar fricción
        if (vehicleWheels != null && vehicleWheels.Length > 0)
        {
            foreach (var wheel in vehicleWheels)
            {
                wheel.forwardFriction = defaultForwardFriction;
                wheel.sidewaysFriction = defaultSidewaysFriction;
            }
        }

        SetHeadlights(false);
    }

    private void SetHeadlights(bool state)
    {
        headlightsOn = state;
        if (headlights != null)
        {
            foreach (var light in headlights)
            {
                if (light != null) light.enabled = state;
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

        // 1. Alternar luces con tecla L
        if (Input.GetKeyDown(KeyCode.L))
        {
            SetHeadlights(!headlightsOn);
        }

        // Parpadeo de la advertencia si las luces están apagadas
        if (!headlightsOn)
        {
            warningBlinkTimer += Time.deltaTime;
            if (warningBlinkTimer >= 0.35f)
            {
                warningBlinkState = !warningBlinkState;
                warningBlinkTimer = 0f;
            }
        }

        float currentSpeedKmh = vehicleRb.linearVelocity.magnitude * 3.6f;

        // 2. Control de velocidad máxima (35 km/h)
        if (currentSpeedKmh > maxAllowedSpeedKmh)
        {
            testFinished = true;
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnTestFailed($"¡Infracción por exceso de velocidad! Límite en niebla: {maxAllowedSpeedKmh} km/h (Ibas a {currentSpeedKmh:F0} km/h).");
            }
            return;
        }

        // 3. Parada final: evaluación estricta de luces y freno
        if (vehicleInStopZone && !stopCompleted)
        {
            if (vehicleRb.linearVelocity.magnitude <= stopSpeedThreshold)
            {
                stoppedTimer += Time.deltaTime;

                if (stoppedTimer >= requiredStopTime)
                {
                    stoppedTimer = requiredStopTime;

                    if (!headlightsOn)
                    {
                        testFinished = true;
                        if (GameFlowManager.Instance != null)
                        {
                            GameFlowManager.Instance.OnTestFailed("¡Fallo en prueba de niebla! Llegaste al alto final sin encender las luces (Tecla L).");
                        }
                        return;
                    }

                    stopCompleted = true;
                    testFinished = true;

                    if (GameFlowManager.Instance != null)
                    {
                        GameFlowManager.Instance.OnTestCompleted("¡Simulador Completado! Manejaste con precaución, luces encendidas y frenado seguro.");
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
        stopCompleted = false;
        testFinished = false;
        SetHeadlights(false);
    }

    private void OnGUI()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.AdverseWeather)
            return;

        if (testFinished) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 17;
        style.fontStyle = FontStyle.Bold;

        GUI.Box(new Rect(20, 20, 330, 175), "Prueba 3: Clima Adverso (Niebla)");

        // 1. Faros con aviso intermitente
        if (headlightsOn)
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(30, 48, 310, 24), "✓ Faros (L): ENCENDIDOS", style);
        }
        else
        {
            style.normal.textColor = warningBlinkState ? Color.red : Color.yellow;
            GUI.Label(new Rect(30, 48, 310, 24), "⚠ Faros (L): ¡ENCIÉNDELOS!", style);
        }

        // 2. Límite de velocidad
        float currentSpeedKmh = vehicleRb != null ? (vehicleRb.linearVelocity.magnitude * 3.6f) : 0f;
        style.normal.textColor = (currentSpeedKmh <= maxAllowedSpeedKmh) ? Color.green : Color.red;
        GUI.Label(new Rect(30, 75, 310, 24), $"Velocidad: {currentSpeedKmh:F0} / {maxAllowedSpeedKmh} km/h", style);

        // 3. Zona de parada final
        style.normal.textColor = vehicleInStopZone ? Color.green : Color.white;
        string zoneText = vehicleInStopZone ? "✓ En zona de parada final" : "○ Dirígete a la parada de niebla";
        GUI.Label(new Rect(30, 102, 310, 24), zoneText, style);

        // 4. Progreso de detención
        if (stopCompleted)
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(30, 129, 310, 24), "✓ Parada segura: 100%", style);
        }
        else if (vehicleInStopZone)
        {
            style.normal.textColor = Color.yellow;
            float progreso = Mathf.Clamp01(stoppedTimer / requiredStopTime) * 100f;
            GUI.Label(new Rect(30, 129, 310, 24), $"Detención obligatoria: {progreso:F0}%", style);
        }
        else
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(30, 129, 310, 24), "○ Detención obligatoria: 0%", style);
        }

        // 5. Mensaje de adherencia
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(30, 153, 310, 20), "⚠ Asfalto resbaladizo ", style);
    }
}