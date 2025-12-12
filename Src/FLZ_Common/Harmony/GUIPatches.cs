using HarmonyLib;
using Il2Cpp;
using Il2CppFGClient;
using Il2CppFGClient.UI.Notifications;
using Il2CppFGDebug;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common.Harmony
{
    internal class GUIPatches
    {
        [HarmonyPatch(typeof(TMP_Text), "text", MethodType.Setter)]
        [HarmonyPrefix]
        static void TextSetterPatch(TMP_Text __instance, ref string value)
        {
            if (Instance.IsOwoifyEnabled)
            {
                var upd = Owoify.CreateString(__instance, value);
                value = upd;
            }
        }

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
                var spr = FLZ_Extensions.SetSprite(Core.MobileLoading);
                __instance.gameObject.transform.FindChild("SplashScreen_Image").gameObject.GetComponent<Image>().sprite = spr;
                __instance.SplashLoadingScreenSprite = spr;
            }
        }

        [HarmonyPatch(typeof(PreInitLoadingScreenViewModel), nameof(PreInitLoadingScreenViewModel.Awake)), HarmonyPostfix]
        static void Show(PreInitLoadingScreenViewModel __instance)
        {
            var targ = __instance.GetComponentsInChildren<Image>().ToList().Find(x => x.name == "SplashScreen_Image");

            if (!File.Exists(Core.MobileLoading) || targ == null) return;
            targ.sprite = FLZ_Extensions.SetSprite(Core.MobileLoading);
        }

        [HarmonyPatch(typeof(BootSplashScreenViewModel), nameof(BootSplashScreenViewModel.Awake)), HarmonyPostfix]
        static void Awake(BootSplashScreenViewModel __instance)
        {
            __instance._slides.Add(FLZ_Extensions.SetSprite(Core.MobileSplash));
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


            __instance.transform.localPosition = new Vector3(float.Parse(__instance.transform.name), __instance.transform.localPosition.y, __instance.transform.localPosition.z);
        }
    }
}
