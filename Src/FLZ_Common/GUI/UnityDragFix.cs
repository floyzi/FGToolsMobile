using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOTFGT.FLZ_Common.GUI
{
    //thank you brave reddit user for fixing shit that needs to work without fixes at the first place
    //https://reddit.com/r/Unity3D/comments/10atw9e/comment/m3w8itw/
    internal class UnityDragFix : MonoBehaviour
    {
        internal ScrollRect _ScrollRect;
        EventTrigger Trigger;
        void Awake()
        {
            Trigger = gameObject.AddComponent<EventTrigger>();

            RegEvent(EventTriggerType.BeginDrag, new(OnBeginDrag));
            RegEvent(EventTriggerType.Drag, new(OnDrag));
            RegEvent(EventTriggerType.EndDrag, new(OnEndDrag));
        }

        void OnBeginDrag(BaseEventData eventData) => _ScrollRect.OnBeginDrag(eventData.Cast<PointerEventData>());
        void OnDrag(BaseEventData eventData) => _ScrollRect.OnDrag(eventData.Cast<PointerEventData>());
        void OnEndDrag(BaseEventData eventData) => _ScrollRect.OnEndDrag(eventData.Cast<PointerEventData>());
        void RegEvent(EventTriggerType type, Action<BaseEventData> act)
        {
            var a = new EventTrigger.Entry
            {
                eventID = type,
            };
            a.callback.AddListener(act);
            Trigger.triggers.Add(a);
        }
    }
}
