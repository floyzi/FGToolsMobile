using Il2Cpp;
using Il2CppEvents;
using Il2CppFG.Common;
using Il2CppFG.Common.Character;
using Il2CppFG.Common.CMS;
using Il2CppFGClient;
using Il2CppFGClient.UI;
using Il2CppFGClient.UI.Core;
using Il2CppFGDebug;
using Il2CppMediatonic.Tools.MVVM;
using Il2CppTMPro;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI;
using NOTFGT.FLZ_Common.Loader;
using NOTFGT.FLZ_Common.Localization;
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
        internal bool IsOwoifyEnabled;
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

        public Config Config { get; private set; }
        public GUI_Util GUIUtil { get; private set; }
        public ToolsMenu SettingsMenu { get; private set; }
        public RoundLoaderService RoundLoader { get; private set; }
        public FLZ_Game InGameManager { get; private set; }

        internal void Awake()
        {
            Instance = this;

            HandlePlayerState(PlayerState.Loading);

            try
            {
                SettingsMenu = new();
                InGameManager = new();
                RoundLoader = new();
                Config = new(() =>
                {
                    GUIUtil = new(() =>
                    {
                        if (TrackGameLog)
                        {
                            OnLog = new(OnLogHandle);
                            Application.add_logMessageReceived(OnLog);
                        }
                    });
                });
            }
            catch (Exception e)
            {
                Core.InitFail(e);
            }
           
            Broadcaster.Instance.Register<IntroCountdownEndedEvent>(new Action<IntroCountdownEndedEvent>(OnRoundStart));
            Broadcaster.Instance.Register<IntroCameraSequenceStartedEvent>(new Action<IntroCameraSequenceStartedEvent>(OnIntroStart));
            Broadcaster.Instance.Register<IntroCameraSequenceEndedEvent>(new Action<IntroCameraSequenceEndedEvent>(OnIntroEnd));
            Broadcaster.Instance.Register<InitialiseClientOverlayEvent>(new Action<InitialiseClientOverlayEvent>(OnOverlayInit));
            Broadcaster.Instance.Register<OnSpectatingPlayer>(new Action<OnSpectatingPlayer>(OnSpectator));
            Broadcaster.Instance.Register<OnRoundOver>(new Action<OnRoundOver>(OnRoundEnd));
            Broadcaster.Instance.Register<OnMainMenuDisplayed>(new Action<OnMainMenuDisplayed>(OnEnterMenu));
            Broadcaster.Instance.Register<OnLocalPlayersFinished>(new Action<OnLocalPlayersFinished>(OnFinish));

            Msg("Successful startup!");
        }

        void FixedUpdate() => GUIUtil?.RefreshEntries();
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
                _ = new GUI_LogEntry(logString, stackTrace, type);

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

        internal void ResolveLogTracking()
        {
            GUIUtil.LogDisabledScreen.SetActive(!TrackGameLog);

            if (TrackGameLog && OnLog == null)
            {
                OnLog = new(OnLogHandle);
                Application.add_logMessageReceived(OnLog);
            }
            else if (OnLog != null && !TrackGameLog)
            {
                Application.remove_logMessageReceived(OnLog);
            }
        }

        internal void ResolveAFK()
        {
            foreach (var afk in Resources.FindObjectsOfTypeAll<AFKManager>())
                afk.enabled = InGameManager.DisableAFK;
        }

        internal void ResolveFPS()
        {
            if (!UnlockFPS)
            {
                switch (GlobalGameStateClient.Instance.PlayerProfile.GraphicsSettings.FPSPreset)
                {
                    case GraphicsSettings.FPSPresets.Low:
                        TargetFPS = 20;
                        break;
                    case GraphicsSettings.FPSPresets.Medium:
                        TargetFPS = 30;
                        break;
                    case GraphicsSettings.FPSPresets.High:
                        TargetFPS = 60;
                        break;
                }
            }
        }

        public void HandlePlayerState(PlayerState playerState)
        {
            PreviousPlayerState = ActivePlayerState;
            ActivePlayerState = playerState;
        }

        public void ForceMainMenu()
        {
            FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("force_menu_modal_title"), LocalizationManager.LocalizedString("force_menu_modal_desc"), ModalType.MT_OK_CANCEL, OKButtonType.Disruptive, new Action<bool>((bool wasok) =>
            {
                if (!wasok) return;

                UIManager.Instance.RemoveAllScreens();
                GlobalGameStateClient.Instance.ResetGame();
                GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(new StateMainMenu(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), false, false).Cast<IGameState>());
            }));
        }

        internal void ResolveOwoify()
        {
            if (!Instance.IsOwoifyEnabled)
                Owoify.DeOwoify();
            else
            {
                var txt = FindObjectsOfType<TMP_Text>(true);

                GUIUtil.FlashImage(1.35f);

                foreach (var tmp in txt)
                    tmp.text = tmp.text; //calling setter on text so patch can do it's job
            }

            Broadcaster.Instance.Broadcast(new LocalisedStrings.StringsChangedEvent());
        }
    }
}
