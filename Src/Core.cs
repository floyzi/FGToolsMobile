using Il2CppFGClient;
using Il2CppFGClient.UI.Core;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using NOTFGT;
using NOTFGT.FLZ_Common;
using NOTFGT.FLZ_Common.GUI;
using NOTFGT.FLZ_Common.Loader;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using static Il2CppFG.Common.GameStateMachine;
using static MelonLoader.MelonLogger;

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
            public readonly string DisplayName;

            public BuildDetails(string config, string file_version, string commit, string date)
            {
                Config = config;
                Commit = commit;
                Version = file_version;

                if (long.TryParse(date, out long unixTimestamp))
                    BuildDate = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            }

            public override string ToString()
            {
                return $"Env: {Config} Commit: #{GetCommit()} Build Date: {BuildDate}";
            }

            internal string GetCommit()
            {
                return Commit.Length > 12 ? Commit[..12] : Commit;
            }
        }

        internal static BuildDetails BuildInfo;
        internal static Color BuildInfoColor = new(0.3764f, 0.0156f, 0.0156f, 1f);

        #region PATHS
        public static string MelonDir => Path.Combine("/sdcard", "MelonLoader", Application.identifier);
        public static string MainDir => Path.Combine(MelonDir, "Mods", "NOT_FGTools");
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

                ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();
                ClassInjector.RegisterTypeInIl2Cpp<ToolsButton>();
                ClassInjector.RegisterTypeInIl2Cpp<UnityDragFix>();
                ClassInjector.RegisterTypeInIl2Cpp<TrackedEntry>();

                HarmonyInstance.PatchAll(typeof(HarmonyPatches.Default));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.CaptureTools));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.GUITweaks));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.RoundLoader));

                StartupDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Error($"Boot failed!\n{ex}");
                SucceedLaunch = false;
            }
        }

        public override void OnLateInitializeMelon()
        {
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
                SucceedLaunch = false;
                FLZ_ToolsManager.Instance.GUIUtil.ShowRepairGUI(e);
            }
        }

        public override void OnUpdate() => OnUpdateСommon?.Invoke();

        void WatermarkGUI()
        {
            string watermark = $"<b>{DefaultName} V{BuildInfo.Version} {Description[Description.IndexOf("by")..]}</b>";

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


        double _peakMemUsage;
        public override void OnGUI()
        {
            if (ShowWatermark)
                WatermarkGUI();

            if (!ShowDebugUI)
                return;

            GUIStyle debugL = new(GUI.skin.label)
            {
                fontSize = (int)(0.018f * Screen.height),
            };

            var sb = new StringBuilder();
            //var memUsage = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            //if (memUsage > _peakMemUsage)
            //    _peakMemUsage = memUsage;

            sb.AppendLine("<b>DEBUG</b>");

            sb.AppendLine($"Active state: {FLZ_ToolsManager.Instance.ActivePlayerState}");
            sb.AppendLine($"Prev state: {FLZ_ToolsManager.Instance.PreviousPlayerState}");
            sb.AppendLine($"Version: {BuildInfo.Version}");
            sb.AppendLine($"Game Version: {Application.version}");
            //sb.AppendLine($"MEM: {memUsage:F2} MB");
            //sb.AppendLine($"MEM PEAK: {_peakMemUsage:F2} MB");
            sb.AppendLine($"Session Length: {DateTime.UtcNow - StartupDate:hh\\:mm\\:ss}");

            var s = sb.ToString();
            var size = debugL.CalcSize(new(s));
            var offset = 25f;

            GUI.Box(new Rect(-1, -1, size.x + offset + 10, size.y + 10f), "");
            GUI.Box(new Rect(-1, -1, size.x + offset + 10, size.y + 10f), "");
            GUI.Box(new Rect(-1, -1, size.x + offset + 10, size.y + 10f), "");

            GUI.Label(new Rect(offset, 15, size.x + 10f, size.y + 10f), s, debugL);
        }
    }
}