using Il2CppTMPro;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Styles.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using static NOTFGT.FLZ_Common.GUI.Attributes.TMPReferenceAttribute;
using static NOTFGT.FLZ_Common.GUI.GUIManager;

namespace NOTFGT.FLZ_Common.GUI.Styles
{
    internal class DefaultStyle : UIStyle
    {
        [GUIReference("HideButton")] readonly Button HideButton;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("HeaderTitle")] readonly TextMeshProUGUI Header;
        [TMPReference(FontType.AsapBold, "2.0_Shadow")]
        [GUIReference("HeaderSlogan")] readonly TextMeshProUGUI Slogan;

        internal DefaultStyle() : base(StyleType.Default)
        {
            Initialize();
        }

        internal override void CreateStyle()
        {
            HideButton.onClick.AddListener(new Action(() => { GUI.ToggleGUI(UIState.Hidden); }));

            Header.text = $"{Constants.DefaultName} V{Core.BuildInfo.Version}";
            Slogan.text = $"{Constants.Description}";
        }

        internal void RefreshEntries()
        {
            if (!GUI.IsUIActive) return;
            foreach (var e in TrackedEntry.TrackedEntries) e.Refresh();
        }
    }
}
