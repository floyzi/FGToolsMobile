using Il2Cpp;
using Il2CppFG.Common.CMS;
using Il2CppSystem.Data;
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
using static Il2Cpp.CatapultAnalyticsClient;
using static MelonLoader.MelonLogger;

namespace NOTFGT.FLZ_Common.GUI.Screens
{
    internal class RoundLoaderScreen : UIScreen
    {
        [AudioReference(Constants.Click)]
        [GUIReference("RoundInputField")] readonly TMP_InputField RoundIdInputField;

        [AudioReference(Constants.Click)]
        [GUIReference("RoundLoadBtn")] readonly Button RoundLoadButton;

        [AudioReference(Constants.Click)]
        [GUIReference("RandomRoundBtn")] readonly Button RoundLoadRandomButton;

        [AudioReference(Constants.Click)]
        [GUIReference("RoundsDropDown")] readonly TMP_Dropdown RoundsDropdown;

        [AudioReference(Constants.Click)]
        [GUIReference("RoundsIDSDropDown")] readonly TMP_Dropdown IdsDropdown;

        string ReadyRound;
        Dictionary<string, string> SceneToNameMap;
        internal RoundLoaderScreen() : base(ScreenType.RoundLoader)
        {
            Initialize();
        }

        protected override void StateChange(bool isActive, bool wasActive)
        {
        }

        internal override void CreateScreen()
        {
            SceneToNameMap = [];

            RoundIdInputField.onValueChanged.AddListener(new Action<string>((str) => { ReadyRound = str; }));
            RoundLoadButton.onClick.AddListener(new Action(() =>
            {
                FLZ_ToolsManager.Instance.RoundLoader.LoadRound(ReadyRound);
            }));

            RoundLoadRandomButton.onClick.AddListener(new Action(() =>
            {
                ReadyRound = FLZ_ToolsManager.Instance.RoundLoader.GetRandomRound();
                if (CMSLoader.Instance.CMSData.Rounds.TryGetValue(ReadyRound, out var cmsRound))
                {
                    var scene = cmsRound.GetSceneName();
                    if (scene != null && SceneToNameMap.TryGetValue(scene, out string roundName))
                    {
                        for (int i = 0; i < RoundsDropdown.options.Count; i++)
                        {
                            if (RoundsDropdown.options[i].text == roundName)
                            {
                                RoundsDropdown.value = i;
                                RoundsDropdown.onValueChanged.Invoke(i);
                                break;
                            }
                        }
                    }
                }

                AudioManager.PlayOneShot(AudioManager.EventMasterData.CustomiserRandomise);
            }));

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
            SceneToNameMap.Clear();

            var rounds = CMSLoader.Instance.CMSData.Rounds;

            foreach (var round in rounds)
            {
                var scene = round.Value.GetSceneName();
                if (scene == null || round.Value == null || round.Value.DisplayName == null)
                    continue;

                if (!SceneToNameMap.ContainsKey(round.Value.GetSceneName()))
                    SceneToNameMap.Add(scene, FLZ_Extensions.CleanStr(round.Value.DisplayName.Text));
            }

            Il2CppSystem.Collections.Generic.List<string> roundNames = new();
            var uniqueNames = SceneToNameMap.Values.Distinct().ToList();
            uniqueNames.Sort();

            foreach (var round in uniqueNames)
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
                Il2CppSystem.Collections.Generic.List<string> ids = new();
                foreach (var round in rounds)
                {
                    var scene = round.Value.GetSceneName();
                    if (scene != null && SceneToNameMap.TryGetValue(scene, out string name) && name == RoundsDropdown.options[val].text)
                        ids.Add(round.Key);
                }

                IdsDropdown.ClearOptions();
                IdsDropdown.AddOptions(ids);

                if (ids.Count > 0)
                    ReadyRound = ids[0];
            }));

            RoundsDropdown.onValueChanged.Invoke(0);
        }
    }
}