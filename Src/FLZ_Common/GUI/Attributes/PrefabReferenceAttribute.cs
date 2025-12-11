namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    /// <summary>
    /// Use this attribute to reference a prefab from bundle.
    /// </summary>
    /// <param name="target">Name of prefab that is in bundle (Names should be unique)</param>
    [AttributeUsage(AttributeTargets.Field)]
    internal class PrefabReferenceAttribute(string target) : NameAttribute(target)
    {
    }
}
