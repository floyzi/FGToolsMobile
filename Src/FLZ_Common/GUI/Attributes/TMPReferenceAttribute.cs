using Il2CppTMPro;
using static NOTFGT.FLZ_Common.GUI.Attributes.TMPReferenceAttribute;

namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    /// <summary>
    /// Use this attribute to setup Fall Guys font on <see cref="TextMeshProUGUI"/> object from bundle.
    /// </summary>
    /// <param name="targetMaterial">Material that will be assigned to font. Find material names via UE on Desktop version of the game</param>
    [AttributeUsage(AttributeTargets.Field)]
    internal class TMPReferenceAttribute(FontType targetFont, string targetMaterial) : Attribute
    {
        public enum FontType
        {
            TitanOne,
            AsapBold
        }

        internal string FontName { get; } = targetFont == FontType.TitanOne ? Constants.TMPFontTitanOne : Constants.TMPFontAsapBold;
        internal string MaterialName { get; } = targetMaterial;
    }
}
