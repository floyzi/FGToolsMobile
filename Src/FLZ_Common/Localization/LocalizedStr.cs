using Il2CppTMPro;
using MelonLoader;
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
                MelonLogger.Error($"[{GetType().Name}] Lacks TextMeshProUGUI on gameobject!");
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

            if (!string.IsNullOrEmpty(Prefix) && !string.IsNullOrEmpty(Text.text))
                Text.text = $"{Prefix}{Text.text}";
        }

        void OnDestroy()
        {
            LocalizeStrings -= LocalizeString;
        }

    }
}
