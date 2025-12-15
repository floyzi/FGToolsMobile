using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.Config.Entries.Configs
{
    internal class ButtonConfig : BaseEntryConfig
    {
        public ButtonConfig(Func<bool> condition = null, Func<bool> dispCondition = null, Func<bool> saveInConfCondition = null)
        {
            InteractableCondition = condition;
            DisplayCondition = dispCondition;
            SaveInConfigCondition = saveInConfCondition;
        }
    }
}
