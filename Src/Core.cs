using Il2Cpp;
using Il2CppEvents;
using Il2CppFG.Common;
using Il2CppFG.Common.Character;
using Il2CppFG.Common.Character.MotorSystem;
using Il2CppFGClient;
using Il2CppFGClient.UI;
using Il2CppFGClient.UI.Core;
using Il2CppFGDebug;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using NOTFGT;
using NOTFGT.GUI;
using NOTFGT.Harmony;
using NOTFGT.Loader;
using NOTFGT.Localization;
using NOTFGT.Logic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using static Il2Cpp.GameStateEvents;
using static Il2CppFG.Common.CommonEvents;
using static Il2CppFG.Common.GameStateMachine;
using static Il2CppFGClient.GlobalGameStateClient;
using static Il2CppFGClient.UI.UIModalMessage;
using static MelonLoader.MelonLogger;

[assembly: MelonInfo(typeof(NOTFGTools), "NOT FGTools", "1.0.2", "Floyzi", null)]
[assembly: MelonGame(null, null)]
namespace NOTFGT
{
    [Obsolete("remove later")]
    public static class BuildInfo
    {
        public const string Name = "NOT FGTools";
        public const string Description = "NOT FallGuys level loader by @floyzi102 on twitter";
        public const string Author = "Floyzi";
        public const string Company = null;
        public const string Version = "1.0.2";
        public const string DownloadLink = "";
    }

    public class NOTFGTools : MelonMod
    {
        internal struct PlayerMeta
        {
            internal PlayerMeta(PlayerInfoDisplay tag, string name, FallGuysCharacterController fgcc, string platfrom)
            {
                Tag = tag;
                Name = name;
                Platform = platfrom;
                FGCC = fgcc;
            }

            public PlayerInfoDisplay Tag;
            public string Name;
            public string Platform;
            public FallGuysCharacterController FGCC;
            public readonly MPGNetID NetId
            {
                get
                {
                    if (FGCC != null && FGCC.NetObject != null)
                        return FGCC.NetObject.NetID;

                    return default;
                }
            }
            public readonly bool LocalPlayer => FGCC != null && FGCC.IsLocalPlayer;
        }

        public static NOTFGTools Instance { get; private set; }

        public enum PlayerState
        {
            Unknown, Loading, Menu, RealGame, RoundLoader
        }
        public PlayerState ActivePlayerState = PlayerState.Unknown;
        public PlayerState PreviousPlayerState = PlayerState.Unknown;

        public static string MainDir;
        public static string LogDir;
        public static string AssetsDir;
        public static string MobileSplash;

        Color BuildInfoColor = new(0.3764f, 0.0156f, 0.0156f, 1f);

        CharacterControllerData ActiveFGCCData;
        object[] DefaultFGCCData = null;

        public GUI_Util GUIUtil = new();
        public ToolsMenu SettingsMenu = new();
        public RoundLoaderService RoundLoader = new();

        readonly string NextLogDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        StringBuilder AllLogs = new();
        bool playersHidden = false;

        Action<string, string, LogType> _logAction;

        EventSystem.Handle GameplayBegin = null;
        EventSystem.Handle IntroStart = null;
        EventSystem.Handle IntroEnd = null;
        EventSystem.Handle GuiInit = null;
        EventSystem.Handle OnSpectator = null;
        EventSystem.Handle OnRoundOver = null;
        EventSystem.Handle OnMenu = null;

        public static bool CaptureTools { get { return Instance.SettingsMenu.GetValue<bool>(ToolsMenu.UseCaptureTools); } }

        DateTime Startup;
        const string AdvancedNamePattern = "[{0}] | {1} | [{2}]";
        readonly List<PlayerMeta> PlayerMetas = [];
        readonly Dictionary<string, string> NamesMap = [];

        public override void OnInitializeMelon()
        {
            Msg("Boot...");

            try
            {
                //temp
                MainDir = Path.Combine("/sdcard", "MelonLoader", "com.Mediatonic.FallGuys_client", "NOT_FGTools");
                LogDir = Path.Combine(MainDir, "Logs");
                AssetsDir = Path.Combine(MainDir, "Assets");
                MobileSplash = Path.Combine(AssetsDir, "FGToolsMSplash.png");

                Startup = DateTime.UtcNow;

                Instance = this;
            }
            catch (Exception ex)
            {
                Error($"Boot failed!\n{ex}");
            }
        }

