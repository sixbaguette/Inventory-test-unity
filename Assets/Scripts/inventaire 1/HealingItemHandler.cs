using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealingItemHandler : MonoBehaviour
{
    [Header("UI Heal Progress")]
    public CanvasGroup healUI;
    public RectTransform healProgressFill;   // ✅ L’image rouge sous le RectMask2D
    public TextMeshProUGUI healTimerText;

    [Header("Références")]
    public HotbarManager hotbarManager;
    public PlayerEquipHandler playerEquipHandler;

    [Header("Références automatiques")]
    [SerializeField] private HealthManager playerHealth;

    private bool isHealing = false;
    private float healTimer = 0f;
    private ItemData currentItem;
    private Vector3 originalFillScale;

    void Start()
    {
        if (playerHealth == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponentInChildren<HealthManager>();

            if (playerHealth == null)
                Debug.LogError("[HealingItemHandler] Aucun HealthManager trouvé sur le Player !");
        }

        if (healUI != null)
        {
            healUI.alpha = 0f;
            healUI.interactable = false;
            healUI.blocksRaycasts = false;
        }

        if (healProgressFill != null)
            originalFillScale = healProgressFill.localScale;
    }

    void Update()
    {
        // 🚫 Empêche l'utilisation du bandage pendant que l'inventaire est ouvert
        if (InventoryToggle.IsInventoryOpen)
            return;

        currentItem = GetCurrentEquippedItem();
        if (currentItem == null || !currentItem.isHealingItem)
            return;

        if (Input.GetMouseButtonDown(0) && !isHealing)
            StartCoroutine(UseHealingItem(currentItem));
    }

    private ItemData GetCurrentEquippedItem()
    {
        if (hotbarManager == null) return null;

        int activeIndex = hotbarManager.GetCurrentHotbarIndex();
        if (activeIndex < 0 || activeIndex >= hotbarManager.hotbarSlots.Count)
            return null;

        var slot = hotbarManager.hotbarSlots[activeIndex].linkedSlot;
        if (slot == null || slot.CurrentItem == null) return null;

        var data = slot.CurrentItem.itemData;
        return (data != null && data.isHealingItem) ? data : null;
    }

    private System.Collections.IEnumerator UseHealingItem(ItemData item)
    {
        if (isHealing) yield break;
        isHealing = true;

        // 🔥 On affiche l’UI
        healUI.alpha = 1f;
        healUI.interactable = true;
        healUI.blocksRaycasts = true;

        healTimer = item.useTime;
        float elapsed = 0f;

        while (elapsed < item.useTime)
        {
            if (!Input.GetMouseButton(0))
            {
                CancelHeal();
                yield break;
            }

            elapsed += Time.deltaTime;
            healTimer = item.useTime - elapsed;

            // Progression de la barre (RectMask)
            float progress = Mathf.Clamp01(elapsed / item.useTime);
            if (healProgressFill != null)
            {
                Vector3 s = originalFillScale;
                s.x = progress;
                healProgressFill.localScale = s;
            }

            if (healTimerText != null)
                healTimerText.text = healTimer.ToString("F1");

            yield return null;
        }

        // ✅ Soin appliqué
        playerHealth.Heal(item.healAmount);

        // ✅ Consommation de l’objet
        ConsumeHealingItem();

        // ✅ Reset visuel
        if (healProgressFill != null)
            healProgressFill.localScale = originalFillScale;

        FadeOutUI();
        isHealing = false;
    }

    private void CancelHeal()
    {
        isHealing = false;
        FadeOutUI();
    }

    private void FadeOutUI()
    {
        if (healUI == null) return;

        LeanTween.value(healUI.gameObject, 1f, 0f, 0.2f)
            .setOnUpdate((float val) => healUI.alpha = val)
            .setOnComplete(() =>
            {
                healUI.interactable = false;
                healUI.blocksRaycasts = false;
            });
    }

    private void ConsumeHealingItem()
    {
        if (hotbarManager == null) return;

        int activeIndex = hotbarManager.GetCurrentHotbarIndex();
        if (activeIndex < 0 || activeIndex >= hotbarManager.hotbarSlots.Count)
            return;

        var slot = hotbarManager.hotbarSlots[activeIndex].linkedSlot;
        if (slot == null || slot.CurrentItem == null)
            return;

        var ui = slot.CurrentItem;
        ui.currentStack--;
        ui.UpdateStackText();

        if (ui.currentStack <= 0)
        {
            InventoryManager.Instance.RemoveItem(ui);
            Destroy(ui.gameObject);
            hotbarManager.UnequipCurrent();
        }
    }
}
