using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Styles.Logic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using Il2Cpp;

namespace NOTFGT.FLZ_Common.GUI.Styles
{
    internal class HiddenStyle : UIStyle
    {
        [GUIReference("ToolsButton")] Button FGTButton;

        RectTransform Rt;
        Canvas Canv;
        RectTransform CanvRt;
        EventTrigger Trigger;
        object DelayCor;
        Action onClick;

        bool Dragging;
        bool PointerDown;

        internal HiddenStyle() : base(StyleType.Hidden)
        {
            Initialize();
        }

        internal override void CreateStyle()
        {
            SetupButton();
        }

        void SetupButton()
        {
            Rt = FGTButton.GetComponent<RectTransform>();
            Canv = FGTButton.transform.parent.GetComponentInParent<Canvas>();
            if (Canv != null)
                CanvRt = Canv.GetComponent<RectTransform>();

            Trigger = FGTButton.gameObject.AddComponent<EventTrigger>();

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
                var wasDragging = Dragging;
                PointerDown = false;
                MelonCoroutines.Stop(DelayCor);
                Dragging = false;
                if (wasDragging) return;
                onClick?.Invoke();
            }));

            onClick = new(() =>
            {
                GUI.ToggleGUI(GUIManager.UIState.Active);
            });
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
            FGTButton.transform.GetChild(0).gameObject.SetActive(false);
        }

        protected override void StateChange(bool isActive, bool wasActive)
        {
            if (isActive && !wasActive)
                AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericBack);
        }
    }
}
