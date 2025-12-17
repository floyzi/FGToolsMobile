using Il2CppDG.Tweening;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppTMPro;
using Il2CppUniRx;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Screens;
using NOTFGT.FLZ_Common.GUI.Screens.Logic;
using NOTFGT.FLZ_Common.GUI.Styles;
using NOTFGT.FLZ_Common.GUI.Styles.Logic;
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using Action = System.Action;
using Image = UnityEngine.UI.Image;


namespace NOTFGT.FLZ_Common.GUI
{
    public class GUIManager
    {
        internal const string TAB_PREFIX = "NavTab";
        internal const string SCREEN_PREFIX = "TAB";
        internal const string STYLE_PREFIX = "Style";
        internal static readonly Color TabActiveCol = new(0.7f, 0.9f, 1f, 1f);

        internal enum UIState
        {
            Disabled,
            Hidden,
            Active
        }

        UIState CurrentUIState = UIState.Disabled;
        internal bool IsUIActive => CurrentUIState == UIState.Active;

        AssetBundle GUI_Bundle;

        #region BUNDLE REFERENCES
        #region PREFABS
        [PrefabReference("NOT_FGToolsGUI")] readonly GameObject GUIObjectPrefab;
        //[PrefabReference("DialogWindow")] readonly GameObject DialogObjectPrefab; //currently not implemented
        [PrefabReference("FlashElement")] readonly GameObject FlashObjectPrefab;
        #endregion

        [PrefabReference("ExpandMore")] internal readonly Sprite SpriteExpandMore;
        [PrefabReference("ExpandLess")] internal readonly Sprite SpriteExpandLess;
        #endregion

        internal GameObject GUIInstance;

        internal List<Transform> ScreensCache;
        internal List<Transform> StylesCache;

        readonly HashSet<string> KnownNames;
        List<Transform> UIContent;
        List<UIScreen> Screens;
        List<UIStyle> Styles;
        Queue<Action> FontSetupQueue;
        HashSet<string> MissingReferences;

        bool HasGUIKilled = false;
        bool SucceedGUISetup = false;
        bool WasInMenu = false;
        bool CanStartUISetup => MissingReferences.Count == 0;
        bool AllowGUIActions => GUI_Bundle != null && GUIInstance != null;
        public GUIManager(Action onInit)
        {
            KnownNames = [];

            MelonCoroutines.Start(LoadGUI(Path.Combine(Core.AssetsDir, Constants.BundleName), (took) =>
            {
                Styles = [];
                Screens = [];
                FontSetupQueue = [];
                MissingReferences = [];

                OnMenuEnter += MenuEvent;

                GUIInstance = GameObject.Instantiate(GUIObjectPrefab);
                GameObject.DontDestroyOnLoad(GUIInstance);
                GUIInstance.GetComponent<Canvas>().sortingOrder = 9990;
              
                UIContent = [.. GUIInstance.transform.GetComponentsInChildren<Transform>(true)];

                StylesCache = UIContent.FindAll(x => x.name.StartsWith($"{STYLE_PREFIX}_"));

                Styles.Add(new HiddenStyle());
                Styles.Add(new DefaultStyle());
                Styles.Add(new GameplayStyle());

                ScreensCache = UIContent.FindAll(x => x.name.StartsWith(TAB_PREFIX) || x.name.StartsWith(SCREEN_PREFIX));

                Screens.Add(new ToolsScreen());
                Screens.Add(new RoundLoaderScreen());
                Screens.Add(new LogScreen());
                Screens.Add(new CreditsScreen());

                ToggleGUI(UIState.Disabled);

                MelonLogger.Msg($"[{GetType().Name}] UI configured, took: {took:F2}s");

                onInit?.Invoke();

                FLZ_AndroidExtensions.ShowToast(LocalizationManager.LocalizedString("toast_init_ok", [Constants.DefaultName]));
            }));
        }

