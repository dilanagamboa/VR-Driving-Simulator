using UnityEngine;

public class IntersectionDetector : MonoBehaviour
{
    [Header("Configuración de Alto")]
    [Tooltip("Velocidad máxima para considerar alto total")]
    public float stopSpeedThreshold = 0.15f;
    [Tooltip("Segundos requeridos completamente detenido")]
    public float requiredStopTime = 2.0f;

    [Header("Referencias de Cabina")]
    [Tooltip("Arrastra aquí la cámara de la cabina (ej. VR_CAMERA)")]
    public Transform cabinCamera;

    [Header("Requisitos Evaluados")]
    public bool stopSatisfied = false;
    public bool lookedLeft = false;
    public bool lookedRight = false;
    public bool blinkerLeftActive = false;

    private bool vehicleInStopZone = false;
    private Rigidbody vehicleRb;
    private float stoppedTimer = 0f;
    private bool testFinished = false;

    // Parpadeo visual de direccional
    private float blinkerTimer = 0f;
    private bool blinkerLightState = false;

    public void OnVehicleEnterStopZone(Rigidbody rb)
    {
        vehicleInStopZone = true;
        vehicleRb = rb;
        stoppedTimer = 0f;
    }

    public void OnVehicleExitStopZone()
    {
        vehicleInStopZone = false;
        stoppedTimer = 0f;
    }

    public void OnVehicleEnterIntersection()
    {
        if (testFinished) return;

        // Evaluación estricta de las reglas
        if (!stopSatisfied)
        {
            FailTest("¡Infracción! No te detuviste completamente en la línea de ALTO.");
        }
        else if (!blinkerLeftActive)
        {
            FailTest("¡Infracción! No pusiste la direccional izquierda (Tecla Q) para doblar.");
        }
        else if (!lookedLeft)
        {
            FailTest("¡Maniobra peligrosa! No miraste a la izquierda antes de cruzar.");
        }
        else if (!lookedRight)
        {
            FailTest("¡Maniobra peligrosa! No miraste a la derecha antes de cruzar.");
        }
        else
        {
            testFinished = true;
            Debug.Log("¡Intersección superada con éxito!");
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnTestCompleted("¡Excelente maniobra! Respetaste el alto, miraste a ambos lados y señalizaste.");
            }
        }
    }

    private void FailTest(string reason)
    {
        testFinished = true;
        Debug.LogWarning(reason);
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnTestFailed(reason);
        }
    }

    private void Update()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.Intersection)
            return;

        if (testFinished) return;

        // 1. Activar direccional izquierda con tecla Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            blinkerLeftActive = !blinkerLeftActive;
        }

        if (blinkerLeftActive)
        {
            blinkerTimer += Time.deltaTime;
            if (blinkerTimer >= 0.35f)
            {
                blinkerLightState = !blinkerLightState;
                blinkerTimer = 0f;
            }
        }

        // 2. Revisión de ángulos de cámara cuando el camión está en la zona de detención
        if (cabinCamera != null && vehicleInStopZone)
        {
            float yRot = cabinCamera.localEulerAngles.y;
            if (yRot > 180f) yRot -= 360f; // Rango de -180 a 180

            // Mirar hacia la izquierda (rotación negativa)
            if (yRot <= -35f && !lookedLeft)
            {
                lookedLeft = true;
            }

            // Mirar hacia la derecha (rotación positiva)
            if (yRot >= 35f && !lookedRight)
            {
                lookedRight = true;
            }
        }

        // 3. Temporizador de frenado
        if (vehicleInStopZone && vehicleRb != null && !stopSatisfied)
        {
            float currentSpeed = vehicleRb.linearVelocity.magnitude;

            if (currentSpeed <= stopSpeedThreshold)
            {
                stoppedTimer += Time.deltaTime;
                if (stoppedTimer >= requiredStopTime)
                {
                    stopSatisfied = true;
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
        vehicleRb = null;
        stoppedTimer = 0f;
        stopSatisfied = false;
        lookedLeft = false;
        lookedRight = false;
        blinkerLeftActive = false;
        testFinished = false;
        blinkerTimer = 0f;
        blinkerLightState = false;
    }

    private void OnGUI()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameFlowManager.TestStage.Intersection)
            return;

        if (testFinished) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 17;
        style.fontStyle = FontStyle.Bold;

        // Caja de checklist en la esquina superior izquierda
        GUI.Box(new Rect(20, 20, 320, 165), "Prueba 2: Intersección con ALTO");

        style.normal.textColor = stopSatisfied ? Color.green : Color.yellow;
        string stopStatus = stopSatisfied ? "✓ Alto completado (2s)" : $"Alto obligatorio: {Mathf.Clamp01(stoppedTimer / requiredStopTime) * 100f:F0}%";
        GUI.Label(new Rect(30, 50, 300, 25), stopStatus, style);

        style.normal.textColor = blinkerLeftActive ? Color.green : Color.white;
        string blinkerText = blinkerLeftActive ? "✓ Direccional Izq (Q): ACTIVA" : "○ Direccional Izq (Q): Apagada";
        GUI.Label(new Rect(30, 80, 300, 25), blinkerText, style);

        style.normal.textColor = lookedLeft ? Color.green : Color.white;
        string leftText = lookedLeft ? "✓ Tráfico Izquierdo: REVISADO" : "○ Mirar Izquierda: Pendiente";
        GUI.Label(new Rect(30, 110, 300, 25), leftText, style);

        style.normal.textColor = lookedRight ? Color.green : Color.white;
        string rightText = lookedRight ? "✓ Tráfico Derecho: REVISADO" : "○ Mirar Derecha: Pendiente";
        GUI.Label(new Rect(30, 140, 300, 25), rightText, style);

        // Flecha direccional parpadeando
        if (blinkerLeftActive && blinkerLightState)
        {
            GUIStyle arrowStyle = new GUIStyle();
            arrowStyle.fontSize = 40;
            arrowStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(Screen.width / 2 - 40, Screen.height - 100, 80, 50), "◄ ◄", arrowStyle);
        }
    }
}