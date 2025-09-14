using Events;
using FG.Common.CMS;
using FGClient;
using MelonLoader;
using NOTFGT.Localization;
using NOTFGT.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;
using Application = UnityEngine.Application;
using BuildInfo = NOTFGT.Logic.BuildInfo;
using Text = UnityEngine.UI.Text;
using static NOTFGT.GUI.ToolsMenu;

namespace NOTFGT.GUI
{

    [AttributeUsage(AttributeTargets.Field)]
    public class GUIReferenceAttribute(string target) : Attribute
    {
        public string Name { get; } = target;
    }

    public class GUI_LogEntry
    {
        public LogType Type { get; set; }
        public string Msg { get; set; }
        public string Stacktrace { get; set; }
    }


    public class GUI_Util
    {
        const string BundleName = "not_fgtoolsGUI";

        const string GitHub = "github.com/floyzi/FGToolsMobile/";
        const string Discord = "discord.gg/PEysxvSE3x";
        const string Twitter = "twitter.com/@floyzi102";

        enum GUIHideState
        {
            Full,
            Hidden,
            Active
        }

        Color tabActiveCol = new(0.7028302f, 0.9941195f, 1f, 1f);
        Color InfoColor = new(0.7019608f, 1f, 0.9569892f, 1f);
        Color WarningColor = new(1f, 0.8809203f, 0.7019608f, 1f);
        Color ErrorColor = new(1f, 0.7028302f, 0.7028302f, 1f);

        AssetBundle GUI_Bundle;
        AssetBundleRequest theGUI;

        GameObject GUIObject;

        [GUIReference("StyleDefault")] readonly GameObject DefaultStyle;
        [GUIReference("StyleHidden")] readonly GameObject HiddenStyle;
        [GUIReference("StyleGameplay")] readonly GameObject GameplayStyle;
        [GUIReference("StyleRepair")] readonly GameObject RepairStyle;

        [GUIReference("DeleteConfig")] readonly Button DeleteConfig;
        [GUIReference("IgnoreThis")] readonly Button IgnoreThis;
        [GUIReference("ErrorDisplay")] readonly Text ErrorDisplay;

        [GUIReference("UnHideButton")] readonly Button UnHideButton;
        [GUIReference("HideButton")] readonly Button HideButton;
        [GUIReference("KillButton")] readonly Button KillButton;

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

        [GUIReference("RoundInputField")] readonly InputField RoundIdInputField;
        [GUIReference("RoundLoadBtn")] readonly Button RoundLoadButton;
        [GUIReference("RandomRoundBtn")] readonly Button RoundLoadRandomButton;
        [GUIReference("RoundID_Entry")] readonly GameObject RoundIDEntry;
        [GUIReference("RoundID_EntryV2")] readonly Button RoundIDEntryV2;
        [GUIReference("RoundIDSView")] readonly Transform RoundIdsView;
        [GUIReference("RoundGenListBtn")] readonly Button RoundGenerateListButton;
        [GUIReference("RoundListCleanup")] readonly Button CleanupList;
        [GUIReference("RoundsDropDown")] readonly Dropdown RoundsDropdown;
        [GUIReference("RoundsIDSDropDown")] readonly Dropdown IdsDropdown;
        [GUIReference("ClickToCopyNote")] readonly GameObject ClickToCopy;

        [GUIReference("LogMessage")] readonly Button LogPrefab;
        [GUIReference("LogInfo")] readonly Text LogInfo;
        [GUIReference("LogDisplay")] readonly Transform LogContent;
        [GUIReference("ClearLogsBtn")] readonly Button ClearLogsBtn;
        [GUIReference("LogStats")] readonly Text LogStats;
        [GUIReference("LogDisabled")] readonly GameObject LogDisabledScreen;

        [GUIReference("GPActive")] readonly GameObject GameplayActive;
        [GUIReference("GPHidden")] readonly GameObject GameplayHidden;
        [GUIReference("OpenGPPanel")] readonly Button OpenGP;
        [GUIReference("HideGPPanel")] readonly Button HideGP;

        [GUIReference("GPActions")] readonly GameObject GPActionsObject;
        [GUIReference("GPActionsView")] readonly Transform GPActionsView;
        [GUIReference("GPButtonPrefab")] readonly Button GPBtn;

        [GUIReference("BTN_Discord")] readonly Button DiscordBtn;
        [GUIReference("BTN_Twitter")] readonly Button TwitterBtn;
        [GUIReference("BTN_GitHub")] readonly Button GitHubBtn;

