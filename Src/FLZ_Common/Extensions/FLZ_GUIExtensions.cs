using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

namespace NOTFGT.FLZ_Common.Extensions
{
    internal static class FLZ_GUIExtensions
    {
        static List<Material> Materials;
        static List<TMP_FontAsset> FontAssets;

        internal static void ConfigureFonts()
        {
            Materials = [.. Resources.FindObjectsOfTypeAll<Material>()];
            FontAssets = [.. Resources.FindObjectsOfTypeAll<TMP_FontAsset>()];

            Materials.ForEach(x => GameObject.DontDestroyOnLoad(x));
            FontAssets.ForEach(x => GameObject.DontDestroyOnLoad(x));
        }

        internal static void SetupFont(TextMeshProUGUI tmpText, string fontName, string materialName)
        {
            if (Materials == null || FontAssets == null)
                ConfigureFonts();

            if (tmpText == null || string.IsNullOrEmpty(fontName) || string.IsNullOrEmpty(materialName))
            {
                MelonLogger.Warning($"Skipping setup of potential font \"{fontName}\" as it's not valid");
                return;
            }

            tmpText.fontMaterial = Materials.Find(x => x.name.Contains(materialName) && !x.name.Contains("Instance"));
            tmpText.font = FontAssets.Find(x => x.name.StartsWith(fontName));

            if (tmpText.fontMaterial == null || tmpText.font == null)
            {
                tmpText.fontMaterial = Materials.Find(x => x.name == Constants.TMPFontMaterialFallback);
                tmpText.font = FontAssets.Find(x => x.name == Constants.TMPFontFallback);
            }

            tmpText.fontMaterial.hideFlags = HideFlags.HideAndDontSave;
            tmpText.font.hideFlags = HideFlags.HideAndDontSave;

            tmpText.enableWordWrapping = true;
            tmpText.UpdateMaterial();
            tmpText.UpdateFontAsset();
        }

    }
}
