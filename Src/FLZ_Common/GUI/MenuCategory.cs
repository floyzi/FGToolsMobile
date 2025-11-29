using Il2CppInterop.Runtime.InteropTypes.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NOTFGT.FLZ_Common.GUI.ToolsMenu;

namespace NOTFGT.FLZ_Common.GUI
{
    internal class MenuCategory : MonoBehaviour
    {
        internal string Name;
        List<GameObject> Entries;
        internal bool AtLeastOneEntryVisible => Entries != null && Entries.Any(x => x.gameObject.activeInHierarchy);

        internal void Create(string name)
        {
            Name = name;
            Entries = [];

            gameObject.SetActive(false);
        }

        internal void AddEntry(GameObject entry) => Entries.Add(entry);
    }
}
