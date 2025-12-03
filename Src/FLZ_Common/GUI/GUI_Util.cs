using Il2CppDG.Tweening;
using Il2CppFG.Common.CMS;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppTMPro;
using Il2CppUniRx;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.Localization;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using static NOTFGT.FLZ_Common.GUI.Attributes.TMPReferenceAttribute;
using static NOTFGT.FLZ_Common.GUI.ToolsMenu;
using Action = System.Action;
using Application = UnityEngine.Application;
using Image = UnityEngine.UI.Image;


namespace NOTFGT.FLZ_Common.GUI
{
    //TODO: rewrite this trash
    public class GUI_Util
    {
        internal enum UIState
        {
            Disabled,
            Hidden,
            Active
        }

        UIState CurrentUIState;
        internal bool IsUIActive => CurrentUIState == UIState.Active;

        Color tabActiveCol = new(0.7028302f, 0.9941195f, 1f, 1f);

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

        //TODO: organize ts

        [GUIReference("MainBG")] internal readonly RectTransform PanelBG;

        [GUIReference("StyleDefault")] readonly GameObject DefaultStyle;
        [GUIReference("ToolsButton")] readonly Button HiddenStyle;
        [GUIReference("StyleGameplay")] readonly GameObject GameplayStyle;
        [GUIReference("StyleRepair")] readonly GameObject RepairStyle;

        [GUIReference("DeleteConfig")] readonly Button DeleteConfig;
        [GUIReference("IgnoreThis")] readonly Button IgnoreThis;
        [GUIReference("ErrorDisplay")] readonly Text ErrorDisplay;

        [GUIReference("HideButton")] readonly Button HideButton;

        [GUIReference("ConfigDisplay")] readonly Transform configMenu;
        [GUIReference("WriteSave")] readonly Button applyChanges;
        [GUIReference("ResetConfig")] readonly Button deleteConfig;

        [GUIReference("PendingChangesAlert")] readonly GameObject PendingChanges;
        [GUIReference("ToggleReference")] readonly GameObject GUI_TogglePrefab;
        [GUIReference("FieldReference")] readonly GameObject GUI_TextFieldPrefab;
        [GUIReference("SliderReference")] readonly GameObject GUI_SliderPrefab;
        [GUIReference("MenuHeaderReference")] readonly GameObject GUI_HeaderPrefab;
        [GUIReference("MenuHeaderDescReference")] readonly GameObject GUI_HeaderDescPrefab;
        [GUIReference("ButtonReference")] readonly GameObject GUI_ButtonPrefab;

        [GUIReference("RoundInputField")] readonly TMP_InputField RoundIdInputField;
        [GUIReference("RoundLoadBtn")] readonly Button RoundLoadButton;
        [GUIReference("RandomRoundBtn")] readonly Button RoundLoadRandomButton;
        [GUIReference("RoundID_Entry")] readonly GameObject RoundIDEntry;
        [GUIReference("RoundID_EntryV2")] readonly Button RoundIDEntryV2;
        [GUIReference("RoundIDSView")] readonly Transform RoundIdsView;
        [GUIReference("RoundGenListBtn")] readonly Button RoundGenerateListButton;
        [GUIReference("RoundListCleanup")] readonly Button CleanupList;
        [GUIReference("RoundsDropDown")] readonly TMP_Dropdown RoundsDropdown;
        [GUIReference("RoundsIDSDropDown")] readonly TMP_Dropdown IdsDropdown;
        [GUIReference("ClickToCopyNote")] readonly GameObject ClickToCopy;

        #region LOGS
        [GUIReference("LogMessage")] internal readonly Button LogPrefab;
        [GUIReference("LogInfo")] internal readonly TextMeshProUGUI LogInfo;
        [GUIReference("LogDisplay")] internal readonly Transform LogContent;
        [GUIReference("ClearLogsBtn")] internal readonly Button ClearLogsBtn;
        [GUIReference("LogStats")] internal readonly TextMeshProUGUI LogStats;
        [GUIReference("LogDisabled")] internal readonly GameObject LogDisabledScreen;

