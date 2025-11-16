using Il2CppFG.Common;
using Il2CppFG.Common.Audio;
using Il2CppFG.Common.CMS;
using Il2CppFG.Common.Definition;
using Il2CppFGClient;
using Il2CppFGClient.UI;
using Il2CppFGClient.UI.Core;
using MelonLoader;
using NOTFGT.FLZ_Common.Localization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Il2CppFG.Common.GameStateMachine;
using static Il2CppFGClient.UI.UIModalMessage;
using Random = UnityEngine.Random;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using NOTFGT.FLZ_Common.Extensions;

namespace NOTFGT.FLZ_Common.Loader
{
    public class RoundLoaderService
    {

        public static StateGameLoading GameLoading;
        public bool RoundLoadingAllowed = true;
        Round CurrentRound;

        public static ClientGameManager CGM;
        public static PlayerTeamManager PTM;
        public static InGameUiManager UIM;

        public void SetNewRound(Round Round) => CurrentRound = Round;

        public void LoadRound(string roundId)
        {
            try
            {
                LoadCmsRound(roundId, LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic", [roundId, LoadSceneMode.Single, e.Message]), ModalType.MT_OK, OKButtonType.Default);
            }
        }

        public void GenerateCMSList(Transform idsView, Button prefab)
        {
            int lineNumber = 1;
           
            foreach (RoundsSO roundsSO in Resources.FindObjectsOfTypeAll<RoundsSO>())
            {
                foreach (var pair in roundsSO.Rounds)
                {
                    Round cmsData = pair.Value;

                    if (cmsData != null && !cmsData.IsUGC())
                    {
                        string roundName = cmsData.DisplayName != null && cmsData.DisplayName != "ЛЫЖЕПАД" ? cmsData.DisplayName : cmsData.DisplayName + " КСТА";
                        string levelType = cmsData.Archetype != null && cmsData.Archetype.Name != null ? cmsData.Archetype.Name : "(EMPTY)";
                        string scene = cmsData.SceneData != null && cmsData.SceneData.PrimeLevel != null && cmsData.SceneData.PrimeLevel.SceneName != null ? cmsData.SceneData.PrimeLevel.SceneName : "(EMPTY)";
                        string cleanName = FLZ_Extensions.CleanStr(roundName);
                        var obj = UnityEngine.Object.Instantiate(prefab, idsView);
                        obj.gameObject.SetActive(true);
                        obj.transform.GetComponentInChildren<Text>().text = $"{lineNumber}. {cleanName} - {pair.Key}";
                        obj.onClick.AddListener(new Action(() => { GUIUtility.systemCopyBuffer = pair.key; }));
                        lineNumber++;
                    }
                }
            }
        }

        public void LoadRandomCms()
        {
            List<string> ids = [];
            foreach (var round in CMSLoader.Instance.CMSData.Rounds)
            {
                if (!round.Value.IsUGC() && round.Value.SceneData != null)
                    ids.Add(round.Key);
            }
            var target = ids[Random.Range(0, ids.Count)];
            MelonLogger.Msg($"[{GetType()}] Loading round with id {target}...");
            LoadCmsRound(target, LoadSceneMode.Single);
        }


        public void LoadCmsRound(string roundToFind, LoadSceneMode mode)
        {
            if (RoundLoadingAllowed)
            {
                if (IsInRealGame)
                {
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_real_game"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                    return;
                }

                RoundLoadingAllowed = false;

                try
                {
                    if (CMSLoader.Instance.CMSData.Rounds.ContainsKey(roundToFind))
                        SetNewRound(CMSLoader.Instance.CMSData.Rounds[roundToFind]);
                    else
                    {
                        FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic_desc", [roundToFind]), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                        RoundLoadingAllowed = true;
                        return;
                    }

                    if (CurrentRound.IsUGC())
                    {
                        FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_fgc"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                        RoundLoadingAllowed = true;
                        return;
                    }

                    if (CurrentRound.SceneData == null)
                    {
                        FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_scene_data"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                        RoundLoadingAllowed = true;
                        return;
                    }

                    CurrentRound.GameRules.ShowQualificationProgressUI = CurrentRound.Archetype.Id.Contains("race");
                    if (CurrentRound.Archetype.Id == "archetype_invisibeans")
                        CurrentRound.Archetype.TagColour = "#5cedeb";
                    CurrentRound.GameRules.TimerVisibilityThreshold = 9999;
                    NetworkGameData.ClearCurrentGameOptions();
                    GlobalGameStateClient.Instance.ResetGame();

                    if (mode == LoadSceneMode.Additive)
                        Addressables.LoadScene(CurrentRound.GetSceneName(), LoadSceneMode.Additive);
                    else
                    {
                        Resources.FindObjectsOfTypeAll<UICanvas>().FirstOrDefault().RemoveAllScreens();
                        StartLoadingScreen();
                        Instance.HandlePlayerState(PlayerState.RoundLoader);
                        NetworkGameData.SetGameOptionsFromRoundData(CurrentRound);
                        NetworkGameData.SetInitialRoundPlayerCount(1);
                        GameLoading = new StateGameLoading(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), GamePermission.Player, false, false);
                        GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(GameLoading.Cast<IGameState>());
                    }
                }
                catch (Exception e)
                {
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic_desc", [roundToFind, mode, e.Message]), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                    RoundLoadingAllowed = true;
                }
            }
            else
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_not_allowed"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
            }
        }

        public void StartLoadingScreen()
        {
            var mmManager = Resources.FindObjectsOfTypeAll<MainMenuManager>().FirstOrDefault();
            try{AudioMixing.Instance.ResetAllSnapshotParams();} catch { };

            if (mmManager == null)
                return;

            mmManager.PauseMusic(false);
            try
            {
                mmManager.HideLobbyScreen();
                mmManager.HideChallenges();
                mmManager.RemoveMainMenuBuilder();
            }
            catch { }
        }

        public void HideLoadingScreens()
        {
            UIManager.Instance.HideScreen<LoadingUGCGameScreenViewModel>(ScreenStackType.LoadingScreen);
        }
    }
}
