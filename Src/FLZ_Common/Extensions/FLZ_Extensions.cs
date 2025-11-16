using Il2Cpp;
using Il2CppFG.Common.CMS;
using Il2CppFGClient.UI;
using Il2CppFGClient.UI.Notifications;
using Il2CppTMPro;
using Il2CppUniRx;
using NOTFGT.FLZ_Common.Localization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppFGClient.UI.UIModalMessage;

namespace NOTFGT.FLZ_Common.Extensions
{
    public static class FLZ_Extensions
    {
        public static void DoModal(string title, string msg, ModalType type, OKButtonType btnType, Il2CppSystem.Action<bool> act = null, bool doSfx = true, string btnOkStr = null, TextAlignmentOptions al = TextAlignmentOptions.Center, float closeDelay = 0f)
        {
            if (btnOkStr != null)
                CMSString("latest_btn_ok", btnOkStr);

            Il2CppSystem.IObservable<Unit> acceptWaitObs = ModalMessageBaseData.CreateTimerObservable(closeDelay);
            string okStr = btnOkStr == null ? null : $"latest_btn_ok";

            var ModalMessageDataDisclaimer = new ModalMessageData
            {
                Title = title,
                Message = $"{msg}",
                LocaliseTitle = LocaliseOption.NotLocalised,
                LocaliseMessage = LocaliseOption.NotLocalised,
                ModalType = type,
                OkButtonType = btnType,
                OnCloseButtonPressed = act,
                OkTextOverrideId = okStr,
                MessageTextAlignment = al,
                AcceptWaitObservable = acceptWaitObs,
                Priority = PopupMessagePriority.ExitGameOrReturnToTitleScreen,

            };

            PopupManager.Instance.Show(PopupInteractionType.Error, ModalMessageDataDisclaimer);
            if (doSfx)
                AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericPopUpAppears);
        }

        public static void CreateNotification(string title, string msg, string headerCol = null, float durination = -1, Action onComplete = null)
        {
            var dat = new TextNotificationData(null)
            {
                Title = title,
                Message = msg,
            };

            if (durination > 0)
                dat._Duration_k__BackingField = durination;

            if (onComplete != null)
                dat.OnComplete += onComplete;

            NotificationManager.Instance.ShowNotificationImmediately(dat);

            var target = NotificationManager.Instance._activeNotifications[NotificationManager.Instance.ActiveNotifications - 1].gameObject;
            var text = target.transform.GetChild(3);
            var header = target.transform.GetChild(1).GetChild(0);

            text.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            text.GetComponent<TextMeshProUGUI>().m_minFontSize = 6;

            if (string.IsNullOrEmpty(headerCol))
                return;

            ColorUtility.TryParseHtmlString(headerCol.ToUpper(), out var color);
            header.GetComponent<Image>().color = color;
        }

        public static string CMSString(string key, string val)
        {
            var strings = CMSLoader.Instance._localisedStrings;
            if (strings == null) return null;
            if (strings.ContainsString(key)) strings._localisedStrings.Remove(key);
            strings._localisedStrings.Add(key, val);
            return key;
        }

        public static Sprite SetSpriteFromFile(string path)
        {
            byte[] ImageAsByte = File.ReadAllBytes(path);
            Texture2D Texture = new(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            if (Texture.LoadImage(ImageAsByte))
            {
                Texture.filterMode = FilterMode.Point;
                return Sprite.Create(Texture, new Rect(0.0f, 0.0f, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        public static string FormatException(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            return LocalizationManager.LocalizedString("generic_exception", [ex.Message, ex.StackTrace]);
        }

        public static string CleanStr(string strIN)
        {
            string strOUT = Regex.Replace(strIN, @"<.*?>|\t|\s{2,}", " ");
            strOUT = Regex.Replace(strOUT, @"(?<=<) | (?=>)", "");
            strOUT = strOUT.Trim();
            strOUT = Regex.Replace(strOUT, @"\s+", " ");
            return strOUT;
        }
    }
}
