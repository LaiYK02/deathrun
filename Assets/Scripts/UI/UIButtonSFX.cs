using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSFX :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISubmitHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (button == null)
            return;

        if (!button.interactable)
            return;

        if (UISoundManager.Instance == null)
            return;

        UISoundManager.Instance.PlayHoverSound();
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (button == null)
            return;

        if (!button.interactable)
            return;

        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        if (UISoundManager.Instance == null)
            return;

        UISoundManager.Instance.PlayClickSound();
    }

    public void OnSubmit(
        BaseEventData eventData)
    {
        if (button == null)
            return;

        if (!button.interactable)
            return;

        if (UISoundManager.Instance == null)
            return;

        UISoundManager.Instance.PlayClickSound();
    }
}