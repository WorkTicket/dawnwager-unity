using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Wolfsun
{
    /// <summary>
    /// Tiny runtime helper that forwards pointer events to callbacks, so the code-built UI can
    /// react to hover, right-click and drag without an EventTrigger (which would swallow scroll events).
    /// </summary>
    public sealed class PointerRelay : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action Enter, Exit;
        public Action<PointerEventData> RightClick, BeginDrag, Drag, EndDrag;

        public void OnPointerEnter(PointerEventData eventData) { if (Enter != null) Enter(); }
        public void OnPointerExit(PointerEventData eventData) { if (Exit != null) Exit(); }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && RightClick != null) RightClick(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData) { if (BeginDrag != null) BeginDrag(eventData); }
        public void OnDrag(PointerEventData eventData) { if (Drag != null) Drag(eventData); }
        public void OnEndDrag(PointerEventData eventData) { if (EndDrag != null) EndDrag(eventData); }
    }
}