        public override void OnLateInitializeMelon()
        {
            Msg("Startup...");

            try
            {
                GUIUtil.Register();

                LocalizationManager.Setup();

                SettingsMenu.LoadConfig(false);

                ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();

                Msg("Starting common setup.");

                HarmonyInstance.PatchAll(typeof(HarmonyPatches.Default));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.CaptureTools));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.GUITweaks));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.RoundLoader));

                if (SettingsMenu.GetValue<bool>(ToolsMenu.TrackGameDebug))
                {
                    _logAction = new Action<string, string, LogType>(HandleLog);
                    Application.add_logMessageReceived(_logAction);
                }

                GameplayBegin = Broadcaster.Instance.Register<IntroCountdownEndedEvent>(new Action<IntroCountdownEndedEvent>(OnGameplayBegin));
                IntroStart = Broadcaster.Instance.Register<IntroCameraSequenceStartedEvent>(new Action<IntroCameraSequenceStartedEvent>(OnIntroStart));
                IntroEnd = Broadcaster.Instance.Register<IntroCameraSequenceEndedEvent>(new Action<IntroCameraSequenceEndedEvent>(OnIntroEnd));
                GuiInit = Broadcaster.Instance.Register<InitialiseClientOverlayEvent>(new Action<InitialiseClientOverlayEvent>(OnGUIInit));
                OnSpectator = Broadcaster.Instance.Register<ClientGameManagerSpectatorModeChanged>(new Action<ClientGameManagerSpectatorModeChanged>(OnSpectatorEvent));
                OnRoundOver = Broadcaster.Instance.Register<OnRoundOver>(new Action<OnRoundOver>(OnRoundOverEvent));
                OnMenu = Broadcaster.Instance.Register<OnMainMenuDisplayed>(new Action<OnMainMenuDisplayed>(MenuEvent));

                HandlePlayerState(PlayerState.Loading);

                Msg("Startup successful.");
            }
            catch (Exception e)
            {
                Error($"Startup failed!\n{e}");
                GUIUtil.ShowRepairGUI(e);
            }
        }   

        internal void RegisterTag(PlayerInfoDisplay tag)
        {

        }

        internal void SetNames()
        {
            if (PlayerMetas == null || PlayerMetas.Count == 0)
                return;

            foreach (var name in PlayerMetas)
            {
                var cleanName = Regex.Replace(name.Name, @"<size=(.*?)>|</size>", "");

                if (!NamesMap.ContainsKey(cleanName))
                    NamesMap.Add(cleanName, name.Name);

                string pName;

                if (SettingsMenu.GetValue<bool>(ToolsMenu.HideBigNames))
                    pName = cleanName;
                else
                    pName = name.Name;

                if (SettingsMenu.GetValue<bool>(ToolsMenu.SeePlayerPlatforms))
                    name.Tag.SetText(string.Format(AdvancedNamePattern, name.FGCC.NetObject.NetID, pName, name.Platform));
                else
                    name.Tag.SetText($"{pName}");
            }
        }

        private void MenuEvent(OnMainMenuDisplayed displayed)
        {
            foreach (var fgcc in Resources.FindObjectsOfTypeAll<FallGuysCharacterController>())
                fgcc.MotorAgent._motorFunctionsConfig = MotorAgent.MotorAgentConfiguration.Default;
        }

        public void HandlePlayerState(PlayerState playerState)
        {
            PreviousPlayerState = ActivePlayerState;
            ActivePlayerState = playerState;
        }

        void OnGameplayBegin(IntroCountdownEndedEvent evt)
        {
            playersHidden = false;
#if CHEATS
            SetupFGCCData();
            RollFGCCSettings();
#endif
            GUIUtil.UpdateGPUI(true, false);

            if (ActivePlayerState == PlayerState.RoundLoader)
            {
                FallGuyBehaviour.FGBehaviour.LoadGPActions();
                RoundLoader.RoundLoadingAllowed = true;
                FallGuyBehaviour.FGBehaviour.FallGuy.GetComponent<Rigidbody>().isKinematic = false;
                FallGuyBehaviour.FGBehaviour.spawnpoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(FallGuyBehaviour.FGBehaviour.spawnpoint.GetComponent<BoxCollider>());
                FallGuyBehaviour.FGBehaviour.spawnpoint.name = "Checkpoint";
                FallGuyBehaviour.FGBehaviour.spawnpoint.transform.SetPositionAndRotation(FallGuyBehaviour.FGBehaviour.FallGuy.transform.position, FallGuyBehaviour.FGBehaviour.FallGuy.transform.rotation);
            }
            else
            {
                //something for future
                GUIUtil.UpdateGPActions(null);
            }
        }

        void OnIntroStart(IntroCameraSequenceStartedEvent evt)
        {
#if CHEATS
            ResetFGCCData();
#endif
            GlobalGameStateClient.Instance.GameStateView.GetCharacterDataMonitor()._timeToRunNextCharacterControllerDataCheck = float.MaxValue;
            if (ActivePlayerState == PlayerState.RoundLoader)
            {

            }
        }

        void OnIntroEnd(IntroCameraSequenceEndedEvent evt)
        {
            SetNames();

            if (RoundLoaderService.CGM == null || ActivePlayerState != PlayerState.RoundLoader)
                return;

            RoundLoader.RoundLoadingAllowed = true;
            FallGuyBehaviour.Create();
            SpeedBoostManager SPM = FallGuyBehaviour.FGBehaviour.FGCC.SpeedBoostManager;
            SPM.SetAuthority(true);
            SPM.SetCharacterController(FallGuyBehaviour.FGBehaviour.FGCC);


            RoundLoaderService.GameLoading.HandleGameServerStartGame(new GameMessageServerStartGame(0, RoundLoaderService.CGM.CurrentGameSession.EndRoundTime, 0, 1, RoundLoaderService.CGM.GameRules.NumPerVsGroup, 1, 0));

        }

        void OnGUIInit(InitialiseClientOverlayEvent evt)
        {
            if (ActivePlayerState != PlayerState.RoundLoader)
                return;
        }

        void OnSpectatorEvent(ClientGameManagerSpectatorModeChanged evt)
        {
#if CHEATS
            ForceUnHidePlayers();
#endif
        }

        void OnRoundOverEvent(OnRoundOver evt)
        {
#if CHEATS
            ForceUnHidePlayers();
#endif
            PlayerMetas.Clear();
        }

        public void LoadRound(string roundId)
        {
            try
            {
                RoundLoader.LoadCmsRound(roundId, LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic", [roundId, LoadSceneMode.Single, e.Message]), ModalType.MT_OK, OKButtonType.Default);
            }
        }

        public void ApplyChanges()
        {
            try
            {
                var debug = SettingsMenu.GetValue<bool>(ToolsMenu.TrackGameDebug);
                if (debug && _logAction == null)
                {
                    _logAction = new Action<string, string, LogType>(HandleLog);
                    Application.add_logMessageReceived(_logAction);
                }
                else if (_logAction != null && !debug)
                {
                    Application.remove_logMessageReceived(_logAction);
                }

                if (SettingsMenu.GetValue<bool>(ToolsMenu.UnlockFPS))
                    Application.targetFrameRate = Convert.ToInt32(SettingsMenu.GetValue<object>(ToolsMenu.TargetFPS));

                else
                    Application.targetFrameRate = GraphicsSettings.DefaultTargetFrameRate;

                var fgdebug = Resources.FindObjectsOfTypeAll<GvrFPS>().FirstOrDefault();
                if (fgdebug != null)
                {
                    fgdebug.gameObject.SetActive(false);
                    fgdebug._keepActive = false;
                    var scale = Convert.ToSingle(SettingsMenu.GetValue<object>(ToolsMenu.FGDebugScale));
                    fgdebug.transform.localScale = new Vector3(scale, scale, scale);
                }

#if CHEATS
                foreach (var afk in Resources.FindObjectsOfTypeAll<AFKManager>())
                    afk.enabled = SettingsMenu.GetValue<bool>(ToolsMenu.DisableAFK);

                GlobalDebug.DebugJoinAsSpectatorEnabled = SettingsMenu.GetValue<bool>(ToolsMenu.JoinAsSpectator);

                RollFGCCSettings();
#endif

                Broadcaster.Instance.Broadcast(new GlobalDebug.DebugToggleMinimalisticFPSCounter());

                Broadcaster.Instance.Broadcast(new GlobalDebug.DebugToggleFPSCounter());

                SettingsMenu.RollSave();

                AudioManager.Instance.PlayOneShot(AudioManager.EventMasterData.SettingsAccept, null, default);
            }
            catch (Exception ex)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_settings_save_title"), LocalizationManager.LocalizedString("error_settings_save_desc", [ex.Message]), ModalType.MT_OK, OKButtonType.Disruptive);
            }
        }

        void WatermarkGUI()
        {
            string watermark = $"<b>{BuildInfo.Name} V{BuildInfo.Version} {BuildInfo.Description.Substring(BuildInfo.Description.IndexOf("by"))}</b>";

            GUIStyle upper = new(UnityEngine.GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = (int)(0.016f * Screen.height),
            };
            GUIStyle bottom = new(UnityEngine.GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = (int)(0.016f * Screen.height),
                normal = { textColor = BuildInfoColor },
            };

            float labelWidth = 500f;
            float labelHeight = 25f;
            float labelX = (Screen.width - labelWidth) / 2f;
            float labelY = Screen.height - labelHeight;

            UnityEngine.GUI.Label(new Rect(labelX, labelY, labelWidth, labelHeight), watermark, bottom);
            UnityEngine.GUI.Label(new Rect(labelX, labelY - 2f, labelWidth, labelHeight), watermark, upper);
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (SettingsMenu.GetValue<bool>(ToolsMenu.TrackGameDebug))
            {
                var c = new GUI_LogEntry()
                {
                    Msg = logString,
                    Stacktrace = stackTrace,
                    Type = type,
                };

                GUIUtil.ProcessNewLog(c);

                string FileEntry = "[" + type + "] : " + logString + " \n[STACK TRACE] " + stackTrace;

                /*switch (type)
                {
                    case LogType.Error:
                        MelonLogger.Error(logString);
                        break;
                    case LogType.Warning:
                        MelonLogger.Warning(logString);
                        break;
                    case LogType.Log:
                        MelonLogger.Msg(logString);
                        break;
                    default:
                        MelonLogger.Msg(logString);
                        break;
                }*/

                AllLogs.AppendLine(FileEntry);

                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);  

                string a = Path.Combine(LogDir, $"Log_{NextLogDate}.log");

                if (!File.Exists(a))
                    File.Create(a);

                File.WriteAllText(a, AllLogs.ToString());
            }
        }
