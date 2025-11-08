using Il2Cpp;
using Il2CppFG.Common.CMS;
using Il2CppFGClient;
using Il2CppFGDebug;
using MelonLoader;
using NOTFGT.FLZ_Common.Localization;
using NOTFGT.FLZ_Common.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using File = System.IO.File;
using Path = System.IO.Path;

namespace NOTFGT.FLZ_Common.GUI
{
    public class ToolsMenu
    {
        const string FGCC_Cat = "menu_fgcc_section";
        const string FPS_Cat = "menu_fps_section";
        const string Def_Cat = "menu_section";
        const string Gameplay_Cat = "menu_gp_section";
       
        internal class MenuEntry(MenuEntry.Type type, string configId, string category, string displayName, string desc, Func<object, object> setter = null, Action onSet = null, List<object> additionalConfig = null)
        {
            public enum Type
            {
                Toggle,
                InputField,
                Slider,
                Button
            }

            /// <summary>
            /// Name of this entry in config file, should be unique for each entry
            /// </summary>
            internal string ConfigID { get; set; } = configId;

            /// <summary>
            /// Category where element will be placed on UI.
            /// Represents ID of localized string
            /// </summary>
            internal string Category { get; set; } = category;

            /// <summary>
            /// Name of element that will be shown on UI.
            /// Represents ID of localized string
            /// </summary>
            internal string DisplayName { get; set; } = displayName;

            /// <summary>
            /// Description of element that will be shown on UI.
            /// Represents ID of localized string
            /// </summary>
            internal string Description { get; set; } = desc;

            /// <summary>
            /// Type of entry, indicates how this entry will be shown on UI
            /// </summary>
            internal Type EntryType { get; set; } = type;

            /// <summary>
            /// Additional properties of entry.
            /// <para><see cref="Type.Slider"/> — [0] value type, [1] slider min value, [2] slider max value</para>
            /// <para><see cref="Type.InputField"/> — [0] value type, [1] char limit</para>
            /// </summary>
            internal List<object> AdditionalConfig = additionalConfig;

            /// <summary>
            /// Used to set and get updated value of field that attached to this entry
            /// </summary>
            internal Func<object, object> Setter => setter;

            /// <summary>
            /// Initial field value of field. Null if field is not specefied
            /// </summary>
            internal object InitialValue = setter?.Invoke(null);

            /// <summary>
            /// Action that will invoked after Setter 
            /// </summary>
            internal Action Act = onSet;

            /// <summary>
            /// Indicates can be entry included in config save or no
            /// </summary>
            internal bool CanBeSaved => EntryType != Type.Button && !string.IsNullOrEmpty(ConfigID);

            /// <summary>
            /// Action that will be invoked after every change of any existing entry
            /// </summary>
            internal Action<MenuEntry, object> OnChange;

            internal object GetValue() => setter?.Invoke(null);

            internal void Set(object val)
            {
                try
                {
                    Setter?.Invoke(val);
                    Act?.Invoke();
                    OnChange?.Invoke(this, val);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Exception on Set on entry {ConfigID} ({EntryType})!\n{ex}");
                }
            }
        }

        internal class ConfigEntry
        {
            public string ConfigID { get; set; }
            public object Value { get; set; }
        }

        internal readonly List<MenuEntry> Entries;
        readonly Queue<Action> RollInMenu;

