using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using File = System.IO.File;
using Path = System.IO.Path;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common.GUI
{
    //i need to rewrite this
    public class ToolsMenu
    {
        //name of config file
        const string TargetCfgName = "ConfigV2.json";

        //sections
        const string FGCC_Cat = "menu_fgcc_section";
        const string FPS_Cat = "menu_fps_section";
        const string Def_Cat = "menu_section";
        const string Debug_Cat = "menu_debug_section";
        const string Gameplay_Cat = "menu_gp_section";

        //slider config
        public const string SliderMin = "sliderMin";
        public const string SliderMax = "sliderMax";
        public const string IsFloat = "isFloat";

        //field config
        public const string CharLimit = "charLimit";

        //menu entries
        public const string GUI = "GUI";
        public const string JoinAsSpectator = "JoinAsSpectator";
        public const string UseCaptureTools = "UseCaptureTools";
        public const string TrackGameDebug = "TrackGameDebug";
        public const string FPSCoutner = "FPSCoutner";
        public const string WholeFGDebug = "WholeFGDebug";
        public const string UnlockFPS = "UnlockFPS";
        public const string TargetFPS = "TargetFPS";
        public const string RunSpeedModifier = "RunSpeedModifier";
        public const string JumpYModifier = "JumpYModifier";
        public const string Watermark = "Watermark";
        public const string DiveSens = "DiveSens";
        public static string DisableMonitorCheck => "DisableMonitorCheck";
        public const string ToFinish = "ToFinish";
        public const string DisableAFK = "DisableAFK";
        public const string GravityChange = "GravityChange";
        public const string DiveForce = "DiveForce";
        public const string DiveInAirForce = "DiveInAirForce";
        public const string ToSafe = "ToSafe";
        public const string ToRandomPlayer = "ToRandomPlayer";
        public const string HidePlayers = "HidePlayers";
        public const string ForceMenu = "ForceMenu";
        public const string FGDebugScale = "FGDebugScale";
        public const string SeePlayerPlatforms = "SeePlayerPlatforms";

        private static readonly Dictionary<string, (string Category, string DisplayName, string Description, object Value, Dictionary<string, object> Config, MenuEntry.Type ValueType)> EditableMenu = new()
        {
            { GUI, (Def_Cat, "cheat_entry_gui_title", "cheat_entry_gui_desc", false, null, MenuEntry.Type.Bool) },
            { TrackGameDebug, (Debug_Cat, "cheat_entry_track_debug_title", "cheat_entry_track_debug_desc", false, null, MenuEntry.Type.Bool) },
            { FPSCoutner, (FPS_Cat, "cheat_entry_fps_counter_title", "cheat_entry_fps_counter_desc", true, null, MenuEntry.Type.Bool) },
            { WholeFGDebug, (Debug_Cat, "cheat_entry_fg_debug_title", "cheat_entry_fg_debug_desc", false, null, MenuEntry.Type.Bool) },
            { UnlockFPS, (FPS_Cat, "cheat_entry_unlock_fps_title", "cheat_entry_unlock_fps_desc", true, null, MenuEntry.Type.Bool) },
            { TargetFPS, (FPS_Cat, "cheat_entry_target_fps_title", "cheat_entry_target_fps_desc", 60, new() {
                { SliderMin, 1 },
                { SliderMax, 300 },
                { IsFloat, false },
            }, MenuEntry.Type.Slider) },
            { Watermark, (Def_Cat, "cheat_entry_watermark_title", "cheat_entry_watermark_desc", true, null, MenuEntry.Type.Bool) },
            { FGDebugScale, (Debug_Cat, "cheat_entry_fg_debug_scale_title", "cheat_entry_fg_debug_scale_desc", 0.602f, new() {
                { SliderMin, 0f },
                { SliderMax, 1f },
                { IsFloat, true }
            }, MenuEntry.Type.Slider) },
            { SeePlayerPlatforms, (Gameplay_Cat, "cheat_entry_platforms_title", "cheat_entry_platforms_desc", false, null, MenuEntry.Type.Bool) },

#if CHEATS
            { UseCaptureTools, (Def_Cat, "cheat_entry_capture_tools_title", "cheat_entry_capture_tools_desc", false, null, MenuEntry.Type.Bool) },
            { RunSpeedModifier, (FGCC_Cat, "cheat_entry_run_speed_title", "cheat_entry_run_speed_desc", 0.0f, null, MenuEntry.Type.Float) },
            { JumpYModifier, (FGCC_Cat, "cheat_entry_jump_y_title", "cheat_entry_jump_y_desc", 0.0f, null, MenuEntry.Type.Float) },
            { DiveSens, (FGCC_Cat, "cheat_entry_dive_sens_title", "cheat_entry_dive_sens_desc", 70.0f, null, MenuEntry.Type.Float) },
            { DisableMonitorCheck, (FGCC_Cat, "cheat_entry_fgcc_check_title", "cheat_entry_fgcc_check_desc", true, null, MenuEntry.Type.Bool) },
            { DisableAFK, (Gameplay_Cat, "cheat_entry_afk_title", "cheat_entry_afk_desc", false, null, MenuEntry.Type.Bool) },
            { GravityChange, (FGCC_Cat, "cheat_entry_gravity_vel_title", "cheat_entry_gravity_vel_desc", 60f, null, MenuEntry.Type.Float) },
            { DiveForce, (FGCC_Cat, "cheat_entry_dive_force_title", "cheat_entry_dive_force_desc", 16.5f, null, MenuEntry.Type.Float) },
            { DiveInAirForce, (FGCC_Cat, "cheat_entry_air_dive_force_title", "cheat_entry_air_dive_force_desc", 7.0f, null, MenuEntry.Type.Float) },
            { JoinAsSpectator, (Def_Cat, "cheat_entry_spectator_title", "cheat_entry_spectator_desc", false, null, MenuEntry.Type.Bool) },
#endif
        };

        public class MenuEntry
        {
            public enum Type
            {
                Bool,
                Int,
                Float,
                String,
                Slider,
                Button
            }

            public string InternalName { get; set; }
            public string Category { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; } 
            public object Value { get; set; }
            public Dictionary<string, object> Config { get; set; }
            public Type ValueType { get; set; }
        }

        public void LoadConfig(bool reset)
        {
            string path = Path.Combine(Launcher.AssetsDir, TargetCfgName);

            MelonLogger.Msg($"[{GetType()}] Loading config at path {path}. Reset = {reset}");
            var definitions = new HashSet<string>
            {
                GUI, TrackGameDebug, FPSCoutner, WholeFGDebug, UnlockFPS, TargetFPS, 
                FGDebugScale, Watermark, SeePlayerPlatforms,
#if CHEATS
                JoinAsSpectator, UseCaptureTools,
                RunSpeedModifier, JumpYModifier, DiveSens, 
                DisableMonitorCheck, DisableAFK, GravityChange, DiveForce, DiveInAirForce,
#endif

            };

            _entries.Clear();
            _changedEntries.Clear();

            if (!File.Exists(path) || reset)
            {
                MelonLogger.Msg($"[{GetType()}] Config not found, creating a new one...");
                foreach (var entry in EditableMenu)
                {
                    RegisterEntry(entry.Key, entry.Value.Category, entry.Value.DisplayName, entry.Value.Description, entry.Value.Value, entry.Value.Config, entry.Value.ValueType);
                }
            }
            else
            {
                var knownDefinitions = new Dictionary<string, MenuEntry>();

                MelonLogger.Msg($"[{GetType()}] Found existing config, it will be loaded");

                foreach (var entry in JsonConvert.DeserializeObject<List<MenuEntry>>(File.ReadAllText(path)))
                {
                    if (definitions.Contains(entry.InternalName))
                    {
                        knownDefinitions[entry.InternalName] = entry;
                    }
                }

                MelonLogger.Msg($"[{GetType()}] Loading config...");

                foreach (var entry2 in definitions)
                {

                    EditableMenu.TryGetValue(entry2, out var defaultEntry);
                    if (!knownDefinitions.ContainsKey(entry2))
                        RegisterEntry(entry2, defaultEntry.Category, defaultEntry.DisplayName, defaultEntry.Description, defaultEntry.Value, defaultEntry.Config, defaultEntry.ValueType);
                    else
                    {
                        var knownEntry = knownDefinitions[entry2];
                        if (knownEntry.ValueType == MenuEntry.Type.Button)
                            RegisterEntry(knownEntry.InternalName, defaultEntry.Category, defaultEntry.DisplayName, defaultEntry.Description, defaultEntry.Value, defaultEntry.Config, defaultEntry.ValueType);
                        else
                            RegisterEntry(knownEntry.InternalName, defaultEntry.Category, defaultEntry.DisplayName, defaultEntry.Description, knownEntry.Value, defaultEntry.Config, defaultEntry.ValueType);
                    }
                }
            }

            MelonLogger.Msg($"[{GetType()}] Config loaded!");

            var json = JsonConvert.SerializeObject(_entries);
            File.WriteAllText(path, json);

            CreateStaticConfig();

            MelonLogger.Msg($"[{GetType()}] All done!");
        }

        public void DeleteConfig()
        {
            string path = Path.Combine(Launcher.AssetsDir, TargetCfgName);
            if (File.Exists(path))
                File.Delete(path);
            Application.Quit();
        }

        public void CreateStaticConfig()
        {
            Dictionary<string, (string Category, string DisplayName, string Description, object Value, Dictionary<string, object> AdditionalData, MenuEntry.Type ValueType)> StaticConfig = new()
            {
#if CHEATS
                { ToFinish, (Gameplay_Cat, "cheat_entry_to_finish_title", "cheat_entry_to_finish_desc", "TeleportToFinish", null, MenuEntry.Type.Button) },
                { ToSafe, (Gameplay_Cat, "cheat_entry_to_safe_title", "cheat_entry_to_safe_desc", "TeleportToSafeZone", null, MenuEntry.Type.Button) },
                { ToRandomPlayer, (Gameplay_Cat, "cheat_entry_to_random_player_title", "cheat_entry_to_random_player_desc", "TeleportToRandomPlayer", null, MenuEntry.Type.Button) },
                { HidePlayers, (Gameplay_Cat, "cheat_entry_toggle_players_title", "cheat_entry_toggle_players_desc", "TogglePlayers", null, MenuEntry.Type.Button) },
#endif
                { ForceMenu, (Gameplay_Cat, "cheat_entry_force_menu_title", "cheat_entry_force_menu_desc", "ForceMainMenu", null, MenuEntry.Type.Button) },
            };

            MelonLogger.Msg($"[{GetType()}] Creating static section...");

            foreach (var entry in StaticConfig)
                RegisterEntry(entry.Key, entry.Value.Category, entry.Value.DisplayName, entry.Value.Description, entry.Value.Value, entry.Value.AdditionalData, entry.Value.ValueType);
        }

        private List<MenuEntry> _entries = [];
        private List<MenuEntry> _changedEntries = [];

        public void RollSave()
        {
            string path = Path.Combine(Launcher.AssetsDir, TargetCfgName);
            var json = JsonConvert.SerializeObject(_entries);
            File.WriteAllText(path, json);
            MelonLogger.Msg($"[{GetType()}] Saved config...");
        }

        public void RegisterEntry(string internalName, string category, string displayName, string desc, object value, Dictionary<string, object> AdditionalData, MenuEntry.Type type)
        {
            MelonLogger.Msg($"[{GetType()}] Register for entry \"{internalName}\" with type \"{type}\"");

            _entries.Add(new MenuEntry {
                InternalName = internalName, 
                Category = category, 
                DisplayName = displayName, 
                Description = desc,
                Value = value, 
                Config = AdditionalData,
                ValueType = type 
            });
        }

        public void UpdateValue(string name, object value)
        {
            var entry = _entries.FirstOrDefault(e => e.InternalName == name);
            if (entry != null)
            {
                var changedEntry = _changedEntries.FirstOrDefault(e => e.InternalName == name);

                FLZ_ToolsManager.Instance.GUIUtil.TriggerPendingChanges(true);

                if (changedEntry == null)
                {
                    changedEntry = new MenuEntry
                    {
                        InternalName = entry.InternalName,
                        Description = entry.Description,
                        DisplayName= entry.DisplayName,
                        Config = entry.Config,
                        Category = entry.Category,
                        ValueType = entry.ValueType,
                        Value = value,
                    };
                    _changedEntries.Add(changedEntry);
                    return;
                }

                changedEntry.Value = value;
            }
        }

        public void RollChanges()
        {
            foreach (var changedEntry in _changedEntries)
            {
                var entry = _entries.FirstOrDefault(e => e.InternalName == changedEntry.InternalName);

                if (changedEntry.InternalName == WholeFGDebug && (bool)changedEntry.Value)
                    _entries.Find(x => x.InternalName == FPSCoutner).Value = false;

                if (changedEntry.InternalName == FPSCoutner && (bool)changedEntry.Value)
                    _entries.Find(x => x.InternalName == WholeFGDebug).Value = false;

                if (entry != null)
                    entry.Value = changedEntry.Value;
            }

            Instance.GUIUtil.UpdateActiveEntries(_changedEntries);
            _changedEntries.Clear();
            Instance.GUIUtil.TriggerPendingChanges(false);
        }

        public T GetValue<T>(string name)
        {
            var entry = _entries.FirstOrDefault(e => e.InternalName == name);

            if (entry == null || entry.Value == null)
                return default;

            if (entry.Value is T value)
                return value;

            throw new InvalidCastException($"Value {name} have type {entry.Value.GetType()} but we tried to get is as {typeof(T)}");
        }

        public void ResetSettings()
        {
            LoadConfig(true);
            Instance.GUIUtil.UpdateActiveEntries();
            _changedEntries.Clear();
            Instance.ApplyChanges();
            Instance.GUIUtil.TriggerPendingChanges(false);
        }


        public MenuEntry TryGetEntry(string internalName)
        {
            return _entries.FirstOrDefault(e => e.InternalName == internalName);
        }

        public List<MenuEntry> GetAllEntries() => _entries;
    }
}