        [GUIReference("HeaderTitle")] readonly Text Header;
        [GUIReference("HeaderSlogan")] readonly Text Slogan;

        readonly List<GameObject> Tabs = [];
        readonly List<GameObject> TabsButtons = [];

        readonly List<GUI_LogEntry> LogEntries = [];

        readonly List<Transform> GameplayActions = [];

        readonly List<GameObject> EntryInstances = [];

        EventSystem.Handle _menuDisplayed;

        string BundlePath;
        string ReadyRound;

        bool HasGUIKilled = false;
        bool SuceedGUISetup = false;
        bool WasInMenu = false;
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
            DeleteConfig.onClick.AddListener(new Action(() => { NOTFGTools.Instance.SettingsMenu.DeleteConfig(); }));
            IgnoreThis.onClick.AddListener(new Action(() => { 
                RepairStyle.gameObject.SetActive(false);
                GameObject.Destroy(GUIObject);
                HasGUIKilled = true;
            }));
        }

        public void Register()
        {
            BundlePath = Path.Combine(NOTFGTools.AssetsDir, BundleName);
            MelonLogger.Msg($"EXPECTED BUNDLE PATH IS: {BundlePath}");
            GUI_Bundle = AssetBundle.LoadFromFile(BundlePath);
            _menuDisplayed = Broadcaster.Instance.Register<OnMainMenuDisplayed>(new Action<OnMainMenuDisplayed>(MenuEvent));

            MelonLogger.Msg($"[{base.GetType()}] Successful GUI_Util register. Is bundle loaded: {GUI_Bundle != null}");
            TryToLoadGUI();
        }

        public void ProcessNewLog(GUI_LogEntry logEntry)
        {
            LogEntries.Add(logEntry);
            if (LogPrefab != null)
            {
                var newLog = GameObject.Instantiate(LogPrefab, LogContent);
                newLog.name = "ActiveLog";
                newLog.gameObject.SetActive(true);
                newLog.transform.SetAsFirstSibling();
                switch (logEntry.Type)
                {
                    case LogType.Log:
                        newLog.image.color = InfoColor;
                        break;
                    case LogType.Warning:
                        newLog.image.color = WarningColor;
                        break;
                    case LogType.Error:
                        newLog.image.color = ErrorColor;
                        break;
                }
                UpdateLogStatsText();
                string result = logEntry.Msg.Length > 55 ? result = logEntry.Msg.Substring(0, 55) + "..." : logEntry.Msg;
                newLog.gameObject.GetComponentInChildren<Text>().text = $" {result}";
                newLog.onClick.AddListener(new Action(() => { ShowAdvancedLog(logEntry); }));
            }
        }

        void ShowAdvancedLog(GUI_LogEntry logEntry)
        {
            LogInfo.text = LocalizationManager.LocalizedString("advanced_log", [logEntry.Type, logEntry.Msg, logEntry.Stacktrace]);
        }

        void ToggleGUI(GUIHideState toggle)
        {
            switch (toggle)
            {
                case GUIHideState.Full:
                    DefaultStyle.gameObject.SetActive(false);
                    HiddenStyle.gameObject.SetActive(false);
                    break;
                case GUIHideState.Hidden:
                    DefaultStyle.gameObject.SetActive(false);
                    HiddenStyle.gameObject.SetActive(true);
                    break;
                case GUIHideState.Active:
                    DefaultStyle.gameObject.SetActive(true);
                    HiddenStyle.gameObject.SetActive(false);
                    break;
            }
        }

