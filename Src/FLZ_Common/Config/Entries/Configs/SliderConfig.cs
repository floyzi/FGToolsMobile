using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.Config.Entries.Configs
{
    internal class SliderConfig : BaseEntryConfig
    {
        public Type ValueType;
        public float MinValue;
        public float MaxValue;

        public SliderConfig(Type t, float min = -1, float max = -1, Func<bool> intCondition = null, Func<bool> dispCondition = null, Func<bool> saveInConfCondition = null)
        {
            ValueType = t;
            MinValue = min;
            MaxValue = max;
            InteractableCondition = intCondition;
            DisplayCondition = dispCondition;
            SaveInConfigCondition = saveInConfCondition;
        }
    }
}