#if CHEATS
        public void SetupFGCCData()
        {

            ActiveFGCCData = Resources.FindObjectsOfTypeAll<CharacterControllerData>().FirstOrDefault();

            if (ActiveFGCCData != null && DefaultFGCCData == null)
            {
                DefaultFGCCData = new object[255];
                DefaultFGCCData[0] = ActiveFGCCData.normalMaxSpeed;
                DefaultFGCCData[1] = ActiveFGCCData.jumpForceUltimateParty;
                DefaultFGCCData[2] = ActiveFGCCData.divePlayerSensitivity;
                DefaultFGCCData[3] = ActiveFGCCData.maxGravityVelocity;
                DefaultFGCCData[4] = ActiveFGCCData.diveForce;
                DefaultFGCCData[5] = ActiveFGCCData.airDiveForce;
            }
    }

        void ResetFGCCData()
        {
            if (ActiveFGCCData != null && DefaultFGCCData != null)
            {
                ActiveFGCCData.normalMaxSpeed = (float)DefaultFGCCData[0];
                ActiveFGCCData.jumpForceUltimateParty = (Vector3)DefaultFGCCData[1];
                ActiveFGCCData.divePlayerSensitivity = (float)DefaultFGCCData[2];
                ActiveFGCCData.maxGravityVelocity = (float)DefaultFGCCData[3];
                ActiveFGCCData.diveForce = (float)DefaultFGCCData[4];
                ActiveFGCCData.airDiveForce = (float)DefaultFGCCData[5];
            }
    }
        public void RollFGCCSettings()
        {
            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);
            if (player != null)
            {
                var motorAgent = player.MotorAgent;

                if (SettingsMenu.GetValue<bool>(ToolsMenu.DisableMonitorCheck))
                {
                    Vector3 defJump = (Vector3)DefaultFGCCData[1];
                    ActiveFGCCData.normalMaxSpeed = (float)DefaultFGCCData[0] + float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.RunSpeedModifier).ToString());
                    ActiveFGCCData.jumpForceUltimateParty = new Vector3(defJump.x, defJump.y + float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.JumpYModifier).ToString()), defJump.z); ;
                    ActiveFGCCData.divePlayerSensitivity = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.DiveSens).ToString());
                    ActiveFGCCData.maxGravityVelocity = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.GravityChange).ToString());
                    ActiveFGCCData.diveForce = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.DiveForce).ToString());
                    ActiveFGCCData.airDiveForce = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.DiveInAirForce).ToString());
                }
                else
                    ResetFGCCData();

                motorAgent.GetMotorFunction<MotorFunctionJump>()._jumpForce = ActiveFGCCData.jumpForceUltimateParty;
            }
        }


        public void TeleportToFinish()
        {
            if (!DefaultCheck())
                return;

            var finish = Resources.FindObjectsOfTypeAll<COMMON_ObjectiveReachEndZone>().FirstOrDefault();
            if (finish == null)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_no_finish"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
                return;
            }

            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);
            player.transform.SetPositionAndRotation(finish.transform.position + new Vector3(0, 10f, 0), finish.transform.rotation);
        }

        public void TeleportToSafeZone()
        {
            if (!DefaultCheck())
                return;

            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);

            var safeZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeZone.name = "Safe";
            safeZone.transform.localScale = new Vector3(200, 5, 200);
            safeZone.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 150, player.transform.position.z);
            player.transform.position = safeZone.transform.position + new Vector3(0, 10, 0);
            safeZone.GetComponent<MeshRenderer>().enabled = false;
        }

        public void TeleportToRandomPlayer()
        {
            if (!DefaultCheck())
                return;

            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);

            var target = players[UnityEngine.Random.RandomRange(0, players.Count)];
            player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        }

        public void TogglePlayers()
        {
            if (!DefaultCheck())
                return;

            playersHidden = !playersHidden;
            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            foreach (var player in players)
                player.gameObject.SetActive(!playersHidden);
        }

        bool DefaultCheck()
        {
            if (GlobalGameStateClient.Instance.IsInMainMenu)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_in_menu"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
                return false;
            }
            if (!GlobalGameStateClient.Instance.GameStateView.IsGamePlaying)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_game_not_active"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
                return false;
            }
            
            return true;
        }

        void ForceUnHidePlayers()
        {
            playersHidden = false;
            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            foreach (var player in players)
                player.gameObject.SetActive(!playersHidden);
        }
