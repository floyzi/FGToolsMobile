using Il2CppDG.Tweening;
using Il2CppTMPro;
using MelonLoader;
using NOTFGT.FLZ_Common.Extensions;
using NOTFGT.FLZ_Common.GUI.Attributes;
using NOTFGT.FLZ_Common.GUI.Screens.Logic;
using NOTFGT.FLZ_Common.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static NOTFGT.FLZ_Common.GUI.Attributes.TMPReferenceAttribute;

namespace NOTFGT.FLZ_Common.GUI.Screens
{
    internal class CreditsScreen : UIScreen
    {
        [GUIReference("CreditsContent")] readonly Transform CreditsContent;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditAboutHeader")] readonly TextMeshProUGUI CreditsAboutHeader;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditSocialsHeader")] readonly TextMeshProUGUI CreditsSocialHeader;

        [TMPReference(FontType.AsapBold, "2.0_Shadow")]
        [GUIReference("CreditText")] readonly TextMeshProUGUI CreditTextPrefab;

        [AudioReference(Constants.Click)]
        [GUIReference("CreditBTN")] readonly Button CreditButtonPrefab;

        [TMPReference(FontType.TitanOne, "PinkOutline")]
        [GUIReference("CreditLocaleHeader")] readonly TextMeshProUGUI CreditsLocaleHeader;

        [AudioReference(Constants.Click)]
        [GUIReference("LocaleDropdown")] readonly TMP_Dropdown LocaleSelectDropdown;

        [GUIReference("FatefulImage")] readonly Button FatefulImage;
        [GUIReference("FatefulImageLoad")] readonly Transform FatefulImageLoading;
        [GUIReference("ImgCreditText")] readonly Transform CreditsImgCredit;

        static int EGG_Counter;
        Sprite CachedCreditsSpr;
        static readonly int EGG_ClicksNeeded = UnityEngine.Random.Range(15, 25);
        internal static bool EnabledSecret => EGG_Counter >= EGG_ClicksNeeded;

        internal CreditsScreen() : base(ScreenType.Credits)
        {
            Initialize();
        }

        internal override void CreateScreen()
        {
            FatefulImage.onClick.AddListener(new Action(() =>
            {
                EasterEgg();
            }));

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
                { "fgmm_propaganda", Constants.FGModMenuURL },
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
                    OpenConfirm(btn.Value);
                }));
                res.gameObject.SetActive(true);
                res.transform.SetSiblingIndex(indx++);
            }

            LocalizationManager.ConfigureDropdown(LocaleSelectDropdown);
        }

        void OpenConfirm(string url)
        {
            FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("credits_url_confirm_title"), LocalizationManager.LocalizedString("credits_url_confirm_desc", [url]), Il2CppFGClient.UI.UIModalMessage.ModalType.MT_OK_CANCEL, Il2CppFGClient.UI.UIModalMessage.OKButtonType.Positive, new Action<bool>(wasok =>
            {
                if (wasok) Application.OpenURL(url);
            }));
        }

        void EasterEgg()
        {
            EGG_Counter++;

            if (EGG_Counter < 5)
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

        protected override void StateChange(bool isActive, bool wasActive)
        {
        }
    }
}
