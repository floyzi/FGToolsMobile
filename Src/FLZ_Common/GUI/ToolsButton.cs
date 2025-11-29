using Il2CppCommon.Input.Vibration;
using Il2CppFG.Common.AI;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSerilog.Events;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOTFGT.FLZ_Common.GUI
{
    public class ToolsButton : MonoBehaviour
    {
        RectTransform Rt;
        Canvas Canv;
        RectTransform CanvRt;
        EventTrigger Trigger;
        object DelayCor;
        internal Action onClick;
        
        bool Dragging;
        bool PointerDown;

        void Awake()
        {
            Rt = GetComponent<RectTransform>();
            Canv = GetComponentInParent<Canvas>();
            if (Canv != null)
                CanvRt = Canv.GetComponent<RectTransform>();
            else
                Destroy(this);

            Trigger = gameObject.AddComponent<EventTrigger>();

            RegEvent(EventTriggerType.PointerDown, new Action<BaseEventData>((data) =>
            {
                PointerDown = true;
                DelayCor = MelonCoroutines.Start(DragDelay(0.15f));
            }));

            RegEvent(EventTriggerType.Drag, new Action<BaseEventData>((data) =>
            {
                if (!Dragging) return;
                if (Canv == null) return;

                var mb = data.Cast<PointerEventData>();
                Rt.anchoredPosition += mb.delta / Canv.scaleFactor;

                if (CanvRt == null) return;

                var pos = Rt.anchoredPosition;

                pos.x = Mathf.Clamp(pos.x, 0 + Rt.sizeDelta.x * Rt.pivot.x - CanvRt.rect.size.x / 2f, CanvRt.rect.size.x - Rt.sizeDelta.x * (1 - Rt.pivot.x) - CanvRt.rect.size.x / 2f);
                pos.y = Mathf.Clamp(pos.y, 0 + Rt.sizeDelta.y * Rt.pivot.y - CanvRt.rect.size.y / 2f, CanvRt.rect.size.y - Rt.sizeDelta.y * (1 - Rt.pivot.y) - CanvRt.rect.size.y / 2f);

                Rt.anchoredPosition = pos;
            }));

            RegEvent(EventTriggerType.EndDrag, new Action<BaseEventData>((data) =>
            {
                Dragging = false;
            }));

            RegEvent(EventTriggerType.PointerUp, new Action<BaseEventData>((data) =>
            {
                PointerDown = false;
                MelonCoroutines.Stop(DelayCor);
                if (Dragging) return;
                onClick?.Invoke();
            }));
        }

        void RegEvent(EventTriggerType type, Action<BaseEventData> act)
        {
            var a = new EventTrigger.Entry 
            { 
                eventID = type,
            };
            a.callback.AddListener(act);
            Trigger.triggers.Add(a);
        }

        IEnumerator DragDelay(float d)
        {
            var t = 0f;

            while (t < d)
            {
                if (!PointerDown) yield break;
                t += Time.deltaTime;
                yield return null;
            }

            FLZ_AndroidExtensions.Vibrate(10);
            Dragging = true;
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }

    }
}
