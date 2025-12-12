using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace NOTFGT.FLZ_Common.Localization
{
    public static class LocalizationManager
    {
        static Dictionary<string, string> InitialLocale =[];
        static Dictionary<string, string> SelectedLocale = [];
        static Dictionary<string, string> LangCodes = [];

        const string LINK_DEF = @"\{ref:(.*?)\}";

        public static void Setup()
        {
            var path = Path.Combine(Core.LocalizationDir, "en.json");
            if (!File.Exists(path))
            {
                MelonLogger.Warning("Fallback localization is missing!");
                return;
            }

            InitialLocale = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            if (File.Exists(Core.LangCodes))
                LangCodes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Core.LangCodes));

            LoadLocale(FLZ_ToolsManager.Instance.Config.SavedConfig.Locale);
        }

        public static bool LoadLocale(string loc)
        {
            var path = Path.Combine(Core.LocalizationDir, $"{loc}.json");
            if (!File.Exists(path))
            {
                MelonLogger.Warning($"Locale {loc} can't be found.");
                return false;
            }

            SelectedLocale.Clear();
            SelectedLocale = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

            LocalizedStr.LocalizeStrings?.Invoke();

            return true;
        }

        public static string LocalizedString(string key, object[] format = null)
        {
            if (!SelectedLocale.TryGetValue(key, out var value) && !InitialLocale.TryGetValue(key, out value))
                return $"MISSING: {key}";

            string result = value;

            foreach (Match match in Regex.Matches(result, LINK_DEF))
            {
                var refKey = match.Groups[1].Value;

                if (!SelectedLocale.TryGetValue(refKey, out var value_2))
                    InitialLocale.TryGetValue(refKey, out value_2);

                if (!string.IsNullOrEmpty(value_2))
                    result = result.Replace(match.Value, value_2);
                else
                    result = result.Replace(match.Value, $"MISSING: {refKey}");
            }

            if (format != null)
            {
                int waitingForFormat = Regex.Matches(result, @"\{\d+\}").Cast<Match>().GroupBy(m => m.Value).Count();
                if (format.Length == waitingForFormat)
                    result = string.Format(result, format);
                else
                    MelonLogger.Warning($"Tried to format str {key} with {format.Length} entries while string expected {waitingForFormat}");
            }
            return result;
        }

        public static void ConfigureDropdown(TMP_Dropdown locDropdown)
        {
            if (locDropdown == null) return;

            var sLoc = FLZ_ToolsManager.Instance.Config.SavedConfig.Locale;
            var files = Directory.GetFiles(Core.LocalizationDir);

            for (int i = 0; i < files.Length; i++)
            {
                var locFile = files[i];
                var unloc = GetLocaleName(Path.GetFileNameWithoutExtension(locFile));

                locDropdown.options.Add(new(unloc));

                if (locDropdown.options[i].text == GetLocaleName(sLoc))
                    locDropdown.value = i;
            }

            locDropdown.onValueChanged.AddListener(new Action<int>((val =>
            {
                var loc = GetLocaleCode(locDropdown.options[val].text);
                var conf = FLZ_ToolsManager.Instance.Config;
                if (LoadLocale(loc))
                {
                    conf.SavedConfig.Locale = loc;
                    conf.SaveConfig();
                    AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericAcceptBold);
                }
            })));
        }

        static string GetLocaleName(string code)
        {
            if (LangCodes.TryGetValue(code, out var name))
                return name;

            return $"{code} [FIX FILE NAME!!]";
        }

        static string GetLocaleCode(string name)
        {
            return LangCodes.FirstOrDefault(x => x.Value == name).Key;
        }
    }
}
