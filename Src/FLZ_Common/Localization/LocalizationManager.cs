using MelonLoader;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NOTFGT.FLZ_Common.Localization
{
    public static class LocalizationManager
    {
        static Dictionary<string, string> LangEntries =[];
        const string linkDef = @"\{ref:(.*?)\}";

        public static void Setup()
        {
            var path = Path.Combine(Application.persistentDataPath, Core.AssetsDir, "text.json");
            if (!File.Exists(path)) return;
            LangEntries = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
        }

        public static string LocalizedString(string key, object[] format = null)
        {
            if (!LangEntries.TryGetValue(key, out var value))
                return $"MISSING: {key}";

            string result = value;

            foreach (Match match in Regex.Matches(result, linkDef))
            {
                var refKey = match.Groups[1].Value;
                var value_2 = LangEntries[refKey];

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
    }
}
