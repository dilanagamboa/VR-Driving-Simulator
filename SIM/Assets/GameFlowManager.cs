using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum TestStage { Parking = 0, Intersection = 1, AdverseWeather = 2 }

    [Header("Estado Actual")]
    public TestStage currentStage = TestStage.Parking;

    [Header("Referencias del Vehículo")]
    public GameObject vehicle;
    private Rigidbody vehicleRb;

    [Header("Control de Vista (Opcional)")]
    [Tooltip("Script de cámara para desactivar mientras el panel esté abierto")]
    public MonoBehaviour cameraLookScript;

    [Header("Spawns")]
    public Transform spawnParking;
    public Transform spawnIntersection;
    public Transform spawnWeather;

    [Header("Referencias de Escenarios")]
    public ParkingDetector parkingDetector;
    public IntersectionDetector intersectionDetector;
    public WeatherStageDetector weatherStageDetector;

    [Header("UI Feedback")]
    public GameObject evaluationPanel;
    public TextMeshProUGUI statusText;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    private bool lastTestSuccess = false;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (vehicle != null)
        {
            vehicleRb = vehicle.GetComponent<Rigidbody>();
        }
    }

    public void OnTestCompleted(string successMessage)
    {
        if (isTransitioning) return;
        lastTestSuccess = true;

        // Si estamos en la última prueba, el botón dice "Terminar", de lo contrario "Siguiente Prueba"
        string buttonText = (currentStage == TestStage.AdverseWeather) ? "Terminar" : "Siguiente Prueba";

        ShowEvaluationUI(successMessage, buttonText, Color.forestGreen);
    }

    public void OnTestFailed(string failureMessage)
    {
        if (isTransitioning) return;
        lastTestSuccess = false;
        ShowEvaluationUI(failureMessage, "Reintentar", Color.red);
    }

    private void ShowEvaluationUI(string message, string buttonText, Color textColor)
    {
        isTransitioning = true;

        if (vehicleRb != null)
        {
            vehicleRb.linearVelocity = Vector3.zero;
            vehicleRb.angularVelocity = Vector3.zero;
            vehicleRb.isKinematic = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = false;
        }

        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = textColor;
        }

        if (actionButtonText != null)
        {
            actionButtonText.text = buttonText;
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        if (evaluationPanel != null)
        {
            evaluationPanel.SetActive(true);
        }
    }

    public void OnActionButtonClicked()
    {
        if (evaluationPanel != null)
        {
            evaluationPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = true;
        }

        if (lastTestSuccess)
        {
            if (currentStage == TestStage.Parking)
            {
                currentStage = TestStage.Intersection;
            }
            else if (currentStage == TestStage.Intersection)
            {
                currentStage = TestStage.AdverseWeather;
            }
            else if (currentStage == TestStage.AdverseWeather)
            {
                // Acción al terminar todo el simulador (reiniciar escena o menú principal)
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
                return;
            }
        }

        StartCoroutine(ExecuteStageReset());
    }

    private IEnumerator ExecuteStageReset()
    {
        Transform targetSpawn = null;

        // Manejar condiciones ambientales según fase
        if (currentStage == TestStage.AdverseWeather)
        {
            if (weatherStageDetector != null)
            {
                weatherStageDetector.EnableWeatherConditions();
                weatherStageDetector.ResetState();
            }
        }
        else
        {
            if (weatherStageDetector != null)
            {
                weatherStageDetector.DisableWeatherConditions();
            }
        }

        switch (currentStage)
        {
            case TestStage.Parking:
                targetSpawn = spawnParking;
                ResetParkingStage();
                break;
            case TestStage.Intersection:
                targetSpawn = spawnIntersection;
                if (intersectionDetector != null) intersectionDetector.ResetState();
                break;
            case TestStage.AdverseWeather:
                targetSpawn = spawnWeather;
                break;
        }

        if (targetSpawn != null && vehicle != null)
        {
            WheelCollider[] wheels = vehicle.GetComponentsInChildren<WheelCollider>();
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = Mathf.Infinity;
                w.enabled = false;
            }

            if (vehicleRb != null)
            {
                vehicleRb.isKinematic = true;
                vehicleRb.linearVelocity = Vector3.zero;
                vehicleRb.angularVelocity = Vector3.zero;
            }

            Vector3 spawnPos = targetSpawn.position + Vector3.up * 0.2f;
            vehicle.transform.position = spawnPos;
            vehicle.transform.rotation = targetSpawn.rotation;

            if (vehicleRb != null)
            {
                vehicleRb.position = spawnPos;
                vehicleRb.rotation = targetSpawn.rotation;
            }

            Physics.SyncTransforms();

            yield return new WaitForFixedUpdate();

            foreach (var w in wheels)
            {
                w.enabled = true;
                w.brakeTorque = 0f;
            }

            if (vehicleRb != null)
            {
                vehicleRb.isKinematic = false;
                vehicleRb.linearVelocity = Vector3.zero;
                vehicleRb.angularVelocity = Vector3.zero;
                vehicleRb.WakeUp();
            }

            Physics.SyncTransforms();
        }

        yield return new WaitForSeconds(0.2f);
        isTransitioning = false;
    }

    private void ResetParkingStage()
    {
        if (parkingDetector != null)
        {
            parkingDetector.ResetState();
        }

        ResettableCone[] cones = FindObjectsByType<ResettableCone>(FindObjectsSortMode.None);
        foreach (var cone in cones)
        {
            cone.ResetToOrigin();
        }
    }
}