        internal ToolsMenu()
        {
            Entries = [];
            RollInMenu = [];

            Entries.Add(new(MenuEntry.Type.Toggle, "show_debug_ui", Def_Cat, "cheat_entry_gui_title", "cheat_entry_gui_desc", (val) => { if (val is bool b) Core.ShowDebugUI = b; return Core.ShowDebugUI; }));
            Entries.Add(new(MenuEntry.Type.Toggle, "show_watermark", Def_Cat, "cheat_entry_watermark_title", "cheat_entry_watermark_desc", (val) => { if (val is bool b) Core.ShowWatermark = b; return Core.ShowWatermark; }));

            Entries.Add(new(MenuEntry.Type.Toggle, "track_log", Def_Cat, "cheat_entry_track_debug_title", "cheat_entry_track_debug_desc", (val) => { if (val is bool b) Instance.TrackGameLog = b; return Instance.TrackGameLog; }, Instance.ResolveLogTracking));
            Entries.Add(new(MenuEntry.Type.Toggle, "fps_counter", Def_Cat, "cheat_entry_fps_counter_title", "cheat_entry_fps_counter_desc", (val) => { if (val is bool b) Instance.FPSCounter = b; return Instance.FPSCounter; }, Instance.ResolveFGDebug));
            Entries.Add(new(MenuEntry.Type.Toggle, "fg_debug", Def_Cat, "cheat_entry_fg_debug_title", "cheat_entry_fg_debug_desc", (val) => { if (val is bool b) Instance.FGDebug = b; return Instance.FGDebug; }, Instance.ResolveFGDebug));
            Entries.Add(new(MenuEntry.Type.Toggle, "unlock_fps", Def_Cat, "cheat_entry_unlock_fps_title", "cheat_entry_unlock_fps_desc", (val) => { if (val is bool b) Instance.UnlockFPS = b; return Instance.UnlockFPS; }));
            Entries.Add(new(MenuEntry.Type.Slider, "unlock_fps_target", Def_Cat, "cheat_entry_target_fps_title", "cheat_entry_target_fps_desc", (val) => { if (val is float i) Instance.TargetFPS = Convert.ToInt32(i); return Instance.TargetFPS; }, new(() => { Application.targetFrameRate = Instance.UnlockFPS ? Instance.TargetFPS : 60; }), [typeof(int), 10, 300]));
            Entries.Add(new(MenuEntry.Type.Slider, "fg_debug_scale", Def_Cat, "cheat_entry_fg_debug_scale_title", "cheat_entry_fg_debug_scale_desc", (val) => { if (val is float f) Instance.FGDebugScale = f; return Instance.FGDebugScale; }, Instance.ResolveFGDebug, [typeof(float), 0.1f, 1f]));

            Entries.Add(new(MenuEntry.Type.Toggle, "advanced_names", Gameplay_Cat, "cheat_entry_platforms_title", "cheat_entry_platforms_desc", (val) => { if (val is bool b) Instance.InGameManager.SeePlayerPlatforms = b; return Instance.InGameManager.SeePlayerPlatforms; }));
            Entries.Add(new(MenuEntry.Type.Toggle, "capture_tools", Gameplay_Cat, "cheat_entry_capture_tools_title", "cheat_entry_capture_tools_desc", (val) => { if (val is bool b) Instance.InGameManager.UseCaptureTools = b; return Instance.InGameManager.UseCaptureTools; }));
            Entries.Add(new(MenuEntry.Type.InputField, "run_modifier", Gameplay_Cat, "cheat_entry_run_speed_title", "cheat_entry_run_speed_desc", (val) => { if (val is float f) Instance.InGameManager.RunSpeedModifier = f; return Instance.InGameManager.RunSpeedModifier; }, null, [typeof(float)]));
            Entries.Add(new(MenuEntry.Type.InputField, "jump_y", Gameplay_Cat, "cheat_entry_jump_y_title", "cheat_entry_jump_y_desc", (val) => { if (val is float f) Instance.InGameManager.JumpYModifier = f; return Instance.InGameManager.JumpYModifier; }, null, [typeof(float)]));
            Entries.Add(new(MenuEntry.Type.InputField, "dive_sens", Gameplay_Cat, "cheat_entry_dive_sens_title", "cheat_entry_dive_sens_desc", (val) => { if (val is float f) Instance.InGameManager.DiveSens = f; return Instance.InGameManager.DiveSens; }, null, [typeof(float)]));
            Entries.Add(new(MenuEntry.Type.Toggle, "disable_fgcc_check", Gameplay_Cat, "cheat_entry_fgcc_check_title", "cheat_entry_fgcc_check_desc", (val) => { if (val is bool b) Instance.InGameManager.DisableFGCCCheck = b; return Instance.InGameManager.DisableFGCCCheck; }));
            Entries.Add(new(MenuEntry.Type.Toggle, "disable_afk", Gameplay_Cat, "cheat_entry_afk_title", "cheat_entry_afk_desc", (val) => { if (val is bool b) Instance.InGameManager.DisableAFK = b; return Instance.InGameManager.DisableAFK; }, Instance.ResolveAFK));
            Entries.Add(new(MenuEntry.Type.InputField, "air_velocity", Gameplay_Cat, "cheat_entry_gravity_vel_title", "cheat_entry_gravity_vel_desc", (val) => { if (val is float f) Instance.InGameManager.GravityModifier = f; return Instance.InGameManager.GravityModifier; }, null, [typeof(float)]));
            Entries.Add(new(MenuEntry.Type.InputField, "dive_force", Gameplay_Cat, "cheat_entry_dive_force_title", "cheat_entry_dive_force_desc", (val) => { if (val is float f) Instance.InGameManager.DiveForce = f; return Instance.InGameManager.DiveForce; }, null, [typeof(float)]));
            Entries.Add(new(MenuEntry.Type.InputField, "air_dive_force", Gameplay_Cat, "cheat_entry_air_dive_force_title", "cheat_entry_air_dive_force_desc", (val) => { if (val is float f) Instance.InGameManager.DiveForceInAir = f; return Instance.InGameManager.DiveForceInAir; }, null, [typeof(float)]));
            Entries.Add(new(MenuEntry.Type.Toggle, "spectator_join", Gameplay_Cat, "cheat_entry_spectator_title", "cheat_entry_spectator_desc", (val) => { if (val is bool b) GlobalDebug.DebugJoinAsSpectatorEnabled = b; return GlobalDebug.DebugJoinAsSpectatorEnabled; }));

            Entries.Add(new(MenuEntry.Type.Button, null, Gameplay_Cat, "cheat_entry_to_finish_title", "cheat_entry_to_finish_desc", null, Instance.InGameManager.TeleportToFinish));
            Entries.Add(new(MenuEntry.Type.Button, null, Gameplay_Cat, "cheat_entry_to_safe_title", "cheat_entry_to_safe_desc", null, Instance.InGameManager.TeleportToSafeZone));
            Entries.Add(new(MenuEntry.Type.Button, null, Gameplay_Cat, "cheat_entry_to_random_player_title", "cheat_entry_to_random_player_desc", null, Instance.InGameManager.TeleportToRandomPlayer));
            Entries.Add(new(MenuEntry.Type.Button, null, Gameplay_Cat, "cheat_entry_toggle_players_title", "cheat_entry_toggle_players_desc", null, Instance.InGameManager.TogglePlayers));
            Entries.Add(new(MenuEntry.Type.Button, null, Gameplay_Cat, "cheat_entry_force_menu_title", "cheat_entry_force_menu_desc", null, Instance.ForceMainMenu));

            CheckConfig();
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
                var cfg = JsonSerializer.Deserialize<List<ConfigEntry>>(File.ReadAllText(Core.ConfigFile));
                foreach (var entry in cfg)
                {
                    var target = Entries.Find(x => x.ConfigID == entry.ConfigID);
                    if (target == null)
                        continue;

                    RollInMenu.Enqueue(new(() =>
                    {
                        target.Set(entry.Value);
                    }));
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Config load failed!\n{e}");
            }
        }

        internal void SaveConfig(bool cleanup)
        {
            var list = new List<ConfigEntry>();

            try
            {
                foreach (var entry in Entries)
                {
                    if (!entry.CanBeSaved)
                        continue;

                    list.Add(new()
                    {
                        ConfigID = entry.ConfigID,
                        Value = cleanup ? entry.InitialValue : entry.GetValue()
                    });
                }

                File.WriteAllText(Core.ConfigFile, JsonSerializer.Serialize(list));
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Config save failed!\n{e}");
            }
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

        internal void ReleaseQueue()
        {
            for (int i = 0; i < RollInMenu.Count; i++)
            {
                RollInMenu.Dequeue().Invoke();
            }
        }

        internal void DeleteConfig()
        {
            if (!File.Exists(Core.ConfigFile))
                return;

            File.Delete(Core.ConfigFile);
        }

        internal void ResetSettings()
        {
            foreach (var entry in Entries.Where(x => x.CanBeSaved))
                entry.Set(entry.InitialValue);

            SaveConfig(true);
        }
    }
}
