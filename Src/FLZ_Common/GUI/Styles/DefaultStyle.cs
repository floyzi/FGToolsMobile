using Il2Cpp;
using Il2CppDG.Tweening;
using Il2CppTMPro;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Styles.Logic;
using NOTFGT.FLZ_Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppSystem.Globalization.TimeSpanFormat;
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

        [GUIReference("Main_Pattern")] readonly Image Pattern;

        internal DefaultStyle() : base(StyleType.Default)
        {
            Initialize();
        }


        Vector3 InitialPos;
        internal override void CreateStyle()
        {
            HideButton.onClick.AddListener(new Action(() => { GUI.ToggleGUI(UIState.Hidden); }));

            var name = UnityEngine.Random.value > 0.001f ? Constants.DefaultName : LocalizationManager.LocalizedString("old_branding");
            Header.text = $"{name} V{Core.BuildInfo.Version}";
            Slogan.text = $"{Constants.Description}";
            Pattern.gameObject.AddComponent<UI_ScrollUvs>();

            InitialPos = CoreObject.transform.localPosition;
        }

        internal static void RefreshEntries()
        {
            if (GUI == null || !GUI.IsUIActive) return;
            foreach (var e in TrackedEntry.TrackedEntries) e.Refresh();
        }

        protected override void StateChange(bool isActive, bool wasActive)
        {
            var rect = CoreObject.GetComponent<RectTransform>();

            if (isActive && !wasActive)
            {
                rect.anchoredPosition = InitialPos;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + -600f, rect.anchoredPosition.y);

                AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericAcceptBold);
                rect.DOAnchorPos(InitialPos, 0.15f).SetEase(Ease.OutQuad);
            }
        }
    }
}