        IEnumerator LoadGUI(string bPath, Action<double> onSucceed)
        {
            if (HasGUIKilled || GUIInstance != null)
                yield break;

            MelonLogger.Msg($"[{GetType().Name}] Trying to load bundle from: \"{bPath}\"");

            if (!File.Exists(bPath))
            {
                FailPopup(LocalizationManager.LocalizedString("init_fail_bundle_missing", [bPath, Constants.DefaultName]));
                yield break;
            }

            var sw = new Stopwatch();
            sw.Start();

            var bReq = AssetBundle.LoadFromFileAsync(bPath);

            while (!bReq.isDone) yield return null;

            GUI_Bundle = bReq.assetBundle;

            if (GUI_Bundle == null)
            {
                FailPopup(LocalizationManager.LocalizedString("init_fail_null_bundle", [Constants.BundleName, bPath]));
                yield break;
            }

            var map = GetFieldsOf<PrefabReferenceAttribute>();

            if (map == null || map.Count == 0)
            {
                FailPopup(LocalizationManager.LocalizedString("init_fail_no_prefabs"));
                yield break;
            }

            var tasks = new List<(FieldInfo field, AssetBundleRequest request, string name)>();

            foreach (var mapped in map)
            {
                var pName = mapped.Value.GetCustomAttribute<PrefabReferenceAttribute>().Name;
                MelonLogger.Msg($"[{GetType().Name}] Loading prefab \"{pName}\"...");

                var req = GUI_Bundle.LoadAssetAsync(pName, Il2CppType.From(mapped.Value.FieldType));
                tasks.Add((mapped.Value, req, pName));
            }

            var cast = typeof(Il2CppObjectBase).GetMethod("TryCast", BindingFlags.Instance | BindingFlags.Public);

            foreach (var (field, request, name) in tasks)
            {
                while (!request.isDone) yield return null;

                try
                {
                    var a = request.asset;

                    if (a == null)
                    {
                        FailPopup(LocalizationManager.LocalizedString("init_fail_null_asset", [name]));
                        yield break;
                    }

                    MelonLogger.Msg($"[{GetType().Name}] Loaded \"{name}\"...");

                    a.hideFlags = HideFlags.HideAndDontSave;

                    field.SetValue(this, cast.MakeGenericMethod(field.FieldType).Invoke(a, null));
                }
                catch (Exception e)
                {
                    Core.InitFail(e);
                    yield break;
                }
            }

            onSucceed?.Invoke(sw.Elapsed.TotalSeconds);

            sw.Stop();
        }


        internal void ToggleGUI(UIState toggle)
        {
            CurrentUIState = toggle;

            foreach (var style in Styles) style.IsActive = false;

            var map = new Dictionary<UIState, List<UIStyle>>
            {
                { UIState.Hidden, [GetStyle<HiddenStyle>()] },
                { UIState.Active, [GetStyle<DefaultStyle>()] }
            };

            if (map.TryGetValue(toggle, out var v))
            {
                foreach (var e in v) e.IsActive = true;
            }

            var gps = GetStyle<GameplayStyle>();
            gps.IsActive = gps.HasItemsInside;
        }

        void MenuEvent()
        {
            if (HasGUIKilled)
                return;

            if (!CanStartUISetup)
            {
                var m = string.Join(", ", MissingReferences);
                MelonLogger.Warning($"MISSING REFERENCES: {m}");
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("generic_error_title"), LocalizationManager.LocalizedString("setup_references_err_desc", [m]), ModalType.MT_OK, OKButtonType.Default);
                return;
            }

            if (!SucceedGUISetup)
            {
                LocalizedStr.LocalizeStrings?.Invoke();

                FLZ_GUIExtensions.ConfigureFonts();
                foreach (var tmp in GUIInstance.transform.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.gameObject.AddComponent<LocalizedStr>().Setup();
                    FLZ_GUIExtensions.SetupFont(tmp, Constants.TMPFontFallback, "2.0_Shadow");
                }

                while (FontSetupQueue.Count > 0)
                {
                    FontSetupQueue.Dequeue().Invoke();
                }

                foreach (var s in Styles)
                    s.CreateStyle();

                foreach (var s in Screens)
                    s.CreateScreen();

                SetupGUI();
            }

            Instance.HandlePlayerState(PlayerState.Menu);

