using HarmonyLib;
using Il2CppCatapult.Network.Credentials;
using Il2CppCatapult.Network.Gateway;
using Il2CppCatapult.Network.Gateway.States;
using Il2CppCatapult.Network.RemoteServices.HttpRequests;
using Il2CppCatapult.Services.Gateway.Protocol.Client;
using Il2CppFGClient;
using Il2CppFGClient.CatapultServices;
using Il2CppFGClient.UI;
using Il2CppFGClient.UI.Core;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.Networking;
using Application = UnityEngine.Application;

namespace NOTFGT.FLZ_Common
{
    internal class FLZ_LoginPatches
    {
        internal class Version
        {
            [JsonPropertyName("clientVersion")]
            public string ClientVersion { get; set; }
            [JsonPropertyName("clientVersionSignature")]
            public string Signature { get; set; }
        }

        internal class WebConfig
        {
            [JsonPropertyName("latest_ver")]
            public string LatestVersion { get; set; }
            [JsonPropertyName("ver_spoof")]
            public bool IsSpoofEnabled { get; set; }
        }

        internal static WebConfig ModConfig;
        internal static Version VersionData;
        static int Attempt;
        const int MaxAttempts = 5;

        [HarmonyPatch(typeof(CatapultGatewayConnection), nameof(CatapultGatewayConnection.PerformLoginFlow)), HarmonyPrefix]
        public static bool PerformLoginFlow(CatapultGatewayConnection __instance, ContentUpdateNotification pendingContentUpdate)
        {
            if (ModConfig == null || VersionData == null && Attempt <= MaxAttempts)
            {
                MelonCoroutines.Start(FetchContent());
                return false;
            }
            else return true;
        }

        [HarmonyPatch(typeof(CatapultServicesManager), nameof(CatapultServicesManager.DisplayClientUpgradeRequiredError)), HarmonyPrefix]
        public static bool DisplayClientUpgradeRequiredError(CatapultServicesManager __instance)
        {
            PopupManager.Instance.Show(PopupInteractionType.Error, new ModalMessageData()
            {
                Title = FLZ_Extensions.CMSString("forced_update_err_t", LocalizationManager.LocalizedString("client_update_error_title")),
                Message = FLZ_Extensions.CMSString("forced_update_err_d", $"{LocalizationManager.LocalizedString("client_update_error_desc", [Constants.DefaultName])}\n\n<size=60%>{LocalizationManager.LocalizedString("client_update_error_lower")}</size>"),
                OkButtonType = UIModalMessage.OKButtonType.CallToAction,
                ModalType = UIModalMessage.ModalType.MT_OK_CANCEL,
                OkTextOverrideId = FLZ_Extensions.CMSString("forced_update_err_k", LocalizationManager.LocalizedString("client_update_error_ok")),
                CancelTextOverrideId = "quit",
                OnCloseButtonPressed = new Action<bool>(wasok =>
                {
                    if (wasok)
                        Application.OpenURL($"{Constants.GitHubURL}/releases/latest");

                    Application.Quit();
                }),
                ShowExternalLinkIcon = true,          
            });
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoginState), nameof(LoginState.BuildLoginRequest))]
        public static bool BuildLoginRequest(LoginState __instance, LoginCredential credential, ref HttpLoginRequest __result)
        {
            if (ModConfig == null || VersionData == null) return true;

            __result = new HttpLoginRequest
            {
                ClientVersion = ModConfig.IsSpoofEnabled ? VersionData.ClientVersion : __instance._config.ClientVersion,
                ClientVersionSignature = ModConfig.IsSpoofEnabled ? VersionData.Signature : __instance._config.ClientVersionSignature,
                Platform = __instance._config.Platform,
                Type = credential.CredentialTypeId,
                Token = credential.Value,
                UserParameters = __instance._config.ContentUserParameters,
                Properties = credential.Properties,
                ContentBranch = __instance._config.ContentBranchOverride
            };

            return false;
        }

        static IEnumerator FetchContent()
        {
            if (Attempt > MaxAttempts)
                yield break;

            Attempt++;

            UIManager.Instance.ShowScreen<LoadingSpinnerScreenViewModel>(new()
            {
                UseScrim = true,
                ScreenStack = ScreenStackType.LoadingScreen,
            });

            yield return GetWebConfig();
            yield return GetLatestVersion();

            UIManager.Instance.HideScreen<LoadingSpinnerScreenViewModel>(ScreenStackType.LoadingScreen);

            yield return new WaitForSecondsRealtime(0.5f);
            CatapultGatewayConnection.Instance.PerformLoginFlow(null);
        }

        static IEnumerator GetWebConfig()
        {
            var req = new UnityWebRequest($"{Constants.URLBase}/FGTools/mobile/config.json")
            {
                timeout = 5,
                downloadHandler = new DownloadHandlerBuffer()
            };
            yield return req.SendWebRequest();
            try
            {
                ModConfig = JsonSerializer.Deserialize<WebConfig>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Conf ex\n{ex}");
            }

        }

        static IEnumerator GetLatestVersion()
        {
            var req = new UnityWebRequest($"{Constants.URLBase}/fallguys/version.json")
            {
                timeout = 5,
                downloadHandler = new DownloadHandlerBuffer()
            };

            yield return req.SendWebRequest();

            try
            {
                VersionData = JsonSerializer.Deserialize<Version>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Ver ex\n{ex}");
            }
        }
    }
}
