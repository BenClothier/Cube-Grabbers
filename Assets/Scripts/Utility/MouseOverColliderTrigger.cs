using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MouseOverColliderTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEvent OnMouseEnter;
    public UnityEvent OnMouseExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnMouseEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnMouseExit?.Invoke();
    }
}
