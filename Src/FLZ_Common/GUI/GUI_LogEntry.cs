using Il2CppTMPro;
using MelonLoader;
using NOTFGT.FLZ_Common.Localization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common.GUI
{
    public class GUI_LogEntry
    {
        static readonly Color InfoColor = new(0.7019608f, 1f, 0.9569892f, 1f);
        static readonly Color WarningColor = new(1f, 0.8809203f, 0.7019608f, 1f);
        static readonly Color ErrorColor = new(1f, 0.7028302f, 0.7028302f, 1f);
        internal static List<GUI_LogEntry> AllEntries = [];
        static Queue<Action> PendingLogs = [];
        internal static LogType PreviewLogType = (LogType)1001;

        public GUI_LogEntry(string line, string trace, LogType type)
        {
            Msg = line;
            Stacktrace = trace;
            Type = type;

            AllEntries.Add(this);

            CreateInstance();
        }

        public LogType Type { get; set; }
        public string Msg { get; set; }
        public string Stacktrace { get; set; }
        public Button EntryInstance { get; set; }
        public bool HasInstance => EntryInstance != null;
        public bool IsInfo => Type == LogType.Log || Type == LogType.Assert;
        public bool IsWarning => Type == LogType.Warning;
        public bool IsError => Type == LogType.Error || Type == LogType.Exception;
        public static bool IsInLogPreview() => PreviewLogType != (LogType)1001;
        public void CreateInstance()
        {
            if (HasInstance) return;
            if (IsInLogPreview() && PreviewLogType != Type) return;

            if (!CanCreateInstances())
            {
                PendingLogs ??= [];
                PendingLogs.Enqueue(CreateInstance);
                return;
            }

            if (PendingLogs != null)
            {
                for (int i = 0; i < PendingLogs.Count; i++)
                    PendingLogs.Dequeue().Invoke();

                PendingLogs = null;
            }

            EntryInstance = UnityEngine.Object.Instantiate(Instance.GUIUtil.LogPrefab, Instance.GUIUtil.LogContent);
            EntryInstance.name = "Log";
            EntryInstance.gameObject.SetActive(true);
            EntryInstance.transform.SetAsFirstSibling();

            switch (Type)
            {
                case LogType.Assert:
                case LogType.Log:
                    EntryInstance.image.color = InfoColor;
                    break;
                case LogType.Warning:
                    EntryInstance.image.color = WarningColor;
                    break;
                case LogType.Exception:
                case LogType.Error:
                    EntryInstance.image.color = ErrorColor;
                    break;
            }

            UpdateLogStats();

            string result = Msg.Length > 55 ? result = string.Concat(Msg.AsSpan(0, 55), "...") : Msg;
            EntryInstance.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = $"{Regex.Replace(result, @"^\d{2}:\d{2}:\d{2}\.\d{3}:\s*", "")}";
            EntryInstance.onClick.AddListener(new Action(() => { Instance.GUIUtil.LogInfo.text = LocalizationManager.LocalizedString("advanced_log", [Type, Msg, Stacktrace]); }));
        }

        public static bool CanCreateInstances() => Instance.GUIUtil.LogPrefab != null;
        public static void UpdateLogStats() => Instance.GUIUtil.LogStats?.text = LocalizationManager.LocalizedString("errors_display", [AllEntries.Count(e => e.IsError), AllEntries.Count(e => e.IsWarning), AllEntries.Count(e => e.IsInfo), AllEntries.Count]);

        public void DestroyInstance()
        {
            if (!HasInstance) return;
            GameObject.Destroy(EntryInstance.gameObject);
            EntryInstance = null;
        }

        public static void CreateAllInstances()
        {
            DestroyAllInstances();

            foreach (var entry in AllEntries)
            {
                entry.CreateInstance();
            }
        }

        public static void DestroyAllInstances()
        {
            foreach (var entry in AllEntries)
            {
                entry.DestroyInstance();
            }

            PreviewLogType = (LogType)1001;
        }

        public static void CreateInstancesOf(LogType lvl)
        {
            DestroyAllInstances();

            PreviewLogType = lvl;

            foreach (var entry in AllEntries)
            {
                if (lvl switch
                {
                    LogType.Log or LogType.Assert => entry.IsInfo,
                    LogType.Warning => entry.IsWarning,
                    LogType.Error or LogType.Exception => entry.IsError,
                    _ => false
                })
                    entry.CreateInstance();
            }
        }
    }

}
