using Il2CppFG.Common.CMS;
using Il2CppTMPro;
using Il2CppUniRx;
using MelonLoader;
using NOTFGT.FLZ_Common.Localization;
using NOTFGT.FLZ_Common.Logic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;
using static NOTFGT.FLZ_Common.GUI.TMPReferenceAttribute;
using static NOTFGT.FLZ_Common.GUI.ToolsMenu;
using Action = System.Action;
using Application = UnityEngine.Application;
using Text = UnityEngine.UI.Text;

namespace NOTFGT.FLZ_Common.GUI
{
    /// <summary>
    /// Use this attribute to assign object from bundle to field
    /// </summary>
    /// <param name="target">Name of object in UI Bundle (Names should be unique)</param>
    [AttributeUsage(AttributeTargets.Field)]
    public class GUIReferenceAttribute(string target) : Attribute
    {
        public string Name { get; } = target;
    }

    /// <summary>
    /// Use this attribute to setup Fall Guys font on <c>TextMeshProUGUI</c> object from bundle.
    /// </summary>
    /// <param name="targetMaterial">Material that will be assigned to font. Find material names via UE on Desktop version of the game</param>
    [AttributeUsage(AttributeTargets.Field)]
    public class TMPReferenceAttribute(FontType targetFont, string targetMaterial) : Attribute
    {
        public enum FontType
        {
            TitanOne,
            AsapBold
        }

        public string FontName { get; } = targetFont == FontType.TitanOne ? TMPFontTitanOne : TMPFontAsapBold;
        public string MaterialName { get; } = targetMaterial;
    }

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
        AssetBundleRequest theGUI;

        GameObject GUIObject;

