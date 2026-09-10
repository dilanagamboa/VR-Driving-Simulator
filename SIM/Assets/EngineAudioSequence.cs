using System.Collections;
using UnityEngine;

public class EngineAudioSequence : MonoBehaviour
{
    [Header("Componente de Audio")]
    public AudioSource audioSource;

    [Header("Sonidos")]
    [Tooltip("Sonido de arranque/ignición (suena una vez)")]
    public AudioClip startupClip;

    [Tooltip("Sonido de motor continuo (en loop)")]
    public AudioClip loopClip;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (startupClip != null)
        {
            StartCoroutine(PlayStartupSequence());
        }
        else if (loopClip != null)
        {
            // Si no hay sonido inicial, pasa directo al loop
            audioSource.clip = loopClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private IEnumerator PlayStartupSequence()
    {
        // 1. Reproducir sonido de arranque sin loop
        audioSource.clip = startupClip;
        audioSource.loop = false;
        audioSource.Play();

        // 2. Esperar a que termine la duración exacta del clip inicial
        yield return new WaitForSeconds(startupClip.length);

        // 3. Cambiar al clip continuo y activar el bucle infinito
        if (loopClip != null)
        {
            audioSource.clip = loopClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}