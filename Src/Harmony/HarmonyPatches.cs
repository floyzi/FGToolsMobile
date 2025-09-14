using FG.Common;
using FG.Common.Messages;
using FGClient;
using FGClient.UI;
using FGDebug;
using HarmonyLib;
using NOTFGT.GUI;
using NOTFGT.Loader;
using NOTFGT.Localization;
using NOTFGT.Logic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using static SRF.UI.SRSpinner;

namespace NOTFGT.Harmony
{
    public class HarmonyPatches
    {
        public class CaptureTools
        {
            [HarmonyLib.HarmonyPostfix]
            [HarmonyLib.HarmonyPatch(typeof(CaptureToolsManager), "CanUseCaptureTools", HarmonyLib.MethodType.Getter)]
            public static void CanUseCaptureTools(ref bool __result)
            {
                __result = NOTFGTools.CaptureTools;
            }
        }

        public class Default
        {
            [HarmonyPatch(typeof(PlayerInfoHUDBase), nameof(PlayerInfoHUDBase.OnIntroCamsComplete)), HarmonyPostfix]
            static void OnIntroCamsComplete(PlayerInfoHUDBase __instance, GameStateEvents.IntroCameraSequenceEndedEvent evt)
            {
                foreach (var tag in __instance._spawnedInfoObjects)
                {
                    NOTFGTools.Instance.RegisterTag(tag.playerInfo);
                }
            }
        }


        public class RoundLoader
        {
            [HarmonyLib.HarmonyPatch(typeof(ClientGameManager), nameof(ClientGameManager.SetReady)), HarmonyLib.HarmonyPostfix]
            static void SetReady(ClientGameManager __instance, PlayerReadinessState readinessState, string sceneName, string levelHash)
            {
                if (NOTFGTools.Instance.ActivePlayerState != NOTFGTools.PlayerState.RoundLoader)
                    return;

                switch (readinessState)
                {
                    case PlayerReadinessState.LevelLoaded:
                        var gameLoading = RoundLoaderService.GameLoading;
                        RoundLoaderService.CGM = gameLoading._clientGameManager;
                        RoundLoaderService.PTM = gameLoading._clientGameManager._playerTeamManager;

                        gameLoading._clientGameManager.GameRules.PreparePlayerStartingPositions(1);
                        var pos = gameLoading._clientGameManager.GameRules.PickStartingPosition(0, 0, -1, 0, false);

                        //SLOP
                        var spawn = new GameMessageServerSpawnObject();
                        spawn.Init(new()
                        {
                            NetID = GlobalGameStateClient.Instance.NetObjectManager.GetNextNetID(),
                            _additionalSpawnData = new PlayerSpawnData(GlobalGameStateClient.Instance.GetLocalClientNetworkID(), 1, GlobalGameStateClient.Instance.GetLocalClientAccountID(), "android_ega", GlobalGameStateClient.Instance.GetLocalPlayerName(), "", 0, -1, "", 0, false, GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections),
                            _creationMode = NetObjectCreationMode.Spawn,
                            _lodControllerBehaviour = FG.Common.LODs.LodController.LodControllerBehaviour.Default,
                            _prefabHash = -491682846,
                            _scale = Vector3.one,
                            _spawnObjectType = EnumSpawnObjectType.PLAYER,
                            _syncTransform = false,
                            _syncScale = false,
                            _position = pos.transform.position,
                            _rotation = pos.transform.rotation,
                            _useUnifiedSetup = true,
                        }, true);

                        RoundLoaderService.CGM._clientPlayerManager._localPlayerCount = 1;
                        RoundLoaderService.CGM._delayedSpawnNetObjectMessages.Enqueue(spawn);

                        CGMDespatcher.process(new GameMessageServerEventGeneric()
                        {
                            Type = GameMessageServerEventGeneric.EventType.StartIntroCameras,
                            Data = new()
                            {
                                StrParam1 = default,
                                StrParam2 = default,
                                FloatParam1 = default,
                                FloatParam2 = default,
                                MpgNetId = default,
                                ObjectArray = new(1)
                            }
                        });
                        break;
                }
            }
        }

