using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Makes a UI button grow slightly while hovered, like a modern menu button.
/// Scale animates smoothly in and out and keeps the object's original proportions.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("How much the button grows while hovered. 1.06 = 6% bigger.")]
    [Range(1f, 1.25f)] public float hoverScale = 1.06f;

    [Tooltip("How fast the scale animates in and out.")]
    [Range(1f, 20f)] public float lerpSpeed = 10f;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Vector3 targetScale;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        baseScale = rectTransform.localScale;
        targetScale = baseScale;
    }

    private void Update()
    {
        // unscaled time so the hover still animates while the game is paused
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            lerpSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // don't pop when the button is disabled (e.g. Multiplayer before sign-in)
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable) return;

        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
    }

    private void OnDisable()
    {
        // snap back so the button never stays stuck in the enlarged state
        if (rectTransform != null)
            rectTransform.localScale = baseScale;
    }
}
