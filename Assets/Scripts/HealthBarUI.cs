using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;                // barre rouge
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI healthText;
    public Image damageOverlay;            // 🩸 overlay rouge
    public float fadeSpeed = 2f;
    public float visibleDuration = 2f;
    public float pulseSpeed = 3f;          // vitesse du clignotement

    private HealthManager healthManager;
    private Coroutine fadeRoutine;
    private float currentFill = 1f;
    private bool isLowHealth = false;

    void Start()
    {
        healthManager = FindFirstObjectByType<HealthManager>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0;

        if (fillImage != null)
            fillImage.rectTransform.pivot = new Vector2(0f, 0.5f);

        if (damageOverlay != null)
            damageOverlay.color = new Color(1, 0, 0, 0); // invisible au début
    }

    void Update()
    {
        if (healthManager == null)
            return;

        float target = Mathf.Clamp01(healthManager.CurrentHealth / healthManager.maxHealth);
        if (Mathf.Abs(target - currentFill) > 0.001f)
        {
            currentFill = target;
            if (fillImage != null)
                fillImage.rectTransform.localScale = new Vector3(currentFill, 1f, 1f);
            ShowTemporarily();
        }

        // 🩸 Texte
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(healthManager.CurrentHealth)}/{Mathf.CeilToInt(healthManager.maxHealth)}";

        // ❤️ Effet de blessure
        if (damageOverlay != null)
        {
            if (healthManager.CurrentHealth <= 20f)
            {
                if (!isLowHealth)
                {
                    isLowHealth = true;
                    StartCoroutine(PulseOverlay());
                }
            }
            else
            {
                isLowHealth = false;
                damageOverlay.color = new Color(1, 0, 0, 0);
            }
        }
    }

    IEnumerator FadeRoutine()
    {
        // Fade in
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        yield return new WaitForSeconds(visibleDuration);

        // Fade out
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }

    IEnumerator PulseOverlay()
    {
        while (isLowHealth)
        {
            float alpha = 0.25f + Mathf.PingPong(Time.time * pulseSpeed, 0.25f); // clignotement doux entre 0.25–0.5
            damageOverlay.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        // Quand on sort du mode blessé → cache l’overlay
        damageOverlay.color = new Color(1, 0, 0, 0);
    }

    void ShowTemporarily()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine());
    }
}
