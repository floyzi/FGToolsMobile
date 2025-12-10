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
        [GUIReference("NoLogsText")] internal readonly TextMeshProUGUI NoLogsText;

        [GUIReference("LogTabs")] readonly Transform LogTabsСontainer;

        List<Button> AllLogBtns;
        internal LogScreen() : base(ScreenType.Log)
        {
            Initialize();
        }

        internal override void CreateScreen()
        {
            AllLogBtns = [.. LogTabsСontainer.GetComponentsInChildren<Button>()];

            foreach (var btn in AllLogBtns)
            {
                if (btn.name.EndsWith("All"))
                    btn.onClick.AddListener(new Action(GUI_LogEntry.CreateAllInstances));
                else
                    btn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(Enum.Parse<LogType>(btn.name.Split("_")[1]))));

                btn.onClick.AddListener(new Action(() => ToggleLogTab(btn)));

                var sfx = btn.gameObject.AddComponent<ElementSFX>();
                sfx.SetSounds(new(Constants.TabMove));
            }

            ToggleLogTab(AllLogBtns[0]);

            LogPrefab.gameObject.SetActive(false);

            ClearLogsBtn.onClick.AddListener(new Action(() =>
            {
                NoLogsText.gameObject.SetActive(false);
                CleanupScreen(LogContent, true, [NoLogsText.transform]);
                GUI_LogEntry.AllEntries.Clear();
                GUI_LogEntry.UpdateLogStats();
                LogInfo.SetText(LocalizationManager.LocalizedString("log_adv_info"));
                NoLogsText.gameObject.SetActive(true);
            }));

            GUI_LogEntry.UpdateLogStats();
        }

        void ToggleLogTab(Button to)
        {
            foreach (var btn in AllLogBtns)
            {
                var block = btn.colors;
                block.normalColor = Color.white;
                block.selectedColor = Color.white;
                block.highlightedColor = Color.white;
                btn.colors = block;
            }

            var block2 = to.colors;
            block2.normalColor = GUIManager.TabActiveCol;
            block2.selectedColor = GUIManager.TabActiveCol;
            block2.highlightedColor = GUIManager.TabActiveCol;
            to.colors = block2;
        }

        internal bool CanCreateLogs() => LogPrefab != null && LogContent != null && LogStats != null;
        internal void UpdateLogStats()
        {
            if (!CanCreateLogs()) return;
            LogStats?.text = LocalizationManager.LocalizedString("errors_display", [GUI_LogEntry.AllEntries.Count(e => e.IsError), GUI_LogEntry.AllEntries.Count(e => e.IsWarning), GUI_LogEntry.AllEntries.Count(e => e.IsInfo), GUI_LogEntry.AllEntries.Count]);
        }
    }
}
