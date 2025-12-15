using Il2CppTMPro;
using MelonLoader;
using NOTFGT.FLZ_Common.Config.Entries;
using NOTFGT.FLZ_Common.Config.Entries.Configs;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Screens.Logic;
using NOTFGT.FLZ_Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;
using static MelonLoader.MelonLogger;

namespace NOTFGT.FLZ_Common.GUI.Screens
{
    internal class ToolsScreen : UIScreen
    {
        [GUIReference("ConfigDisplay")] readonly Transform configMenu;

        [AudioReference(Constants.Click)]
        [GUIReference("WriteSave")] readonly Button ApplyChanges;

        [AudioReference(Constants.Click)]
        [GUIReference("ResetConfig")] readonly Button DeleteConfig;

        [GUIReference("PendingChangesAlert")] readonly GameObject PendingChanges;
        [GUIReference("ToggleReference")] readonly GameObject GUI_TogglePrefab;
        [GUIReference("FieldReference")] readonly GameObject GUI_TextFieldPrefab;
        [GUIReference("SliderReference")] readonly GameObject GUI_SliderPrefab;
        [GUIReference("MenuHeaderReference")] readonly GameObject GUI_HeaderPrefab;
        [GUIReference("MenuHeaderDescReference")] readonly GameObject GUI_HeaderDescPrefab;
        [GUIReference("ButtonReference")] readonly GameObject GUI_ButtonPrefab;
        [GUIReference("ErroredEntryReference")] readonly GameObject GUI_ErroredEntry;

        [GUIReference("CheatsScrollView")] readonly ScrollRect CheatsScrollView;
        [GUIReference("GroupLayout")] readonly Transform ToolsCategory;
        internal ToolsScreen() : base(ScreenType.Cheats)
        {
            Initialize();
        }

        protected override void StateChange(bool isActive, bool wasActive)
        {
        }

        internal override void CreateScreen()
        {
            ApplyChanges.onClick.AddListener(new Action(FLZ_ToolsManager.Instance.Config.DoUIConfigSave));

            DeleteConfig.onClick.AddListener(new Action(() => {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("reset_config_alert_title"), LocalizationManager.LocalizedString("reset_config_alert_desc"), ModalType.MT_OK_CANCEL, OKButtonType.Disruptive, new Action<bool>((val) => {
                    if (val)
                        FLZ_ToolsManager.Instance.Config.EntriesManager.ResetSettings();
                }));
            }));

            CreateConfigMenu(configMenu);
        }

        //TODO: rewrite this hell
        void CreateConfigMenu(Transform cfgTrans)
        {
            GUI_TogglePrefab.SetActive(false);
            GUI_TextFieldPrefab.SetActive(false);
            GUI_SliderPrefab.SetActive(false);
            GUI_HeaderPrefab.SetActive(false);
            GUI_ButtonPrefab.SetActive(false);
            GUI_ErroredEntry.SetActive(false);

            MenuCategory currentCateg = null;
            string currentCategStr = "";

            foreach (var entry in FLZ_ToolsManager.Instance.Config.EntriesManager.Entries.OrderByDescending(entry => entry.Category.Priority).ToList())
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

                    var fillerInst = UnityEngine.Object.Instantiate(GUI_ErroredEntry, cfgTrans);
                    fillerInst.SetActive(true);
                    var fillerLoc = fillerInst.gameObject.GetComponentInChildren<TextMeshProUGUI>().gameObject.AddComponent<LocalizedStr>();
                    fillerLoc.Setup("errored_entry", formatting: [entry.ID]);

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
                            toggle.onValueChanged.AddListener(new Action<bool>(val => { entry.SetValue(val); }));
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
                                    entry.SetValue(val);
                                }));
                            }
                            else if (fieldConf.ValueType == typeof(int))
                            {
                                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                                inputField.onValueChanged.AddListener(new Action<string>(val =>
                                {
                                    if (int.TryParse(val, out int intVal))
                                    {
                                        entry.SetValue(intVal);
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
                                        entry.SetValue(floatVal);
                                    }
                                }));
                            }

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
                                entry.SetValue(val);

                                if (sliderConf.ValueType == typeof(float))
                                    sliderValue.text = $"{val:F1} / {slider.maxValue:F1}";
                                else
                                    sliderValue.text = $"{Convert.ToInt32(val)} / {Convert.ToInt32(slider.maxValue)}";
                            }));

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
                                entry.SetValue(null);
                            }));
                            break;
                        default:
                            MelonLogger.Warning($"Fallback on: {entry.ID}");
                            break;
                    }

                    fillerLoc.Cleanup();
                    GameObject.Destroy(fillerInst);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[{GetType()}] CreateConfigMenu() - Creating entry \"{entry.ID}\" with type \"{entry.EntryType}\" failed!\n{ex}");
                }
            }
        }
    }
}
