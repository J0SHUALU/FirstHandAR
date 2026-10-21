using UnityEngine;

namespace FirstHand.Presentation
{
    /// <summary>
    /// FR-30: hands the CSV to the phone's share menu. On Android this uses an ACTION_SEND intent with the CSV
    /// as text, which needs no FileProvider setup and works with WhatsApp, Gmail and Drive. Elsewhere it is a
    /// no-op (the file path is still copied to the clipboard by the caller).
    /// </summary>
    public static class NativeShare
    {
        public static void ShareText(string text, string subject)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, subject))
                        activity.Call("startActivity", chooser);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NativeShare] " + e.Message);
            }
#else
            Debug.Log($"[NativeShare] {subject} ({text.Length} chars) - share sheet only on Android");
#endif
        }
    }
}
