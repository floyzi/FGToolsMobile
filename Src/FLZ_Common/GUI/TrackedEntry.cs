using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Il2CppRewired.Demos.GamepadTemplateUI.GamepadTemplateUI;
using static NOTFGT.FLZ_Common.GUI.ToolsMenu;

namespace NOTFGT.FLZ_Common.GUI
{
    internal class TrackedEntry : MonoBehaviour
    {
        internal static List<TrackedEntry> TrackedEntries = [];

        internal Action<object> OnEntryUpdated;
        MenuEntry AttachedEntry;
        object LastKnownValue;
        Selectable UIElement;
        bool IsInteractableTracked;
        InteractableConfig InteractableConfig;

        internal void Create(MenuEntry entry, Selectable uiElement)
        {
            AttachedEntry = entry;
            UIElement = uiElement;  

            LastKnownValue = AttachedEntry.InitialValue;
            AttachedEntry.OnEntryChanged += SetEntry;

            InteractableConfig = AttachedEntry?.AdditionalConfig as InteractableConfig;
            IsInteractableTracked = InteractableConfig != null;

            if (AttachedEntry != null && AttachedEntry.EntryType != MenuEntry.Type.Button) 
                TrackedEntries.Add(this);
        }

        void SetEntry(object newVal) 
        {
            if (!Equals(newVal, LastKnownValue))
            {
                LastKnownValue = newVal;
                OnEntryUpdated(newVal);
            }
        }

        internal void Refresh()
        {
            if (IsInteractableTracked)
            {
                //var cState = UIElement.interactable;
                var nState = InteractableConfig.InteractableCondition == null || InteractableConfig.InteractableCondition();
                UIElement.interactable = nState;
            }

            SetEntry(AttachedEntry.GetValue());
        }

        void OnDestroy()
        {
            AttachedEntry.OnEntryChanged -= SetEntry;
            TrackedEntries.Remove(this);
        }
    }
}
