﻿using FG.Common.CMS;
using FG.Common;
using FGClient.UI.Core;
using FGClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FG.Common.GameStateMachine;
using static MPG.Utility.MPGMonoBehaviour;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine;
using FG.Common.Audio;
using UnityEngine.InputSystem.LowLevel;
using FG.Common.Definition;
using UnityEngine.UI;
using FG.Common.Character.MotorSystem;
using Random = UnityEngine.Random;
using FGClient.UI;
using MelonLoader;
using FG.Common.LODs;
using NOTFGT.Logic;
using NOTFGT.Localization;

namespace NOTFGT.Loader
{
    public class RoundLoaderService
    {

        public static StateGameLoading GameLoading;
        public bool RoundLoadingAllowed = true;
        Round CurrentRound;

        public static ClientGameManager CGM;
        public static PlayerTeamManager PTM;
        public static InGameUiManager UIM;

        public CameraDirector RoundCamera;
        public void SetNewRound(Round Round) => CurrentRound = Round;

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
                        string scene = (cmsData.SceneData != null && cmsData.SceneData.PrimeLevel != null && cmsData.SceneData.PrimeLevel.SceneName != null) ? cmsData.SceneData.PrimeLevel.SceneName : "(EMPTY)";
                        string cleanName = InternalTools.CleanStr(roundName);
                        var obj = GameObject.Instantiate(prefab, idsView);
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
            MelonLogger.Msg($"[{base.GetType()}] Loading round with id {target}...");
            LoadCmsRound(target, LoadSceneMode.Single);
        }


        public void LoadCmsRound(string roundToFind, LoadSceneMode mode)
        {
            if (RoundLoadingAllowed)
            {
                if (NOTFGTools.Instance.ActivePlayerState == NOTFGTools.PlayerState.RealGame)
                {
                    InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_real_game"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                    return;
                }
                RoundLoadingAllowed = false;
                try
                {
                    if (CMSLoader.Instance.CMSData.Rounds.ContainsKey(roundToFind))
                        SetNewRound(CMSLoader.Instance.CMSData.Rounds[roundToFind]);
                    else
                    {
                        InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic_desc", [roundToFind]), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                        RoundLoadingAllowed = true;
                        return;
                    }

                    if (CurrentRound.IsUGC())
                    {
                        InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_fgc"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                        RoundLoadingAllowed = true;
                        return;
                    }

                    if (CurrentRound.SceneData == null)
                    {
                        InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_scene_data"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
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
                        NOTFGTools.Instance.HandlePlayerState(NOTFGTools.PlayerState.RoundLoader);
                        NetworkGameData.SetGameOptionsFromRoundData(CurrentRound);
                        NetworkGameData.SetInitialRoundPlayerCount(1);
                        GameLoading = new StateGameLoading(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), GamePermission.Player, false, false);
                        GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(GameLoading.Cast<IGameState>());
                    }
                }
                catch (Exception e)
                {
                    InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic_desc", [roundToFind, mode, e.Message]), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
                    RoundLoadingAllowed = true;
                }
            }
            else
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_not_allowed"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);
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

        void OnSpawnFail(Exception e) => InternalTools.DoModal(LocalizationManager.LocalizedString("generic_error_title"), $"{LocalizationManager.LocalizedString("error_fallguy_spawn", [InternalTools.FormatException(e)])}", UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Default);

        public void SpawnFallGuy()
        {
            try
            {
                int playerTeam = Random.Range(0, CGM.GameRules.NumTeamsWanted());
                FallGuysCharacterController fgobj = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(x => x.name == "FallGuy");

                if (fgobj == null)
                {
                    OnSpawnFail(null);
                    return;
                }
                MultiplayerStartingPosition randPos;

                randPos = CGM.GameRules.PickStartingPosition(FallGuyBehaviour.PeakId, 0, playerTeam, 0, false);
                fgobj.transform.SetPositionAndRotation(randPos.transform.position, randPos.transform.rotation);
                fgobj.GetComponent<MotorAgent>()._motorFunctionsConfig = MotorAgent.MotorAgentConfiguration.Offline;

                var fg = UnityEngine.Object.Instantiate(fgobj.gameObject);
                var fgcc = fg.GetComponent<FallGuysCharacterController>();
                fgcc.enabled = true;
                var fginput = fg.GetComponent<FallGuysCharacterControllerInput>();
                fg.name = "FallGuy[0]";
                fg.tag = "Player";

                var mpgfg = fg.AddComponent<MPGNetObject>();
                mpgfg.NetID = new MPGNetID(FallGuyBehaviour.PeakId);
                mpgfg.FGCharacterController = fgcc;
                mpgfg.IsFallGuy = true;
                mpgfg.pNetTX_ = new MPGNetTransform(mpgfg, null, null, null, false, 0);
                mpgfg.SpawnObjectType = EnumSpawnObjectType.PLAYER;

                fgcc.IsControlledLocally = true;
                fgcc.IsLocalPlayer = true;
                fgcc._pNetObject = mpgfg;
                fgcc._mpgNetObjectManager = CGM._netObjectManager;

                var fgb = fg.AddComponent<FallGuyBehaviour>();
                fgb.PreInit();

                fginput.SetPlayerIndex(0);

                CGM.SetupPlayerUpdateManagerAndRegister(fgcc, true);
                RewiredManager.Instance.SetActiveMap(0, 0, false, false);

                CGM._clientPlayerManager.ClearAllPlayers();

                if (CGM.GameRules.IsTeamGameMode && CGM.GameRules.TeamCount > 0)
                {
                    fgb.PlayerTeamId = Random.Range(0, CGM.GameRules.NumTeamsWanted());
                    CGM.CreateTeams(CGM.GameRules.NumTeamsWanted());
                }
                CGM.HandlePlayerBeingSpawned(fgcc.NetObject, FallGuyBehaviour.PeakId, GlobalGameStateClient.Instance.GetLocalClientNetworkID(), GlobalGameStateClient.Instance.GetLocalClientAccountID(), ClientBuildDetails.Platform, GlobalGameStateClient.Instance.PlayerProfile.PlatformAccountName, GlobalGameStateClient.Instance.PlayerProfile.PlatformAccountName, 0, fgb.PlayerTeamId, "0", 0, true, GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections);
            }
            catch (Exception e)
            {
                OnSpawnFail(e); 
            }
        }

        public void HideLoadingScreens()
        {
            UIManager.Instance.HideScreen<LoadingUGCGameScreenViewModel>(ScreenStackType.LoadingScreen);
        }
    }
}
