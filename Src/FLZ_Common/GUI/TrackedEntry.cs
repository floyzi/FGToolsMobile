using Il2CppFGClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NOTFGT.FLZ_Common.GUI.ToolsMenu;

namespace NOTFGT.FLZ_Common.GUI
{
    internal class TrackedEntry : MonoBehaviour
    {
        internal Action<object> OnEntryUpdated;
        MenuEntry AttachedEntry;
        object LastKnownEntryValue;

        internal void Create(MenuEntry entry)
        {
            AttachedEntry = entry;
            LastKnownEntryValue = AttachedEntry.InitialValue;
        }

        void Update()
        {
            var v = AttachedEntry.GetValue();
            if (v != LastKnownEntryValue)
            {
                LastKnownEntryValue = v;
                OnEntryUpdated(v);
            }
        }
    }
}
