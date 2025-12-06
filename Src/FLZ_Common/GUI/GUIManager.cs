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
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using static NOTFGT.FLZ_Common.GUI.Attributes.TMPReferenceAttribute;
using Action = System.Action;
using Image = UnityEngine.UI.Image;


namespace NOTFGT.FLZ_Common.GUI
{
    public class GUIManager
    {
        internal const string TAB_PREFIX = "NavTab";
        internal const string SCREEN_PREFIX = "TAB";
        readonly Color TabActiveCol = new(0.7f, 0.9f, 1f, 1f);

        internal enum UIState
        {
            Disabled,
            Hidden,
            Active
        }

        UIState CurrentUIState;
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

        [GUIReference("PanelBG")] internal readonly RectTransform PanelBG;
        [GUIReference("StyleDefault")] readonly GameObject DefaultStyle;
        [GUIReference("ToolsButton")] readonly Button HiddenStyle;
        [GUIReference("StyleGameplay")] readonly GameObject GameplayStyle;
        [GUIReference("StyleRepair")] readonly GameObject RepairStyle;

        [GUIReference("HideButton")] readonly Button HideButton;

        [GUIReference("GPActive")] readonly GameObject GameplayActive;
        [GUIReference("GPHidden")] readonly GameObject GameplayHidden;
        [GUIReference("OpenGPPanel")] readonly Button OpenGP;
        [GUIReference("HideGPPanel")] readonly Button HideGP;

        [GUIReference("GPActionsView")] readonly Transform GPActionsView;
        [GUIReference("GPButtonPrefab")] readonly Button GPBtn;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("HeaderTitle")] readonly TextMeshProUGUI Header;
        [TMPReference(FontType.AsapBold, "2.0_Shadow")]
        [GUIReference("HeaderSlogan")] readonly TextMeshProUGUI Slogan;

        readonly HashSet<string> KnownNames;
        List<Transform> UIContent;
        internal List<Transform> ScreensCache;
        List<UIScreen> Screens;
        Queue<Action> FontSetupQueue;
        List<Transform> GameplayActions;

        bool HasGUIKilled = false;
        bool SucceedGUISetup = false;
        bool WasInMenu = false;
        bool CanStartUISetup = true;
        bool AllowGUIActions => GUI_Bundle != null && GUIInstance != null;
        public GUIManager(Action onInit)
        {
            KnownNames = [];

            MelonCoroutines.Start(LoadGUI(Path.Combine(Core.AssetsDir, Constants.BundleName), (took) =>
            {
                GameplayActions = [];
                Screens = [];
                FontSetupQueue = [];

                OnMenuEnter += MenuEvent;

                GUIInstance = GameObject.Instantiate(GUIObjectPrefab);
                GameObject.DontDestroyOnLoad(GUIInstance);
                GUIInstance.GetComponent<Canvas>().sortingOrder = 9990;
              
                UIContent = [.. GUIInstance.transform.GetComponentsInChildren<Transform>(true)];
                ScreensCache = UIContent.FindAll(x => x.name.StartsWith(TAB_PREFIX) || x.name.StartsWith(SCREEN_PREFIX));

                var map = GetFieldsOf<GUIReferenceAttribute>();

                Reference(map);

                Screens.Add(new ToolsScreen());
                Screens.Add(new RoundLoaderScreen());
                Screens.Add(new LogScreen());
                Screens.Add(new CreditsScreen());

                ToggleGUI(UIState.Disabled);

                MelonLogger.Msg($"[{GetType()}] UI configured, took: {took:F2}s");

                onInit?.Invoke();

                FLZ_AndroidExtensions.ShowToast($"{Constants.DefaultName} initialized successfully");
            }));
        }

        IEnumerator LoadGUI(string bPath, Action<double> onSucceed)
        {
            if (HasGUIKilled || GUIInstance != null)
                yield break;

            MelonLogger.Msg($"Trying to load bundle from: \"{bPath}\"");

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
                MelonLogger.Msg($"Loading prefab \"{pName}\"...");

                var req = GUI_Bundle.LoadAssetAsync(pName, Il2CppType.From(mapped.Value.FieldType));
                tasks.Add((mapped.Value, req, pName));
            }