#endif

        public void ForceMainMenu()
        {
            UIManager.Instance.RemoveAllScreens();
            GlobalGameStateClient.Instance.ResetGame();
            GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(new StateMainMenu(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), false, false).Cast<IGameState>());
        }

        double _peakMemUsage;
        public override void OnGUI()
        {
            if (SettingsMenu.GetValue<bool>(ToolsMenu.Watermark))
                WatermarkGUI();

            if (!SettingsMenu.GetValue<bool>(ToolsMenu.GUI))
                return;

            GUIStyle debugL = new(UnityEngine.GUI.skin.label)
            {
                fontSize = (int)(0.018f * Screen.height),
            };

            var sb = new StringBuilder();
            //var memUsage = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            //if (memUsage > _peakMemUsage)
            //    _peakMemUsage = memUsage;

            sb.AppendLine("<b>DEBUG</b>");

            sb.AppendLine($"Active state: {ActivePlayerState}");
            sb.AppendLine($"Prev state: {PreviousPlayerState}");
            sb.AppendLine($"Version: {BuildInfo.Version}");
            sb.AppendLine($"Game Version: {Application.version}");
            //sb.AppendLine($"MEM: {memUsage:F2} MB");
            //sb.AppendLine($"MEM PEAK: {_peakMemUsage:F2} MB");
            sb.AppendLine($"Session Length: {DateTime.UtcNow - Startup:hh\\:mm\\:ss}");

            var s = sb.ToString();
            var size = debugL.CalcSize(new(s));
            var offset = 25f;

            UnityEngine.GUI.Box(new Rect(-1, -1, size.x + offset + 10, size.y + 10f), "");
            UnityEngine.GUI.Box(new Rect(-1, -1, size.x + offset + 10, size.y + 10f), "");
            UnityEngine.GUI.Box(new Rect(-1, -1, size.x + offset + 10, size.y + 10f), "");

            UnityEngine.GUI.Label(new Rect(offset, 15, size.x + 10f, size.y + 10f), s, debugL);
        }
    }
}