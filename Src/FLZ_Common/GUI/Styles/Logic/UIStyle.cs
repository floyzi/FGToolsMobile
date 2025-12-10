using MelonLoader;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NOTFGT.FLZ_Common.GUI.Styles.Logic.UIStyle;

namespace NOTFGT.FLZ_Common.GUI.Styles.Logic
{
    internal abstract class UIStyle(StyleType type) : UIElement
    {
        internal enum StyleType
        {
            Default,
            Gameplay,
            Hidden,
            Repair,
        }

        internal StyleType Type { get; } = type;
        internal GameObject StyleContainer { get; private set; }

        protected override void Initialize()
        {
            var t = GetType();

            StyleContainer = GUI.StylesCache.FirstOrDefault(x => x.name == $"{GUIManager.STYLE_PREFIX}_{Type}").gameObject;

            GUI.Reference(GUI.GetFieldsOf<GUIReferenceAttribute>(t), this);

            StyleContainer.gameObject.SetActive(false);

            MelonLogger.Msg($"[{t.Name}] Style initalized");
        }

        internal abstract void CreateStyle();
    }
}
