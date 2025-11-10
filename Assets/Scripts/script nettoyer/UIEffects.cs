using UnityEngine;

public static class UIEffects
{
    public static void Shake(RectTransform target, float intensity = 8f, float duration = 0.25f)
    {
        if (target == null) return;

        Vector2 original = target.anchoredPosition;
        LeanTween.cancel(target);

        LeanTween.value(target.gameObject, -intensity, intensity, duration)
            .setEaseShake()
            .setOnUpdate((float val) =>
            {
                if (target != null)
                    target.anchoredPosition = original + new Vector2(val, 0f);
            })
            .setOnComplete(() =>
            {
                if (target != null)
                    target.anchoredPosition = original; // retour pile
            });
    }
}