            if (!WasInMenu)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("welcome_title", [Constants.DefaultName]), LocalizationManager.LocalizedString("welcome_desc", [Constants.DefaultName]), ModalType.MT_OK, OKButtonType.Default, new Action<bool>((wasok) =>
                {
                    WasInMenu = true;
                    ToggleGUI(UIState.Active);
                    ToggleScreen(UIScreen.ScreenType.Cheats);
                    Instance.Config.EntriesManager.ReleaseQueue();
                }));
            }
        }

        bool GetField<T>(FieldInfo field, object from, out T res) where T : class
        {
            var guiAttr = field.GetCustomAttribute<GUIReferenceAttribute>();
            res = null;

            if (guiAttr == null)
            {
                MelonLogger.Error($"Field missing GUI Refrence on \"{field.Name}\"");
                return false;
            }

            if (!typeof(T).IsAssignableFrom(field.FieldType))
            {
                MelonLogger.Error($"Field on \"{guiAttr.Name}\" is not {typeof(T).Name} (but {field.FieldType.Name} instead)");
                return false;
            }

            var f = from ?? this;
            var val = field.GetValue(f);
            if (val == null)
            {
                MelonLogger.Error($"Field on \"{guiAttr.Name}\" is null");
                return false;
            }

            res = val as T;
            return true;
        }

        void SetupAudio(FieldInfo field, AudioReferenceAttribute attr, object from = null)
        {
            if (attr == null)
                return;

            if (GetField<Selectable>(field, from, out var res))
            {
                res.gameObject.AddComponent<ElementSFX>();
                ElementSFX.RegisterPrefab(res, attr.Data);
            }
        }

        void SetupText(FieldInfo field, TMPReferenceAttribute attr, object from = null)
        {
            if (attr == null)
                return;

            if (GetField<TextMeshProUGUI>(field, from, out var res))
                FLZ_GUIExtensions.SetupFont(res, attr.FontName, attr.MaterialName);
        }

        void SetupGUI()
        {
            if (SucceedGUISetup)
                return;

            try
            {
               
                GUIInstance.gameObject.SetActive(true);
                SucceedGUISetup = true;
            }
            catch (Exception e)
            {
                var exstr = FLZ_Extensions.FormatException(e);
                MelonLogger.Msg($"GUI Setup failed. {exstr}");
                FailPopup(LocalizationManager.LocalizedString("init_fail_setup_exception", [exstr]));
            }
        }

        internal void FlashImage(float length) => MelonCoroutines.Start(FlashImpl(length));

        IEnumerator FlashImpl(float length)
        {
            var flashInst = GameObject.Instantiate(FlashObjectPrefab);
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(0.15f);
            flashInst.GetComponentInChildren<Image>().DOColor(new Color(255, 255, 255, 0), length);
            GameObject.Destroy(flashInst, length + 0.2f);
        }

        static void FailPopup(string addotionalMsg) 
        {
            Core.InitFail();
            FLZ_AndroidExtensions.ShowModal(LocalizationManager.LocalizedString("gui_init_fail_generic_title"), LocalizationManager.LocalizedString("gui_init_fail_generic_desc", [addotionalMsg]));
        }

        internal Dictionary<string, FieldInfo> GetFieldsOf<T>(Type from = null) where T : NameAttribute
        {
            var map = new Dictionary<string, FieldInfo>();
            var type = from ?? GetType();

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<T>() != null);
            if (fields == null)
                return map;

            foreach (var field in fields)
            {
                var reference = field.GetCustomAttribute<T>();
                if (reference == null) continue;

                if (KnownNames.Contains(reference.Name))
                {
                    FailPopup(LocalizationManager.LocalizedString("setup_name_conflict", [reference.GetType().Name, reference.Name]));
                    break;
                }

                map[reference.Name] = field;
                KnownNames.Add(reference.Name);
            }

            return map;
        }

        internal void Reference(Dictionary<string, FieldInfo> map, object from = null)
        {
            var cast = typeof(Il2CppObjectBase).GetMethod("TryCast", BindingFlags.Instance | BindingFlags.Public);
            var f = from ?? this;

            foreach (var t in UIContent)
            {
                if (map.TryGetValue(t.name, out var field))
                {
                    object component = null;

                    if (field.FieldType == typeof(GameObject))
                        component = t.gameObject;
                    else
                    {
                        component = t.GetComponent(Il2CppType.From(field.FieldType));

                        if (component != null)
                            component = cast.MakeGenericMethod(field.FieldType).Invoke(component, null);
                    }

                    if (component != null)
                    {
                        field.SetValue(f, component);
                        var tmpAttr = field.GetCustomAttribute<TMPReferenceAttribute>();
                        var audAttr = field.GetCustomAttribute<AudioReferenceAttribute>();

                        if (tmpAttr != null)
                        {
                            if (WasInMenu)
                                SetupText(field, tmpAttr, f);
                            else
                            {
                                FontSetupQueue.Enqueue(new Action(() =>
                                {
                                    SetupText(field, tmpAttr, f);
                                }));
                            }
                        }

                        if (audAttr != null)
                            SetupAudio(field, audAttr, f);
                    }
                    else
                        MelonLogger.Error($"COMPONENT {field.FieldType.Name} ON {t.name} IS NULL!");

                    if (field.GetValue(f) == null)
                        MelonLogger.Error($"VALUE OF FIELD \"{field.Name}\" (type of \"{field.FieldType.Name}\") IS NULL AFTER SET!!!");

                    map.Remove(t.name);
                }
            }

            foreach (var (name, field) in map)
            {
                if (field.GetValue(f) == null)
                {
                    MissingReferences.Add($"{name} ({field.Name})");
                }
            }
        }

        internal void ToggleScreen(UIScreen.ScreenType newScreen)
        {
            foreach (var screen in Screens)
            {
                var block = screen.ScreenTab.colors;
                block.normalColor = Color.white;
                block.selectedColor = Color.white;
                block.highlightedColor = Color.white;
                screen.ScreenTab.GetComponent<Button>().colors = block;

                screen.IsActive = false;
            }

            var nextScreen = Screens.FirstOrDefault(x => x.Type == newScreen);

            var block2 = nextScreen.ScreenTab.colors;
            block2.normalColor = TabActiveCol;
            block2.selectedColor = TabActiveCol;
            block2.highlightedColor = TabActiveCol;
            nextScreen.ScreenTab.colors = block2;

            nextScreen.IsActive = true;
        }

        internal T GetScreen<T>() where T : UIScreen => Screens.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
        internal T GetStyle<T>() where T : UIStyle => Styles.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
    }
}
