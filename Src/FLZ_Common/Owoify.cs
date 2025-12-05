using Il2CppFG.Common.CMS;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace NOTFGT.FLZ_Common
{
    //https://github.com/KieronQuinn/owoify/ :3
    internal static class Owoify
    {
        internal static Dictionary<int, string> Sources = [];

        readonly static string[] Prefixes = [
            "<3 ",
            "0w0 ",
            "H-hewwo?? ",
            "HIIII! ",
            "Haiiii! ",
            "Huohhhh. ",
            "OWO ",
            "OwO ",
            "UwU "
        ];

        readonly static string[] Suffixes = [
            " :3",
            " UwU",
            " ÙωÙ",
            " >_>",
            " ^_^",
            "..",
            " Huoh.",
            " ^-^",
            " ;_;",
            " ;-;",
            " xD",
            " x3",
            " :D",
            " :P",
            " ;3",
            " XDDD",
            ", fwendo",
            " ㅇㅅㅇ",
            "（＾ｖ＾）",
            " x3",
            " ._.",
            " (；ω；)",
            " >_<",
            " >w<",
            " ^w^",
            " Nyaa~"
        ];

        readonly static Dictionary<string, string> Substitutions = new()
        {
            { "r", "w" },
            { "l", "w" },
            { "R", "W" },
            { "L", "W" },
            { "no", "nu" },
            { "has", "haz" },
            { "have", "haz" },
            { "you", "uu" },
            { "the ", "da " },
            { "The ", "Da " }
        };

        static string OwoifyString(string src)
        {
            if (string.IsNullOrEmpty(src)) return src;

            if (Regex.IsMatch(src, @"^\d{1,3}(?:,\d{3})*$") || Regex.IsMatch(src, @"^[^\w\d]+$"))
                return src;

            src = Regex.Replace(src, @"(<[^>]*>|[^<]+)", match =>
            {
                var t = match.Value;

                if (t.StartsWith("<") && t.EndsWith(">"))
                    return t;

                foreach (var s in Substitutions)
                {
                    t = t.Replace(s.Key, s.Value);
                }

                return t;
            }, RegexOptions.Compiled);

            var rand1 = UnityEngine.Random.Range(0, 4);
            var rand2 = UnityEngine.Random.Range(0, 4);

            if (rand1 < 3)
                src = $"{Prefixes[UnityEngine.Random.Range(0, Prefixes.Length)]} {src}";

            if (rand2 < 3)
                src += Suffixes[UnityEngine.Random.Range(0, Suffixes.Length)];

            return src;
        }

        internal static string CreateString(TMP_Text srcObj, string txt)
        {
            var h = srcObj.gameObject.GetInstanceID();

            if (Sources.ContainsKey(h))
            {
                Sources[h] = txt;
                return OwoifyString(txt);
            }

            Sources.Add(h, txt);

            return OwoifyString(txt);
        }

        internal static void DeOwoify()
        {
            foreach (var s in Sources)
            {
                var src = GameObject.FindObjectFromInstanceID(s.Key);
                if (src == null) continue;

                src.Cast<GameObject>().GetComponent<TMP_Text>().SetText(s.Value);
            }

            Sources.Clear();
        }
    }
}
