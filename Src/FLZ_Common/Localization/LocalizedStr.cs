using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NOTFGT.FLZ_Common.Localization
{
    internal class LocalizedStr : MonoBehaviour
    {
        internal static Action LocalizeStrings;

        TextMeshProUGUI Text;
        string ContentID;
        internal bool Setup(string idOverride = null)
        {
            Text = gameObject.GetComponent<TextMeshProUGUI>();
            if (Text == null)
            {
                MelonLogger.Error($"[{GetType().Name}] Lacks TextMeshProUGUI on gameobject!");
                GameObject.Destroy(this);
                return false;
            }

            var hasOverride = !string.IsNullOrEmpty(idOverride);
            if (!hasOverride && !Text.text.StartsWith('$'))
            {
                MelonLogger.Error($"[{GetType().Name}] Lacks configured string ID!");
                GameObject.Destroy(this);
                return false;
            }

            ContentID = !hasOverride ? Text.text[1..] : idOverride;

            LocalizeStrings += LocalizeString;
            LocalizeString();

            return !string.IsNullOrEmpty(Text.text);
        }

        void LocalizeString() => Text?.SetText(LocalizationManager.LocalizedString(ContentID));

        void OnDestroy()
        {
            LocalizeStrings -= LocalizeString;
        }

    }
}
