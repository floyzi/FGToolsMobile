using Il2Cpp;
using Il2CppFGClient;
using Il2CppFGClient.UI;
using Il2CppFGDebug;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Text.Json;
using UnityEngine;
using static Il2CppFG.Benchmarking.LoadLevel.UGC.LevelLoader;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.Config;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using File = System.IO.File;

namespace NOTFGT.FLZ_Common.GUI
{
    public class ToolsMenu
    {
        const string FGCC_Cat = "menu_fgcc_section";
        const string FPS_Cat = "menu_fps_section";
        const string Def_Cat = "menu_section";
        const string Gameplay_Cat = "menu_gp_section";
        const string Secret_Cat = "menu_bonus_section";

        internal interface IEntryConfig {}
        internal abstract class BaseEntryConfig : IEntryConfig
        {
            public Func<bool> InteractableCondition;
            public Func<bool> DisplayCondition;
        }

        internal class SliderConfig : BaseEntryConfig
        {
            public Type ValueType;
            public float MinValue;
            public float MaxValue;

            public SliderConfig(Type t, float min = -1, float max = -1, Func<bool> intCondition = null, Func<bool> dispCondition = null)
            {
                ValueType = t;
                MinValue = min;
                MaxValue = max;
                InteractableCondition = intCondition;
                DisplayCondition = dispCondition;
            }
        }
        internal class FieldConfig : BaseEntryConfig
        {
            public Type ValueType;
            public int CharacterLimit;

            public FieldConfig(Type t, int charLimit = -1, Func<bool> condition = null, Func<bool> dispCondition = null)
            {
                ValueType = t;
                CharacterLimit = charLimit;
                InteractableCondition = condition;
                DisplayCondition = dispCondition;
            }
        }
        internal class ToggleConfig : BaseEntryConfig
        {
            public ToggleConfig(Func<bool> condition = null, Func<bool> dispCondition = null)
            {
                InteractableCondition = condition;
                DisplayCondition = dispCondition;
            }
        }

        internal class MenuEntry(MenuEntry.Type type, string id, string category, string displayName, string desc, Func<object, object> setter = null, Action postSet = null, IEntryConfig additionalConfig = null)
        {
            public enum Type
            {
                Toggle,
                InputField,
                Slider,
                Button
            }

            /// <summary>
            /// ID of the entry, should be unique for each entry.
            /// </summary>
            internal string ID { get; private set; } = id;

            /// <summary>
            /// Category where element will be placed on UI.
            /// Represents ID of localized string
            /// </summary>
            internal string Category { get; private set; } = category;

            /// <summary>
            /// Name of element that will be shown on UI.
            /// Represents ID of localized string
            /// </summary>
            internal string DisplayName { get; private set; } = displayName;

            /// <summary>
            /// Description of element that will be shown on UI.
            /// Represents ID of localized string
            /// </summary>
            internal string Description { get; private set; } = desc;

            /// <summary>
            /// Type of entry, indicates how this entry will be shown on UI
            /// </summary>
            internal Type EntryType { get; private set; } = type;

            /// <summary>
            /// Additional properties of entry.
            /// </summary>
            internal IEntryConfig AdditionalConfig => additionalConfig;

            /// <summary>
            /// Used to set and get updated value of field that attached to this entry
            /// </summary>
            internal Func<object, object> Setter => setter;

            /// <summary>
            /// Initial field value of field. Null if field is not specefied
            /// </summary>
            internal object InitialValue = setter?.Invoke(null);

            /// <summary>
            /// Action that will be invoked after Setter 
            /// </summary>
            internal Action PostSetAction = postSet;

            /// <summary>
            /// Indicates can be entry included in config save or no
            /// </summary>
            internal bool CanBeSaved => EntryType != Type.Button && !string.IsNullOrEmpty(ID);

            /// <summary>
            /// Action that will be invoked after entry value is changed
            /// </summary>
            internal Action<object> OnEntryChanged;

            internal object GetValue() => setter?.Invoke(null);

            internal void Set(object val)
            {
                try
                {
                    Setter?.Invoke(val);
                    PostSetAction?.Invoke();
                    OnEntryChanged?.Invoke(val);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Exception on Set on entry {ID} ({EntryType})!\n{ex}");
                }
            }
        }

        internal readonly List<MenuEntry> Entries = [];
        readonly Queue<Action> RollInMenu = [];

