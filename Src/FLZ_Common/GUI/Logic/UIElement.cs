using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.GUI.Logic
{
    internal abstract class UIElement
    {
        protected GUIManager GUI => FLZ_ToolsManager.Instance.GUIUtil;
        protected abstract void Initialize();
    }
}