        void MenuEvent(OnMainMenuDisplayed evt)
        {
            LastFailModal?.Invoke();

            if (OnRepairScreen || HasGUIKilled)
                return;

            try
            {
                SetupGUI();
                ToggleGUI(GUIHideState.Full);
                ResetGPUI();
                void toggle(bool wasok)
                {
                    ToggleGUI(GUIHideState.Active);
                    ToggleTab(Tabs[1], TabsButtons[0].GetComponent<Button>());
                    SaveSettings();
                }
                NOTFGTools.Instance.HandlePlayerState(NOTFGTools.PlayerState.Menu);

                if (!WasInMenu)
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("welcome_title", [BuildInfo.Name]), LocalizationManager.LocalizedString("welcome_desc", new object[] { BuildInfo.Name }), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default, new Action<bool>(toggle));
                else
                    toggle(true);

                WasInMenu = true;
            }
            catch (Exception e)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("failed_title", [BuildInfo.Name]), FLZ_Extensions.FormatException(e), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default);
            }
        }

        void UpdateLogStatsText() => LogStats.text = LocalizationManager.LocalizedString("errors_display", [LogEntries.FindAll(x => x.Type == LogType.Error).Count, LogEntries.FindAll(x => x.Type == LogType.Warning).Count, LogEntries.FindAll(x => x.Type == LogType.Log).Count, LogEntries.Count]);

        void SaveSettings()
        {
            try
            {
                var settings = NOTFGTools.Instance.SettingsMenu;
                settings.RollChanges();
                LogDisabledScreen.SetActive(!settings.GetValue<bool>(TrackGameDebug));
                NOTFGTools.Instance.ApplyChanges();
            }
            catch (Exception ex)
            {
                FLZ_Extensions.CreateNotification("Whoops", $"Unable to save the settings, try resetting\n{ex.Message}", "", 5);
                AudioManager.PlayOneShot("UI_Matchmaking_Failed");
            }
        }

        void SetupGUI()
        {
            if (SuceedGUISetup)
                return;

            try
            {
                ClearLogsBtn.onClick.AddListener(new Action(() =>
                {
                    CleanupScreen(LogContent, true);
                    LogEntries.Clear();
                    UpdateLogStatsText();
                }));
                applyChanges.onClick.AddListener(new Action(SaveSettings));
                UnHideButton.onClick.AddListener(new Action(() => { ToggleGUI(GUIHideState.Active); }));
                HideButton.onClick.AddListener(new Action(() => { ToggleGUI(GUIHideState.Hidden); }));
                KillButton.onClick.AddListener(new Action(() =>
                {
                    HasGUIKilled = true;
                    GameObject.Destroy(GUIObject);
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("gui_destroyed_title"), LocalizationManager.LocalizedString("gui_destroyed_desc"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default);
                }));
                RoundIdInputField.onValueChanged.AddListener(new Action<string>((str) => { ReadyRound = str; }));
                RoundLoadButton.onClick.AddListener(new Action(() =>
                {
                    NOTFGTools.Instance.LoadRound(ReadyRound);
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
                    NOTFGTools.Instance.RoundLoader.GenerateCMSList(RoundIdsView, RoundIDEntryV2);
                    ClickToCopy.SetActive(true);
                }));
                RoundLoadRandomButton.onClick.AddListener(new Action(NOTFGTools.Instance.RoundLoader.LoadRandomCms));
                DiscordBtn.onClick.AddListener(new Action(() => { Application.OpenURL($"https://{Discord}"); }));
                TwitterBtn.onClick.AddListener(new Action(() => { Application.OpenURL($"https://{Twitter}"); }));
                GitHubBtn.onClick.AddListener(new Action(() => { Application.OpenURL($"https://{GitHub}"); }));
                HideGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, false); }));
                OpenGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, true); }));
                deleteConfig.onClick.AddListener(new Action(() => {
                    FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("reset_config_alert_title"), LocalizationManager.LocalizedString("reset_config_alert_desc"), FGClient.UI.UIModalMessage.ModalType.MT_OK_CANCEL, FGClient.UI.UIModalMessage.OKButtonType.Disruptive, new Action<bool>((val) => {
                        if (val)
                            NOTFGTools.Instance.SettingsMenu.ResetSettings();
                    } ));
                
                }));
                RoundIDEntry.SetActive(false);
                Header.text = $"{BuildInfo.Name} V{BuildInfo.Version}";
                Slogan.text = $"{BuildInfo.Description}";
                ConfigureTabs();
                CreateConfigMenu(configMenu, NOTFGTools.Instance.SettingsMenu.GetAllEntries());
                InitRoundsDropdown();
                UpdateLogStatsText();
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

            GameObject.Instantiate(theGUI.asset);
            GUIObject = GameObject.Find($"{assetName}(Clone)");
            if (GUIObject != null)
            {
                GameObject.DontDestroyOnLoad(GUIObject);
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
                    GameObject.Destroy(child.gameObject);
                }
            }
        }


        public void UpdateGPActions(Dictionary<Action, string> actions = null)
        {
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    GameObject btnPrefab = GameObject.Instantiate(GPBtn.gameObject, GPActionsView);
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
                    GameObject.Destroy(trans.gameObject);
                }
                GameplayActions.Clear();
                UpdateGPUI(false, false);
            }
        }

        void TryTriggerFailedToLoadUIModal(string addotionalMsg)
        {
            LastFailModal = (new Action(() => { FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("gui_init_fail_generic_title"), LocalizationManager.LocalizedString("gui_init_fail_generic_desc", [addotionalMsg]), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive); }));
        }

        void ConfigureObjects()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<GUIReferenceAttribute>() != null);
            if (fields == null)
                return;

            foreach (Transform t in GUIObject.transform.GetComponentsInChildren<Transform>(true))
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

                        if (component != null)
                        {
                            try{field.SetValue(this, component);} catch {}
                        }
                    }
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

        public void UpdateActiveEntries(List<MenuEntry> changed = null)
        {
            foreach (GameObject obj in EntryInstances)
            {
                var entry = NOTFGTools.Instance.SettingsMenu.TryGetEntry(obj.name);
                if (entry != null)
                {
                    switch (entry.ValueType)
                    {
                        case MenuEntry.Type.Bool:
                            var toggle = obj.GetComponentInChildren<Toggle>();
                            var tVal = Convert.ToBoolean(entry.Value);
                            toggle.isOn = tVal;
                            toggle.onValueChanged.Invoke(tVal);
                            break;
                        case MenuEntry.Type.Int:
                        case MenuEntry.Type.Float:
                        case MenuEntry.Type.String:
                            var field = obj.GetComponentInChildren<InputField>();
                            field.text = entry.Value.ToString();
                            field.onValueChanged.Invoke(field.text);
                            break;
                        case MenuEntry.Type.Slider:
                            var slider = obj.GetComponentInChildren<Slider>();
                            entry.Config.TryGetValue(IsFloat, out var isFloat);
                            slider.value = float.Parse(entry.Value.ToString());
                            slider.onValueChanged.Invoke(slider.value);
                            break;
                        case MenuEntry.Type.Button:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        void CreateConfigMenu(Transform ConfigTransform, List<MenuEntry> configEntries)
        {
            HashSet<string> categories = [];

            foreach (var entry in configEntries.OrderBy(entry => entry.Category).ToList())
            {
                MelonLogger.Msg($"[{base.GetType()}] CreateConfigMenu() - Creating entry \"{entry.InternalName}\" with type \"{entry.ValueType}\"");
                if (!string.IsNullOrEmpty(entry.Category) && !categories.Contains(entry.Category))
                {
                    GameObject haderInst = GameObject.Instantiate(GUI_HeaderPrefab, ConfigTransform);
                    haderInst.SetActive(true);
                    haderInst.name = $"Header_{entry.Category}";

                    var headerText = haderInst.GetComponentInChildren<Text>();
                    if (headerText != null)
                        headerText.text = string.Format(headerText.text, LocalizationManager.LocalizedString(entry.Category));

                    categories.Add(entry.Category);
                }

                var localizedDesc = LocalizationManager.LocalizedString(entry.Description);

                switch (entry.ValueType)
                {
                    case MenuEntry.Type.Bool:
                        GameObject toggleInst = GameObject.Instantiate(GUI_TogglePrefab, ConfigTransform);
                        toggleInst.SetActive(true);
                        toggleInst.name = entry.InternalName;

                        var toggle = toggleInst.transform.Find("Toggle").GetComponent<Toggle>();
                        var toggleTitle = toggleInst.transform.Find("Toggle").GetComponentInChildren<Text>();
                        var toggleDesc = toggleInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(localizedDesc))
                            toggleDesc.text = $"*{localizedDesc}";
                        else
                            toggleDesc.gameObject.SetActive(false);

                        if (toggleTitle != null)
                            toggleTitle.text = LocalizationManager.LocalizedString(entry.DisplayName);

                        toggle.isOn = Convert.ToBoolean(entry.Value);

                        toggle.onValueChanged.AddListener(new Action<bool>(val =>
                        {
                            NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, val);
                        }));

                        EntryInstances.Add(toggleInst);
                        break;

                    case MenuEntry.Type.Int:
                    case MenuEntry.Type.Float:
                    case MenuEntry.Type.String:
                        GameObject fieldInst = GameObject.Instantiate(GUI_TextFieldPrefab, ConfigTransform);
                        fieldInst.SetActive(true);
                        fieldInst.name = entry.InternalName;

                        var inputField = fieldInst.transform.Find("InputField").GetComponent<InputField>();
                        var fieldTitle = fieldInst.transform.Find("FieldTitle").GetComponent<Text>();
                        var fieldDesc = fieldInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(localizedDesc))
                            fieldDesc.text = $"*{localizedDesc}";
                        else
                            fieldDesc.gameObject.SetActive(false);

                        if (fieldTitle != null)
                            fieldTitle.text = LocalizationManager.LocalizedString(entry.DisplayName);

                        inputField.text = entry.Value.ToString();
                        if (entry.Config != null && entry.Config != null && entry.Config.Count == 1)
                        {
                            entry.Config.TryGetValue(CharLimit, out var limit);
                            inputField.characterLimit = Convert.ToInt32(limit.ToString());
                        }

                        if (entry.ValueType == MenuEntry.Type.Int)
                        {
                            inputField.contentType = InputField.ContentType.IntegerNumber;
                            inputField.onValueChanged.AddListener(new Action<string>(val =>
                            {
                                if (int.TryParse(val, out int intVal))
                                {
                                    NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, intVal);
                                }
                            }));
                        }
                        else if (entry.ValueType == MenuEntry.Type.Float)
                        {
                            inputField.contentType = InputField.ContentType.DecimalNumber;
                            inputField.onValueChanged.AddListener(new Action<string>(val =>
                            {
                                if (float.TryParse(val, out float floatVal))
                                {
                                    NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, floatVal);
                                }
                            }));
                        }
                        else if (entry.ValueType == MenuEntry.Type.String)
                        {
                            inputField.contentType = InputField.ContentType.Standard;
                            inputField.onValueChanged.AddListener(new Action<string>(val => 
                            {
                                NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, val);
                            }));
                        }

                        EntryInstances.Add(fieldInst);
                        break;
                    case MenuEntry.Type.Slider:
                        GameObject sliderInst = GameObject.Instantiate(GUI_SliderPrefab, ConfigTransform);
                        sliderInst.SetActive(true);
                        sliderInst.name = entry.InternalName;

                        var slider = sliderInst.transform.Find("Slider").GetComponent<Slider>();
                        var sliderTitle = sliderInst.transform.Find("SliderTitle").GetComponent<Text>();
                        var sliderDesc = sliderInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(localizedDesc))
                            sliderDesc.text = $"*{localizedDesc}";
                        else
                            sliderDesc.gameObject.SetActive(false);

                        if (sliderTitle != null)
                            sliderTitle.GetComponentInChildren<Text>().text = LocalizationManager.LocalizedString(entry.DisplayName);

                        var sliderValue = slider.transform.Find("SliderValue").GetComponent<Text>();
                        if (entry.Config != null && entry.Config.Count == 3)
                        {
                            entry.Config.TryGetValue(IsFloat, out var isFloat);
                            entry.Config.TryGetValue(SliderMin, out var min);
                            entry.Config.TryGetValue(SliderMax, out var max);


                            slider.minValue = float.Parse(min.ToString());
                            slider.maxValue = float.Parse(max.ToString());

                            slider.value = float.Parse(entry.Value.ToString());
                            sliderValue.text = $"{entry.Value:F1} / {slider.maxValue:F1}";

                            slider.onValueChanged.AddListener(new Action<float>(val =>
                            {
                                NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, val);

                                if ((bool)isFloat)
                                    sliderValue.text = $"{val:F1} / {slider.maxValue:F1}";
                                else
                                    sliderValue.text = $"{Convert.ToInt32(val)} / {Convert.ToInt32(slider.maxValue)}";
                            }));

                        }
                        else
                            Debug.LogError($"Can't setup slider {entry.InternalName}. Data is null");

                        EntryInstances.Add(sliderInst);
                        break;
                    case MenuEntry.Type.Button:
                        GameObject buttonInst = GameObject.Instantiate(GUI_ButtonPrefab, ConfigTransform);
                        buttonInst.SetActive(true);
                        buttonInst.name = entry.InternalName;

                        var button = buttonInst.transform.Find("Button").GetComponent<Button>();
                        var buttonDesc = buttonInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(localizedDesc))
                            buttonDesc.text = $"*{localizedDesc}";
                        else
                            buttonDesc.gameObject.SetActive(false);

                        button.GetComponentInChildren<Text>().text = LocalizationManager.LocalizedString(entry.DisplayName);

                        button.onClick.AddListener(new Action(() => {
                            MethodInfo theMethod = typeof(NOTFGTools).GetMethod(entry.Value.ToString());
                            theMethod.Invoke(NOTFGTools.Instance, null);
                        }));
                        EntryInstances.Add(buttonInst);
                        break;
                    default:
                        Debug.LogWarning($"Fallback on: {entry.InternalName}");
                        break;
                }
            }
        }

    }
}
