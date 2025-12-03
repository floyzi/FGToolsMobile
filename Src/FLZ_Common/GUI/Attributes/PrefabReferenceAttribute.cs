using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    /// <summary>
    /// Use this attribute to reference a prefab from bundle.
    /// </summary>
    /// <param name="target">Name of prefab that is in bundle (Names should be unique)</param>
    [AttributeUsage(AttributeTargets.Field)]
    internal class PrefabReferenceAttribute : NameAttribute
    {
        public PrefabReferenceAttribute(string target) : base(target)
        {
        }
    }
}
