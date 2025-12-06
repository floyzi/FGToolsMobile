using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.Config.Entries.Configs
{
    internal class FieldConfig : BaseEntryConfig
    {
        public Type ValueType;
        public int CharacterLimit;

        public FieldConfig(Type t, int charLimit = -1, Func<bool> condition = null, Func<bool> dispCondition = null, Func<bool> saveInConfCondition = null)
        {
            ValueType = t;
            CharacterLimit = charLimit;
            InteractableCondition = condition;
            DisplayCondition = dispCondition;
            SaveInConfigCondition = saveInConfCondition;
        }
    }
}
