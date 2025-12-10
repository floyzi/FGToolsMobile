using Il2Cpp;
using Il2CppFG.Common;
using Il2CppInterop.Runtime;
using MelonLoader;
using NOTFGT.FLZ_Common.Config.Entries.Configs;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOTFGT.FLZ_Common.GUI
{
    internal class ElementSFX : MonoBehaviour
    {
        internal struct SfxData
        {
            internal string Click;
            internal string Drag;

            internal SfxData(string pClickSfx = null, string dragSfx = null)
            {
                Click = pClickSfx;
                Drag = dragSfx;
            }

            internal readonly bool IsValid()
            {
                return !string.IsNullOrEmpty(Click) || !string.IsNullOrEmpty(Drag); 
            }
        }

        EventTrigger Trigger;
        Selectable UIElement;

        SfxData Data;

        Slider Slider;
        float LastSliderVal;
        float SliderStep;

        //i personally don't like this but i couldn't find a better way
        static Dictionary<string, SfxData> KnownPrefabs = [];
        internal static bool RegisterPrefab(Selectable prefab, SfxData data)
        {
            if (prefab == null || !data.IsValid()) return false;
            prefab.name += $"_{prefab.GetHashCode()}";

            return KnownPrefabs.TryAdd(prefab.name, data);
        }

        internal void Setup(IEntryConfig cfg = null)
        {
            if (cfg != null && cfg is SliderConfig sConf)
                SliderStep = sConf.ValueType == typeof(float) ? 0.1f : 1f;
            else
                SliderStep = 1f;
        }

        bool Init()
        {
            if (UIElement != null) return true;

            Trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            UIElement = gameObject.GetComponent<Selectable>();
            Slider = UIElement.TryCast<Slider>();

            return UIElement != null;
        }

        void Awake()
        {
            if (!Init())
            {
                MelonLogger.Error($"[{GetType().Name}].Awake() Initialization failed on {name}");
                return;
            }

            if (KnownPrefabs.TryGetValue(name.Replace("(Clone)", "").Trim(), out var data))
                Data = data;

            if (Data.IsValid())
                SetSounds(Data);
        }

        internal void SetSounds(SfxData dat)
        {
            if (!Init())
            {
                MelonLogger.Error($"[{GetType().Name}].SetSounds() Initialization failed on {name}");
                return;
            }

            Data = dat;
            Trigger.triggers.Clear();

            if (!string.IsNullOrEmpty(dat.Click))
            {
                RegEvent(EventTriggerType.PointerClick, new Action<BaseEventData>((data) =>
                {
                    if (!UIElement.interactable) return;
                    AudioManager.PlayOneShot(dat.Click);
                }));
            }

            if (!string.IsNullOrEmpty(dat.Drag) && Slider != null)
            {
                RegEvent(EventTriggerType.BeginDrag, new Action<BaseEventData>((data) =>
                {
                    if (!UIElement.interactable) return;
                    AudioManager.PlayOneShot(dat.Drag);
                }));

                RegEvent(EventTriggerType.Drag, new Action<BaseEventData>((data) =>
                {
                    if (!UIElement.interactable) return;

                    var val = Slider.value;

                    if (Mathf.Abs(val - LastSliderVal) >= SliderStep)
                    {
                        LastSliderVal = val;
                        AudioManager.PlayOneShot(dat.Drag);
                    }
                }));

                RegEvent(EventTriggerType.EndDrag, new Action<BaseEventData>((data) =>
                {
                    if (!UIElement.interactable) return;
                    AudioManager.PlayOneShot(dat.Drag);
                }));
            }
            else
            {
                //this (somewhat) fixes scrolling in scrollviews, it still a bad solution but since this is unity ui we have what we have!

                RegEvent(EventTriggerType.BeginDrag, (data) =>
                {
                    if (transform.parent == null) return;

                    if (UIElement.currentSelectionState != Selectable.SelectionState.Disabled)
                    {
                        UIElement.interactable = false;
                        UIElement.targetGraphic.CrossFadeColor(UIElement.colors.normalColor, 0, true, true);
                    }

                    transform.parent.GetComponentInParent<IBeginDragHandler>()?.OnBeginDrag(data.Cast<PointerEventData>());
                });

                RegEvent(EventTriggerType.Drag, (data) =>
                {
                    if (transform.parent == null) return;

                    //this SUCKS
                    if (UIElement.currentSelectionState != Selectable.SelectionState.Disabled)
                    {
                        UIElement.interactable = false;
                        UIElement.targetGraphic.CrossFadeColor(UIElement.colors.normalColor, 0, true, true);
                    }

                    transform.parent.GetComponentInParent<IDragHandler>()?.OnDrag(data.Cast<PointerEventData>());
                });

                RegEvent(EventTriggerType.EndDrag, (data) =>
                {
                    if (transform.parent == null) return;

                    UIElement.interactable = true;

                    transform.parent.GetComponentInParent<IEndDragHandler>()?.OnEndDrag(data.Cast<PointerEventData>());
                });
            }
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
    }
}
