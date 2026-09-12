using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles UI")]
    [Tooltip("El panel completo que contiene los botones y bienvenida")]
    public GameObject welcomePanel;

    [Header("Control de Vista / Cámara")]
    [Tooltip("Arrastra aquí el script de rotación de vista (ej. CabinLook)")]
    public MonoBehaviour cameraLookScript;

    void Awake()
    {
        // Congelar físicas y tiempo
        Time.timeScale = 0f;

        // Liberar y mostrar cursor obligatoriamente
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);
        }

        // Asegurar que el script de cámara no capture el cursor todavía
        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = false;
        }
    }

    void Update()
    {
        // Si el menú sigue abierto, asegurar que el mouse nunca quede bloqueado
        if (welcomePanel != null && welcomePanel.activeSelf)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void StartSimulation()
    {
        // Ocultar menú
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }

        // Reanudar tiempo
        Time.timeScale = 1f;

        // Activar el control de la cámara
        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = true;
        }

        // Bloquear cursor para manejar la cabina libremente
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ExitSimulation()
    {
        Application.Quit();
    }
}