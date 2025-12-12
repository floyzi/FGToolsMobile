using Il2CppFGClient;
using Il2CppFGDebug;
using MelonLoader;
using NOTFGT.FLZ_Common.Config.Entries.Configs;
using NOTFGT.FLZ_Common.GUI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NOTFGT.FLZ_Common.Config.Config;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common.Config.Entries
{
    internal class EntriesManager
    {
        const string DEBUG_CATEG = "menu_debug_section";
        const string FGCC_CATEG = "menu_fgcc_section";
        const string DEFAULT_CATEG = "menu_section";
        const string GAMEPLAY_CATEG = "menu_gp_section";
        const string SECRET_CATEG = "menu_bonus_section";

        public struct CategoryData(int priority, string id)
        {
            public int Priority = priority;
            public string LocaleID = id;
        }

        internal readonly List<MenuEntry> Entries = [];
        readonly Queue<Action> RollInMenu = [];

        readonly List<CategoryData> CategoryDatas =
        [
            new(999, DEBUG_CATEG),
            new(102, DEFAULT_CATEG),
            new(90, GAMEPLAY_CATEG),
            new(80, FGCC_CATEG),
            new(0, SECRET_CATEG)
        ];

        internal EntriesManager()
        {
            var devCat = CategoryDatas.First(x => x.LocaleID == DEBUG_CATEG);
            var defCat = CategoryDatas.First(x => x.LocaleID == DEFAULT_CATEG);
            var gpCat = CategoryDatas.First(x => x.LocaleID == GAMEPLAY_CATEG);
            var fgccCat = CategoryDatas.First(x => x.LocaleID == FGCC_CATEG);
            var secCat = CategoryDatas.First(x => x.LocaleID == SECRET_CATEG);

            #region DEBUG CATEGORY
#if MELON_LOGS
            CreateEntry(MenuEntry.Type.Toggle, "melon_log", devCat, () => Core.ShouldShowMelonLog, v => Core.ShouldShowMelonLog = v);
#endif
            #endregion
            
            #region DEFAULT CATEGORY
            CreateEntry(MenuEntry.Type.Toggle, "show_debug_ui", defCat, () => Core.ShowDebugUI, v => Core.ShowDebugUI = v);
            CreateEntry(MenuEntry.Type.Toggle, "show_watermark", defCat, () => Core.ShowWatermark, v => Core.ShowWatermark = v);
            CreateEntry(MenuEntry.Type.Toggle, "track_log", defCat, () => Instance.TrackGameLog, v => Instance.TrackGameLog = v, Instance.ResolveLogTracking);
            CreateEntry(MenuEntry.Type.Toggle, "fps_counter", defCat, () => Instance.FPSCounter, v => Instance.FPSCounter = v, Instance.ResolveFGDebug);
            CreateEntry(MenuEntry.Type.Toggle, "fg_debug", defCat, () => Instance.FGDebug, v => Instance.FGDebug = v, Instance.ResolveFGDebug);
            CreateEntry(MenuEntry.Type.Toggle, "unlock_fps", defCat, () => Instance.UnlockFPS, v => Instance.UnlockFPS = v, Instance.ResolveFPS);

            CreateEntry<float>(MenuEntry.Type.Slider, "unlock_fps_target", defCat, () => Instance.TargetFPS, v => Instance.TargetFPS = Convert.ToInt32(v), new(() => { Application.targetFrameRate = Instance.UnlockFPS ? Instance.TargetFPS : 60; }), new SliderConfig(typeof(int), 10, 300, (() => Instance.UnlockFPS)));
            CreateEntry(MenuEntry.Type.Slider, "fg_debug_scale", defCat, () => Instance.FGDebugScale, v => Instance.FGDebugScale = v, Instance.ResolveFGDebug, new SliderConfig(typeof(float), 0.1f, 1f));

            CreateEntry(MenuEntry.Type.Button, "force_menu", defCat, Instance.ForceMainMenu);
            #endregion

            #region FGCC CATEGORY
#if CHEATS
            CreateEntry(MenuEntry.Type.Toggle, "air_jumps", fgccCat, () => FLZ_Game.IsAirJumpEnabled, v => FLZ_Game.IsAirJumpEnabled = v);
            CreateEntry(MenuEntry.Type.Toggle, "disable_fgcc_check", fgccCat, () => Instance.InGameManager.DisableFGCCCheck, v => Instance.InGameManager.DisableFGCCCheck = v, Instance.InGameManager.ResolveFGCC);
            CreateEntry(MenuEntry.Type.InputField, "run_modifier", fgccCat, () => Instance.InGameManager.RunSpeedModifier, v => Instance.InGameManager.RunSpeedModifier = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "jump_y", fgccCat, () => Instance.InGameManager.JumpYModifier, v => Instance.InGameManager.JumpYModifier = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "dive_sens", fgccCat, () => Instance.InGameManager.DiveSens, v => Instance.InGameManager.DiveSens = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "air_velocity", fgccCat, () => Instance.InGameManager.GravityModifier, v => Instance.InGameManager.GravityModifier = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "dive_force", fgccCat, () => Instance.InGameManager.DiveForce, v => Instance.InGameManager.DiveForce = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "air_dive_force", fgccCat, () => Instance.InGameManager.DiveForceInAir, v => Instance.InGameManager.DiveForceInAir = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
#endif
            #endregion

            #region GAMEPLAY CATEGORY
            CreateEntry(MenuEntry.Type.Toggle, "names_toggle", gpCat, () => GlobalGameStateClient.Instance.PlayerProfile.TouchSettings.ShowPlayerNames, v => GlobalGameStateClient.Instance.PlayerProfile.TouchSettings.ShowPlayerNames = v);
            CreateEntry(MenuEntry.Type.Toggle, "advanced_names", gpCat, () => Instance.InGameManager.SeePlayerPlatforms, v => Instance.InGameManager.SeePlayerPlatforms = v, Instance.InGameManager.SetNames);
#if CHEATS
            CreateEntry(MenuEntry.Type.Toggle, "capture_tools", gpCat, () => Instance.InGameManager.UseCaptureTools, v => Instance.InGameManager.UseCaptureTools = v);
            CreateEntry(MenuEntry.Type.Toggle, "disable_afk", gpCat, () => Instance.InGameManager.DisableAFK, v => Instance.InGameManager.DisableAFK = v, Instance.ResolveAFK);

            CreateEntry(MenuEntry.Type.Button, "to_finish", gpCat, Instance.InGameManager.TeleportToFinish);
            CreateEntry(MenuEntry.Type.Button, "to_safe", gpCat, Instance.InGameManager.TeleportToSafeZone);
            CreateEntry(MenuEntry.Type.Button, "to_random_player", gpCat, Instance.InGameManager.TeleportToRandomPlayer);
            CreateEntry(MenuEntry.Type.Button, "toggle_players", gpCat, Instance.InGameManager.TogglePlayers);
#endif
            #endregion

            #region SECRET CATEGORY
            CreateEntry(MenuEntry.Type.Toggle, "owoify", secCat, () => Instance.IsOwoifyEnabled, v => Instance.IsOwoifyEnabled = v, Instance.ResolveOwoify, new ToggleConfig(dispCondition: () => CreditsScreen.EnabledSecret, saveInConfCondition: () => false));
            #endregion
        }

        internal void LoadFromSave(List<ConfigEntry> entries)
        {
            foreach (var entry in entries)
            {
                var target = Entries.Find(x => x.ID == entry.ConfigID);
                if (target == null)
                    continue;

                RollInMenu.Enqueue(new(() =>
                {
                    target.SetValue(entry.Value);
                }));
            }
        }

        internal List<ConfigEntry> GetForSave(bool cleanup)
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
                        ConfigID = entry.ID,
                        Value = cleanup ? entry.InitialValue : entry.GetValue()
                    });
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Config save failed!\n{e}");
            }

            return list;
        }

        void CreateEntry<T>(MenuEntry.Type type, string id, CategoryData category, Func<T> getter = null, Action<T> setter = null, Action onSet = null, IEntryConfig additionalConfig = null)
        {
            if (Entries.Any(x => x.ID == id))
            {
                MelonLogger.Warning($"Unable to create entry with id {id} as another entry with the same id is exist");
                return;
            }

            Entries.Add(new(type, id, category, $"{id}_title", $"{id}_desc", val => { if (val is T v) setter(v); return getter(); }, onSet, additionalConfig));
        }

        void CreateEntry(MenuEntry.Type type, string id, CategoryData category, Action onSet = null, IEntryConfig additionalConfig = null)
        {
            if (Entries.Any(x => x.ID == id))
            {
                MelonLogger.Warning($"Unable to create entry with id {id} as another entry with the same id is exist");
                return;
            }

            Entries.Add(new(type, id, category, $"{id}_title", $"{id}_desc", null, onSet, additionalConfig));
        }

        internal void ReleaseQueue()
        {
            while (RollInMenu.Count > 0)
            {
                RollInMenu.Dequeue().Invoke();
            }
        }

        internal void ResetSettings()
        {
            foreach (var entry in Entries.Where(x => x.CanBeSaved))
                entry.SetValue(entry.InitialValue);

            FLZ_ToolsManager.Instance.Config.SaveConfig(true);
        }
    }
}
