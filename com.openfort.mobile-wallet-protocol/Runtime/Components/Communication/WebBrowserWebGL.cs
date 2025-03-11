#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace MobileWalletProtocol
{
    class WebBrowserWebGL : IWebBrowser
    {
        [DllImport("__Internal")]
        private static extern void _open_popup(string name, string url, string redirectUri);

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

            var callbackReceiver = new GameObject("__WebBrowserWebGLCallbackReceiver__").AddComponent<WebBrowserWebGLCallbackReceiver>();
    
            try
            {
                callbackReceiver.onMessage += (message) =>
                {
                    var i = message.IndexOf(':', 0);

                    if (i == -1)
                    {
                        onError.Invoke("Error", message);
                        return;
                    }

                    switch (message.Substring(0, i))
                    {
                        case "CallOnError":
                            onError.Invoke("Error", message.Substring(i + 1));
                            break;
                        case "CallFromAuthCallback":
                            onSuccess.Invoke(message.Substring(i + 1));
                            break;
                    }
                };

                _open_popup(callbackReceiver.gameObject.name, url, returnUrlScheme);

                while (!isDone)
                {
                    await Task.Yield();
                }

                return result;
            }
            finally
            {
                GameObject.Destroy(callbackReceiver.gameObject);
            }
        }
    }
}
#endif