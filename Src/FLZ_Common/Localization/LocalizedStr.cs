using Il2CppTMPro;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace NOTFGT.FLZ_Common.Localization
{
    internal class LocalizedStr : MonoBehaviour
    {
        internal static Action LocalizeStrings;

        TextMeshProUGUI Text;
        string ContentID;
        string UnlocalizedValue;
        object[] Formatting;
        string Prefix;

        internal bool Setup(string idOverride = null, object[] formatting = null, string prefix = null)
        {
            Text = gameObject.GetComponent<TextMeshProUGUI>();
            Formatting = formatting;
            Prefix = prefix;

            if (Text == null)
            {
                MelonLogger.Error($"[{GetType().Name}] Lacks TextMeshProUGUI !");
                GameObject.Destroy(this);
                return false;
            }

            var hasOverride = !string.IsNullOrEmpty(idOverride);
            if (!hasOverride && !Text.text.Contains('$'))
            {
                GameObject.Destroy(this);
                return false;
            }

            if (!hasOverride)
                UnlocalizedValue = Text.text;

            ContentID = !hasOverride ? Text.text[1..] : idOverride;

            LocalizeStrings += LocalizeString;
            LocalizeString();

            return !string.IsNullOrEmpty(Text.text);
        }

        void LocalizeString()
        {
            if (gameObject == null) return;

            if (string.IsNullOrEmpty(UnlocalizedValue))
            {
                Text?.SetText(LocalizationManager.LocalizedString(ContentID, Formatting));
            }
            else
            {
                Text.SetText(Regex.Replace(UnlocalizedValue, @"\$(\w+)", match =>
                {
                    return LocalizationManager.LocalizedString(match.Groups[1].Value, Formatting);
                }));
            }

            var hasText = !string.IsNullOrEmpty(Text.text);

            if (hasText && !string.IsNullOrEmpty(Prefix))
                Text.text = $"{Prefix}{Text.text}";

            gameObject.SetActive(hasText);
        }

        internal void Cleanup() => LocalizeStrings -= LocalizeString;

        void OnDestroy()
        {
            Cleanup();
        }

    }
}
