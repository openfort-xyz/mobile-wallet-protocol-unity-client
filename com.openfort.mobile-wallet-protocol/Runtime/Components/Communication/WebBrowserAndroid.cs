#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace MobileWalletProtocol
{
    class WebBrowserAndroid : IWebBrowser
    {
        public async Task<WebBrowserResult> OpenPopupAsync(string url, string returnUrlScheme)
        {
            var result = new WebBrowserResult();
            var isDone = false;
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var customTabLauncher = new AndroidJavaClass("com.openfort.unity.OpenfortActivity");
            var callback = new AndroidCallback();

            callback.onDismissed += () =>
            {
                result.Type = "cancel";
                isDone = true;
            };

            callback.onDeepLinkResult += (url) =>
            {
                result.Type = "success";
                result.Url = url;
                isDone = true;
            };            

            customTabLauncher.CallStatic("startActivity", activity, url, callback);

            try
            {
                while (!isDone)
                {
                    callback.ProcessActionQueue();
                    await Task.Yield();
                }

                return result;
            }
            finally
            {
                
            }
        }
    }

    class AndroidCallback : AndroidJavaProxy
    {
        public event Action onDismissed;
        public event Action<string> onDeepLinkResult;
    
        ConcurrentQueue<Action> m_Actions = new ConcurrentQueue<Action>();
        
        public AndroidCallback() : base("com.openfort.unity.OpenfortActivity$Callback") { }

        public void ProcessActionQueue()
        {
            while (m_Actions.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }

        void onCustomTabsDismissed(string url)
        {
            m_Actions.Enqueue(() => onDismissed?.Invoke());
        }

        void onDeeplinkResult(string url)
        {
            m_Actions.Enqueue(() => onDeepLinkResult?.Invoke(url));
        }
    }
}
#endif