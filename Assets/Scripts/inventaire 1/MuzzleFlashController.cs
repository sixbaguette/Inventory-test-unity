using UnityEngine;
using System.Collections;

public class MuzzleFlashController : MonoBehaviour
{
    [Header("Références")]
    public ParticleSystem flashParticles;
    public Light flashLight;

    [Header("Réglages")]
    public float flashLightIntensity = 3f;
    public float flashLightRange = 3f;
    public float flashDuration = 0.05f;

    private Coroutine lightRoutine;

    void Awake()
    {
        if (flashLight != null)
        {
            flashLight.enabled = false;
            flashLight.intensity = flashLightIntensity;
            flashLight.range = flashLightRange;
        }
    }

    void OnEnable()
    {
        // 🔇 Éteint tout au moment où l’arme apparaît
        if (flashParticles != null)
        {
            flashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (flashLight != null)
        {
            flashLight.enabled = false;
        }
    }

    public void PlayFlash()
    {
        // 🟡 Joue le particle system
        if (flashParticles != null)
            flashParticles.Play(true);

        // 💡 Allume brièvement la lumière
        if (flashLight != null)
        {
            if (lightRoutine != null)
                StopCoroutine(lightRoutine);
            lightRoutine = StartCoroutine(FlashLightRoutine());
        }
    }

    private IEnumerator FlashLightRoutine()
    {
        flashLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        flashLight.enabled = false;
    }
}
