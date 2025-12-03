using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    /// <summary>
    /// Use this attribute to assign object from bundle to field
    /// </summary>
    /// <param name="target">Name of object in UI Bundle (Names should be unique)</param>
    [AttributeUsage(AttributeTargets.Field)]
    internal class GUIReferenceAttribute : NameAttribute
    {
        public GUIReferenceAttribute(string target) : base(target)
        {
        }
    }
}
