using Harmony;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Il2CppMono.Security.X509.X520;
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
        BaseEntryConfig EntryConfig;
        bool IsActiveTracked;
        MenuCategory Owner;

        internal void Create(MenuEntry entry, Selectable uiElement, MenuCategory owner)
        {
            AttachedEntry = entry;
            UIElement = uiElement;  
            Owner = owner;

            LastKnownValue = AttachedEntry.InitialValue;
            AttachedEntry.OnEntryChanged += SetEntry;

            EntryConfig = AttachedEntry?.AdditionalConfig as BaseEntryConfig;
            if (EntryConfig != null)
            {
                IsInteractableTracked = EntryConfig.InteractableCondition != null;
                IsActiveTracked = EntryConfig.DisplayCondition != null;
            }

            Owner?.AddEntry(gameObject);

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
                var nState = EntryConfig.InteractableCondition == null || EntryConfig.InteractableCondition();
                UIElement.interactable = nState;
            }

            if (IsActiveTracked)
            {
                var nState = EntryConfig.DisplayCondition == null || EntryConfig.DisplayCondition();
                if (!nState)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    if (!Owner.KeepEntriesHidden)
                        gameObject.SetActive(true);
                }
            }

            if (Owner.AtLeastOneEntryVisible)
                Owner.gameObject.SetActive(true);

            SetEntry(AttachedEntry.GetValue());
        }

        void OnDestroy()
        {
            AttachedEntry.OnEntryChanged -= SetEntry;
            TrackedEntries.Remove(this);
        }
    }
}
