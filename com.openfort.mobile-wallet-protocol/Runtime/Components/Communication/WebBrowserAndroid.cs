#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MobileWalletProtocol
{
    class WebBrowserAndroid : IWebBrowser, ICallback
    {
        void ICallback.OnDismissed()
        {
            Debug.Log("OnDismissed");
        }

        void ICallback.OnDeepLinkResult(string url)
        {
            Debug.Log("OnDeepLinkResult: " + url);
        }

        public async Task<WebBrowserResult> OpenPopupAsync(string url, string returnUrlScheme)
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var customTabLauncher = new AndroidJavaClass("com.openfort.unity.OpenfortActivity");

            customTabLauncher.CallStatic("startActivity", activity, url, new AndroidCallback(this));

            var result = new WebBrowserResult();
            var isDone = false;

            var onError = new Action<string, string>((key, message) =>
            {
                result.Type = string.IsNullOrEmpty(message) ? "cancel" : "error";
                result.Url = message;
                isDone = true;
            });

            var onSuccess = new Action<string>((message) =>
            {
                result.Type = "success";
                result.Url = message;
                isDone = true;
            });

            try
            {
                while (!isDone)
                {
                    await Task.Yield();
                }

                return result;
            }
            finally
            {
                
            }
        }
    }

    interface ICallback
    {
        void OnDismissed();
        void OnDeepLinkResult(string url);
    }

    class AndroidCallback : AndroidJavaProxy
    {
        private ICallback callback;

        public AndroidCallback(ICallback callback) : base("com.openfort.unity.OpenfortActivity$Callback")
        {
            this.callback = callback;
        }

        void onCustomTabsDismissed(string url)
        {
            callback.OnDismissed();
        }

        void onDeeplinkResult(string url)
        {
            callback.OnDeepLinkResult(url);
        }
    }
}
#endif