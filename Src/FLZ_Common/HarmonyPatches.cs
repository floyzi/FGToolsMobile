using HarmonyLib;
using Il2Cpp;
using Il2CppCatapult.Network.Gateway;
using Il2CppCatapult.Services.Gateway.Protocol.Client;
using Il2CppFG.Common;
using Il2CppFG.Common.Messages;
using Il2CppFGClient;
using Il2CppFGClient.UI.Notifications;
using Il2CppFGDebug;
using Il2CppInterop.Runtime;
using Il2CppRewired.Utils.Classes.Data;
using Il2CppTMPro;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI;
using NOTFGT.FLZ_Common.Loader;
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Il2CppFG.Common.LODs.LodController;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common
{
    public class HarmonyPatches
    {
        public class CaptureTools
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CaptureToolsManager), "CanUseCaptureTools", MethodType.Getter)]
            public static void CanUseCaptureTools(ref bool __result)
            {
                __result = Instance.InGameManager.UseCaptureTools;
            }
        }

        public class Default
        {

            [HarmonyPatch(typeof(PlayerInfoHUDBase), nameof(PlayerInfoHUDBase.SpawnPlayerTag)), HarmonyPostfix]
            static void SpawnPlayerTag(PlayerInfoHUDBase __instance, SpawnPlayerTagEvent spawnEvent)
            {
                FLZ_ToolsManager.Instance.InGameManager.RegisterTag(__instance._spawnedInfoObjects[^1].playerInfo, spawnEvent);
            }


            [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnSelect)), HarmonyPrefix]
            static bool OnSelect(TMP_InputField __instance, PointerEventData eventData)
            {
                //by default it activates input field the moment you touch it (without releasing the finger)
                //Had to make this to disable such behavior in my UI because it makes navigation almost impossible

                return __instance.name != "SLOP";
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(NotificationViewModelBase), nameof(NotificationViewModelBase.Update))]
            static void Update([Obfuscation(Exclude = true)] NotificationViewModelBase __instance)
            {
                if (__instance.GetIl2CppType() != Il2CppType.Of<TextNotificationViewModel>())
                    return;

                //why not lol
                if (__instance.name.StartsWith("PB_UI_"))
                    __instance.name = __instance.transform.position.x.ToString();

                if (Instance.GUIUtil.IsUIActive)
                    //slop!
                    __instance.transform.localPosition = new Vector3(float.Parse(__instance.transform.name) + Instance.GUIUtil.PanelBG.rect.width + 175, __instance.transform.localPosition.y, __instance.transform.localPosition.z);
                else
                    __instance.transform.localPosition = new Vector3(float.Parse(__instance.transform.name), __instance.transform.localPosition.y, __instance.transform.localPosition.z);
            }
        }


        public class RoundLoader
        {
            [HarmonyPatch(typeof(ClientGameManager), nameof(ClientGameManager.SetReady)), HarmonyPostfix]
            static void SetReady(ClientGameManager __instance, PlayerReadinessState readinessState, string sceneName, string levelHash)
            {
                if (!IsInRoundLoader)
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
                            _additionalSpawnData = new PlayerSpawnData(
                                GlobalGameStateClient.Instance.GetLocalClientNetworkID(), 
                                1, 
                                GlobalGameStateClient.Instance.GetLocalClientAccountID(), 
                                "android_ega", 
                                GlobalGameStateClient.Instance.GetLocalPlayerName(), 
                                "", 
                                0, 
                                -1, 
                                "", 
                                0, 
                                false, 
                                GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections),
                            _creationMode = NetObjectCreationMode.Spawn,
                            _lodControllerBehaviour = LodControllerBehaviour.Default,
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
            [HarmonyPatch(typeof(GvrFPS), nameof(GvrFPS.ToggleMinimalisticFPSCounter)), HarmonyPrefix]
            static bool ToggleMinimalisticFPSCounter(GvrFPS __instance, GlobalDebug.DebugToggleMinimalisticFPSCounter toggleEvent)
            {
                var target = Instance.FPSCounter;
                if (target && !Instance.FGDebug)
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

            [HarmonyPatch(typeof(GvrFPS), nameof(GvrFPS.ToggleFPSCounter)), HarmonyPrefix]
            static bool ToggleFPSCounter(GvrFPS __instance, GlobalDebug.DebugToggleFPSCounter toggleEvent)
            {
                var target = Instance.FGDebug;
                if (target && !Instance.FPSCounter)
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

            [HarmonyPatch(typeof(StateMainMenu), nameof(StateMainMenu.HandleConnectEvent)), HarmonyPrefix]
            static bool HandleConnectEvent(StateMainMenu __instance, ConnectEvent evt)
            {
                if (Instance.InGameManager.DisableFGCCCheck && PlayerPrefs.GetInt("FLZ_CONNECT_WARN") != 1)
                {
                    PlayerPrefs.SetInt("FLZ_CONNECT_WARN", 1);
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("fgcc_alert_title"), LocalizationManager.LocalizedString("fgcc_alert_desc"), ModalType.MT_OK_CANCEL, OKButtonType.Disruptive, new Action<bool>(Go));
                }
                else
                    Go(true);

                void Go(bool wasok)
                {
                    if (wasok)
                    {
                        ServerSettings serverSettings = evt.playerProfile.serverSettings;
                        __instance.StartConnecting(serverSettings.ServerAddress, serverSettings.ServerPort, serverSettings.MatchmakingEnv);
                        Instance.HandlePlayerState(PlayerState.RealGame);
                    }
                }
                return false;
            }
            [HarmonyPatch(typeof(LoadingScreenViewModel), nameof(LoadingScreenViewModel.Awake)), HarmonyPostfix]
            static void ShowScreen(LoadingScreenViewModel __instance)
            {
                if (File.Exists(Core.MobileLoading))
                {
                    var spr = FLZ_Extensions.SetSpriteFromFile(Core.MobileLoading);
                    __instance.gameObject.transform.FindChild("SplashScreen_Image").gameObject.GetComponent<Image>().sprite = spr;
                    __instance.SplashLoadingScreenSprite = spr;
                }
            }

            [HarmonyPatch(typeof(PreInitLoadingScreenViewModel), nameof(PreInitLoadingScreenViewModel.Awake)), HarmonyPostfix]
            static void Show(PreInitLoadingScreenViewModel __instance)
            {
                var targ = __instance.GetComponentsInChildren<Image>().ToList().Find(x => x.name == "SplashScreen_Image");

                if (!File.Exists(Core.MobileLoading) || targ == null) return;
                targ.sprite = FLZ_Extensions.SetSpriteFromFile(Core.MobileLoading);
            }
        }
    }
}
