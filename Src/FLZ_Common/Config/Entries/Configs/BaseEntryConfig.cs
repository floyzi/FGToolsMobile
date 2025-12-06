using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.Config.Entries.Configs
{
    internal interface IEntryConfig { }
    internal abstract class BaseEntryConfig : IEntryConfig
    {
        public Func<bool> InteractableCondition;
        public Func<bool> DisplayCondition;
        public Func<bool> SaveInConfigCondition;
    }
}