            if (tasks == null || tasks.Count == 0)
            {
                FailPopup(LocalizationManager.LocalizedString("init_fail_no_tasks"));
                yield break;
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

                    MelonLogger.Msg($"Loaded \"{name}\"...");

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

        void ToggleGUI(UIState toggle)
        {
            CurrentUIState = toggle;
            switch (toggle)
            {
                case UIState.Disabled:
                    DefaultStyle?.SetActive(false);
                    HiddenStyle?.gameObject.SetActive(false);
                    break;
                case UIState.Hidden:
                    DefaultStyle?.SetActive(false);
                    HiddenStyle?.gameObject.SetActive(true);
                    break;
                case UIState.Active:
                    DefaultStyle?.SetActive(true);
                    HiddenStyle?.gameObject.SetActive(false);
                    break;
            }
        }

        void MenuEvent()
        {
            if (HasGUIKilled)
                return;

            try
            {
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

                    foreach (var s in Screens)
                        s.CreateScreen();

                    SetupGUI();
                }

                ResetGPUI();

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
            catch (Exception e)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("failed_title", [Constants.DefaultName]), FLZ_Extensions.FormatException(e), ModalType.MT_OK, OKButtonType.Default);
            }
        }


        void SetupText(FieldInfo field, TMPReferenceAttribute attr, object from = null)
        {
            var f = from ?? this;
            var guiAttr = field.GetCustomAttribute<GUIReferenceAttribute>();

            if (attr == null)
                return;

            if (guiAttr == null)
            {
                MelonLogger.Warning($"TMP Reference missing GUI Refrence on \"{field.Name}\"");
                return;
            }

            if (field.FieldType != typeof(TextMeshProUGUI))
            {
                MelonLogger.Warning($"TMP Reference on \"{guiAttr.Name}\" is not TextMeshProUGUI");
                return;
            }

            if (field.GetValue(f) == null)
            {
                MelonLogger.Warning($"TMP Reference on \"{guiAttr.Name}\" is null");
                return;
            }

            var tmpText = field.GetValue(f) as TextMeshProUGUI;

            FLZ_GUIExtensions.SetupFont(tmpText, attr.FontName, attr.MaterialName);
        }

        void SetupGUI()
        {
            if (!CanStartUISetup)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("generic_error_title"), LocalizationManager.LocalizedString("setup_references_err_desc"), ModalType.MT_OK, OKButtonType.Default);
                return;
            }

            if (SucceedGUISetup)
                return;

            try
            {
                HiddenStyle.gameObject.AddComponent<ToolsButton>().onClick = new Action(() => { ToggleGUI(UIState.Active); });

                HideButton.onClick.AddListener(new Action(() => { ToggleGUI(UIState.Hidden); }));

                HideGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, false); }));
                OpenGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, true); }));

                Header.text = $"{Constants.DefaultName} V{Core.BuildInfo.Version}";
                Slogan.text = $"{Constants.Description}";

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

        public void UpdateGPActions(Dictionary<string, Action> actions = null)
        {
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    GameObject btnPrefab = UnityEngine.Object.Instantiate(GPBtn.gameObject, GPActionsView);
                    btnPrefab.SetActive(true);
                    btnPrefab.name = action.Key;

                    btnPrefab.GetComponentInChildren<Button>().onClick.AddListener(action.Value);
                    btnPrefab.GetComponentInChildren<TextMeshProUGUI>().gameObject.AddComponent<LocalizedStr>().Setup(action.Key);

                    GameplayActions.Add(btnPrefab.transform);
                }
            }
            else
            {     
                foreach (var trans in GameplayActions)
                {
                    UnityEngine.Object.Destroy(trans.gameObject);
                }
                GameplayActions.Clear();
                UpdateGPUI(false, false);
            }
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

                        if (tmpAttr != null)
                        {
                            FontSetupQueue.Enqueue(new Action(() =>
                            {
                                SetupText(field, tmpAttr, f);
                            }));
                        }
                    }
                    else
                        MelonLogger.Error($"COMPONENT {field.FieldType.Name} ON {t.name} IS NULL!");

                    if (field.GetValue(f) == null)
                    {
                        MelonLogger.Error($"VALUE OF FIELD \"{field.Name}\" (type of \"{field.FieldType.Name}\") IS NULL AFTER SET!!!");
                        CanStartUISetup = false;
                    }

                    map.Remove(t.name);
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

                screen.ScreenContainer.gameObject.SetActive(false);
            }

            var nextScreen = Screens.FirstOrDefault(x => x.Type == newScreen);

            var block2 = nextScreen.ScreenTab.colors;
            block2.normalColor = TabActiveCol;
            block2.selectedColor = TabActiveCol;
            block2.highlightedColor = TabActiveCol;
            nextScreen.ScreenTab.colors = block2;

            nextScreen.ScreenContainer.SetActive(true);
        }

        public void UpdateGPUI(bool keepGUIOn, bool active)
        {
            GameplayStyle.SetActive(keepGUIOn);
            GameplayHidden.SetActive(!active);
            GameplayActive.SetActive(active);
        }

        void ResetGPUI()
        {
            GameplayActive.SetActive(false);
            GameplayHidden.SetActive(true);
            GameplayStyle.SetActive(false);
            GameplayActions.Clear();
        }

        internal void RefreshEntries()
        {
            if (!IsUIActive) return;
            foreach (var e in TrackedEntry.TrackedEntries) e.Refresh();
        }
    }
}
