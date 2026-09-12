using UnityEngine;

public class CabinLook : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float sensitivity = 2.0f;

    [Header("Límites de giro horizontal (grados)")]
    public float minYaw = -90f; // Mirar a la izquierda (ventana)
    public float maxYaw = 90f;  // Mirar a la derecha (asiento copiloto)

    [Header("Límites de giro vertical (grados)")]
    public float minPitch = -35f; // Mirar hacia abajo (palanca/tablero)
    public float maxPitch = 40f;  // Mirar hacia arriba (parasol/techo)

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        // Bloquea el cursor dentro de la ventana de juego para evitar salir de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Presionar Escape para liberar el cursor si necesitas interactuar con el editor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Si el cursor vuelve a hacer clic en la pantalla de juego, se reengancha
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            yaw += mouseX;
            pitch -= mouseY;

            // Restringir el rango angular para no romper el cuello
            yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Aplica la rotación relativa al chasis del camión
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}