using Il2Cpp;
using Il2CppEvents;
using Il2CppFG.Common;
using Il2CppFG.Common.Character;
using Il2CppFGClient;
using Il2CppFGClient.UI;
using Il2CppFGClient.UI.Core;
using Il2CppFGDebug;
using MelonLoader;
using NOTFGT.FLZ_Common.GUI;
using NOTFGT.FLZ_Common.Loader;
using NOTFGT.FLZ_Common.Localization;
using NOTFGT.FLZ_Common.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2Cpp.GameStateEvents;
using static Il2CppFG.Common.CommonEvents;
using static Il2CppFG.Common.GameStateMachine;
using static Il2CppFGClient.GlobalGameStateClient;
using static Il2CppFGClient.UI.UIModalMessage;
using static MelonLoader.MelonLogger;

namespace NOTFGT.FLZ_Common
{
    internal class FLZ_ToolsManager : MonoBehaviour
    {
        internal static FLZ_ToolsManager Instance;
        internal static bool IsInGameplay => GlobalGameStateClient.Instance.GameStateView.IsGamePlaying;
        internal static bool IsOnRound => GlobalGameStateClient.Instance.GameStateView.IsGameLevelLoaded;
        internal static bool IsOnCountdown => GlobalGameStateClient.Instance.GameStateView.IsGameCountingDown;
        internal static bool IsInMenu => GlobalGameStateClient.Instance._gameStateMachine.IsInState<StateMainMenu>();
        internal static bool IsSpectator => GlobalGameStateClient.Instance.GameStateView.IsSpectator;
        internal static bool IsPartyLeader => PlatformServices.Instance != null && PlatformServices.Instance._corePartyService.IsPartyLeader();
        internal static bool IsInRoundLoader => Instance.ActivePlayerState == PlayerState.RoundLoader;
        internal static bool IsInRealGame => Instance.ActivePlayerState == PlayerState.RealGame;

        internal static Action OnRoundStarts;
        internal static Action OnRoundEnds;
        internal static Action OnIntroStarts;
        internal static Action OnIntroEnds;
        internal static Action OnMenuEnter;
        internal static Action OnSpectatorEvent;
        internal static Action OnAllPlayersSpawnedEvent;
        internal static Action OnFinished;
        internal static Action<string, string, LogType> OnLog;
        internal static Action OnGUIInit;

        internal bool TrackGameLog = true;
        bool _FPSCounter;
        internal bool FPSCounter
        {
            get => _FPSCounter;
            set
            {
                if (value)
                {
                    _FPSCounter = true;
                    _FGDebug = false;
                }
                else
                {
                    _FPSCounter = false;
                }
            }
        }
        bool _FGDebug;
        internal bool FGDebug
        {
            get => _FGDebug;
            set
            {
                if (value)
                {
                    _FGDebug = true;
                    _FPSCounter = false;
                }
                else
                {
                    _FGDebug = false;
                }
            }
        }
        internal bool UnlockFPS;
        internal int TargetFPS = 60;
        internal float FGDebugScale = 0.6f;

        public enum PlayerState
        {
            Unknown, 
            Loading, 
            Menu, 
            RealGame, 
            RoundLoader
        }
        public PlayerState ActivePlayerState = PlayerState.Unknown;
        public PlayerState PreviousPlayerState = PlayerState.Unknown;

        readonly string NextLogDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        StringBuilder AllLogs = new();

        public GUI_Util GUIUtil;
        public ToolsMenu SettingsMenu;
        public RoundLoaderService RoundLoader;
        public FLZ_Game InGameManager;

        internal void Awake()
        {
            Instance = this;

            GUIUtil = new();
            RoundLoader = new();
            InGameManager = new();
            SettingsMenu = new();

            GUIUtil.Register();

            LocalizationManager.Setup();

            HandlePlayerState(PlayerState.Loading);

            if (TrackGameLog)
            {
                OnLog = new(OnLogHandle);
                Application.add_logMessageReceived(OnLog);
            }

            Broadcaster.Instance.Register<IntroCountdownEndedEvent>(new Action<IntroCountdownEndedEvent>(OnRoundStart));
            Broadcaster.Instance.Register<IntroCameraSequenceStartedEvent>(new Action<IntroCameraSequenceStartedEvent>(OnIntroStart));
            Broadcaster.Instance.Register<IntroCameraSequenceEndedEvent>(new Action<IntroCameraSequenceEndedEvent>(OnIntroEnd));
            Broadcaster.Instance.Register<InitialiseClientOverlayEvent>(new Action<InitialiseClientOverlayEvent>(OnOverlayInit));
            Broadcaster.Instance.Register<OnSpectatingPlayer>(new Action<OnSpectatingPlayer>(OnSpectator));
            Broadcaster.Instance.Register<OnRoundOver>(new Action<OnRoundOver>(OnRoundEnd));
            Broadcaster.Instance.Register<OnMainMenuDisplayed>(new Action<OnMainMenuDisplayed>(OnEnterMenu));
            Broadcaster.Instance.Register<OnLocalPlayersFinished>(new Action<OnLocalPlayersFinished>(OnFinish));
        }

