using MelonLoader;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static NOTFGT.FLZ_Common.GUI.Screens.Logic.UIScreen;

namespace NOTFGT.FLZ_Common.GUI.Screens.Logic
{
    internal abstract class UIScreen(ScreenType type) : UIElement
    {
        internal enum ScreenType
        {
            Cheats,
            RoundLoader,
            Log,
            Credits
        }

        internal ScreenType Type { get; } = type;
        internal Button ScreenTab { get; private set; }
        internal GameObject ScreenContainer { get; private set; }

        internal override void Initialize()
        {
            var t = GetType();

            ScreenTab = GUI.ScreensCache.FirstOrDefault(x => x.name == $"{GUIManager.TAB_PREFIX}_{Type}").GetComponent<Button>();
            ScreenContainer = GUI.ScreensCache.FirstOrDefault(x => x.name == $"{GUIManager.SCREEN_PREFIX}_{Type}").gameObject;

            ScreenTab.onClick.AddListener(new Action(() => { GUI.ToggleScreen(Type); }));

            GUI.Reference(GUI.GetFieldsOf<GUIReferenceAttribute>(t), this);

            ScreenContainer.gameObject.SetActive(false);

            MelonLogger.Msg($"[{t.Name}] Screen initalized");
        }

        internal abstract void CreateScreen();

        internal static void CleanupScreen(Transform screen, bool includeOnlyActive)
        {
            for (int i = screen.childCount - 1; i >= 0; i--)
            {
                var child = screen.GetChild(i);
                if (!includeOnlyActive || child.gameObject.activeSelf)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

    }
}
