using UnityEngine;

public class FloatingSign : MonoBehaviour
{
    [Header("Animación de Flotación")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    [Header("Escala Dinámica por Distancia")]
    [Tooltip("Escala cuando estás lejos (ej. 0.01)")]
    public float maxScale = 0.01f;

    [Tooltip("Escala mínima cuando estás muy cerca (ej. 0.003)")]
    public float minScale = 0.003f;

    [Tooltip("Distancia a partir de la cual empieza a encogerse")]
    public float maxDistance = 25f;

    [Tooltip("Distancia donde alcanza su tamaño mínimo")]
    public float minDistance = 4f;

    private Vector3 initialPos;
    private Camera mainCam;

    void Start()
    {
        initialPos = transform.position;
        mainCam = Camera.main;
    }

    void Update()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // 1. Efecto suave de subir y bajar
        float newY = initialPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(initialPos.x, newY, initialPos.z);

        // 2. Orientar siempre hacia la cámara del conductor (Billboard)
        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                         mainCam.transform.rotation * Vector3.up);

        // 3. Ajustar escala según la distancia a la cámara
        float distance = Vector3.Distance(transform.position, mainCam.transform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float currentScale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = new Vector3(currentScale, currentScale, currentScale);
    }
}