using Il2CppTMPro;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Styles.Logic;
using NOTFGT.FLZ_Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


namespace NOTFGT.FLZ_Common.GUI.Styles
{
    internal class GameplayStyle : UIStyle
    {
        [GUIReference("GPActive")] readonly GameObject GameplayActive;
        [GUIReference("GPHidden")] readonly GameObject GameplayHidden;
        [GUIReference("OpenGPPanel")] readonly Button OpenGP;
        [GUIReference("HideGPPanel")] readonly Button HideGP;
        [GUIReference("GPActionsView")] readonly Transform GPActionsView;
        [GUIReference("GPButtonPrefab")] readonly Button GPBtn;

        List<Transform> GameplayActions;
        internal bool HasItemsInside;

        internal GameplayStyle() : base(StyleType.Gameplay) 
        {
            Initialize();
        }

        internal override void CreateStyle()
        {
            GameplayActions = [];

            HideGP.onClick.AddListener(new Action(() => { UpdateGPUI(false); }));
            OpenGP.onClick.AddListener(new Action(() => { UpdateGPUI(true); }));
        }

        public void UpdateGPUI(bool active)
        {
            GameplayHidden.SetActive(!active);
            GameplayActive.SetActive(active);
        }

        void ResetGPUI()
        {
            GameplayActive.SetActive(false);
            GameplayHidden.SetActive(true);
            StyleContainer.SetActive(false);
            GameplayActions.Clear();
        }

        internal void UpdateGPActions(Dictionary<string, Action> actions = null)
        {
            HasItemsInside = actions != null && actions.Count > 0;
            StyleContainer.SetActive(HasItemsInside);

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
                UpdateGPUI(false);
            }
        }
    }
}
