using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.GUI.Logic
{
    internal abstract class UIElement
    {
        internal GUIManager GUI => FLZ_ToolsManager.Instance.GUIUtil;

        internal abstract void Initialize();
    }
}