        [GUIReference("LogBtn_All")] readonly Button AllLogsBtn;
        [GUIReference("LogBtn_Info")] readonly Button InfoLogsBtn;
        [GUIReference("LogBtn_Warn")] readonly Button WarnLogsBtn;
        [GUIReference("LogBtn_Error")] readonly Button ErrorLogsBtn;
        #endregion

        [GUIReference("GPActive")] readonly GameObject GameplayActive;
        [GUIReference("GPHidden")] readonly GameObject GameplayHidden;
        [GUIReference("OpenGPPanel")] readonly Button OpenGP;
        [GUIReference("HideGPPanel")] readonly Button HideGP;

        [GUIReference("GPActions")] readonly GameObject GPActionsObject;
        [GUIReference("GPActionsView")] readonly Transform GPActionsView;
        [GUIReference("GPButtonPrefab")] readonly Button GPBtn;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("HeaderTitle")] readonly TextMeshProUGUI Header;
        [TMPReference(FontType.AsapBold, "2.0_Shadow")]
        [GUIReference("HeaderSlogan")] readonly TextMeshProUGUI Slogan;

        [GUIReference("CheatsScrollView")] readonly ScrollRect CheatsScrollView;
        [GUIReference("GroupLayout")] readonly Transform ToolsCategory;