        void OnEnterMenu(OnMainMenuDisplayed evt) => OnMenuEnter();
        void OnSpectator(OnSpectatingPlayer evt) => OnSpectatorEvent();
        void OnFinish(OnLocalPlayersFinished evt) => OnFinished();
        void OnRoundStart(IntroCountdownEndedEvent evt) => OnRoundStarts();
        void OnRoundEnd(OnRoundOver evt) => OnRoundEnds();
        void OnIntroStart(IntroCameraSequenceStartedEvent evt) => OnIntroStarts();
        void OnIntroEnd(IntroCameraSequenceEndedEvent evt) => OnIntroEnds();
        void OnOverlayInit(InitialiseClientOverlayEvent evt) => OnGUIInit();
        void OnLogHandle(string logString, string stackTrace, LogType type)
        {
            if (TrackGameLog)
            {
                var c = new GUI_LogEntry()
                {
                    Msg = logString,
                    Stacktrace = stackTrace,
                    Type = type,
                };

                GUIUtil.ProcessNewLog(c);

                var stacktaceLine = string.IsNullOrEmpty(stackTrace) ? null : $"\n{stackTrace}";
                var entry = $"[{type}] {logString}{stacktaceLine}";

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

                AllLogs.AppendLine(entry);

                if (!Directory.Exists(Core.LogDir))
                    Directory.CreateDirectory(Core.LogDir);

                var a = Path.Combine(Core.LogDir, $"Log_{NextLogDate}.log");

                if (!File.Exists(a))
                    File.Create(a);

                File.WriteAllText(a, AllLogs.ToString());
            }
        }

        [Obsolete("")]
        public void ApplyChanges()
        {
            try
            {
                if (TrackGameLog && OnLog == null)
                {
                    OnLog = new(OnLogHandle);
                    Application.add_logMessageReceived(OnLog);
                }
                else if (OnLog != null && !TrackGameLog)
                {
                    Application.remove_logMessageReceived(OnLog);
                }

                if (UnlockFPS)
                    Application.targetFrameRate = TargetFPS;
                else
                    Application.targetFrameRate = 60;

#if CHEATS
                foreach (var afk in Resources.FindObjectsOfTypeAll<AFKManager>())
                    afk.enabled = InGameManager.DisableAFK;

#endif

                SettingsMenu.SaveConfig();

                AudioManager.Instance.PlayOneShot(AudioManager.EventMasterData.SettingsAccept, null, default);
            }
            catch (Exception ex)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_settings_save_title"), LocalizationManager.LocalizedString("error_settings_save_desc", [ex.Message]), ModalType.MT_OK, OKButtonType.Disruptive);
            }
        }

        internal void ResolveFGDebug()
        {
            var fgdebug = Resources.FindObjectsOfTypeAll<GvrFPS>().FirstOrDefault();
            if (fgdebug != null)
            {
                fgdebug.gameObject.SetActive(false);
                fgdebug._keepActive = false;
                fgdebug.transform.localScale = new Vector3(FGDebugScale, FGDebugScale, FGDebugScale);
            }

            Broadcaster.Instance.Broadcast(new GlobalDebug.DebugToggleMinimalisticFPSCounter());
            Broadcaster.Instance.Broadcast(new GlobalDebug.DebugToggleFPSCounter());
        }

        public void HandlePlayerState(PlayerState playerState)
        {
            PreviousPlayerState = ActivePlayerState;
            ActivePlayerState = playerState;
        }

        public void ForceMainMenu()
        {
            UIManager.Instance.RemoveAllScreens();
            GlobalGameStateClient.Instance.ResetGame();
            GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(new StateMainMenu(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), false, false).Cast<IGameState>());
        }
    }
}
