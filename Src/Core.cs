using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using NOTFGT;
using NOTFGT.FLZ_Common;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI;
using NOTFGT.FLZ_Common.Loader;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using static MelonLoader.MelonLogger;
using static NOTFGT.FLZ_Common.HarmonyPatches;

//version from this attribute is not used, change version in project instead
[assembly: MelonInfo(typeof(Core), DefaultName, "0.0.0", "Floyzi", null)]
[assembly: MelonGame("Mediatonic", "Fall Guys")]
namespace NOTFGT
{
    public class Core : MelonMod
    {
        public readonly struct BuildDetails
        {
            public readonly string Config;
            public readonly string Commit;
            public readonly DateTime BuildDate;
            public readonly string Version;

            public BuildDetails(string config, string file_version, string commit, string date)
            {
                Config = config;
                Commit = commit;
                Version = file_version;

                if (long.TryParse(date, out long unixTimestamp))
                    BuildDate = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            }

            public override string ToString() => $"Env: {Config} Commit: #{GetCommit()} Build Date: {BuildDate}";
            internal string GetCommit() => Commit.Length > 12 ? Commit[..12] : Commit;
        }

        internal static BuildDetails BuildInfo;
        internal static Color BuildInfoColor = new(0.3764f, 0.0156f, 0.0156f, 1f);

        #region PATHS
        public static string MelonDir => Path.Combine("/sdcard", "MelonLoader");
        public static string MelonGameDir => Path.Combine(MelonDir, Application.identifier);
        public static string MelonLogsDir => Path.Combine(MelonGameDir, "MelonLoader", "Logs");
        public static string CurrentMelonLog => Path.Combine(MelonGameDir, "MelonLoader", "Latest.log");
        public static string MainDir => Path.Combine(MelonGameDir, "Mods", "NOT_FGTools");
        public static string LogDir => Path.Combine(MainDir, "Logs");
        public static string AssetsDir => Path.Combine(MainDir, "Assets");
        public static string MobileLoading => Path.Combine(AssetsDir, "loading_screen.png");
        public static string MobileSplash => Path.Combine(AssetsDir, "splash.png");
        public static string ConfigFile => Path.Combine(AssetsDir, "ConfigV3.json");
        #endregion

        internal static DateTime StartupDate;
        internal static bool SucceedLaunch = true;

        internal static Action OnUpdateСommon;
        internal static Action OnDrawGUI;

        internal static bool ShowDebugUI;
        internal static bool ShowWatermark = true;
        string ByMarker;
#if MELON_LOGS
        readonly HashSet<string> MelonLogs = [];
        StreamReader LogsReader;
        FileStream LogsStream;
#endif

        public override void OnInitializeMelon()
        {
            Msg("Boot...");

            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                var version = FileVersionInfo.GetVersionInfo(assembly.Location);
                var productVersion = version.ProductVersion.Split('+');
                var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
                var cfg = assembly.GetCustomAttributes<AssemblyConfigurationAttribute>().ToList()[0].Configuration;

                var buildDate = metadata.FirstOrDefault(x => x.Key == "BuildDate")?.Value;

                BuildInfo = new BuildDetails(cfg, productVersion[0], productVersion.Length > 1 ? productVersion[1] : "LOCAL BUILD", buildDate);

                Msg(BuildInfo.ToString());

                StartupDate = DateTime.UtcNow;

                ByMarker = Description[Description.IndexOf("by")..];

                ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();
                ClassInjector.RegisterTypeInIl2Cpp<ToolsButton>();
                ClassInjector.RegisterTypeInIl2Cpp<UnityDragFix>();
                ClassInjector.RegisterTypeInIl2Cpp<MenuCategory>();
                ClassInjector.RegisterTypeInIl2Cpp<TrackedEntry>();

                HarmonyInstance.PatchAll(typeof(FLZ_LoginPatches));
                HarmonyInstance.PatchAll(typeof(Default));
                HarmonyInstance.PatchAll(typeof(CaptureTools));
                HarmonyInstance.PatchAll(typeof(GUITweaks));
                HarmonyInstance.PatchAll(typeof(RoundLoader));

#if MELON_LOGS
                LogsStream = new FileStream(CurrentMelonLog, FileMode.Open, FileAccess.Read, FileShare.Read);
                LogsReader = new StreamReader(LogsStream);
#endif
            }

            catch (Exception ex)
            {
                Error($"Boot failed!\n{ex}");
                InitFail();
            }
        }

        public override void OnLateInitializeMelon()
        {
            if (!SucceedLaunch) return;

            Msg("Startup...");

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<FLZ_ToolsManager>();

                var monoMain = new GameObject(DefaultName);
                monoMain.AddComponent<FLZ_ToolsManager>();
                monoMain.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e)
            {
                Error($"Startup failed!\n{e}");
                InitFail();
            }
        }

        void InitFail()
        {
            SucceedLaunch = false;

            FLZ_AndroidExtensions.ShowToast($"Unable to init {DefaultName}. See logs for details");
        }

        public override void OnUpdate()
        {
            OnUpdateСommon?.Invoke();
#if MELON_LOGS
            string line;
            while ((line = LogsReader.ReadLine()) != null)
            {
                MelonLogs.Add(line);
            }
#endif
        }

        void WatermarkGUI()
        {
            string watermark = $"<b>{DefaultName} V{BuildInfo.Version} {ByMarker}</b>";

            GUIStyle upper = new(GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = (int)(0.016f * Screen.height),
            };
            GUIStyle bottom = new(GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = (int)(0.016f * Screen.height),
                normal = { textColor = BuildInfoColor },
            };

            float labelWidth = 500f;
            float labelHeight = 25f;
            float labelX = (Screen.width - labelWidth) / 2f;
            float labelY = Screen.height - labelHeight;

            GUI.Label(new Rect(labelX, labelY, labelWidth, labelHeight), watermark, bottom);
            GUI.Label(new Rect(labelX, labelY - 2f, labelWidth, labelHeight), watermark, upper);
        }

#if MELON_LOGS
        void ShowLog()
        {
            GUIStyle logs = new(GUI.skin.label)
            {
                fontSize = (int)(0.018f * Screen.height),
            };

            GUI.Label(new(5, 5, Screen.width - 1300, Screen.height), string.Join("\n", MelonLogs.TakeLast(10)), logs);
        }
#endif

        public override void OnGUI()
        {
#if MELON_LOGS
            ShowLog();
#endif
            if (ShowWatermark)
                WatermarkGUI();

            if (!ShowDebugUI)
                return;

            GUIStyle debugL = new(GUI.skin.label)
            {
                fontSize = (int)(0.018f * Screen.height),
            };

            var sb = new StringBuilder();

            sb.AppendLine("<b>DEBUG</b>");

            sb.AppendLine($"Active state: {FLZ_ToolsManager.Instance.ActivePlayerState}");
            sb.AppendLine($"Prev state: {FLZ_ToolsManager.Instance.PreviousPlayerState}");
            sb.AppendLine($"Version: {BuildInfo.Version}");
            sb.AppendLine($"Game Version: {Application.version}");
            sb.AppendLine($"Session Length: {DateTime.UtcNow - StartupDate:hh\\:mm\\:ss}");

            var s = sb.ToString();
            var size = debugL.CalcSize(new(s));
            var offset = 25f;

            var r = new Rect(-1, -1, size.x + offset + 10, size.y + 10f);
            GUI.Box(r, "");
            GUI.Box(r, "");
            GUI.Box(r, "");

            GUI.Label(new Rect(offset, 15, size.x + 10f, size.y + 10f), s, debugL);
        }
    }
}