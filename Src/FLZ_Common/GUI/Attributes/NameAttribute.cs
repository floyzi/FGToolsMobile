using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    internal abstract class NameAttribute : Attribute
    {
        internal NameAttribute(string name)
        {
            Name = name;
        }

        internal string Name { get; }
    }
}
