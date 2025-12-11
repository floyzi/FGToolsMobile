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