        public class GUITweaks
        {
            [HarmonyLib.HarmonyPatch(typeof(GvrFPS), nameof(GvrFPS.ToggleMinimalisticFPSCounter)), HarmonyLib.HarmonyPrefix]
            static bool ToggleMinimalisticFPSCounter(GvrFPS __instance, GlobalDebug.DebugToggleMinimalisticFPSCounter toggleEvent)
            {
                var target = NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.FPSCoutner);
                if (target && !NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.WholeFGDebug))
                {
                    if (!__instance.gameObject.activeSelf)
                    {
                        __instance.gameObject.SetActive(true);
                        __instance._keepActive = true;
                    }

                    foreach (TextMeshProUGUI TMP in __instance.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (TMP != __instance.fpsText)
                        {
                            TMP.gameObject.SetActive(false);
                        }
                        else
                            TMP.gameObject.SetActive(target);

                    }
                }
                return false;
            }

            [HarmonyLib.HarmonyPatch(typeof(GvrFPS), nameof(GvrFPS.ToggleFPSCounter)), HarmonyLib.HarmonyPrefix]
            static bool ToggleFPSCounter(GvrFPS __instance, GlobalDebug.DebugToggleFPSCounter toggleEvent)
            {
                var target = NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.WholeFGDebug);
                if (target && !NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.FPSCoutner))
                {
                    __instance.gameObject.SetActive(target);
                    foreach (TextMeshProUGUI TMP in __instance.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        TMP.gameObject.SetActive(target);
                    }
                    __instance._keepActive = __instance.gameObject.activeSelf;
                }
                return false;
            }

            [HarmonyLib.HarmonyPatch(typeof(StateMainMenu), nameof(StateMainMenu.HandleConnectEvent)), HarmonyLib.HarmonyPrefix]
            static bool HandleConnectEvent(StateMainMenu __instance, ConnectEvent evt)
            {
                if (NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.DisableMonitorCheck) && PlayerPrefs.GetInt("FLZ_CONNECT_WARN") != 1)
                {
                    PlayerPrefs.SetInt("FLZ_CONNECT_WARN", 1);
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("fgcc_alert_title"), LocalizationManager.LocalizedString("fgcc_alert_desc"), UIModalMessage.ModalType.MT_OK_CANCEL, UIModalMessage.OKButtonType.Disruptive, new Action<bool>(Go));
                }
                else
                    Go(true);

                void Go(bool wasok)
                {
                    if (wasok)
                    {
                        ServerSettings serverSettings = evt.playerProfile.serverSettings;
                        __instance.StartConnecting(serverSettings.ServerAddress, serverSettings.ServerPort, serverSettings.MatchmakingEnv);
                        NOTFGTools.Instance.HandlePlayerState(NOTFGTools.PlayerState.RealGame);
                    }
                }
                return false;
            }
            [HarmonyLib.HarmonyPatch(typeof(LoadingScreenViewModel), nameof(LoadingScreenViewModel.Awake)), HarmonyLib.HarmonyPrefix]
            static bool ShowScreen(LoadingScreenViewModel __instance)
            {
                __instance._canvasFader = __instance.GetComponent<CanvasGroupFader>();
                if (File.Exists(NOTFGTools.MobileSplash))
                {
                    var spr = FLZ_Extensions.SetSpriteFromFile(NOTFGTools.MobileSplash, 1920, 1080);
                    __instance.gameObject.transform.FindChild("SplashScreen_Image").gameObject.GetComponent<UnityEngine.UI.Image>().sprite = spr;
                    __instance.SplashLoadingScreenSprite = spr;
                }
                return false;
            }
        }
    }
}
