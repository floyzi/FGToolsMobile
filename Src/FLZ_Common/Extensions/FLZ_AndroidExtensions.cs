using Il2CppCommon.Input.Vibration;
using UnityEngine;
using static UnityEngine.ScriptingUtility;

namespace NOTFGT.FLZ_Common.Extensions
{
    internal static class FLZ_AndroidExtensions
    {
        internal static void ShowModal(string title, string msg)
        {
            var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            var builder = new AndroidJavaObject("android.app.AlertDialog$Builder", activity);

            builder.Call<AndroidJavaObject>("setTitle", title);
            builder.Call<AndroidJavaObject>("setMessage", msg);

            builder.Call<AndroidJavaObject>("create").Call("show");
        }

        internal static void ShowToast(string msg)
        {
            var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            var toast = new AndroidJavaClass("android.widget.Toast");
            toast.CallStatic<AndroidJavaObject>("makeText", activity, msg, 0).Call("show");
        }

        internal static void Vibrate(long ms)
        {
            var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            vibrator.Call("vibrate", vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, 255));
        }
    }
}
