using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [Header("Références UI")]
    public Image fillImage;                // barre rouge
    public CanvasGroup canvasGroup;        // fade de la barre
    public TextMeshProUGUI healthText;     // texte (HP)
    public Image damageOverlay;            // overlay rouge plein écran

    [Header("Paramètres visuels")]
    public float fadeSpeed = 2f;
    public float visibleDuration = 2f;
    public float flashDuration = 0.5f;
    public float lowHealthThreshold = 15f; // HP critique
    public float lowHealthAlpha = 0.5f;    // opacité du rouge constant

    private HealthManager healthManager;
    private Coroutine fadeRoutine;
    private Coroutine flashRoutine;
    private float currentFill = 1f;
    private float lastHealth;
    private bool isCritical = false;

    public static HealthBarUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        healthManager = FindFirstObjectByType<HealthManager>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0;

        if (fillImage != null)
            fillImage.rectTransform.pivot = new Vector2(0f, 0.5f);

        if (damageOverlay != null)
            damageOverlay.color = new Color(1, 0, 0, 0);

        if (healthManager != null)
            lastHealth = healthManager.CurrentHealth;
    }

    void Update()
    {
        if (healthManager == null)
            return;

        float current = healthManager.CurrentHealth;
        float max = healthManager.maxHealth;
        float target = Mathf.Clamp01(current / max);

        // Synchronise la barre rouge
        if (Mathf.Abs(target - currentFill) > 0.001f)
        {
            currentFill = target;
            if (fillImage != null)
                fillImage.rectTransform.localScale = new Vector3(currentFill, 1f, 1f);
            ShowTemporarily();
        }

        // Texte HP toujours synchronisé
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

        // ⚡ Détection du dégât pour flash
        if (healthManager.CurrentHealth < lastHealth)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashOverlay());
        }

        lastHealth = healthManager.CurrentHealth;

        // ❤️ Effet rouge permanent si HP <= seuil
        if (damageOverlay != null)
        {
            if (healthManager.CurrentHealth <= lowHealthThreshold)
            {
                isCritical = true;
                SetOverlayAlpha(lowHealthAlpha);
            }
            else if (isCritical)
            {
                isCritical = false;
                SetOverlayAlpha(0f);
            }
        }
    }

    IEnumerator FadeRoutine()
    {
        // fade in de la barre (PAS du rouge)
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        yield return new WaitForSeconds(visibleDuration);

        // fade out (le rouge reste actif)
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }

    IEnumerator FlashOverlay()
    {
        // 💥 flash rouge rapide 0.5s
        float timer = 0f;
        while (timer < flashDuration)
        {
            float alpha = Mathf.Lerp(0.6f, 0f, timer / flashDuration);
            SetOverlayAlpha(alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        // si pas critique → revient à normal
        if (!isCritical)
            SetOverlayAlpha(0f);
    }

    void SetOverlayAlpha(float alpha)
    {
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = alpha;
            damageOverlay.color = c;
        }
    }

    void ShowTemporarily()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine());
    }

    public void UpdateHealthBar(float current, float max)
    {
        if (fillImage == null) return;

        float fill = Mathf.Clamp01(current / max);

        // si ta barre utilise un RectMask2D → scale
        fillImage.rectTransform.localScale = new Vector3(fill, 1f, 1f);

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
