#if UNITY_IPHONE && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;

namespace MobileWalletProtocol
{
    class WebBrowserIOS : IWebBrowser
    {
        private delegate void DelegateMessage(string key, string message);

        [DllImport("__Internal")]
        private static extern IntPtr _CWebViewPlugin_Init(string ua);
        [DllImport("__Internal")]
        private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
        [DllImport("__Internal")]
        private static extern void _CWebViewPlugin_LaunchAuthURL(IntPtr instance, string url, string redirectUri);
        [DllImport("__Internal")]
        private static extern void _CWebViewPlugin_SetDelegate(DelegateMessage callback);

        private static event Action<string, string> s_OnError;
        private static event Action<string> s_OnSuccess;

        [MonoPInvokeCallback(typeof(DelegateMessage))] 
        private static void delegateMessageReceived(string key, string message) {
            if (key == "CallOnError" || key == "CallFromAuthCallbackError") {
                
                s_OnError.Invoke(key, message);
                return;
            }

            if (key == "CallFromAuthCallback") {
                s_OnSuccess?.Invoke(message);
                return;
            }
        }

        IntPtr m_WebView;

        public async Task<WebBrowserResult> OpenPopupAsync(string url, string returnUrlScheme)
        {
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
                m_WebView = _CWebViewPlugin_Init(string.Empty);
                
                _CWebViewPlugin_SetDelegate(delegateMessageReceived);

                s_OnError += onError;
                s_OnSuccess += onSuccess;

                _CWebViewPlugin_LaunchAuthURL(m_WebView, url, returnUrlScheme);

                while (!isDone)
                {
                    await Task.Yield();
                }

                return result;
            }
            finally
            {
                s_OnError -= onError;
                s_OnSuccess -= onSuccess;

                if (m_WebView != IntPtr.Zero)
                {
                    _CWebViewPlugin_Destroy(m_WebView);
                }

                m_WebView = IntPtr.Zero;
            }
        }
    }
}
#endif