        #region CREDITS
        [GUIReference("CreditsContent")] readonly Transform CreditsContent;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditAboutHeader")] readonly TextMeshProUGUI CreditsAboutHeader;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditSocialsHeader")] readonly TextMeshProUGUI CreditsSocialHeader;

        [TMPReference(FontType.AsapBold, "2.0_Shadow")]
        [GUIReference("CreditText")] readonly TextMeshProUGUI CreditTextPrefab;

        [GUIReference("CreditBTN")] readonly Button CreditButtonPrefab;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditLocaleHeader")] readonly TextMeshProUGUI CreditsLocaleHeader;

        [GUIReference("LocaleDropdown")] readonly TMP_Dropdown LocaleSelectDropdown;

        #region CREDITS IMAGE
        [GUIReference("FatefulImage")] readonly Button FatefulImage;
        [GUIReference("FatefulImageLoad")] readonly Transform FatefulImageLoading;
        [GUIReference("ImgCreditText")] readonly Transform CreditsImgCredit;
        #endregion

        #endregion

        readonly List<GameObject> Tabs = [];
        readonly List<GameObject> TabsButtons = [];

        readonly List<Transform> GameplayActions = [];

        readonly List<GameObject> EntryInstances = [];

        string ReadyRound;

        bool HasGUIKilled = false;
        bool SucceedGUISetup = false;
        bool WasInMenu = false;
        bool CanStartUISetup = true;
        bool AllowGUIActions => GUI_Bundle != null && GUIInstance != null;
        internal bool EnabledSecret => EGG_Counter >= 15;
        public GUI_Util(Action onInit)
        {
            MelonCoroutines.Start(LoadGUI(Path.Combine(Core.AssetsDir, Constants.BundleName), (took) =>
            {
                OnMenuEnter += MenuEvent;

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
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_bundle_missing", [bPath, Constants.DefaultName]));
                yield break;
            }

            var sw = new Stopwatch();
            sw.Start();

            var bReq = AssetBundle.LoadFromFileAsync(bPath);

            while (!bReq.isDone) yield return null;

            GUI_Bundle = bReq.assetBundle;

            if (GUI_Bundle == null)
            {
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_bundle", [Constants.BundleName, bPath]));
                yield break;
            }

            var map = GetFieldsOf<PrefabReferenceAttribute>();

            var tasks = new List<(FieldInfo field, AssetBundleRequest request, string name)>();

            foreach (var mapped in map)
            {
                var pName = mapped.Value.GetCustomAttribute<PrefabReferenceAttribute>().Name;
                MelonLogger.Msg($"Loading prefab \"{pName}\"...");

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
                        TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_asset", [name]));
                        yield break;
                    }

                    MelonLogger.Msg($"Loaded \"{name}\"...");

                    a.hideFlags = HideFlags.HideAndDontSave;

                    field.SetValue(this, cast.MakeGenericMethod(field.FieldType).Invoke(a, null));

                    if (name == "NOT_FGToolsGUI") //sucks
                    {
                        GUIInstance = GameObject.Instantiate(field.GetValue(this) as GameObject);
                        GameObject.DontDestroyOnLoad(GUIInstance);
                        GUIInstance.GetComponent<Canvas>().sortingOrder = 9990;
                        ConfigureObjects();
                        GUIInstance.gameObject.SetActive(false);
                        RepairStyle.gameObject.SetActive(false);
                    }
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
                    SetupText();
                    SetupGUI();
                    ToggleGUI(UIState.Disabled);
                }

                ResetGPUI();

                Instance.HandlePlayerState(PlayerState.Menu);

                if (!WasInMenu)
                {
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("welcome_title", [Constants.DefaultName]), LocalizationManager.LocalizedString("welcome_desc", [Constants.DefaultName]), ModalType.MT_OK, OKButtonType.Default, new Action<bool>((wasok) =>
                    {
                        WasInMenu = true;
                        ToggleGUI(UIState.Active);
                        ToggleTab(Tabs[0], TabsButtons[0].GetComponent<Button>());
                        Instance.SettingsMenu.ReleaseQueue();
                    }));
                }
            }
            catch (Exception e)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("failed_title", [Constants.DefaultName]), FLZ_Extensions.FormatException(e), ModalType.MT_OK, OKButtonType.Default);
            }
        }

  
        void SetupText()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<TMPReferenceAttribute>() != null).ToList();

            FLZ_GUIExtensions.ConfigureFonts();

            foreach (var tmp in GUIInstance.transform.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                FLZ_GUIExtensions.SetupFont(tmp, Constants.TMPFontFallback, "2.0_Shadow");
                tmp.gameObject.AddComponent<LocalizedStr>().Setup();
            }

            foreach (var field in fields)
            {
                var tmpAttr = field.GetCustomAttribute<TMPReferenceAttribute>();
                var guiAttr = field.GetCustomAttribute<GUIReferenceAttribute>();

                if (tmpAttr == null)
                    continue;

                if (guiAttr == null)
                {
                    MelonLogger.Warning($"TMP Reference missing GUI Refrence on \"{field.Name}\"");
                    continue;
                }

                if (field.FieldType != typeof(TextMeshProUGUI))
                {
                    MelonLogger.Warning($"TMP Reference on \"{guiAttr.Name}\" is not TextMeshProUGUI");
                    continue;
                }
                
                if (field.GetValue(this) == null)
                {
                    MelonLogger.Warning($"TMP Reference on \"{guiAttr.Name}\" is null");
                    continue;
                }

                var tmpText = field.GetValue(this) as TextMeshProUGUI;

                FLZ_GUIExtensions.SetupFont(tmpText, tmpAttr.FontName, tmpAttr.MaterialName);
            }
        }

        void SetupLogsScreen()
        {
            LogPrefab.gameObject.SetActive(false);

            AllLogsBtn.onClick.AddListener(new Action(GUI_LogEntry.CreateAllInstances));
            InfoLogsBtn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(LogType.Log)));
            WarnLogsBtn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(LogType.Warning)));
            ErrorLogsBtn.onClick.AddListener(new Action(() => GUI_LogEntry.CreateInstancesOf(LogType.Error)));

            GUI_LogEntry.UpdateLogStats();
        }

        void CreateCreditsScreen()
        {
            CreditTextPrefab.gameObject.SetActive(false);
            CreditButtonPrefab.gameObject.SetActive(false);

            var aboutStrings = new Dictionary<string, object[]>()
            {
                { "about_by", [Constants.DefaultName] },
                { "about_build_date", [Core.BuildInfo.BuildDate] },
                { "about_commit", [Core.BuildInfo.GetCommit()] },
            };

            var socialBtns = new Dictionary<string, string>()
            {
                { "about_sources", Constants.GitHubURL },
                { "about_check_commit", $"{Constants.GitHubURL}/commit/{Core.BuildInfo.Commit}" },
                { "twitter", Constants.TwitterURL },
                { "discord", Constants.DiscordURL },
            };

            var indx = CreditTextPrefab.transform.GetSiblingIndex();

            foreach (var line in aboutStrings)
            {
                var res = UnityEngine.Object.Instantiate(CreditTextPrefab, CreditsContent);
                res.GetComponentInChildren<TextMeshProUGUI>().GetComponent<LocalizedStr>().Setup(line.Key, line.Value);
                res.gameObject.SetActive(true);
                res.transform.SetSiblingIndex(indx++);
            } 
            
            indx = CreditButtonPrefab.transform.GetSiblingIndex();

            foreach (var btn in socialBtns)
            {
                var res = UnityEngine.Object.Instantiate(CreditButtonPrefab, CreditsContent);
                res.GetComponentInChildren<TextMeshProUGUI>().GetComponent<LocalizedStr>().Setup(btn.Key);
                res.onClick.AddListener(new Action(() =>
                {
                    Application.OpenURL(btn.Value);
                }));
                res.gameObject.SetActive(true);
                res.transform.SetSiblingIndex(indx++);
            }

        }

        int EGG_Counter;
        Sprite CachedCreditsSpr;
        readonly int EGG_ClicksNeeded = UnityEngine.Random.Range(15, 25);
        void EasterEgg()
        {
            EGG_Counter++;

            if (EGG_Counter < 3)
                return;

            FatefulImage.transform.GetParent().DOShakePosition(0.25f, EGG_Counter > EGG_ClicksNeeded ? 15 : 15 + (EGG_Counter * 2), 80, 400);
            FLZ_AndroidExtensions.Vibrate(EGG_Counter > EGG_ClicksNeeded ? 10 : 10 + EGG_Counter);

            if (EGG_Counter < EGG_ClicksNeeded)
                return;
            else if (EGG_Counter == EGG_ClicksNeeded)
            {
                FLZ_AndroidExtensions.Vibrate(700);
                FLZ_AndroidExtensions.ShowToast("Woowie, you found something useless!");
            }

            if (EGG_Counter == 100 + EGG_ClicksNeeded)
            {
                FLZ_AndroidExtensions.ShowToast("Still not tired?");
            }

            if (CachedCreditsSpr == null)
                CachedCreditsSpr = FatefulImage.GetComponent<Image>().sprite;

            MelonCoroutines.Start(RequestImage());
        }

        IEnumerator RequestImage()
        {
            if (!FatefulImage.interactable) yield break;

            FatefulImage.interactable = false;
            FatefulImageLoading.gameObject.SetActive(true);

            var req = new UnityWebRequest(Constants.ImagesAPI)
            {
                timeout = 5,
                downloadHandler = new DownloadHandlerBuffer()
            };

            yield return req.SendWebRequest();

            FatefulImage.interactable = true;
            FatefulImageLoading.gameObject.SetActive(false);

            var spr = FLZ_Extensions.SetSprite(req.downloadHandler.data, CachedCreditsSpr);
            CreditsImgCredit.gameObject.SetActive(spr == CachedCreditsSpr);
            FatefulImage.GetComponent<Image>().sprite = spr;
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
                FatefulImage.onClick.AddListener(new Action(() =>
                {
                    EasterEgg();
                }));
                ClearLogsBtn.onClick.AddListener(new Action(() =>
                {
                    CleanupScreen(LogContent, true);
                    GUI_LogEntry.UpdateLogStats();
                }));
                applyChanges.onClick.AddListener(new Action(Instance.Config.DoUIConfigSave));

                HiddenStyle.gameObject.AddComponent<ToolsButton>().onClick = new Action(() => { ToggleGUI(UIState.Active); });

                HideButton.onClick.AddListener(new Action(() => { ToggleGUI(UIState.Hidden); }));

                RoundIdInputField.onValueChanged.AddListener(new Action<string>((str) => { ReadyRound = str; }));
                RoundLoadButton.onClick.AddListener(new Action(() =>
                {
                    Instance.RoundLoader.LoadRound(ReadyRound);
                }));
                CleanupList.onClick.AddListener(new Action(() =>
                {
                    ClickToCopy.SetActive(false);
                    CleanupScreen(RoundIdsView, true);
                }));
                RoundGenerateListButton.onClick.AddListener(new Action(() =>
                {
                    ClickToCopy.SetActive(false);
                    CleanupScreen(RoundIdsView, true);
                    Instance.RoundLoader.GenerateCMSList(RoundIdsView, RoundIDEntryV2);
                    ClickToCopy.SetActive(true);
                }));
                RoundLoadRandomButton.onClick.AddListener(new Action(Instance.RoundLoader.LoadRandomCms));

                HideGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, false); }));
                OpenGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, true); }));
                deleteConfig.onClick.AddListener(new Action(() => {
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("reset_config_alert_title"), LocalizationManager.LocalizedString("reset_config_alert_desc"), ModalType.MT_OK_CANCEL, OKButtonType.Disruptive, new Action<bool>((val) => {
                        if (val)
                            Instance.SettingsMenu.ResetSettings();
                    } ));
                
                }));
                RoundIDEntryV2.gameObject.SetActive(false);

                Header.text = $"{Constants.DefaultName} V{Core.BuildInfo.Version}";
                Slogan.text = $"{Constants.Description}";

                ConfigureTabs();
                CreateConfigMenu(configMenu);
                InitRoundsDropdown();
                CreateCreditsScreen();
                SetupLogsScreen();

                LocalizationManager.ConfigureDropdown(LocaleSelectDropdown);

                GUIInstance.gameObject.SetActive(true);
                SucceedGUISetup = true;
            }
            catch (Exception e)
            {
                var exstr = FLZ_Extensions.FormatException(e);
                MelonLogger.Msg($"GUI Setup failed. {exstr}");
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_setup_exception", [exstr]));
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


        void InitRoundsDropdown()
        {
            if (RoundsDropdown != null && IdsDropdown != null)
            {
                RoundsDropdown.onValueChanged.RemoveAllListeners();
                IdsDropdown.onValueChanged.RemoveAllListeners();

                RoundsDropdown.ClearOptions();
                IdsDropdown.ClearOptions();

                var rounds = CMSLoader.Instance.CMSData.Rounds;

                Dictionary<string, string> uniqRounds = [];

                foreach (var round in rounds)
                {
                    var scene = round.Value.GetSceneName();
                    if (scene == null || round.Value == null || round.Value.DisplayName == null)
                        continue;

                    if (!uniqRounds.ContainsKey(round.Value.GetSceneName()))
                        uniqRounds.Add(scene, FLZ_Extensions.CleanStr(round.Value.DisplayName.Text));
                }

                Il2CppSystem.Collections.Generic.List<string> roundNames = new();

                foreach (var round in uniqRounds.Values)
                {
                    roundNames.Add(round);
                }

                roundNames.Sort();
                RoundsDropdown.AddOptions(roundNames);

                IdsDropdown.onValueChanged.AddListener(new Action<int>(val => {
                    ReadyRound = IdsDropdown.options[val].text;
                }));

                RoundsDropdown.onValueChanged.AddListener(new Action<int>(val =>
                {
                    var scene = string.Empty;

                    foreach (var round in rounds)
                    {
                        if (round.Value.DisplayName != null && FLZ_Extensions.CleanStr(round.Value.DisplayName.Text) == RoundsDropdown.options[val].text)
                        {
                            scene = round.Value.GetSceneName();
                            break;
                        }
                    }

                    if (scene != null)
                    {
                        Il2CppSystem.Collections.Generic.List<string> ids = new();

                        foreach (var round in rounds)
                            if (round.Value.GetSceneName() == scene)
                                ids.Add(round.Key);
                        
                        IdsDropdown.ClearOptions();
                        IdsDropdown.AddOptions(ids);
                        ReadyRound = IdsDropdown.options[0].text;
                    }
                }));

                RoundsDropdown.onValueChanged.Invoke(0);
            }
        }



        static void CleanupScreen(Transform screen, bool includeOnlyActive)
        {
            for (int i = screen.childCount - 1; i >= 0; i--)
            {
                var child = screen.GetChild(i);
                if (!includeOnlyActive || child.gameObject.activeSelf)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
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

        static void TryTriggerFailedToLoadUIModal(string addotionalMsg) 
        {
            Core.InitFail();
            FLZ_AndroidExtensions.ShowModal(LocalizationManager.LocalizedString("gui_init_fail_generic_title"), LocalizationManager.LocalizedString("gui_init_fail_generic_desc", [addotionalMsg]));
        }

        Dictionary<string, FieldInfo> GetFieldsOf<T>() where T : NameAttribute
        {
            var map = new Dictionary<string, FieldInfo>();

            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<T>() != null);
            if (fields == null)
                return map;

            foreach (var field in fields)
            {
                var reference = field.GetCustomAttribute<T>();
                if (map.ContainsKey(reference.Name))
                {
                    TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("setup_name_conflict", [reference.GetType().Name, reference.Name]));
                    break;
                }
                if (reference != null) map[reference.Name] = field;
            }

            return map;
        }

        void ConfigureObjects()
        {
            var map = GetFieldsOf<GUIReferenceAttribute>();

            var cast = typeof(Il2CppObjectBase).GetMethod("TryCast", BindingFlags.Instance | BindingFlags.Public);

            foreach (var t in GUIInstance.transform.GetComponentsInChildren<Transform>(true))
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
                        field.SetValue(this, component);
                    else
                        MelonLogger.Error($"COMPONENT {field.FieldType.Name} ON {t.name} IS NULL!");

                    if (field.GetValue(this) == null)
                    {
                        MelonLogger.Error($"VALUE OF FIELD \"{field.Name}\" (type of \"{field.FieldType.Name}\") IS NULL AFTER SET!!!");
                        CanStartUISetup = false;
                    }

                    map.Remove(t.name); 
                }

                if (t.gameObject.name.StartsWith("NavTab_") && !TabsButtons.Exists(target => target.name == t.gameObject.name))
                    TabsButtons.Add(t.gameObject);

                if (t.gameObject.name.StartsWith("TAB_") && !Tabs.Exists(target => target.name == t.gameObject.name))
                    Tabs.Add(t.gameObject);
            }
        }

        void ConfigureTabs()
        {
            foreach (var navTab in TabsButtons)
            {
                Button btn = navTab.GetComponent<Button>();

                if (btn != null)
                {
                    GameObject tabOfBtn = Tabs.FirstOrDefault(tab => tab.name == navTab.name.Replace("NavTab_", "TAB_"));

                    if (tabOfBtn != null)
                    {
                        btn.onClick.AddListener(new Action(() =>
                        {
                            ToggleTab(tabOfBtn, btn);
                        }));
                    }
                }
            }
        }

        void ToggleTab(GameObject activeTab, Button btn)
        {
            foreach (var tab in Tabs)
            {
                tab.SetActive(false);
            }

            foreach (var tab in TabsButtons)
            {
                var block = tab.gameObject.GetComponent<Button>().colors;
                block.normalColor = Color.white;
                block.selectedColor = Color.white;
                block.highlightedColor = Color.white;
                tab.gameObject.GetComponent<Button>().colors = block;
            }

            var block2 = btn.colors;
            block2.normalColor = tabActiveCol;
            block2.selectedColor = tabActiveCol;
            block2.highlightedColor = tabActiveCol;
            btn.colors = block2;

            activeTab.SetActive(true);
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

        void CreateConfigMenu(Transform cfgTrans)
        {
            GUI_TogglePrefab.SetActive(false);
            GUI_TextFieldPrefab.SetActive(false);
            GUI_SliderPrefab.SetActive(false);
            GUI_HeaderPrefab.SetActive(false);
            GUI_ButtonPrefab.SetActive(false);

            MenuCategory currentCateg = null;
            string currentCategStr = "";

            foreach (var entry in Instance.SettingsMenu.Entries.OrderByDescending(entry => entry.Category.Priority).ToList())
            {
                try
                {
                    MelonLogger.Msg($"[{GetType().Name}] CreateConfigMenu() - Creating entry \"{entry.ID}\" with type \"{entry.EntryType}\"");

                    if (!string.IsNullOrEmpty(entry.Category.LocaleID) && currentCategStr != entry.Category.LocaleID)
                    {
                        currentCategStr = entry.Category.LocaleID;

                        var haderInst = UnityEngine.Object.Instantiate(GUI_HeaderPrefab, cfgTrans);
                        haderInst.name = $"Header_{entry.Category.LocaleID}";

                        currentCateg = haderInst.AddComponent<MenuCategory>();
                        currentCateg.Create(entry.Category.LocaleID);
                    }

                    switch (entry.EntryType)
                    {
                        case MenuEntry.Type.Toggle:
                            GameObject toggleInst = UnityEngine.Object.Instantiate(GUI_TogglePrefab, cfgTrans);
                            toggleInst.SetActive(true);
                            toggleInst.name = entry.ID;

                            var toggle = toggleInst.transform.Find("Toggle").GetComponent<Toggle>();
                            toggle.gameObject.AddComponent<UnityDragFix>()._ScrollRect = CheatsScrollView;
                            var toggleTracker = toggle.transform.parent.gameObject.AddComponent<TrackedEntry>();
                            toggleTracker.Create(entry, toggle, currentCateg);

                            toggleTracker.OnEntryUpdated += new Action<object>(newVal => { toggle.isOn = bool.Parse(newVal.ToString()); });
                            var toggleTitle = toggleInst.transform.Find("Toggle").GetComponentInChildren<TextMeshProUGUI>();
                            var toggleDesc = toggleInst.transform.Find("ToggleDesc").GetComponent<TextMeshProUGUI>();

                            var toggleDescRes = toggleDesc.gameObject.GetComponent<LocalizedStr>().Setup(entry.Description, prefix: "*");
                            if (!toggleDescRes)
                                toggleDesc.gameObject.SetActive(false);

                            toggleTitle.gameObject.GetComponent<LocalizedStr>().Setup(entry.DisplayName);

                            toggle.isOn = (bool)entry.GetValue();
                            toggle.onValueChanged.AddListener(new Action<bool>(val => { entry.Set(val); }));

                            EntryInstances.Add(toggleInst);
                            break;

                        case MenuEntry.Type.InputField:
                            if (entry.AdditionalConfig is not FieldConfig fieldConf)
                            {
                                MelonLogger.Error($"Can't create entry {entry.ID}. Configuration required");
                                continue;
                            }

                            GameObject fieldInst = UnityEngine.Object.Instantiate(GUI_TextFieldPrefab, cfgTrans);
                            fieldInst.SetActive(true);
                            fieldInst.name = entry.ID;

                            var inputField = fieldInst.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
                            inputField.gameObject.AddComponent<UnityDragFix>()._ScrollRect = CheatsScrollView;
                            inputField.gameObject.name = "SLOP"; //yeah...
                            var fieldTracker = inputField.transform.parent.gameObject.AddComponent<TrackedEntry>();
                            fieldTracker.Create(entry, inputField, currentCateg);

                            fieldTracker.OnEntryUpdated += new Action<object>(newVal =>
                            {
                                inputField.text = newVal.ToString();
                            });

                            var fieldTitle = fieldInst.transform.Find("FieldTitle").GetComponent<TextMeshProUGUI>();
                            var fieldDesc = fieldInst.transform.Find("FieldDesc").GetComponent<TextMeshProUGUI>();

                            var fieldDescRes = fieldDesc.gameObject.GetComponent<LocalizedStr>().Setup(entry.Description, prefix: "*");
                            if (!fieldDescRes)
                                fieldDesc.gameObject.SetActive(false);

                            fieldTitle.gameObject.GetComponent<LocalizedStr>().Setup(entry.DisplayName);

                            inputField.text = entry.GetValue().ToString();

                            if (fieldConf.CharacterLimit > 0)
                                inputField.characterLimit = fieldConf.CharacterLimit;

                            if (fieldConf.ValueType == typeof(string))
                            {
                                inputField.contentType = TMP_InputField.ContentType.Standard;
                                inputField.onValueChanged.AddListener(new Action<string>(val =>
                                {
                                    entry.Set(val);
                                }));
                            }
                            else if (fieldConf.ValueType == typeof(int))
                            {
                                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                                inputField.onValueChanged.AddListener(new Action<string>(val =>
                                {
                                    if (int.TryParse(val, out int intVal))
                                    {
                                        entry.Set(intVal);
                                    }
                                }));
                            }
                            else
                            {
                                inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                                inputField.onValueChanged.AddListener(new Action<string>(val =>
                                {
                                    if (float.TryParse(val, out float floatVal))
                                    {
                                        entry.Set(floatVal);
                                    }
                                }));
                            }

                            EntryInstances.Add(fieldInst);
                            break;
                        case MenuEntry.Type.Slider:
                            if (entry.AdditionalConfig is not SliderConfig sliderConf)
                            {
                                MelonLogger.Error($"Can't create entry {entry.ID}. Configuration required");
                                continue;
                            }

                            GameObject sliderInst = UnityEngine.Object.Instantiate(GUI_SliderPrefab, cfgTrans);
                            sliderInst.SetActive(true);
                            sliderInst.name = entry.ID;

                            var slider = sliderInst.transform.Find("Slider").GetComponent<Slider>();
                            slider.gameObject.AddComponent<UnityDragFix>()._ScrollRect = CheatsScrollView;
 
                            var sliderTitle = sliderInst.transform.Find("SliderTitle").GetComponent<TextMeshProUGUI>();
                            var sliderDesc = sliderInst.transform.Find("FieldDesc").GetComponent<TextMeshProUGUI>();

                            var sliderDescRes = sliderDesc.gameObject.GetComponent<LocalizedStr>().Setup(entry.Description, prefix: "*");
                            if (!sliderDescRes)
                                sliderDesc.gameObject.SetActive(false);

                            sliderTitle?.gameObject.GetComponent<LocalizedStr>().Setup(entry.DisplayName);

                            var sliderValue = slider.transform.Find("SliderValue").GetComponent<TextMeshProUGUI>();

                            var sliderTracker = slider.transform.parent.gameObject.AddComponent<TrackedEntry>();
                            sliderTracker.Create(entry, slider, currentCateg);

                            sliderTracker.OnEntryUpdated += new Action<object>(newVal =>
                            {
                                if (float.TryParse(newVal.ToString(), out var res))
                                    slider.value = res;

                                if (sliderConf.ValueType == typeof(float))
                                    sliderValue.text = $"{slider.value:F1} / {slider.maxValue:F1}";
                                else
                                    sliderValue.text = $"{Convert.ToInt32(slider.value)} / {Convert.ToInt32(slider.maxValue)}";
                            });

                            if (sliderConf.MinValue > 0)
                                slider.minValue = sliderConf.MinValue;
                            if (sliderConf.MaxValue > 0)
                                slider.maxValue = sliderConf.MaxValue;

                            slider.value = float.Parse(entry.InitialValue.ToString());

                            if (sliderConf.ValueType == typeof(float))
                                sliderValue.text = $"{slider.value:F1} / {slider.maxValue:F1}";
                            else
                                sliderValue.text = $"{Convert.ToInt32(slider.value)} / {Convert.ToInt32(slider.maxValue)}";

                            slider.onValueChanged.AddListener(new Action<float>(val =>
                            {
                                entry.Set(val);

                                if (sliderConf.ValueType == typeof(float))
                                    sliderValue.text = $"{val:F1} / {slider.maxValue:F1}";
                                else
                                    sliderValue.text = $"{Convert.ToInt32(val)} / {Convert.ToInt32(slider.maxValue)}";
                            }));

                            EntryInstances.Add(sliderInst);
                            break;
                        case MenuEntry.Type.Button:
                            GameObject buttonInst = UnityEngine.Object.Instantiate(GUI_ButtonPrefab, cfgTrans);
                            buttonInst.SetActive(true);
                            buttonInst.name = entry.ID;

                            var button = buttonInst.transform.Find("Button").GetComponent<Button>();
                            button.gameObject.AddComponent<UnityDragFix>()._ScrollRect = CheatsScrollView;
                            var buttonDesc = buttonInst.transform.Find("FieldDesc").GetComponent<TextMeshProUGUI>();

                            var btnDescRes = buttonDesc.gameObject.GetComponent<LocalizedStr>().Setup(entry.Description, prefix: "*");
                            if (!btnDescRes)
                                buttonDesc.gameObject.SetActive(false);

                            var buttonTracker = button.transform.parent.gameObject.AddComponent<TrackedEntry>();
                            buttonTracker.Create(entry, button, currentCateg);

                            button.GetComponentInChildren<TextMeshProUGUI>().GetComponent<LocalizedStr>().Setup(entry.DisplayName);

                            button.onClick.AddListener(new Action(() =>
                            {
                                entry.Set(null);
                            }));
                            EntryInstances.Add(buttonInst);
                            break;
                        default:
                            MelonLogger.Warning($"Fallback on: {entry.ID}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[{GetType()}] CreateConfigMenu() - Creating entry \"{entry.ID}\" with type \"{entry.EntryType}\" failed! {ex}");
                }
            }
        }

    }
}
