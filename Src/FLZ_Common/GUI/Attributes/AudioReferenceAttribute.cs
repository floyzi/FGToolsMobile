using static NOTFGT.FLZ_Common.GUI.ElementSFX;

namespace NOTFGT.FLZ_Common.GUI.Attributes
{
    /// <summary>
    /// Use this attribute to setup audio on UI element. Audio being played via <see cref="ElementSFX"/>
    /// </summary>
    /// <param name="clickSfx">Audio that plays when you click on this element</param>
    /// <param name="dragSfx">Audio that plays when you drag this element (only used by sliders)</param>
    [AttributeUsage(AttributeTargets.Field)]
    internal class AudioReferenceAttribute(string pClickSfx = null, string dragSfx = null) : Attribute
    {
        internal SfxData Data { get; } = new()
        {
            Click = pClickSfx,
            Drag = dragSfx,
        };
    }
}
