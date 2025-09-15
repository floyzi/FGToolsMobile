using MelonLoader;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NOTFGT.Localization
{
    public static class LocalizationManager
    {
        public class LangEntry
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        static List<LangEntry> LangEntries =[];
        const string linkDef = @"\{ref:(.*?)\}";

        public static void Setup()
        {
            var path = Path.Combine(Application.persistentDataPath, NOTFGTools.AssetsDir, "text.json");
            LangEntries = JsonConvert.DeserializeObject<List<LangEntry>>(File.ReadAllText(path));
        }

        public static string LocalizedString(string key, object[] format = null)
        {
            var value = LangEntries.Find(x => x.Key == key);

            if (value == null)
                return $"MISSING: {key}";

            string result = value.Value;

            foreach (Match match in Regex.Matches(result, linkDef))
            {
                var refKey = match.Groups[1].Value;
                var value_2 = LangEntries.Find(x => x.Key == refKey);

                if (value_2 != null)
                    result = result.Replace(match.Value, value_2.Value);
                else
                    result = result.Replace(match.Value, $"MISSING: {refKey}");
            }

            if (format != null)
            {
                int waitingForFormat = Regex.Matches(result, @"\{\d+\}").Count;
                if (format.Length == waitingForFormat)
                    result = string.Format(result, format);
                else
                    MelonLogger.Warning($"Tried to format str {key} with {format.Length} entries while string expected {waitingForFormat}");
            }
            return result;
        }
    }
}
