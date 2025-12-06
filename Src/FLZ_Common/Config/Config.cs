using Il2Cpp;
using Il2CppFGClient;
using MelonLoader;
using NOTFGT.FLZ_Common.Config.Entries;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Il2CppFGClient.UI.UIModalMessage;
using static UnityStandardAssets.Utility.TimedObjectActivator;
using Action = System.Action;

namespace NOTFGT.FLZ_Common.Config
{
    internal class Config
    {
        internal class ConfigEntry
        {
            [JsonPropertyName("id")]
            public string ConfigID { get; set; }
            [JsonPropertyName("val")]
            public object Value { get; set; }
        }

        internal class ConfigModel
        {
            [JsonPropertyName("loc")]
            public string Locale { get; set; } = "en";
            [JsonPropertyName("saved_values")]
            public List<ConfigEntry> SavedEntries { get; set; }
        }

        internal ConfigModel SavedConfig;
        internal EntriesManager EntriesManager;
        internal Config(Action onInit)
        {
            MelonCoroutines.Start(WaitForFallGuys(new(() =>
            {
                EntriesManager = new();
                CheckConfig();
                LocalizationManager.Setup();
                onInit.Invoke();
            })));
        }

        IEnumerator WaitForFallGuys(Action onceReady)
        {
            while (GlobalGameStateClient.Instance.PlayerProfile == null)
                yield return null;

            onceReady();
        }

        void CheckConfig()
        {
            if (!File.Exists(Core.ConfigFile))
            {
                SaveConfig(true);
                return;
            }

            MelonLogger.Msg("Attempt to load exising config...");

            try
            {
                var cfg = JsonSerializer.Deserialize<ConfigModel>(File.ReadAllText(Core.ConfigFile));
                EntriesManager.LoadFromSave(cfg.SavedEntries);
                SavedConfig = cfg;
                MelonLogger.Msg("Config loaded");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Config load failed!\n{e}");
            }
        }

        internal void SaveConfig(bool cleanup)
        {
            SavedConfig ??= new();
            var res = EntriesManager.GetForSave(cleanup);
            SavedConfig.SavedEntries = res;
            File.WriteAllText(Core.ConfigFile, JsonSerializer.Serialize<ConfigModel>(SavedConfig));
        }

        internal void DoUIConfigSave()
        {
            try
            {
                SaveConfig(false);
                AudioManager.Instance.PlayOneShot(AudioManager.EventMasterData.SettingsAccept, null, default);
            }
            catch (Exception ex)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_settings_save_title"), LocalizationManager.LocalizedString("error_settings_save_desc", [ex.Message]), ModalType.MT_OK, OKButtonType.Disruptive);
            }
        }

        internal void DeleteConfig()
        {
            if (!File.Exists(Core.ConfigFile))
                return;

            File.Delete(Core.ConfigFile);
        }
    }
}
