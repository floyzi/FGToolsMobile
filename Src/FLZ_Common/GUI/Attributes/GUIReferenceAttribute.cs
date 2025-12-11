namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    /// <summary>
    /// Use this attribute to assign object from bundle to field
    /// </summary>
    /// <param name="target">Name of object in UI Bundle (Names should be unique)</param>
    [AttributeUsage(AttributeTargets.Field)]
    internal class GUIReferenceAttribute(string target) : NameAttribute(target)
    {
    }
}
