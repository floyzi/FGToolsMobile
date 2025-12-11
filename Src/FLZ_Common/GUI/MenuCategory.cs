using Il2CppTMPro;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace NOTFGT.FLZ_Common.GUI
{
    internal class MenuCategory : MonoBehaviour
    {
        internal string Category;
        List<GameObject> Entries;
        internal bool KeepEntriesHidden;
        internal bool AtLeastOneEntryVisible => Entries != null && Entries.Any(x => x.activeInHierarchy);

        internal void Create(string category)
        {
            Category = category;
            Entries = [];

            var headerText = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            var expBtn = gameObject.GetComponentInChildren<Button>();
            expBtn.gameObject.AddComponent<ElementSFX>().SetSounds(new(Constants.Click));
           
            var ico = gameObject.GetComponentsInChildren<Image>()[1]; //not a good way of doing this

            FLZ_GUIExtensions.SetupFont(headerText, Constants.TMPFontTitanOne, "PinkOutline");

            headerText.gameObject.AddComponent<LocalizedStr>().Setup(category, prefix: "— ");

            expBtn.onClick.AddListener(new Action(() =>
            {
                KeepEntriesHidden = !KeepEntriesHidden;
                ico.sprite = KeepEntriesHidden ? FLZ_ToolsManager.Instance.GUIUtil.SpriteExpandMore : FLZ_ToolsManager.Instance.GUIUtil.SpriteExpandLess;

                foreach (var e in Entries)
                    e.SetActive(!KeepEntriesHidden);
            }));

            gameObject.SetActive(false);
        }

        internal void AddEntry(GameObject entry) => Entries.Add(entry);
    }
}
