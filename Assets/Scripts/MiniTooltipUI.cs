using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniTooltipUI : MonoBehaviour
{
    public static MiniTooltipUI Instance;

    [Header("Refs")]
    public CanvasGroup canvasGroup;
    public RectTransform background;     // <- drag "Background"
    public RectTransform content;        // <- drag "Content"
    public TextMeshProUGUI titleText;    // <- drag
    public TextMeshProUGUI descriptionText; // <- drag

    [Header("Style")]
    public Vector2 padding = new Vector2(12f, 10f); // x=left/right, y=top/bottom
    public float lineSpacing = 6f;                  // espace entre titre et description
    public float maxWidth = 280f;                   // largeur max du tooltip
    public Vector2 offset = new Vector2(18f, -18f);
    public float appearDelay = 0.35f;
    public Sprite backgroundSprite;                 // optionnel (9-slice)

    RectTransform self;
    bool visible;
    Coroutine fadeCo;

    void Awake()
    {
        Instance = this;
        self = GetComponent<RectTransform>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Auto-find si oublié
        if (!background) background = transform.Find("Background") as RectTransform;
        if (!content && background) content = background.Find("Content") as RectTransform;

        // Fix anchors/pivots
        SetupRT(self);
        SetupRT(background);
        SetupRT(content);
        SetupRT(titleText ? (RectTransform)titleText.transform : null);
        SetupRT(descriptionText ? (RectTransform)descriptionText.transform : null);

        // Texte: wrap ON, auto-size OFF, rich text OK
        if (titleText)
        {
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.richText = true;
            titleText.enableAutoSizing = false;
        }
        if (descriptionText)
        {
            descriptionText.textWrappingMode = TextWrappingModes.Normal;
            descriptionText.richText = true;
            descriptionText.enableAutoSizing = false;
        }

        // Met le Canvas tout devant
        var cv = GetComponentInParent<Canvas>();
        if (cv)
        {
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 9999;
        }

        // Background visuel propre
        var img = background ? background.GetComponent<Image>() : null;
        if (img)
        {
            img.raycastTarget = false;
            if (backgroundSprite) img.sprite = backgroundSprite;
            img.type = Image.Type.Sliced; // si 9-slice
        }

        HideInstant();
    }

    void SetupRT(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
    }

    void Update()
    {
        if (visible)
        {
            self.position = Input.mousePosition + (Vector3)offset;
        }
    }

    public void Show(ItemData item)
    {
        if (!item) return;
        StopFade();

        // 1) Remplir le texte
        if (titleText) titleText.text = $"<b>{item.itemName}</b>";
        if (descriptionText) descriptionText.text = BuildDesc(item);

        // 2) Mesurer → dimensionner
        ResizeToFit();

        // 3) Afficher
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0f;

        fadeCo = StartCoroutine(FadeInAfterDelay());
        visible = true;
    }

    public void Hide()
    {
        StopFade();

        // ✅ sécurité : ne pas lancer de coroutine si déjà inactif
        if (!gameObject.activeInHierarchy)
            return;

        fadeCo = StartCoroutine(FadeOut());
        visible = false;
    }

    public void HideInstant()
    {
        StopFade();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
        visible = false;
    }

    void StopFade()
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = null;
        LeanTween.cancel(gameObject);
    }

    System.Collections.IEnumerator FadeInAfterDelay()
    {
        yield return new WaitForSeconds(appearDelay);
        LeanTween.value(gameObject, 0f, 1f, 0.12f)
                 .setOnUpdate(a => canvasGroup.alpha = a);
    }

    System.Collections.IEnumerator FadeOut()
    {
        LeanTween.value(gameObject, canvasGroup.alpha, 0f, 0.12f)
                 .setOnUpdate(a => canvasGroup.alpha = a);
        yield return new WaitForSeconds(0.12f);
        gameObject.SetActive(false);
    }

    // ======= Dimensionnement manuel =======
    void ResizeToFit()
    {
        if (!background || !content) return;

        // Largeur cible
        float maxTextWidth = Mathf.Max(50f, maxWidth - padding.x * 2f);

        // Preferred size des textes (contraints par largeur)
        Vector2 tSize = Vector2.zero, dSize = Vector2.zero;

        if (titleText)
        {
            // largeur imposée → hauteur auto
            tSize = titleText.GetPreferredValues(titleText.text, maxTextWidth, Mathf.Infinity);
            tSize.x = Mathf.Min(tSize.x, maxTextWidth);
        }

        if (descriptionText)
        {
            dSize = descriptionText.GetPreferredValues(descriptionText.text, maxTextWidth, Mathf.Infinity);
            dSize.x = Mathf.Min(dSize.x, maxTextWidth);
        }

        float contentW = Mathf.Clamp(Mathf.Max(tSize.x, dSize.x), 50f, maxWidth - padding.x * 2f);
        float contentH = 0f;

        // Positionner les blocs dans Content
        if (titleText)
        {
            var rt = (RectTransform)titleText.transform;
            rt.anchoredPosition = Vector2.zero; // top-left du Content
            rt.sizeDelta = new Vector2(contentW, tSize.y);
            contentH += tSize.y;
        }

        if (descriptionText)
        {
            var rt = (RectTransform)descriptionText.transform;
            float y = contentH > 0f ? contentH + lineSpacing : 0f;
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(contentW, dSize.y);
            contentH = y + dSize.y;
        }

        // Taille du Content
        content.sizeDelta = new Vector2(contentW, contentH);

        // Taille du Background (padding)
        float bgW = contentW + padding.x * 2f;
        float bgH = contentH + padding.y * 2f;
        background.sizeDelta = new Vector2(bgW, bgH);

        // Décaler Content à l’intérieur du Background (padding)
        content.anchoredPosition = new Vector2(padding.x, -padding.y);
    }

    string BuildDesc(ItemData item)
    {
        if (item.isGun)
        {
            return $"<color=#cccccc>Dégâts :</color> {item.damage}\n" +
                   $"<color=#cccccc>Cadence :</color> {item.fireRate:F2}s\n" +
                   $"<color=#cccccc>Type :</color> {item.bulletType}\n" +
                   $"<color=#cccccc>Chargeur :</color> {item.ammoCapacity}";
        }
        if (item.isHealingItem)
        {
            return $"<color=#99ff99>Soigne :</color> +{item.healAmount} HP\n" +
                   $"<color=#cccccc>Durée :</color> {item.useTime:F1}s";
        }
        if (item.isArmor)
        {
            return $"<color=#66ccff>Protection :</color> {item.armorValue}\n" +
                   $"<color=#cccccc>Slot :</color> {item.armorSlotType}";
        }
        if (item.isAmmo)
        {
            return $"<color=#cccccc>Type :</color> {item.ammoType}";
        }
        return string.IsNullOrEmpty(item.description) ? "" : item.description;
    }
}
