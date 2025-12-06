using Il2CppFG.Common.CMS;
using Il2CppTMPro;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Screens.Logic;
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
    internal class RoundLoaderScreen : UIScreen
    {
        [GUIReference("RoundInputField")] readonly TMP_InputField RoundIdInputField;
        [GUIReference("RoundLoadBtn")] readonly Button RoundLoadButton;
        [GUIReference("RandomRoundBtn")] readonly Button RoundLoadRandomButton;
        [GUIReference("RoundID_Entry")] readonly GameObject RoundIDEntry;
        [GUIReference("RoundID_EntryV2")] readonly Button RoundIDEntryV2;
        [GUIReference("RoundIDSView")] readonly Transform RoundIdsView;
        [GUIReference("RoundGenListBtn")] readonly Button RoundGenerateListButton;
        [GUIReference("RoundListCleanup")] readonly Button CleanupList;
        [GUIReference("RoundsDropDown")] readonly TMP_Dropdown RoundsDropdown;
        [GUIReference("RoundsIDSDropDown")] readonly TMP_Dropdown IdsDropdown;
        [GUIReference("ClickToCopyNote")] readonly GameObject ClickToCopy;

        string ReadyRound;
        internal RoundLoaderScreen() : base(ScreenType.RoundLoader)
        {
            Initialize();
        }

        internal override void CreateScreen()
        {
            RoundIdInputField.onValueChanged.AddListener(new Action<string>((str) => { ReadyRound = str; }));
            RoundLoadButton.onClick.AddListener(new Action(() =>
            {
                FLZ_ToolsManager.Instance.RoundLoader.LoadRound(ReadyRound);
            }));
            CleanupList.onClick.AddListener(new Action(() =>
            {
                ClickToCopy.SetActive(false);
                CleanupScreen(RoundIdsView, true);
            }));
            RoundGenerateListButton.onClick.AddListener(new Action(() =>
            {
                ClickToCopy.SetActive(false);
                CleanupScreen(RoundIdsView, true);
                FLZ_ToolsManager.Instance.RoundLoader.GenerateCMSList(RoundIdsView, RoundIDEntryV2);
                ClickToCopy.SetActive(true);
            }));
            RoundLoadRandomButton.onClick.AddListener(new Action(FLZ_ToolsManager.Instance.RoundLoader.LoadRandomCms));

            RoundIDEntryV2.gameObject.SetActive(false);

            InitRoundsDropdown();
        }

        void InitRoundsDropdown()
        {
            if (RoundsDropdown == null && IdsDropdown == null)
                return;

            RoundsDropdown.onValueChanged.RemoveAllListeners();
            IdsDropdown.onValueChanged.RemoveAllListeners();

            RoundsDropdown.ClearOptions();
            IdsDropdown.ClearOptions();

            var rounds = CMSLoader.Instance.CMSData.Rounds;

            Dictionary<string, string> uniqRounds = [];

            foreach (var round in rounds)
            {
                var scene = round.Value.GetSceneName();
                if (scene == null || round.Value == null || round.Value.DisplayName == null)
                    continue;

                if (!uniqRounds.ContainsKey(round.Value.GetSceneName()))
                    uniqRounds.Add(scene, FLZ_Extensions.CleanStr(round.Value.DisplayName.Text));
            }

            Il2CppSystem.Collections.Generic.List<string> roundNames = new();

            foreach (var round in uniqRounds.Values)
            {
                roundNames.Add(round);
            }

            roundNames.Sort();
            RoundsDropdown.AddOptions(roundNames);

            IdsDropdown.onValueChanged.AddListener(new Action<int>(val =>
            {
                ReadyRound = IdsDropdown.options[val].text;
            }));

            RoundsDropdown.onValueChanged.AddListener(new Action<int>(val =>
            {
                var scene = string.Empty;

                foreach (var round in rounds)
                {
                    if (round.Value.DisplayName != null && FLZ_Extensions.CleanStr(round.Value.DisplayName.Text) == RoundsDropdown.options[val].text)
                    {
                        scene = round.Value.GetSceneName();
                        break;
                    }
                }

                if (scene != null)
                {
                    Il2CppSystem.Collections.Generic.List<string> ids = new();

                    foreach (var round in rounds)
                        if (round.Value.GetSceneName() == scene)
                            ids.Add(round.Key);

                    IdsDropdown.ClearOptions();
                    IdsDropdown.AddOptions(ids);
                    ReadyRound = IdsDropdown.options[0].text;
                }
            }));

            RoundsDropdown.onValueChanged.Invoke(0);
        }
    }
}