        //todo: organize ts

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
        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditAboutHeader")] readonly TextMeshProUGUI CreditsAboutHeader;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditSocialsHeader")] readonly TextMeshProUGUI CreditsSocialHeader;

        [GUIReference("CreditsContent")] readonly Transform CreditsAboutContent;

        [TMPReference(FontType.AsapBold, "2.0_Shadow")]
        [GUIReference("CreditText")] readonly TextMeshProUGUI CreditTextPrefab;

        [GUIReference("CreditsButtonsContent")] readonly Transform CreditsSocialContent;

        [GUIReference("CreditBTN")] readonly Button CreditButtonPrefab;
        #endregion


        readonly List<GameObject> Tabs = [];
        readonly List<GameObject> TabsButtons = [];

        readonly List<Transform> GameplayActions = [];

        readonly List<GameObject> EntryInstances = [];

        string BundlePath;
        string ReadyRound;

        bool HasGUIKilled = false;
        bool SuceedGUISetup = false;
        bool WasInMenu = false;
        bool CanStartUISetup = true;
        bool OnRepairScreen { get { return RepairStyle.gameObject != null && RepairStyle.gameObject.activeSelf; } }
        bool AllowGUIActions { get { return GUI_Bundle != null && GUIObject != null; } }

        Action LastFailModal;


        public void ShowRepairGUI(Exception EX)
        {
            if (!AllowGUIActions)
            {
                MelonLogger.Msg("Can't show repair screen, bundle not loaded?");
                return;
            }

            ErrorDisplay.text = $"{EX.Message}";
            RepairStyle.gameObject.SetActive(true);
            DeleteConfig.onClick.AddListener(new Action(Instance.SettingsMenu.DeleteConfig));
            IgnoreThis.onClick.AddListener(new Action(() => { 
                RepairStyle.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(GUIObject);
                HasGUIKilled = true;
            }));
        }

        public void Register()
        {
            BundlePath = Path.Combine(Core.AssetsDir, BundleName);
            MelonLogger.Msg($"EXPECTED BUNDLE PATH IS: \"{BundlePath}\"");

            GUI_Bundle = AssetBundle.LoadFromFile(BundlePath);
            OnMenuEnter += MenuEvent;

            MelonLogger.Msg($"[{GetType()}] Successful GUI_Util register. Is bundle loaded: {GUI_Bundle != null}");

            TryToLoadGUI();
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
            LastFailModal?.Invoke();

            if (OnRepairScreen || HasGUIKilled)
                return;

            try
            {
                SetupText();
                SetupGUI();
                ToggleGUI(UIState.Disabled);
                ResetGPUI();

                void toggle(bool wasok)
                {
                    ToggleGUI(UIState.Active);
                    ToggleTab(Tabs[0], TabsButtons[0].GetComponent<Button>());
                    Instance.SettingsMenu.ReleaseQueue();
                }

                Instance.HandlePlayerState(PlayerState.Menu);

                if (!WasInMenu)
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("welcome_title", [DefaultName]), LocalizationManager.LocalizedString("welcome_desc", [DefaultName]), ModalType.MT_OK, OKButtonType.Default, new Action<bool>(toggle));
                else
                    toggle(true);

                WasInMenu = true;
            }
            catch (Exception e)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("failed_title", [DefaultName]), FLZ_Extensions.FormatException(e), ModalType.MT_OK, OKButtonType.Default);
            }
        }

        List<Material> Materials;
        List<TMP_FontAsset> FontAssets;
        void SetupText()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<TMPReferenceAttribute>() != null).ToList();

            Materials = Resources.FindObjectsOfTypeAll<Material>().ToList();
            FontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().ToList();

            Materials.ForEach(x => GameObject.DontDestroyOnLoad(x));
            FontAssets.ForEach(x => GameObject.DontDestroyOnLoad(x));

            foreach (var tmp in GUIObject.transform.GetComponentsInChildren<TextMeshProUGUI>(true))
                SetupFont(tmp, TMPFontFallback, "2.0_Shadow");

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

                SetupFont(tmpText, tmpAttr.FontName, tmpAttr.MaterialName);
            }
        }

        void SetupFont(TextMeshProUGUI tmpText, string fontName, string materialName)
        {
            tmpText.fontMaterial = Materials.Find(x => x.name.Contains(materialName) && !x.name.Contains("Instance"));
            tmpText.font = FontAssets.Find(x => x.name.StartsWith(fontName));

            if (tmpText.fontMaterial == null || tmpText.font == null)
            {
                tmpText.fontMaterial = Materials.Find(x => x.name == TMPFontMaterialFallback);
                tmpText.font = FontAssets.Find(x => x.name == TMPFontFallback);
            }

            tmpText.fontMaterial.hideFlags = HideFlags.HideAndDontSave;
            tmpText.font.hideFlags = HideFlags.HideAndDontSave;

            tmpText.enableWordWrapping = true;
            tmpText.UpdateMaterial();
            tmpText.UpdateFontAsset();
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

            var aboutSb = new StringBuilder();
            aboutSb.AppendLine($"{DefaultName} by @floyzi102 on Twitter");
            aboutSb.AppendLine($"Compiled on: {Core.BuildInfo.BuildDate} (UTC)");
            aboutSb.AppendLine($"Commit: #{Core.BuildInfo.GetCommit()}");

            var socialBtns = new Dictionary<string, string>()
            {
                { "Source Code On GitHub", GitHubURL },
                { "Commit Details", $"{GitHubURL}/commit/{Core.BuildInfo.Commit}" },
                { "Twitter", TwitterURL },
                { "Discord", DiscordURL },
            };

            foreach (var line in aboutSb.ToString().Split(Environment.NewLine.ToCharArray()))
            {
                if (string.IsNullOrEmpty(line)) continue;

                var res = UnityEngine.Object.Instantiate(CreditTextPrefab, CreditsAboutContent);
                res.SetText(line);
                res.gameObject.SetActive(true);
            }

            foreach (var btn in socialBtns)
            {
                var res = UnityEngine.Object.Instantiate(CreditButtonPrefab, CreditsSocialContent);
                res.GetComponentInChildren<TextMeshProUGUI>().text = btn.Key;
                res.onClick.AddListener(new Action(() =>
                {
                    Application.OpenURL(btn.Value);
                }));
                res.gameObject.SetActive(true);
            }

        }

        void SetupGUI()
        {
            if (!CanStartUISetup)
            {
                FLZ_Extensions.DoModal("ERROR", "Can't start UI setup as missing UI references been found, check logs to find what references are missing and resolve them", ModalType.MT_OK, OKButtonType.Default);
                return;
            }

            if (SuceedGUISetup)
                return;

            try
            {
                ClearLogsBtn.onClick.AddListener(new Action(() =>
                {
                    CleanupScreen(LogContent, true);
                    CleanupScreen(LogContent, true);
                    GUI_LogEntry.UpdateLogStats();
                }));
                applyChanges.onClick.AddListener(new Action(Instance.SettingsMenu.DoUIConfigSave));

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
                Header.text = $"{DefaultName} V{Core.BuildInfo.Version}";
                Slogan.text = $"{Description}";

                ConfigureTabs();
                CreateConfigMenu(configMenu);
                InitRoundsDropdown();
                CreateCreditsScreen();
                SetupLogsScreen();
                GUIObject.gameObject.SetActive(true);
                SuceedGUISetup = true;
            }
            catch (Exception e)
            {
                var exstr = FLZ_Extensions.FormatException(e);
                MelonLogger.Msg($"GUI Setup failed. {exstr}");
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_setup_exception", [exstr]));
            }
        }

        void TryToLoadGUI()
        {
            if (HasGUIKilled || GUIObject != null)
                return;

            var assetName = "NOT_FGToolsGUI";

            if (GUI_Bundle == null)
            {
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_bundle", [BundleName, BundlePath]));
                return;
            }

            theGUI = GUI_Bundle.LoadAssetAsync<GameObject>(assetName);

            if (theGUI.asset == null)
            {
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_asset", [assetName]));
                return;
            }

            GUIObject = UnityEngine.Object.Instantiate(theGUI.asset).Cast<GameObject>();
            if (GUIObject != null)
            {
                UnityEngine.Object.DontDestroyOnLoad(GUIObject);
                GUIObject.GetComponent<Canvas>().sortingOrder = 9999;
                ConfigureObjects();
                GUIObject.gameObject.SetActive(false);
                RepairStyle.gameObject.SetActive(false);
            }
            else
                TryTriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_object", [$"{assetName}(Clone)"]));
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



        public void CleanupScreen(Transform screen, bool includeOnlyActive)
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


        public void UpdateGPActions(Dictionary<Action, string> actions = null)
        {
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    GameObject btnPrefab = UnityEngine.Object.Instantiate(GPBtn.gameObject, GPActionsView);
                    btnPrefab.SetActive(true);
                    btnPrefab.name = action.Value;

                    btnPrefab.GetComponentInChildren<Button>().onClick.AddListener(action.Key);
                    btnPrefab.GetComponentInChildren<Text>().text = action.Value;

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

        void TryTriggerFailedToLoadUIModal(string addotionalMsg)
        {
            LastFailModal = new Action(() => { FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("gui_init_fail_generic_title"), LocalizationManager.LocalizedString("gui_init_fail_generic_desc", [addotionalMsg]), ModalType.MT_OK, OKButtonType.Disruptive); });
        }

        void ConfigureObjects()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<GUIReferenceAttribute>() != null);
            if (fields == null)
                return;

            foreach (var t in GUIObject.transform.GetComponentsInChildren<Transform>(true))
            {
                foreach (var field in fields)
                {
                    var refrerence = field.GetCustomAttribute<GUIReferenceAttribute>();
                    if (refrerence != null && refrerence.Name == t.name)
                    {
                        object component = null;

                        if (field.FieldType == typeof(Button))
                            component = t.GetComponent<Button>();
                        else if (field.FieldType == typeof(Text))
                            component = t.GetComponent<Text>();
                        else if (field.FieldType == typeof(InputField))
                            component = t.GetComponent<InputField>();
                        else if (field.FieldType == typeof(Dropdown))
                            component = t.GetComponent<Dropdown>();
                        else if (field.FieldType == typeof(GameObject))
                            component = t.gameObject;
                        else if (field.FieldType == typeof(Transform))
                            component = t;
                        else if (field.FieldType == typeof(TextMeshProUGUI))
                            component = t.GetComponent<TextMeshProUGUI>();
                        else if (field.FieldType == typeof(TMP_InputField))
                            component = t.GetComponent<TMP_InputField>();
                        else if (field.FieldType == typeof(TMP_Dropdown))
                            component = t.GetComponent<TMP_Dropdown>();
                        else if (field.FieldType == typeof(ScrollRect))
                            component = t.GetComponent<ScrollRect>();
                        else if (field.FieldType == typeof(RectTransform))
                            component = t.GetComponent<RectTransform>();
                        else
                            MelonLogger.Error($"TYPE {field.FieldType.Name} ON {refrerence.Name} IS NOT IMPLEMENTED!!!");

                        if (component != null)
                            field.SetValue(this, component);
                    }
                }

                if (t.gameObject.name.StartsWith("NavTab_") && !TabsButtons.Exists(target => target.name == t.gameObject.name))
                    TabsButtons.Add(t.gameObject);

                if (t.gameObject.name.StartsWith("TAB_") && !Tabs.Exists(target => target.name == t.gameObject.name))
                    Tabs.Add(t.gameObject);
            }

            foreach (var f in fields)
            {
                if (f.GetValue(this) == null)
                {
                    MelonLogger.Error($"VALUE OF FIELD \"{f.Name}\" (type of \"{f.FieldType.Name}\") IS NULL AFTER SET!!!");
                    CanStartUISetup = false;
                }
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

        public void TriggerPendingChanges(bool on)
        {
            PendingChanges.SetActive(on);
        }

        Transform GetTransformFromGUI(string name)
        {
            var a = GUIObject.transform.GetComponentsInChildren<Transform>(true).ToList();
            return a.Find(x => x.name == name);
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
            HashSet<string> categories = [];

            GUI_TogglePrefab.SetActive(false);
            GUI_TextFieldPrefab.SetActive(false);
            GUI_SliderPrefab.SetActive(false);
            GUI_HeaderPrefab.SetActive(false);
            GUI_ButtonPrefab.SetActive(false);

            foreach (var entry in Instance.SettingsMenu.Entries.OrderBy(entry => entry.Category).ToList())
            {
                try
                {
                    MelonLogger.Msg($"[{GetType()}] CreateConfigMenu() - Creating entry \"{entry.ID}\" with type \"{entry.EntryType}\"");

                    if (!string.IsNullOrEmpty(entry.Category) && !categories.Contains(entry.Category))
                    {
                        GameObject haderInst = UnityEngine.Object.Instantiate(GUI_HeaderPrefab, cfgTrans);
                        haderInst.name = $"Header_{entry.Category}";

                        var headerText = haderInst.GetComponentInChildren<TextMeshProUGUI>();

                        SetupFont(headerText, TMPFontTitanOne, "PinkOutline");

                        if (headerText != null)
                        {
                            headerText.text = string.Format(headerText.text, LocalizationManager.LocalizedString(entry.Category));
                        }
                        categories.Add(entry.Category);
                        haderInst.SetActive(true);
                    }

                    var localizedDesc = LocalizationManager.LocalizedString(entry.Description);

                    switch (entry.EntryType)
                    {
                        case MenuEntry.Type.Toggle:
                            GameObject toggleInst = UnityEngine.Object.Instantiate(GUI_TogglePrefab, cfgTrans);
                            toggleInst.SetActive(true);
                            toggleInst.name = entry.ID;

                            var toggle = toggleInst.transform.Find("Toggle").GetComponent<Toggle>();
                            toggle.gameObject.AddComponent<UnityDragFix>()._ScrollRect = CheatsScrollView;
                            var toggleTracker = toggle.gameObject.AddComponent<TrackedEntry>();
                            toggleTracker.Create(entry, toggle);
                            toggleTracker.OnEntryUpdated += new Action<object>(newVal => { toggle.isOn = bool.Parse(newVal.ToString()); });
                            var toggleTitle = toggleInst.transform.Find("Toggle").GetComponentInChildren<TextMeshProUGUI>();
                            var toggleDesc = toggleInst.transform.Find("ToggleDesc").GetComponent<TextMeshProUGUI>();

                            if (!string.IsNullOrEmpty(localizedDesc))
                                toggleDesc.text = $"*{localizedDesc}";
                            else
                                toggleDesc.gameObject.SetActive(false);

                            toggleTitle?.text = LocalizationManager.LocalizedString(entry.DisplayName);

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
                            var fieldTracker = inputField.gameObject.AddComponent<TrackedEntry>();
                            fieldTracker.Create(entry, inputField);
                            fieldTracker.OnEntryUpdated += new Action<object>(newVal =>
                            {
                                inputField.text = newVal.ToString();
                            });

                            var fieldTitle = fieldInst.transform.Find("FieldTitle").GetComponent<TextMeshProUGUI>();
                            var fieldDesc = fieldInst.transform.Find("FieldDesc").GetComponent<TextMeshProUGUI>();

                            if (!string.IsNullOrEmpty(localizedDesc))
                                fieldDesc.text = $"*{localizedDesc}";
                            else
                                fieldDesc.gameObject.SetActive(false);

                            if (fieldTitle != null)
                                fieldTitle.text = LocalizationManager.LocalizedString(entry.DisplayName);

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

                            if (!string.IsNullOrEmpty(localizedDesc))
                                sliderDesc.text = $"*{localizedDesc}";
                            else
                                sliderDesc.gameObject.SetActive(false);

                            sliderTitle?.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.LocalizedString(entry.DisplayName);

                            var sliderValue = slider.transform.Find("SliderValue").GetComponent<TextMeshProUGUI>();

                            var sliderTracker = slider.gameObject.AddComponent<TrackedEntry>();
                            sliderTracker.Create(entry, slider);
                            
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

                            if (!string.IsNullOrEmpty(localizedDesc))
                                buttonDesc.text = $"*{localizedDesc}";
                            else
                                buttonDesc.gameObject.SetActive(false);

                            var buttonTracker = button.gameObject.AddComponent<TrackedEntry>();
                            buttonTracker.Create(entry, button);

                            button.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.LocalizedString(entry.DisplayName);

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