        internal void Create()
        {
            CreateEntry(MenuEntry.Type.Toggle, "show_debug_ui", Def_Cat, () => Core.ShowDebugUI, v => Core.ShowDebugUI = v);
            CreateEntry(MenuEntry.Type.Toggle, "show_watermark", Def_Cat, () => Core.ShowWatermark, v => Core.ShowWatermark = v);

            CreateEntry(MenuEntry.Type.Toggle, "track_log", Def_Cat, () => Instance.TrackGameLog, v => Instance.TrackGameLog = v, Instance.ResolveLogTracking);
            CreateEntry(MenuEntry.Type.Toggle, "fps_counter", Def_Cat, () => Instance.FPSCounter, v => Instance.FPSCounter = v, Instance.ResolveFGDebug);
            CreateEntry(MenuEntry.Type.Toggle, "fg_debug", Def_Cat, () => Instance.FGDebug, v => Instance.FGDebug = v, Instance.ResolveFGDebug);
            CreateEntry(MenuEntry.Type.Toggle, "unlock_fps", Def_Cat, () => Instance.UnlockFPS, v => Instance.UnlockFPS = v, Instance.ResolveFPS);
            CreateEntry<float>(MenuEntry.Type.Slider, "unlock_fps_target", Def_Cat, () => Instance.TargetFPS, v => Instance.TargetFPS = Convert.ToInt32(v), new(() => { Application.targetFrameRate = Instance.UnlockFPS ? Instance.TargetFPS : 60; }), new SliderConfig(typeof(int), 10, 300, (() => Instance.UnlockFPS)));
            CreateEntry(MenuEntry.Type.Slider, "fg_debug_scale", Def_Cat, () => Instance.FGDebugScale, v => Instance.FGDebugScale = v, Instance.ResolveFGDebug, new SliderConfig(typeof(float), 0.1f, 1f));

            CreateEntry(MenuEntry.Type.Toggle, "names_toggle", Gameplay_Cat, () => GlobalGameStateClient.Instance.PlayerProfile.TouchSettings.ShowPlayerNames, v => GlobalGameStateClient.Instance.PlayerProfile.TouchSettings.ShowPlayerNames = v);
            CreateEntry(MenuEntry.Type.Toggle, "advanced_names", Gameplay_Cat, () => Instance.InGameManager.SeePlayerPlatforms, v => Instance.InGameManager.SeePlayerPlatforms = v, Instance.InGameManager.SetNames);
#if CHEATS
            CreateEntry(MenuEntry.Type.Toggle, "capture_tools", Gameplay_Cat, () => Instance.InGameManager.UseCaptureTools, v => Instance.InGameManager.UseCaptureTools = v);
            CreateEntry(MenuEntry.Type.Toggle, "disable_afk", Gameplay_Cat, () => Instance.InGameManager.DisableAFK, v => Instance.InGameManager.DisableAFK = v, Instance.ResolveAFK);
            CreateEntry(MenuEntry.Type.Toggle, "disable_fgcc_check", Gameplay_Cat, () => Instance.InGameManager.DisableFGCCCheck, v => Instance.InGameManager.DisableFGCCCheck = v, Instance.InGameManager.ResolveFGCC);
            CreateEntry(MenuEntry.Type.InputField, "run_modifier", Gameplay_Cat, () => Instance.InGameManager.RunSpeedModifier, v => Instance.InGameManager.RunSpeedModifier = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "jump_y", Gameplay_Cat, () => Instance.InGameManager.JumpYModifier, v => Instance.InGameManager.JumpYModifier = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "dive_sens", Gameplay_Cat, () => Instance.InGameManager.DiveSens, v => Instance.InGameManager.DiveSens = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "air_velocity", Gameplay_Cat, () => Instance.InGameManager.GravityModifier, v => Instance.InGameManager.GravityModifier = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "dive_force", Gameplay_Cat, () => Instance.InGameManager.DiveForce, v => Instance.InGameManager.DiveForce = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.InputField, "air_dive_force", Gameplay_Cat, () => Instance.InGameManager.DiveForceInAir, v => Instance.InGameManager.DiveForceInAir = v, Instance.InGameManager.RollFGCCSettings, new FieldConfig(typeof(float), condition: () => Instance.InGameManager.DisableFGCCCheck));
            CreateEntry(MenuEntry.Type.Toggle, "spectator_join", Gameplay_Cat, () => GlobalDebug.DebugJoinAsSpectatorEnabled, v => GlobalDebug.DebugJoinAsSpectatorEnabled = v);

            CreateEntry(MenuEntry.Type.Button, "to_finish", Gameplay_Cat, Instance.InGameManager.TeleportToFinish);
            CreateEntry(MenuEntry.Type.Button, "to_safe", Gameplay_Cat, Instance.InGameManager.TeleportToSafeZone);
            CreateEntry(MenuEntry.Type.Button, "to_random_player", Gameplay_Cat, Instance.InGameManager.TeleportToRandomPlayer);
            CreateEntry(MenuEntry.Type.Button, "toggle_players", Gameplay_Cat, Instance.InGameManager.TogglePlayers);
#endif
            CreateEntry(MenuEntry.Type.Button, "force_menu", Gameplay_Cat, Instance.ForceMainMenu);

            CreateEntry(MenuEntry.Type.Toggle, "owoify", Secret_Cat, () => Instance.IsOwoifyEnabled, v => Instance.IsOwoifyEnabled = v, Instance.ResolveOwoify, new ToggleConfig(dispCondition: () => Instance.GUIUtil.EnabledSecret));
        }

        internal void ConfigureSave(List<ConfigEntry> entries)
        {
            foreach (var entry in entries)
            {
                var target = Entries.Find(x => x.ID == entry.ConfigID);
                if (target == null)
                    continue;

                RollInMenu.Enqueue(new(() =>
                {
                    target.Set(entry.Value);
                }));
            }
        }

        internal List<ConfigEntry> ConfigureForSave(bool cleanup)
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

        void CreateEntry<T>(MenuEntry.Type type, string id, string category, Func<T> getter = null, Action<T> setter = null, Action onSet = null, IEntryConfig additionalConfig = null)
        {
            if (Entries.Any(x => x.ID == id))
            {
                MelonLogger.Warning($"Unable to create entry with id {id} as another entry with the same id is exist");
                return;
            }

            Entries.Add(new(type, id, category, $"{id}_title", $"{id}_desc", val => { if (val is T v) setter(v); return getter(); }, onSet, additionalConfig));
        }

        void CreateEntry(MenuEntry.Type type, string id, string category, Action onSet = null, IEntryConfig additionalConfig = null)
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
            for (int i = 0; i < RollInMenu.Count; i++)
            {
                RollInMenu.Dequeue().Invoke();
            }
        }

        internal void ResetSettings()
        {
            foreach (var entry in Entries.Where(x => x.CanBeSaved))
                entry.Set(entry.InitialValue);

            FLZ_ToolsManager.Instance.Config.SaveConfig(true);
        }
    }
}
