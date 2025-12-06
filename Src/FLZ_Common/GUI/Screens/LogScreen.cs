using Il2CppTMPro;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Screens.Logic;
using NOTFGT.FLZ_Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static MelonLoader.MelonLogger;

namespace NOTFGT.FLZ_Common.GUI.Screens
{
    internal class LogScreen : UIScreen
    {
        [GUIReference("LogMessage")] internal readonly Button LogPrefab;
        [GUIReference("LogInfo")] internal readonly TextMeshProUGUI LogInfo;
        [GUIReference("LogDisplay")] internal readonly Transform LogContent;
        [GUIReference("ClearLogsBtn")] internal readonly Button ClearLogsBtn;
        [GUIReference("LogStats")] internal readonly TextMeshProUGUI LogStats;
        [GUIReference("LogDisabled")] internal readonly GameObject LogDisabledScreen;

        [GUIReference("LogBtn_All")] readonly Button AllLogsBtn;
        [GUIReference("LogBtn_Info")] readonly Button InfoLogsBtn;
        [GUIReference("LogBtn_Warn")] readonly Button WarnLogsBtn;
        [GUIReference("LogBtn_Error")] readonly Button ErrorLogsBtn;

        internal LogScreen() : base(ScreenType.Log)
        {
            Initialize();
        }

        internal override void CreateScreen()
        {
            LogPrefab.gameObject.SetActive(false);

            ClearLogsBtn.onClick.AddListener(new Action(() =>
            {
                CleanupScreen(LogContent, true);
                GUI_LogEntry.AllEntries.Clear();
                GUI_LogEntry.UpdateLogStats();
            }));

            AllLogsBtn.onClick.AddListener(new Action(GUI_LogEntry.CreateAllInstances));
            InfoLogsBtn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(LogType.Log)));
            WarnLogsBtn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(LogType.Warning)));
            ErrorLogsBtn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(LogType.Error)));

            GUI_LogEntry.UpdateLogStats();
        }

        internal bool CanCreateLogs() => LogPrefab != null && LogContent != null && LogStats != null;
        internal void UpdateLogStats()
        {
            if (!CanCreateLogs()) return;
            LogStats?.text = LocalizationManager.LocalizedString("errors_display", [GUI_LogEntry.AllEntries.Count(e => e.IsError), GUI_LogEntry.AllEntries.Count(e => e.IsWarning), GUI_LogEntry.AllEntries.Count(e => e.IsInfo), GUI_LogEntry.AllEntries.Count]);
        }
    }
}
