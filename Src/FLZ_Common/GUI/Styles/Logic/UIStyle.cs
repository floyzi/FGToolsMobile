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
        protected override void Initialize()
        {
            var t = GetType();

            CoreObject = GUI.StylesCache.FirstOrDefault(x => x.name == $"{GUIManager.STYLE_PREFIX}_{Type}").gameObject;

            GUI.Reference(GUI.GetFieldsOf<GUIReferenceAttribute>(t), this);

            IsActive = false;
            StateChangeCallback = StateChange;

            MelonLogger.Msg($"[{t.Name}] Style initalized");
        }

        internal abstract void CreateStyle();
    }
}
